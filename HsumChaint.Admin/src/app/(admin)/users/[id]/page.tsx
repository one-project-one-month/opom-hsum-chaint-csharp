import { adminApi } from '@/lib/session';
import { allRows, options } from '@/lib/options';
import { localInput, userFields } from '@/lib/form-fields';
import type { Role, User } from '@/lib/types';
import type { Field } from '@/lib/workflows';
import { PageHead, Empty } from '@/components/page-parts';
import { WorkflowDialog } from '@/components/workflow-dialog';
import { Card, CardContent } from '@/components/ui/card';
type Settings = { pickupTime: string | null; dropoffTime: string | null; pickupNotificationTime: string | null; dropoffNotificationTime: string | null };
export default async function UserDetail({ params }: { params: Promise<{ id: string }> }) {
  const id = Number((await params).id); if (!Number.isSafeInteger(id) || id <= 0) return <Empty />;
  const [result, roles, settings] = await Promise.all([adminApi<User>(`/api/User/${id}`), allRows<Role>('/api/v1/roles'), adminApi<Settings>(`/api/User/${id}/settings`)]);
  const user = result.data;
  const settingFields: Field[] = ([['pickupTime', 'ပုံမှန်လာယူချိန်'], ['dropoffTime', 'ပုံမှန်ပို့ဆောင်ချိန်'], ['pickupNotificationTime', 'လာယူချိန် အသိပေးရက်/အချိန်'], ['dropoffNotificationTime', 'ပို့ဆောင်ချိန် အသိပေးရက်/အချိန်']] as const).map(([name, label]) => ({ name, label: `${label} (မြန်မာစံတော်ချိန်)`, type: 'datetime-local', value: localInput(settings.data[name]) }));
  const profile = user.roleName === 'Monk' ? (await adminApi<{ monasteryName: string | null; monasteryAddress: string | null }>(`/api/User/${id}/monk-profile`)).data : null;
  return <><PageHead title={user.name || `User #${id}`} description={`${user.phoneNumber} · ${user.roleName}`} action={<WorkflowDialog operation="user.update" id={id} title="အသုံးပြုသူ ပြင်ရန်" fields={userFields(options(roles, r => r.name), user)} />} /><Card><CardContent><dl className="details-grid"><div><dt>အီးမေးလ်</dt><dd>{user.email || '—'}</dd></div><div><dt>ဆက်သွယ်ရန်ဖုန်း</dt><dd>{user.contactPhoneNumber || '—'}</dd></div></dl><WorkflowDialog operation="user.settings" id={id} title="အချိန်နှင့် အသိပေးမှု ပြင်ရန်" fields={settingFields} />{profile && <section className="mt-7"><h2>သံဃာကိုယ်ရေးအချက်အလက်</h2><p className="muted">{profile.monasteryName} · {profile.monasteryAddress}</p><div className="row-actions mt-4"><WorkflowDialog operation="user.profile" id={id} title="သံဃာကိုယ်ရေးအချက်အလက် ပြင်ရန်" fields={[{ name: 'monasteryName', label: 'ကျောင်းအမည်', value: profile.monasteryName || '' }, { name: 'monasteryAddress', label: 'ကျောင်းလိပ်စာ', type: 'textarea', value: profile.monasteryAddress || '' }]} /><WorkflowDialog operation="user.profileDelete" id={id} title="သံဃာကိုယ်ရေးအချက်အလက် ဖျက်ရန်" destructive /></div></section>}</CardContent></Card></>;
}
