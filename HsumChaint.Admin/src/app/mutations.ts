'use server';
import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';
import { requireAdmin } from '@/lib/session';
import { api, ApiError } from '@/lib/api';
import type { MutationState, Operation } from '@/lib/workflows';

export async function mutate(operation: Operation, id: number | undefined, parent: number | undefined, _previous: MutationState, form: FormData): Promise<MutationState> {
  const { session } = await requireAdmin();
  const text = (key: string, required = false, max = 2000) => {
    const value = String(form.get(key) ?? '').trim();
    if ((required && !value) || value.length > max) throw new Error(`${key}: မှန်ကန်စွာ ဖြည့်ပါ။`);
    return value;
  };
  const numeric = (key: string, required = true, min = 1, max = 2147483647): number | null => {
    const value = text(key, required); if (!value) return null;
    const n = Number(value); if (!Number.isFinite(n) || n < min || n > max || (min === 1 && !Number.isInteger(n))) throw new Error(`${key}: ပမာဏကို စစ်ဆေးပါ။`);
    return n;
  };
  const target = (value: number | undefined) => { if (!Number.isSafeInteger(value) || !value || value <= 0) throw new Error('မှတ်တမ်းအမှတ် မှားနေပါသည်။'); return value; };
  const instant = (key: string) => {
    const value = text(key); if (!value) return null;
    const date = new Date(`${value.length === 16 ? value + ':00' : value}+06:30`);
    if (Number.isNaN(date.getTime())) throw new Error('ရက်နှင့်အချိန်ကို စစ်ဆေးပါ။');
    return date.toISOString();
  };
  const permissions = () => form.getAll('permissionIds').map(value => { const n = Number(value); if (!Number.isInteger(n) || n <= 0) throw new Error('ခွင့်ပြုချက်အမှတ် မှားနေပါသည်။'); return n; });
  const password = () => { const value = form.get('password'); if (typeof value !== 'string' || !value.trim() || value.length > 256) throw new Error('စကားဝှက်ကို စစ်ဆေးပါ။'); return value; };
  const donation = (prefix = '') => ({ monasterySpaceId: numeric('monasterySpaceId'), donorId: numeric(`${prefix}donorId`, false), donorName: text(`${prefix}donorName`), donationType: numeric(`${prefix}donationType`, true, 0, 5), customDonationType: text(`${prefix}customDonationType`), amount: numeric(`${prefix}amount`, false, 0), quantity: numeric(`${prefix}quantity`, false, 0), note: text(`${prefix}note`), pickupTime: instant(`${prefix}pickupTime`), dropoffTime: instant(`${prefix}dropoffTime`) });
  try {
    const destructive: Operation[] = ['donation.cancel', 'donation.delete', 'user.delete', 'user.profileDelete', 'role.delete', 'spaceRole.delete', 'monastery.delete', 'member.remove', 'invitation.revoke', 'notification.delete', 'notification.clearAll'];
    if (destructive.includes(operation) && text('confirm') !== 'yes') return { error: 'လုပ်ဆောင်ချက်ကို အတည်ပြုရန် လိုအပ်ပါသည်။' };
    let path = '', method = 'POST', body: unknown = undefined;
    if (operation === 'donation.create') { path = '/api/v1/donations/manual'; body = donation(); }
    else if (operation === 'donation.batch') {
      const count = numeric('rowCount', true, 1, 50)!;
      path = '/api/v1/donations/batch'; body = { monasterySpaceId: numeric('monasterySpaceId'), donations: Array.from({ length: count }, (_, i) => donation(`row${i}.`)) };
    }
    else if (operation === 'donation.recurring') {
      const d = donation(); path = '/api/v1/donations/recurring/manual';
      const time = (name: string) => { const value = text(name); if (value && !/^\d{2}:\d{2}$/.test(value)) throw new Error('အချိန် မှားနေပါသည်။'); return value ? `${value}:00` : null; };
      body = { ...d, timeZoneOffsetMinutes: 390, startDate: `${text('startDate', true)}T00:00:00Z`, dayOfMonth: numeric('dayOfMonth', true, 1, 31), monthsCount: numeric('monthsCount', true, 1, 24), pickupTimeOfDay: time('pickupTimeOfDay'), dropoffTimeOfDay: time('dropoffTimeOfDay') };
    }
    else if (operation.startsWith('donation.')) {
      const action = operation.split('.')[1]; path = `/api/v1/donations/${target(id)}`;
      if (action === 'delete') method = 'DELETE';
      else if (action === 'review') { path += '/review'; body = { id, status: numeric('status', true, 0, 7), note: text('note') }; }
      else if (action === 'schedule') { path += '/schedule'; method = 'PUT'; body = { id, pickupTime: instant('pickupTime'), dropoffTime: instant('dropoffTime') }; }
      else if (action === 'complete' || action === 'cancel') { path += `/${action}`; body = { id }; }
      else throw new Error('လုပ်ဆောင်ချက် မမှန်ပါ။');
    }
    else if (operation === 'user.create') { path = '/api/v1/admin/users'; body = { name: text('name', true, 100), phoneNumber: text('phoneNumber', true, 30), password: password(), email: text('email'), roleId: numeric('roleId'), contactPhoneNumber: text('contactPhoneNumber'), monasteryName: text('monasteryName'), monasteryAddress: text('monasteryAddress') }; }
    else if (operation === 'user.update') { path = `/api/v1/admin/users/${target(id)}`; method = 'PUT'; body = { id, name: text('name', true, 100), phoneNumber: text('phoneNumber', true, 30), email: text('email'), contactPhoneNumber: text('contactPhoneNumber'), roleId: numeric('roleId') }; }
    else if (operation === 'user.delete') { path = `/api/v1/admin/users/${target(id)}`; method = 'DELETE'; }
    else if (operation === 'user.settings') { path = `/api/User/${target(id)}/settings`; method = 'PUT'; body = { userId: id, pickupNotificationTime: instant('pickupNotificationTime'), dropoffNotificationTime: instant('dropoffNotificationTime'), pickupTime: instant('pickupTime'), dropoffTime: instant('dropoffTime') }; }
    else if (operation === 'user.profile' || operation === 'user.profileDelete') { path = `/api/User/${target(id)}/monk-profile`; method = operation === 'user.profile' ? 'PATCH' : 'DELETE'; body = method === 'PATCH' ? { userId: id, monasteryName: text('monasteryName'), monasteryAddress: text('monasteryAddress') } : undefined; }
    else if (operation.startsWith('role.') || operation.startsWith('spaceRole.')) {
      path = operation.startsWith('role.') ? '/api/v1/roles' : '/api/v1/space-roles';
      if (operation.endsWith('.delete')) { path += `/${target(id)}`; method = 'DELETE'; }
      else { if (operation.endsWith('.update')) { path += `/${target(id)}`; method = 'PUT'; } body = { id, name: text('name', true, 50), description: text('description', false, 255), permissionIds: permissions() }; }
    }
    else if (operation.startsWith('monastery.')) {
      path = '/api/v1/monasteries';
      if (operation === 'monastery.delete') { path += `/${target(id)}`; method = 'DELETE'; }
      else { if (operation === 'monastery.update') { path += `/${target(id)}`; method = 'PATCH'; } body = { id, monasteryName: text('monasteryName', true, 255), address: text('address'), description: text('description') }; }
    }
    else if (operation === 'member.role' || operation === 'member.remove') {
      path = `/api/v1/monasteries/${target(parent)}/members/${target(id)}`;
      if (operation === 'member.role') { path += '/role'; method = 'PUT'; body = { monasterySpaceId: parent, memberUserId: id, spaceRoleId: numeric('spaceRoleId') }; } else method = 'DELETE';
    }
    else if (operation === 'invitation.create') { const monasterySpaceId = numeric('monasterySpaceId'); path = `/api/v1/monasteries/${monasterySpaceId}/invitations`; body = { monasterySpaceId, userId: numeric('userId'), spaceRoleId: numeric('spaceRoleId') }; }
    else if (operation === 'invitation.respond') { path = `/api/v1/monasteries/invitations/${target(id)}/respond`; body = { invitationId: id, status: numeric('status', true, 1, 2) }; }
    else if (operation === 'invitation.revoke') { path = `/api/v1/admin/invitations/${target(id)}`; method = 'DELETE'; }
    else if (operation === 'notification.create') { path = '/api/v1/admin/notifications'; body = { userId: numeric('userId'), message: text('message', true), type: numeric('type', true, 0, 3) }; }
    else if (operation === 'notification.readAll') { path = '/api/v1/admin/notifications/read-all'; body = {}; }
    else if (operation === 'notification.clearAll') { path = '/api/v1/admin/notifications'; method = 'DELETE'; }
    else if (operation === 'notification.read' || operation === 'notification.delete') { path = `/api/v1/admin/notifications/${target(id)}`; method = operation === 'notification.read' ? 'PATCH' : 'DELETE'; if (method === 'PATCH') path += '/read'; }
    else throw new Error('လုပ်ဆောင်ချက် မမှန်ပါ။');
    await api<unknown>(path, session.accessToken, body, method);
    revalidatePath('/', 'layout');
  } catch (error) {
    return { error: error instanceof ApiError || (error instanceof Error && !error.message.includes('NEXT_')) ? error.message : 'လုပ်ဆောင်မှု မအောင်မြင်ပါ။' };
  }
  // The detail resource no longer exists after deactivation; do not refresh its URL.
  // Keep redirect outside try/catch because Next.js implements navigation by throwing.
  if (operation === 'monastery.delete') redirect('/monasteries');
  return { success: true };
}
