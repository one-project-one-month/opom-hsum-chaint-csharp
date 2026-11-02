import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { spawn } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { chromium } from '@playwright/test';

// Isolated API fixture: no real accounts, database writes, or production credentials.
test('admin SSR, session security, filtering, pagination and responsive navigation', { timeout: 120000 }, async (t) => {
  const calls = [];
  let unavailable = false;
  const token = `${Buffer.from('{}').toString('base64url')}.${Buffer.from(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 3600 })).toString('base64url')}.fixture`;
  const user = { id: 1, name: 'Test Administrator', phoneNumber: '0900000000', roleName: 'Admin', roleId: 1, email: 'admin@example.invalid' };
  const donations = Array.from({ length: 12 }, (_, i) => ({ id: i + 1, donorName: `Fixture Donor ${i + 1}`, monasteryName: `Fixture Monastery ${i % 2 + 1}`, monasterySpaceId: i % 2 + 1, donationType: 1, status: i % 2 ? 5 : 2, amount: null, quantity: 12, note: '<script>alert("unsafe")</script>', createdAt: '2026-10-30T07:00:00Z', pickupTime: null, dropoffTime: null }));
  donations[10].note = '=1+1';
  const envelope = (data) => ({ isSuccess: true, message: 'Success', data });
  const fixture = createServer(async (req, res) => {
    const url = new URL(req.url, 'http://localhost');
    const call = { path: url.pathname, method: req.method, query: url.searchParams, authorization: req.headers.authorization };
    calls.push(call);
    res.setHeader('Content-Type', 'application/json');
    if (unavailable) { res.writeHead(503).end('{}'); return; }
    if (url.pathname === '/api/v1/Auth/login') {
      let body = ''; for await (const chunk of req) body += chunk;
      const login = JSON.parse(body);
      if (login.password !== 'fixture-password') { res.writeHead(400).end(JSON.stringify({ isSuccess: false })); return; }
      res.end(JSON.stringify(envelope({ ...user, roleName: login.phoneNumber === 'donor' ? 'Donor' : 'Admin', accessToken: token, refreshToken: 'fixture-refresh' })));
      return;
    }
    if (req.headers.authorization !== `Bearer ${token}`) { res.writeHead(401).end('{}'); return; }
    if (req.method !== 'GET') {
      let body = ''; for await (const chunk of req) body += chunk;
      call.payload = body ? JSON.parse(body) : null;
      if (url.pathname === '/api/v1/roles' && call.payload?.name === 'Blocked role') { res.writeHead(400).end(JSON.stringify({ isSuccess: false, message: 'Role name already exists.' })); return; }
      const match = url.pathname.match(/^\/api\/v1\/donations\/(\d+)\/(review|schedule|complete|cancel)$/);
      if (match) {
        const row = donations.find(d => d.id === Number(match[1]));
        if (row) row.status = match[2] === 'review' ? call.payload.status : { schedule: 4, complete: 5, cancel: 7 }[match[2]];
      }
      res.end(JSON.stringify(envelope({}))); return;
    }
    if (url.pathname === '/api/User/1') { res.end(JSON.stringify(envelope(user))); return; }
    if (url.pathname === '/api/User/1/settings') { res.end(JSON.stringify(envelope({ pickupTime: null, dropoffTime: null, pickupNotificationTime: null, dropoffNotificationTime: null }))); return; }
    if (url.pathname === '/api/v1/admin/monasteries/1') { res.end(JSON.stringify(envelope({ id: 1, monasteryName: 'Fixture Monastery', address: 'Yangon', isOwner: true }))); return; }
    if (url.pathname === '/api/v1/monasteries/1/members') { res.end(JSON.stringify(envelope([{ id: 1, userId: 1, userName: 'Owner fixture', phoneNumber: '0901', spaceRoleName: 'Owner', spaceRoleId: 1, isOwner: true }, { id: 2, userId: 2, userName: 'Member fixture', phoneNumber: '0902', spaceRoleName: 'Viewer', spaceRoleId: 4, isOwner: false }]))); return; }
    if (url.pathname === '/api/v1/admin/invitations') { res.end(JSON.stringify(envelope([{ id: 1, invitedUserId: 2, userName: 'Invited fixture', monasteryName: 'Fixture Monastery', status: 0, spaceRoleName: 'Viewer' }]))); return; }
    if (url.pathname === '/api/v1/admin/notifications') { res.end(JSON.stringify(envelope([{ id: 1, userId: 1, userName: 'Admin', message: 'Fixture notification', type: 2, isRead: false }]))); return; }
    if (url.pathname === '/api/v1/permissions' || url.pathname === '/api/v1/space-permissions') { res.end(JSON.stringify(envelope([{ id: 1, name: 'Roles.View' }]))); return; }
    if (url.pathname === '/api/v1/space-roles') { res.end(JSON.stringify(envelope([{ id: 1, name: 'Owner', permissions: [{ id: 1, name: 'Roles.View' }] }, { id: 4, name: 'Viewer', permissions: [] }]))); return; }
    if (url.pathname === '/api/v1/admin/summary') { res.end(JSON.stringify(envelope({ donations: 12, pendingReview: 6, completed: 6, monasteries: 2, users: 3 }))); return; }
    if (url.pathname === '/api/v1/admin/donations') {
      const page = Number(url.searchParams.get('PageNumber') || 1), size = Number(url.searchParams.get('PageSize') || 10);
      let rows = donations;
      if (url.searchParams.get('Search')) rows = rows.filter(x => x.donorName.includes(url.searchParams.get('Search')));
      if (url.searchParams.get('Status')) rows = rows.filter(x => x.status === 2);
      const total = rows.length;
      res.end(JSON.stringify({ ...envelope(rows.slice((page - 1) * size, page * size)), pagination: { pageNumber: page, totalCount: total, totalPages: Math.ceil(total / size), hasPreviousPage: page > 1, hasNextPage: page * size < total } })); return;
    }
    const data = url.pathname === '/api/User' ? [user] : url.pathname === '/api/v1/roles' ? [{ id: 1, name: 'Admin', permissions: [{ id: 1, name: 'Roles.View' }] }] : [{ id: 1, monasteryName: 'Fixture Monastery', address: 'Yangon', description: 'Fixture' }];
    res.end(JSON.stringify(envelope(data)));
  });
  await new Promise(resolve => fixture.listen(0, '127.0.0.1', resolve));
  const apiPort = fixture.address().port;
  const port = 3197;
  const origin = `http://localhost:${port}`;
  let output = '';
  const app = spawn(process.execPath, ['node_modules/next/dist/bin/next', 'start', '-p', String(port)], { env: { ...process.env, API_BASE_URL: `http://127.0.0.1:${apiPort}`, SESSION_SECRET: randomBytes(32).toString('hex') }, stdio: ['ignore', 'pipe', 'pipe'] });
  app.stdout.on('data', chunk => { output += chunk; }); app.stderr.on('data', chunk => { output += chunk; });
  t.after(async () => { app.kill(); fixture.closeAllConnections(); await new Promise(resolve => fixture.close(resolve)); });
  for (let attempt = 0; attempt < 100; attempt++) {
    try { if ((await fetch(`${origin}/login`)).ok) break; } catch {}
    if (attempt === 99) throw new Error(`Next server failed to start: ${output}`);
    await new Promise(resolve => setTimeout(resolve, 200));
  }
  const browser = await chromium.launch({ headless: true });
  t.after(() => browser.close());
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  const browserCalls = [], errors = [];
  page.on('request', req => browserCalls.push(req.url()));
  page.on('pageerror', err => errors.push(err.message));
  await page.goto(`${origin}/dashboard`);
  await page.waitForURL('**/login');
  await page.getByLabel('ဖုန်းနံပါတ်', { exact: true }).fill('donor');
  await page.getByLabel('စကားဝှက်', { exact: true }).fill('fixture-password');
  await page.getByRole('button', { name: 'အကောင့်ဝင်ရန်', exact: true }).click();
  await page.getByRole('alert').waitFor();
  await page.getByRole('button', { name: 'အကောင့်ဝင်ရန်', exact: true }).waitFor();
  assert.equal((await context.cookies()).filter(x => x.name === 'hsumchaint_admin').length, 0, 'Donor must not get an admin session');
  await page.getByLabel('ဖုန်းနံပါတ်', { exact: true }).fill('0900000000');
  await page.getByLabel('စကားဝှက်', { exact: true }).fill('wrong-password');
  await page.getByRole('button', { name: 'အကောင့်ဝင်ရန်', exact: true }).click();
  await page.getByText('ဖုန်းနံပါတ် သို့မဟုတ် စကားဝှက်ကို စစ်ဆေးပါ။', { exact: true }).waitFor();
  await page.getByLabel('စကားဝှက်', { exact: true }).fill('fixture-password');
  await page.getByRole('button', { name: 'အကောင့်ဝင်ရန်', exact: true }).click();
  await page.waitForURL('**/dashboard');
  await page.getByText('Fixture Donor 1', { exact: true }).waitFor();
  const session = (await context.cookies()).find(x => x.name === 'hsumchaint_admin');
  assert.ok(session?.httpOnly && session.secure && session.sameSite === 'Lax');
  assert.ok(!(await page.evaluate(() => document.cookie)).includes('hsumchaint_admin'));
  const html = await (await fetch(`${origin}/dashboard`, { headers: { cookie: `${session.name}=${session.value}` } })).text();
  assert.ok(html.includes('Fixture Donor 1'), 'Data must be rendered in server HTML');
  assert.ok(!html.includes(token) && !html.includes('fixture-refresh'), 'Tokens must not reach HTML or client props');
  assert.equal(await page.evaluate(() => Object.keys(localStorage).length), 0);
  await page.screenshot({ path: 'test-results/dashboard-desktop.png', fullPage: true });
  await page.goto(`${origin}/donations`);
  await page.getByRole('link', { name: 'နောက်စာမျက်နှာ' }).click();
  await page.getByText('Fixture Donor 11', { exact: true }).waitFor();
  await page.getByLabel('အလှူရှင်အမည် ရှာရန်').fill('Fixture Donor 1');
  await page.getByLabel('အလှူအခြေအနေ').selectOption('PendingReview');
  await page.locator('#from').fill('2026-10-01'); await page.locator('#to').fill('2026-10-31');
  await page.getByRole('button', { name: 'ရှာရန်', exact: true }).click();
  await page.waitForURL(url => url.searchParams.get('status') === 'PendingReview');
  await page.getByRole('button', { name: 'အလှူ #1 အသေးစိတ်', exact: true }).click();
  await page.getByRole('dialog').waitFor();
  assert.ok(await page.getByRole('dialog').getByText('<script>alert("unsafe")</script>', { exact: true }).isVisible());
  await page.keyboard.press('Escape');
  const filtered = calls.findLast(x => x.path === '/api/v1/admin/donations');
  assert.equal(filtered.query.get('FromDate'), '2026-09-30T17:30:00.000Z');
  assert.equal(filtered.query.get('ToDate'), '2026-10-31T17:29:59.999Z');
  assert.equal(filtered.query.get('PageNumber'), '1');
  // Exercise actual Server Actions from dialogs, including validation, methods, payloads and confirmation.
  await page.goto(`${origin}/donations`);
  await page.getByRole('button', { name: 'အလှူအသစ်', exact: true }).click();
  let dialog = page.getByRole('dialog');
  await dialog.getByLabel('ကျောင်းတိုက်', { exact: false }).selectOption('1');
  await dialog.getByLabel('အလှူရှင်အမည်', { exact: true }).fill('Walk-in fixture');
  await dialog.getByLabel('အလှူအမျိုးအစား', { exact: false }).first().selectOption('1');
  await dialog.getByLabel('အရေအတွက်', { exact: true }).fill('12');
  await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click();
  await dialog.waitFor({ state: 'hidden' });
  assert.equal(calls.findLast(c => c.path === '/api/v1/donations/manual').payload.quantity, 12);
  await page.getByRole('button', { name: 'စုပေါင်းအလှူ', exact: true }).click();
  dialog = page.getByRole('dialog');
  await dialog.locator('[name="monasterySpaceId"]').selectOption('1');
  await dialog.locator('[name="row0.donorName"]').fill('Batch first');
  await dialog.locator('[name="row0.donationType"]').selectOption('1');
  await dialog.getByRole('button', { name: 'နောက်ထပ်အလှူ ထည့်ရန်' }).click();
  await dialog.locator('[name="row1.donorName"]').fill('Batch second');
  await dialog.locator('[name="row1.donationType"]').selectOption('0');
  await dialog.locator('[name="row1.amount"]').fill('1000');
  await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click(); await dialog.waitFor({ state: 'hidden' });
  const batchCall = calls.findLast(c => c.path === '/api/v1/donations/batch');
  assert.equal(batchCall.payload.donations.length, 2); assert.equal(batchCall.payload.donations[1].amount, 1000);
  await page.getByRole('button', { name: 'လစဉ်အလှူ', exact: true }).click();
  dialog = page.getByRole('dialog');
  await dialog.locator('[name="monasterySpaceId"]').selectOption('1');
  await dialog.locator('[name="donorName"]').fill('Monthly fixture');
  await dialog.locator('[name="donationType"]').selectOption('1');
  await dialog.locator('[name="startDate"]').fill('2027-02-01');
  await dialog.locator('[name="dayOfMonth"]').fill('31');
  await dialog.locator('[name="monthsCount"]').fill('2');
  await dialog.locator('[name="pickupTimeOfDay"]').fill('03:00');
  await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click(); await dialog.waitFor({ state: 'hidden' });
  assert.equal(calls.findLast(c => c.path === '/api/v1/donations/recurring/manual').payload.timeZoneOffsetMinutes, 390);
  let firstRow = page.getByRole('row').filter({ hasText: 'Fixture Donor 1' }).first();
  await firstRow.getByRole('button', { name: 'စိစစ်ရန်', exact: true }).click();
  dialog = page.getByRole('dialog');
  await dialog.getByLabel('ဆုံးဖြတ်ချက်', { exact: false }).selectOption('3');
  await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click(); await dialog.waitFor({ state: 'hidden' });
  assert.equal(calls.findLast(c => c.path.endsWith('/1/review')).payload.status, 3);
  await firstRow.getByRole('button', { name: 'အချိန်သတ်မှတ်', exact: true }).click();
  dialog = page.getByRole('dialog');
  await dialog.getByLabel('လာယူမည့်အချိန်', { exact: false }).fill('2026-11-01T09:00');
  await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click(); await dialog.waitFor({ state: 'hidden' });
  const scheduleCall = calls.findLast(c => c.path.endsWith('/1/schedule'));
  assert.equal(scheduleCall.method, 'PUT'); assert.equal(scheduleCall.payload.pickupTime, '2026-11-01T02:30:00.000Z');
  await firstRow.getByRole('button', { name: 'ပြီးစီး', exact: true }).click();
  dialog = page.getByRole('dialog'); await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click(); await dialog.waitFor({ state: 'hidden' });
  await page.goto(`${origin}/roles`);
  await page.getByRole('button', { name: 'အခန်းကဏ္ဍအသစ် ဖန်တီးရန်', exact: true }).click();
  dialog = page.getByRole('dialog');
  await dialog.getByLabel('အခန်းကဏ္ဍအမည်', { exact: false }).fill('Blocked role');
  await dialog.getByLabel('Roles.View', { exact: true }).check();
  await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click();
  await dialog.getByRole('alert').waitFor();
  assert.equal(await dialog.getByLabel('အခန်းကဏ္ဍအမည်', { exact: false }).inputValue(), 'Blocked role', 'Errors must preserve entered fields');
  await dialog.getByLabel('အခန်းကဏ္ဍအမည်', { exact: false }).fill('Custom fixture');
  await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click(); await dialog.waitFor({ state: 'hidden' });
  assert.deepEqual(calls.findLast(c => c.path === '/api/v1/roles' && c.method === 'POST').payload.permissionIds, [1]);
  await page.goto(`${origin}/monasteries/1`);
  await page.getByRole('row').filter({ hasText: 'Member fixture' }).getByRole('button', { name: 'အခန်းကဏ္ဍပြောင်း', exact: true }).click();
  dialog = page.getByRole('dialog'); await dialog.getByLabel('အခန်းကဏ္ဍ', { exact: false }).selectOption('4');
  await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click(); await dialog.waitFor({ state: 'hidden' });
  assert.equal(calls.findLast(c => c.path.endsWith('/members/2/role')).method, 'PUT');
  await page.getByRole('button', { name: 'ကျောင်းတိုက် ရပ်ဆိုင်းရန်', exact: true }).click();
  dialog = page.getByRole('dialog'); await dialog.locator('[name="confirm"]').check();
  await dialog.getByRole('button', { name: 'အတည်ပြုရန်', exact: true }).click();
  await page.waitForURL('**/monasteries');
  assert.equal(calls.findLast(c => c.path === '/api/v1/monasteries/1').method, 'DELETE');
  await page.goto(`${origin}/notifications`);
  await page.getByRole('button', { name: 'ဖျက်ရန်', exact: true }).click();
  dialog = page.getByRole('dialog');
  await dialog.getByRole('button', { name: 'အတည်ပြုရန်', exact: true }).click();
  assert.ok(await dialog.isVisible(), 'Destructive mutations must require confirmation');
  await dialog.getByRole('checkbox').check();
  await dialog.getByRole('button', { name: 'အတည်ပြုရန်', exact: true }).click(); await dialog.waitFor({ state: 'hidden' });
  assert.equal(calls.findLast(c => c.path === '/api/v1/admin/notifications/1').method, 'DELETE');
  for (const path of ['calendar', 'space-roles', 'invitations', 'users/1']) {
    await page.goto(`${origin}/${path}`); await page.locator('main h1').waitFor();
  }
  await page.goto(`${origin}/users`);
  await page.getByRole('button', { name: 'အသုံးပြုသူအသစ် ဖန်တီးရန်', exact: true }).click();
  dialog = page.getByRole('dialog');
  await dialog.locator('[name="name"]').fill('New fixture');
  await dialog.locator('[name="phoneNumber"]').fill('0900000002');
  await dialog.locator('[name="roleId"]').selectOption('1');
  await dialog.locator('[name="password"]').fill('new-fixture-password');
  await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click(); await dialog.waitFor({ state: 'hidden' });
  assert.equal(calls.findLast(c => c.path === '/api/v1/admin/users').payload.roleId, 1);
  await page.getByRole('button', { name: 'ပြင်ရန်', exact: true }).click();
  dialog = page.getByRole('dialog'); await dialog.locator('[name="name"]').fill('Edited fixture');
  await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click(); await dialog.waitFor({ state: 'hidden' });
  assert.equal(calls.findLast(c => c.path === '/api/v1/admin/users/1').method, 'PUT');
  await page.goto(`${origin}/monasteries`);
  await page.getByRole('button', { name: 'ကျောင်းတိုက်အသစ် ဖန်တီးရန်', exact: true }).click();
  dialog = page.getByRole('dialog'); await dialog.locator('[name="monasteryName"]').fill('New monastery'); await dialog.locator('[name="address"]').fill('Yangon');
  await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click(); await dialog.waitFor({ state: 'hidden' });
  assert.equal(calls.findLast(c => c.path === '/api/v1/monasteries' && c.method === 'POST').payload.address, 'Yangon');
  await page.goto(`${origin}/notifications`);
  await page.getByRole('button', { name: 'အသိပေးချက် ဖန်တီးရန်', exact: true }).click();
  dialog = page.getByRole('dialog'); await dialog.locator('[name="userId"]').selectOption('1'); await dialog.locator('[name="type"]').selectOption('2'); await dialog.locator('[name="message"]').fill('Test announcement');
  await dialog.getByRole('button', { name: 'သိမ်းဆည်းရန်', exact: true }).click(); await dialog.waitFor({ state: 'hidden' });
  assert.equal(calls.findLast(c => c.path === '/api/v1/admin/notifications' && c.method === 'POST').payload.type, 2);
  const csv = await context.request.get(`${origin}/donations/export?Status=PendingReview`);
  const csvText = await csv.text();
  assert.equal(csv.status(), 200); assert.ok(csv.headers()['content-type'].includes('text/csv')); assert.ok(csvText.includes('Donor')); assert.ok(csvText.includes('"\'=1+1"'), 'CSV formulas must be neutralized');
  for (const path of ['users', 'monasteries', 'roles']) {
    await page.goto(`${origin}/${path}`);
    await page.locator('main h1').waitFor();
  }
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`${origin}/dashboard`);
  await page.getByRole('button', { name: 'မီနူး ဖွင့်ရန်' }).click();
  await page.getByRole('link', { name: /အလှူမှတ်တမ်းများ Donations/ }).click();
  await page.waitForURL('**/donations');
  await page.getByText('Fixture Donor 1', { exact: true }).waitFor();
  await page.waitForFunction(() => document.querySelector('.sidebar').getBoundingClientRect().right <= 0);
  assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'Mobile viewport must not overflow');
  await page.screenshot({ path: 'test-results/donations-mobile.png', fullPage: true });
  unavailable = true;
  await page.goto(`${origin}/dashboard`);
  await page.getByRole('heading', { name: 'စနစ်ကို ချိတ်ဆက်၍ မရသေးပါ' }).waitFor();
  unavailable = false;
  await page.goto(`${origin}/dashboard`);
  await page.getByRole('button', { name: 'မီနူး ဖွင့်ရန်' }).click();
  await page.getByRole('button', { name: 'အကောင့်ထွက်ရန်' }).click();
  await page.waitForURL('**/login');
  assert.equal((await context.cookies()).filter(x => x.name === 'hsumchaint_admin').length, 0);
  await context.addCookies([{ name: session.name, value: `${session.value.slice(0, -5)}XXXXX`, url: origin }]);
  await page.goto(`${origin}/dashboard`); await page.waitForURL('**/login');
  assert.equal(browserCalls.some(url => url.includes(`:${apiPort}`)), false, 'Browser must not call the C# API directly');
  assert.deepEqual(errors, []);
});
