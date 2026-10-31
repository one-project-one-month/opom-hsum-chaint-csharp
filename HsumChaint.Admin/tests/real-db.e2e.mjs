// Opt-in real MySQL/browser regression. Credentials are supplied only through environment variables.
// Clones the local database, disables push tokens in the clone, and drops only that generated clone.
import assert from 'node:assert/strict';
import { spawn, spawnSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { mkdir, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { chromium } from '@playwright/test';

const phone = process.env.REAL_ADMIN_PHONE, password = process.env.REAL_ADMIN_PASSWORD;
assert.ok(phone && password, 'Supply REAL_ADMIN_PHONE and REAL_ADMIN_PASSWORD in the environment.');
const container = process.env.REAL_MYSQL_CONTAINER || 'hsumchaint_csharp-db-1';
const source = process.env.REAL_MYSQL_DATABASE || 'hsumchaint_db';
assert.match(source, /^[a-zA-Z0-9_]+$/);
const stamp = Date.now().toString(), tag = `E2E${stamp}`;
const clone = `hsumchaint_e2e_${stamp}_${randomBytes(3).toString('hex')}`;
const apiOrigin = 'http://127.0.0.1:5187', origin = 'http://localhost:3198';
const results = [], children = []; let browser, page, created = false, before;
const reportPath = resolve('test-results/real-db-report.json');
const docker = (args, input) => {
  const result = spawnSync('docker', ['exec', ...(input ? ['-i'] : []), container, ...args], { input, encoding: 'utf8', maxBuffer: 20 * 1024 * 1024 });
  if (result.status !== 0) throw new Error('MySQL container operation failed (credentials and SQL output suppressed).');
  return result.stdout.trim();
};
const sql = query => docker(['sh', '-c', 'MYSQL_PWD="$MYSQL_ROOT_PASSWORD" exec mysql -uroot -N --batch'], query);
const fingerprint = () => {
  const tables = sql(`SHOW TABLES FROM \`${source}\`;`).split('\n').filter(Boolean);
  assert.ok(tables.every(name => /^[a-zA-Z0-9_]+$/.test(name)));
  return sql(`CHECKSUM TABLE ${tables.map(name => `\`${source}\`.\`${name}\``).join(',')};`);
};
const start = (command, args, env, cwd) => {
  const child = spawn(command, args, { cwd, env: { ...process.env, ...env }, stdio: ['ignore', 'pipe', 'pipe'] });
  // Keep diagnostics in memory. Never dump process environment, credentials, tokens, or raw server logs.
  child.stdout.resume(); child.stderr.resume(); children.push(child); return child;
};
const wait = async url => { for (let i = 0; i < 100; i++) { try { const r = await fetch(url); if (r.status < 500) return; } catch {} await new Promise(r => setTimeout(r, 200)); } throw new Error('Isolated test server did not become ready.'); };
let token;
async function api(path, method = 'GET', body, expected = 200, accessToken = token) {
  const response = await fetch(`${apiOrigin}${path}`, { method, headers: { ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}), ...(body ? { 'Content-Type': 'application/json' } : {}) }, ...(body ? { body: JSON.stringify(body) } : {}) });
  const json = await response.json().catch(() => ({}));
  assert.equal(response.status, expected, `${method} ${path} status ${response.status}`);
  if (expected === 200) assert.equal(json.isSuccess, true, `${method} ${path} envelope failed`);
  return json;
}
const stage = async (name, fn) => { process.stdout.write(`Testing: ${name}\n`); await fn(); results.push({ name, passed: true }); };
const go = async path => { await page.goto(`${origin}${path}`); await page.locator('main h1').waitFor(); assert.equal(await page.getByText('စာရင်းရယူ၍ မရသေးပါ', { exact: true }).count(), 0, `${path} API error page`); };
async function open(label, scope = page) { await scope.getByRole('button', { name: label, exact: true }).click(); await page.getByRole('dialog').waitFor(); }
async function fill(values) {
  const dialog = page.getByRole('dialog');
  for (const [name, value] of Object.entries(values)) {
    const input = dialog.locator(`[name="${name}"]`);
    if (await input.evaluate(el => el.tagName) === 'SELECT') await input.selectOption(String(value)); else await input.fill(String(value));
  }
}
async function save(destructive = false, error = false) {
  const dialog = page.getByRole('dialog');
  if (destructive) await dialog.locator('[name="confirm"]').check();
  await dialog.getByRole('button', { name: destructive ? 'အတည်ပြုရန်' : 'သိမ်းဆည်းရန်', exact: true }).click();
  if (error) { await dialog.getByRole('alert').waitFor(); return; }
  await Promise.race([
    dialog.waitFor({ state: 'hidden', timeout: 15000 }),
    dialog.getByRole('alert').waitFor({ timeout: 15000 }).then(async () => { throw new Error(`UI mutation rejected: ${(await dialog.getByRole('alert').innerText()).slice(0, 300)}`); }),
  ]);
}
async function lookup(path, predicate) {
  for (let n = 1; n <= 100; n++) {
    const result = await api(`${path}${path.includes('?') ? '&' : '?'}PageNumber=${n}&PageSize=100`);
    const found = result.data.find(predicate); if (found) return found;
    if (!result.pagination?.hasNextPage) break;
  }
  throw new Error(`Created test record missing: ${path}`);
}
const testPassword = randomBytes(18).toString('base64url');
let userId, monasteryId, roleId, spaceRoleId, donationId, monkId, adminId;
const donorPhone = `099${stamp.slice(-9)}`, monkPhone = `098${stamp.slice(-9)}`;
try {
  before = fingerprint();
  await stage('Clone actual MySQL schema/data and disable external push', async () => {
    assert.match(clone, /^hsumchaint_e2e_\d+_[a-f0-9]{6}$/);
    sql(`CREATE DATABASE \`${clone}\`;`); created = true;
    docker(['sh', '-c', `MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mysqldump -uroot --single-transaction --no-tablespaces --set-gtid-purged=OFF ${source} | MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mysql -uroot ${clone}`]);
    sql(`UPDATE \`${clone}\`.\`User\` SET fcm_token = NULL;`);
    const inspect = spawnSync('docker', ['inspect', container], { encoding: 'utf8' });
    const vars = JSON.parse(inspect.stdout)[0].Config.Env;
    const rootPassword = vars.find(v => v.startsWith('MYSQL_ROOT_PASSWORD=')).slice('MYSQL_ROOT_PASSWORD='.length);
    const ports = JSON.parse(inspect.stdout)[0].NetworkSettings.Ports['3306/tcp'];
    const connection = `Server=127.0.0.1;Port=${ports[0].HostPort};Database=${clone};User=root;Password=${rootPassword};`;
    start('dotnet', [resolve('../HsumChaint.API/bin/Debug/net10.0/HsumChaint.API.dll'), '--urls', apiOrigin], { ASPNETCORE_ENVIRONMENT: 'Development', ConnectionStrings__DefaultConnection: connection }, resolve('../HsumChaint.API'));
    start(process.execPath, ['node_modules/next/dist/bin/next', 'start', '-p', '3198'], { API_BASE_URL: apiOrigin, SESSION_SECRET: randomBytes(32).toString('hex') }, process.cwd());
    await Promise.all([wait(`${apiOrigin}/api/v1/admin/summary`), wait(`${origin}/login`)]);
  });
  await stage('Real Admin login and all SSR pages', async () => {
    const login = await api('/api/v1/Auth/login', 'POST', { phoneNumber: phone, password }); token = login.data.accessToken; adminId = login.data.id; assert.equal(login.data.roleName, 'Admin');
    browser = await chromium.launch({ headless: true }); page = await browser.newPage({ viewport: { width: 1440, height: 1000 } }); page.setDefaultTimeout(15000);
    await page.goto(`${origin}/login`); await page.locator('[name="phoneNumber"]').fill(phone); await page.locator('[name="password"]').fill(password);
    await page.getByRole('button', { name: 'အကောင့်ဝင်ရန်', exact: true }).click(); await page.waitForURL('**/dashboard');
    for (const path of ['/dashboard', '/donations', '/users', '/monasteries', '/roles', '/space-roles', '/calendar', '/invitations', '/notifications']) await go(path);
  });
  await stage('System role create, edit, permission assignment', async () => {
    await go('/roles'); await open('အခန်းကဏ္ဍအသစ် ဖန်တီးရန်'); await fill({ name: `${tag}Role` });
    await page.getByRole('dialog').locator('[name="permissionIds"]').first().check(); await save();
    roleId = (await lookup('/api/v1/roles', r => r.name === `${tag}Role`)).id;
    const card = page.locator('[data-slot="card"]').filter({ has: page.getByText(`${tag}Role`, { exact: true }) });
    await open('ပြင်ရန်', card); await fill({ name: `${tag}RoleEdited` }); await save();
    assert.equal((await api(`/api/v1/roles/${roleId}`)).data.name, `${tag}RoleEdited`);
  });
  await stage('Space role create and edit', async () => {
    await go('/space-roles'); await open('အခန်းကဏ္ဍအသစ် ဖန်တီးရန်'); await fill({ name: `${tag}Space`, description: tag }); await save();
    spaceRoleId = (await lookup('/api/v1/space-roles', r => r.name === `${tag}Space`)).id;
    const card = page.locator('[data-slot="card"]').filter({ has: page.getByText(`${tag}Space`, { exact: true }) });
    await open('ပြင်ရန်', card); await fill({ description: `${tag} updated` }); await save();
  });
  await stage('User create, duplicate validation, edit, role assignment', async () => {
    await go('/users'); await open('အသုံးပြုသူအသစ် ဖန်တီးရန်'); await fill({ name: `${tag}Donor`, phoneNumber: donorPhone, password: testPassword, roleId: 3 }); await save();
    userId = (await lookup('/api/User', u => u.phoneNumber === donorPhone)).id;
    await open('အသုံးပြုသူအသစ် ဖန်တီးရန်'); await fill({ name: `${tag}Duplicate`, phoneNumber: donorPhone, password: testPassword, roleId: 3 }); await save(false, true); await page.keyboard.press('Escape');
    await go(`/users/${userId}`); await open('အသုံးပြုသူ ပြင်ရန်'); await fill({ name: `${tag}DonorEdited`, email: 'e2e@example.invalid', roleId }); await save();
    assert.equal((await api(`/api/User/${userId}`)).data.roleId, roleId);
    await open('အသုံးပြုသူ ပြင်ရန်'); await fill({ roleId: 3 }); await save();
  });
  await stage('User schedule/reminder settings and Monk profile lifecycle', async () => {
    await open('အချိန်နှင့် အသိပေးမှု ပြင်ရန်'); await fill({ pickupTime: '2026-11-01T09:00', pickupNotificationTime: '2026-11-01T08:30' }); await save();
    assert.ok((await api(`/api/User/${userId}/settings`)).data.pickupTime.startsWith('2026-11-01T02:30'));
    await go('/users'); await open('အသုံးပြုသူအသစ် ဖန်တီးရန်'); await fill({ name: `${tag}Monk`, phoneNumber: monkPhone, password: testPassword, roleId: 2, monasteryName: tag, monasteryAddress: 'Yangon' }); await save();
    const monk = await lookup('/api/User', u => u.phoneNumber === monkPhone);
    monkId = monk.id;
    await go(`/users/${monk.id}`); await open('သံဃာကိုယ်ရေးအချက်အလက် ပြင်ရန်'); await fill({ monasteryName: `${tag}Profile`, monasteryAddress: 'Mandalay' }); await save();
    assert.equal((await api(`/api/User/${monk.id}/monk-profile`)).data.monasteryName, `${tag}Profile`);
    await open('သံဃာကိုယ်ရေးအချက်အလက် ဖျက်ရန်'); await save(true);
  });
  await stage('Monastery create/edit, Owner protection, member invite', async () => {
    await go('/monasteries'); await open('ကျောင်းတိုက်အသစ် ဖန်တီးရန်'); await fill({ monasteryName: `${tag}Monastery`, address: 'Yangon', description: tag }); await save();
    monasteryId = (await lookup('/api/v1/monasteries', m => m.monasteryName === `${tag}Monastery`)).id;
    await go(`/monasteries/${monasteryId}`); await open('ကျောင်းတိုက် ပြင်ရန်'); await fill({ address: 'Mandalay' }); await save();
    assert.equal((await api(`/api/v1/admin/monasteries/${monasteryId}`)).data.address, 'Mandalay');
    const owner = page.getByRole('row').filter({ hasText: '· Owner' }); assert.ok(await owner.getByRole('button', { name: 'ဖယ်ရှားရန်', exact: true }).isDisabled());
    await open('အဖွဲ့ဝင် ဖိတ်ခေါ်ရန်'); await fill({ userId, spaceRoleId }); await save();
  });
  await stage('Invitation recipient acceptance, member role edit/removal, revoke', async () => {
    let invite = await lookup('/api/v1/admin/invitations', i => i.monasterySpaceId === monasteryId && i.invitedUserId === userId && i.status === 0);
    await api(`/api/v1/monasteries/invitations/${invite.id}/respond`, 'POST', { status: 1 }, 403);
    const donorLogin = await api('/api/v1/Auth/login', 'POST', { phoneNumber: donorPhone, password: testPassword });
    await api(`/api/v1/monasteries/invitations/${invite.id}/respond`, 'POST', { status: 1 }, 200, donorLogin.data.accessToken);
    await go(`/monasteries/${monasteryId}`); let member = page.getByRole('row').filter({ hasText: `${tag}DonorEdited` });
    await open('အခန်းကဏ္ဍပြောင်း', member); await fill({ spaceRoleId: 4 }); await save();
    assert.equal((await lookup(`/api/v1/monasteries/${monasteryId}/members`, m => m.userId === userId)).spaceRoleId, 4);
    await open('ဖယ်ရှားရန်', member); await save(true);
    await open('အဖွဲ့ဝင် ဖိတ်ခေါ်ရန်'); await fill({ userId, spaceRoleId: 4 }); await save();
    invite = await lookup('/api/v1/admin/invitations', i => i.monasterySpaceId === monasteryId && i.status === 0);
    await go('/invitations'); const row = page.getByRole('row').filter({ hasText: `${tag}DonorEdited` }).filter({ hasText: 'စောင့်ဆိုင်းဆဲ' }); await open('ရုပ်သိမ်းရန်', row); await save(true);
    assert.equal((await lookup('/api/v1/admin/invitations', i => i.id === invite.id)).status, 2);
  });
  await stage('Admin invitation accept/reject UI and non-owner/account/role protections', async () => {
    const monkLogin = await api('/api/v1/Auth/login', 'POST', { phoneNumber: monkPhone, password: testPassword });
    const access = monkLogin.data.accessToken;
    const external = (await api('/api/v1/monasteries', 'POST', { monasteryName: `${tag}External`, address: 'Yangon' }, 200, access)).data.id;
    await api(`/api/v1/monasteries/${external}/invitations`, 'POST', { userId: adminId, spaceRoleId: 4 }, 200, access);
    await go(`/monasteries/${external}`); assert.ok(await page.getByRole('button', { name: 'ကျောင်းတိုက် ရပ်ဆိုင်းရန်', exact: true }).isDisabled());
    await api(`/api/v1/monasteries/${external}`, 'DELETE', undefined, 403);
    await api(`/api/v1/admin/users/${monkId}`, 'DELETE', undefined, 400);
    await api(`/api/v1/admin/users/${adminId}`, 'DELETE', undefined, 400);
    await api(`/api/v1/admin/users/${adminId}`, 'PUT', { name: 'Admin', phoneNumber: phone, roleId: 3 }, 400);
    await api('/api/v1/roles/1', 'DELETE', undefined, 400);
    await api('/api/v1/space-roles/1', 'DELETE', undefined, 400);
    await api('/api/v1/Auth/register', 'POST', { name: 'Disallowed', phoneNumber: `097${stamp.slice(-9)}`, password: testPassword, roleId: 1 }, 403);
    for (const decision of [1, 2]) {
      if (decision === 2) {
        await api(`/api/v1/monasteries/${external}/members/${adminId}`, 'DELETE');
        await api(`/api/v1/monasteries/${external}/invitations`, 'POST', { userId: adminId, spaceRoleId: 4 }, 200, access);
      }
      await go('/invitations'); const row = page.getByRole('row').filter({ hasText: `${tag}External` }).filter({ hasText: 'စောင့်ဆိုင်းဆဲ' });
      await open('ဖိတ်ခေါ်မှု တုံ့ပြန်ရန်', row); await fill({ status: decision }); await save();
      assert.equal((await lookup('/api/v1/admin/invitations', i => i.monasterySpaceId === external && i.status === decision)).status, decision);
    }
  });
  await stage('Manual donation create, review, schedule, complete, terminal safeguards', async () => {
    await go('/donations'); await open('အလှူအသစ်'); await fill({ monasterySpaceId: monasteryId, donorName: `${tag}Complete`, donationType: 1, quantity: 12, note: tag }); await save();
    donationId = (await lookup(`/api/v1/admin/donations?Search=${tag}Complete`, d => d.donorName === `${tag}Complete`)).id;
    await go(`/donations?search=${tag}Complete`); let row = page.getByRole('row').filter({ hasText: `${tag}Complete` });
    await open('စိစစ်ရန်', row); await fill({ status: 3, note: `${tag} accepted` }); await save();
    await open('အချိန်သတ်မှတ်', row); await fill({ pickupTime: '2026-11-01T09:00', dropoffTime: '2026-11-01T10:00' }); await save();
    assert.ok((await api(`/api/v1/donations/${donationId}`)).data.pickupTime.startsWith('2026-11-01T02:30'));
    await open('ပြီးစီး', row); await save(); assert.equal((await api(`/api/v1/donations/${donationId}`)).data.status, 5);
    await api(`/api/v1/donations/${donationId}`, 'DELETE', undefined, 400);
    await api(`/api/v1/donations/${donationId}/cancel`, 'POST', {}, 400);
  });
  await stage('Review reject, cancel/delete and validation failure retention', async () => {
    await go('/donations'); await open('အလှူအသစ်'); await fill({ monasterySpaceId: monasteryId, donorName: `${tag}Reject`, donationType: 5 }); await save(false, true);
    assert.equal(await page.getByRole('dialog').locator('[name="donorName"]').inputValue(), `${tag}Reject`);
    await fill({ customDonationType: 'E2E supplies' }); await save();
    await lookup(`/api/v1/admin/donations?Search=${tag}Reject`, d => d.donorName === `${tag}Reject`);
    await go(`/donations?search=${tag}Reject`); const row = page.getByRole('row').filter({ hasText: `${tag}Reject` });
    await open('စိစစ်ရန်', row); await fill({ status: 6 }); await save();
    await open('ပယ်ဖျက်', row); await save(true); await open('ဖျက်ရန်', row); await save(true);
    assert.equal((await api(`/api/v1/admin/donations?Search=${tag}Reject`)).data.length, 0);
  });
  await stage('Batch, monthly recurrence, timezone/clamping, calendar and filtered export', async () => {
    await go('/donations'); await open('စုပေါင်းအလှူ'); await fill({ monasterySpaceId: monasteryId, 'row0.donorName': `${tag}BatchA`, 'row0.donationType': 1, 'row0.quantity': 2 });
    await page.getByRole('dialog').getByRole('button', { name: 'နောက်ထပ်အလှူ ထည့်ရန်' }).click(); await fill({ 'row1.donorName': `${tag}BatchB`, 'row1.donationType': 0, 'row1.amount': 1000 }); await save();
    assert.equal((await api(`/api/v1/admin/donations?Search=${tag}Batch`)).data.length, 2);
    await open('လစဉ်အလှူ'); await fill({ monasterySpaceId: monasteryId, donorName: `${tag}Monthly`, donationType: 1, quantity: 1, startDate: '2027-02-01', dayOfMonth: 31, monthsCount: 2, pickupTimeOfDay: '03:00' }); await save();
    const monthly = (await api(`/api/v1/admin/donations?Search=${tag}Monthly`)).data; assert.equal(monthly.length, 2); assert.ok(monthly.some(d => d.pickupTime.startsWith('2027-02-27T20:30')));
    await go('/calendar?month=2027-02'); await page.locator('.calendar-event').filter({ hasText: `${tag}Monthly` }).waitFor();
    assert.equal(await page.locator('.calendar-day').filter({ hasText: `${tag}Monthly` }).locator(':scope > strong').innerText(), '28');
    const csv = await page.context().request.get(`${origin}/donations/export?Search=${tag}Monthly`); assert.equal(csv.status(), 200); assert.ok((await csv.text()).includes(`${tag}Monthly`));
  });
  await stage('Database notifications create/read/delete and global bulk actions', async () => {
    await go('/notifications'); await open('အသိပေးချက် ဖန်တီးရန်'); await fill({ userId, type: 2, message: `${tag} Announcement` }); await save();
    const notification = await lookup('/api/v1/admin/notifications', n => n.message === `${tag} Announcement`);
    let row = page.getByRole('row').filter({ hasText: `${tag} Announcement` }); await open('ဖတ်ပြီး', row); await save();
    assert.equal((await lookup('/api/v1/admin/notifications', n => n.id === notification.id)).isRead, true);
    await open('ဖျက်ရန်', row); await save(true);
    await open('အားလုံး ဖတ်ပြီး'); await save(); await open('အားလုံးဖျက်ရန်'); await save(true);
    assert.equal((await api('/api/v1/admin/notifications')).pagination.totalCount, 0);
  });
  await stage('Role deletion, user deactivation, owner-only monastery deactivation', async () => {
    await go('/roles'); let card = page.locator('[data-slot="card"]').filter({ has: page.getByText(`${tag}RoleEdited`, { exact: true }) }); await open('ဖျက်ရန်', card); await save(true);
    await go('/space-roles'); card = page.locator('[data-slot="card"]').filter({ has: page.getByText(`${tag}Space`, { exact: true }) }); await open('ဖျက်ရန်', card); await save(true);
    // User list is paginated; navigate directly to the page containing the test user.
    let userPage = 1; for (; userPage < 100; userPage++) { const users = await api(`/api/User?PageNumber=${userPage}&PageSize=10`); if (users.data.some(u => u.id === userId)) break; }
    await go(`/users?page=${userPage}`); const row = page.getByRole('row').filter({ hasText: `${tag}DonorEdited` }); await open('ရပ်ဆိုင်း', row); await save(true);
    await api(`/api/User/${userId}`, 'GET', undefined, 404);
    await go(`/monasteries/${monasteryId}`); await open('ကျောင်းတိုက် ရပ်ဆိုင်းရန်'); await save(true);
    await page.waitForURL('**/monasteries');
    await api(`/api/v1/admin/monasteries/${monasteryId}`, 'GET', undefined, 404);
  });
  await stage('Mobile navigation, session/logout, source database unchanged', async () => {
    await page.setViewportSize({ width: 390, height: 844 }); await go('/dashboard');
    await page.getByRole('button', { name: 'မီနူး ဖွင့်ရန်' }).click(); await page.getByRole('link', { name: /အလှူမှတ်တမ်းများ Donations/ }).click(); await page.waitForURL('**/donations');
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
    await page.getByRole('button', { name: 'မီနူး ဖွင့်ရန်' }).click(); await page.getByRole('button', { name: 'အကောင့်ထွက်ရန်' }).click(); await page.waitForURL('**/login');
    assert.equal((await page.context().cookies()).some(c => c.name === 'hsumchaint_admin'), false);
    assert.equal(fingerprint(), before, 'Source database checksum changed during isolated test run.');
  });
} catch (error) {
  const message = [phone, password, testPassword].reduce((text, secret) => text.replaceAll(secret, '[redacted]'), error.message);
  results.push({ name: 'failure', passed: false, message, stack: error.stack?.split('\n').filter(line => line.includes('real-db.e2e')).slice(0, 3) });
  process.exitCode = 1;
  if (page) await page.screenshot({ path: 'test-results/real-db-failure.png', fullPage: true }).catch(() => {});
} finally {
  await browser?.close();
  await Promise.all(children.map(child => new Promise(resolve => { if (child.exitCode !== null) return resolve(); child.once('exit', resolve); child.kill(); })));
  if (created) {
    assert.match(clone, /^hsumchaint_e2e_\d+_[a-f0-9]{6}$/); assert.notEqual(clone, source);
    sql(`DROP DATABASE \`${clone}\`;`);
  }
  await mkdir('test-results', { recursive: true });
  const report = { timestamp: new Date().toISOString(), database: 'real MySQL clone of local source', sourceUnchanged: before ? fingerprint() === before : null, cloneRemoved: created, results };
  await writeFile(reportPath, JSON.stringify(report, null, 2));
  console.log(JSON.stringify(report, null, 2));
}
