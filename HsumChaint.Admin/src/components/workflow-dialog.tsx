'use client';
import { startTransition, useActionState, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { LoaderCircle, Plus, Trash2, X } from 'lucide-react';
import { mutate } from '@/app/mutations';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Dialog, DialogTrigger, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from './ui/dialog';
import type { Field, Operation } from '@/lib/workflows';
import { SearchableSelect } from './ui/searchable-select';
import { DateInput } from './ui/date-input';
import { SchedulePicker } from './ui/schedule-picker';
import { DeliveryTimePicker } from './ui/delivery-time-picker';
import { useLanguage } from './localization';

export function WorkflowDialog({ operation, id, parent, title, label, fields = [], description, destructive, disabled, repeatFields }: { operation: Operation; id?: number; parent?: number; title: string; label?: string; fields?: Field[]; description?: string; destructive?: boolean; disabled?: boolean; repeatFields?: Field[] }) {
  const { t } = useLanguage();
  const [open, setOpen] = useState(false);
  const [message, setMessage] = useState('');
  const visibleTitle = title.replace(/\s*#\d+/g, '');
  return <><Dialog open={open} onOpenChange={setOpen}><DialogTrigger asChild><Button size="sm" variant={destructive ? 'destructive' : 'outline'} disabled={disabled}>{t(label || visibleTitle)}</Button></DialogTrigger>{open && <WorkflowForm operation={operation} id={id} parent={parent} fields={fields} title={t(visibleTitle)} description={description ? t(description) : description} destructive={destructive} repeatFields={repeatFields} done={() => { setOpen(false); setMessage(t('လုပ်ဆောင်မှု အောင်မြင်ပါသည်။')); }} />}</Dialog>{message && <div className="workflow-toast" role="status"><span>{message}</span><Button size="icon" variant="ghost" aria-label={t('အသိပေးစာ ပိတ်ရန်')} onClick={() => setMessage('')}><X size={15} /></Button></div>}</>;
}
function WorkflowForm({ operation, id, parent, fields, title, description, destructive, repeatFields, done }: { operation: Operation; id?: number; parent?: number; fields: Field[]; title: string; description?: string; destructive?: boolean; repeatFields?: Field[]; done: () => void }) {
  const [state, action, pending] = useActionState(mutate.bind(null, operation, id, parent), {});
  const [rows, setRows] = useState([0]);
  const router = useRouter();
  useEffect(() => { if (state.success) { done(); router.refresh(); } }, [state.success, done, router]);
  return <DialogContent className="workflow-dialog" onInteractOutside={event => { if (pending) event.preventDefault(); }}><DialogHeader><DialogTitle>{title}</DialogTitle><DialogDescription>{description || (destructive ? 'ဤလုပ်ဆောင်ချက်ကို အတည်ပြုပါ။ မှတ်တမ်းကို ပြန်လည်ရယူနိုင်မည်ဟု အာမမခံပါ။' : 'လိုအပ်သော အချက်အလက်များကို ဖြည့်ပြီး သိမ်းဆည်းပါ။')}</DialogDescription></DialogHeader><form onSubmit={event => { event.preventDefault(); const data = new FormData(event.currentTarget); startTransition(() => action(data)); }}><fieldset disabled={pending} className="workflow-fields">{fields.map(field => <FormField key={field.name} field={field} />)}{repeatFields && <><input type="hidden" name="rowCount" value={rows.length} />{rows.map((row, index) => <div className="batch-row" key={row}><div className="batch-heading"><strong>အလှူ {index + 1}</strong>{rows.length > 1 && <Button type="button" variant="ghost" size="icon" aria-label="အလှူတန်း ဖယ်ရန်" onClick={() => setRows(rows.filter(x => x !== row))}><Trash2 size={16} /></Button>}</div>{repeatFields.map(field => <FormField key={field.name} field={{ ...field, name: `row${index}.${field.name}` }} />)}</div>)}<Button type="button" variant="outline" disabled={rows.length >= 50} onClick={() => setRows([...rows, Math.max(...rows) + 1])}><Plus />နောက်ထပ်အလှူ ထည့်ရန်</Button></>}{destructive && <label className="confirm-checkbox"><input type="checkbox" name="confirm" value="yes" required />ဤလုပ်ဆောင်ချက်ကို အတည်ပြုပါသည်။</label>}</fieldset>{state.error && !pending && <p role="alert" className="error-message">{state.error}</p>}<DialogFooter><Button type="submit" variant={destructive ? 'destructive' : 'default'} disabled={pending}>{pending && <LoaderCircle className="animate-spin" />}{pending ? 'သိမ်းဆည်းနေပါသည်…' : destructive ? 'အတည်ပြုရန်' : 'သိမ်းဆည်းရန်'}</Button></DialogFooter></form></DialogContent>;
}
function FormField({ field: f }: { field: Field }) {
  const id = `field-${f.name}`;
  if (f.type === 'schedule-picker') { const values = JSON.parse(String(f.value || '{}')) as { pickup?: string; dropoff?: string }; return <SchedulePicker pickupValue={values.pickup} dropoffValue={values.dropoff} />; }
  if (f.type === 'delivery-time-picker') { const values = JSON.parse(String(f.value || '{}')) as { pickup?: string; dropoff?: string }; return <DeliveryTimePicker pickupValue={values.pickup} dropoffValue={values.dropoff} />; }
  if (f.type === 'permissions') return <fieldset className="permission-picker"><legend>{f.label}</legend>{f.options?.map(option => <label key={option.value}><input type="checkbox" name={f.name} value={option.value} defaultChecked={f.selected?.includes(Number(option.value))} />{option.label}</label>)}</fieldset>;
  return <div className="field"><label htmlFor={id}>{f.label}{f.required ? ' *' : ''}</label>{f.type === 'searchable-select' ? <SearchableSelect name={f.name} label={f.label} options={f.options || []} value={f.value} required={f.required} /> : f.type === 'select' ? <select id={id} className="select" name={f.name} defaultValue={f.value ?? ''} required={f.required}><option value="">ရွေးချယ်ပါ</option>{f.options?.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}</select> : f.type === 'textarea' ? <textarea id={id} className="select textarea" name={f.name} defaultValue={f.value} maxLength={2000} required={f.required} /> : f.type === 'date' || f.type === 'datetime-local' ? <DateInput id={id} name={f.name} value={f.value} required={f.required} withTime={f.type === 'datetime-local'} /> : <Input id={id} name={f.name} type={f.type || 'text'} defaultValue={f.value} required={f.required} min={f.min} max={f.max} step={f.type === 'number' ? 'any' : undefined} maxLength={f.type === 'password' ? 256 : 2000} autoComplete={f.type === 'password' ? 'new-password' : undefined} />}{f.hint && <small className="muted">{f.hint}</small>}</div>;
}
