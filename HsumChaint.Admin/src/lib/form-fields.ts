import type { Field, Option } from './workflows';
import { donationTypes, type User } from './types';
export const monasteryFields: Field[] = [
  { name: 'monasteryName', label: 'ကျောင်းတိုက်အမည်', required: true },
  { name: 'address', label: 'လိပ်စာ', type: 'textarea', required: true },
  { name: 'description', label: 'ဖော်ပြချက်', type: 'textarea' },
];
export const scheduleFields: Field[] = [
  { name: 'pickupTime', label: 'လာယူမည့်အချိန် (မြန်မာစံတော်ချိန်)', type: 'datetime-local' },
  { name: 'dropoffTime', label: 'ပို့ဆောင်မည့်အချိန် (မြန်မာစံတော်ချိန်)', type: 'datetime-local' },
];
export function donationFields(users: Option[], scheduled = true): Field[] { return [
  { name: 'donorId', label: 'အလှူရှင်အကောင့်', type: 'select', options: users, hint: 'အကောင့်မရှိလျှင် အောက်တွင် အလှူရှင်အမည် ဖြည့်ပါ။' },
  { name: 'donorName', label: 'အလှူရှင်အမည်' },
  { name: 'donationType', label: 'အလှူအမျိုးအစား', type: 'select', required: true, options: donationTypes.map((label, i) => ({ value: String(i), label })) },
  { name: 'customDonationType', label: 'အခြား အလှူအမျိုးအစား' },
  { name: 'amount', label: 'ငွေပမာဏ (ကျပ်)', type: 'number', min: 0 },
  { name: 'quantity', label: 'အရေအတွက်', type: 'number', min: 0 },
  { name: 'note', label: 'မှတ်ချက်', type: 'textarea' },
  ...(scheduled ? scheduleFields : []),
]; }
export function userFields(roles: Option[], user?: User): Field[] { return [
  { name: 'name', label: 'အမည်', required: true, value: user?.name || '' },
  { name: 'phoneNumber', label: 'ဖုန်းနံပါတ်', type: 'tel', required: true, value: user?.phoneNumber },
  { name: 'email', label: 'အီးမေးလ်', type: 'email', value: user?.email || '' },
  { name: 'contactPhoneNumber', label: 'ဆက်သွယ်ရန်ဖုန်း', type: 'tel', value: user?.contactPhoneNumber || '' },
  { name: 'roleId', label: 'System role', type: 'select', required: true, options: roles, value: user?.roleId },
  ...(!user ? [{ name: 'password', label: 'စကားဝှက်', type: 'password' as const, required: true }, { name: 'monasteryName', label: 'သံဃာကိုယ်ရေး ကျောင်းအမည် (Monk အတွက်)' }, { name: 'monasteryAddress', label: 'သံဃာကိုယ်ရေး ကျောင်းလိပ်စာ' }] : []),
]; }
export function localInput(value: string | null | undefined) {
  if (!value) return '';
  const normalized = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : `${value}Z`;
  return new Date(new Date(normalized).getTime() + 390 * 60000).toISOString().slice(0, 16);
}
