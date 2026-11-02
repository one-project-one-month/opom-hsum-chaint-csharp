import { WorkflowDialog } from './workflow-dialog';
import { localInput, scheduleFields } from '@/lib/form-fields';
import type { Donation } from '@/lib/types';
export function DonationActions({ donation: d }: { donation: Donation }) {
  const terminal = [5, 7].includes(d.status);
  return <div className="row-actions">
    {!terminal && <WorkflowDialog operation="donation.review" id={d.id} title={`အလှူ #${d.id} စိစစ်ရန်`} label="စိစစ်ရန်" fields={[{ name: 'status', label: 'ဆုံးဖြတ်ချက်', type: 'select', required: true, options: [{ value: '3', label: 'လက်ခံရန်' }, { value: '6', label: 'ငြင်းပယ်ရန်' }] }, { name: 'note', label: 'စိစစ်မှတ်ချက်', type: 'textarea', value: d.note || '' }]} />}
    {![5, 6, 7].includes(d.status) && <><WorkflowDialog operation="donation.schedule" id={d.id} title={`အလှူ #${d.id} အချိန်သတ်မှတ်ရန်`} label="အချိန်သတ်မှတ်" fields={scheduleFields.map(f => ({ ...f, value: localInput(f.name === 'pickupTime' ? d.pickupTime : d.dropoffTime) }))} /><WorkflowDialog operation="donation.complete" id={d.id} title={`အလှူ #${d.id} ပြီးစီးကြောင်း အတည်ပြုရန်`} label="ပြီးစီး" description="အလှူလက်ခံမှုနှင့် ပို့ဆောင်မှု ပြီးစီးကြောင်း အတည်ပြုပါ။" /></>}
    {d.status !== 5 && <><WorkflowDialog operation="donation.cancel" id={d.id} title={`အလှူ #${d.id} ပယ်ဖျက်ရန်`} label="ပယ်ဖျက်" destructive disabled={d.status === 7} /><WorkflowDialog operation="donation.delete" id={d.id} title={`အလှူ #${d.id} မှတ်တမ်း ဖျက်ရန်`} label="ဖျက်ရန်" destructive /></>}
  </div>;
}
