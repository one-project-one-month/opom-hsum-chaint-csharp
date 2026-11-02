import { allRows, options } from '@/lib/options';
import { donationFields } from '@/lib/form-fields';
import type { Monastery, User } from '@/lib/types';
import type { Field } from '@/lib/workflows';
import { WorkflowDialog } from './workflow-dialog';
export async function DonationCreate() {
  const [users, monasteries] = await Promise.all([allRows<User>('/api/User'), allRows<Monastery>('/api/v1/monasteries')]);
  const monastery: Field = { name: 'monasterySpaceId', label: 'ကျောင်းတိုက်', required: true, type: 'select', options: options(monasteries, m => m.monasteryName || `#${m.id}`) };
  const userOptions = options(users, u => `${u.name} · ${u.phoneNumber}`);
  return <div className="head-actions"><WorkflowDialog operation="donation.create" title="အလှူအသစ် မှတ်တမ်းတင်ရန်" label="အလှူအသစ်" fields={[monastery, ...donationFields(userOptions)]} /><WorkflowDialog operation="donation.batch" title="အလှူများ စုပေါင်းထည့်ရန်" label="စုပေါင်းအလှူ" fields={[monastery]} repeatFields={donationFields(userOptions)} /><WorkflowDialog operation="donation.recurring" title="လစဉ်အလှူ စီစဉ်ရန်" label="လစဉ်အလှူ" description="လတိုင်း သတ်မှတ်ရက်တွင် အလှူမှတ်တမ်းများ ကြိုတင်ဖန်တီးမည်။ ရက်မရှိသောလတွင် လကုန်ရက်ကို သုံးမည်။" fields={[monastery, ...donationFields(userOptions, false), { name: 'startDate', label: 'စတင်မည့်လ၏ ရက်တစ်ရက်', type: 'date', required: true }, { name: 'dayOfMonth', label: 'လစဉ်ရက်', type: 'number', required: true, min: 1, max: 31 }, { name: 'monthsCount', label: 'လအရေအတွက်', type: 'number', required: true, min: 1, max: 24 }, { name: 'pickupTimeOfDay', label: 'လာယူချိန် (မြန်မာစံတော်ချိန်)', type: 'time' }, { name: 'dropoffTimeOfDay', label: 'ပို့ဆောင်ချိန် (မြန်မာစံတော်ချိန်)', type: 'time' }]} /></div>;
}
