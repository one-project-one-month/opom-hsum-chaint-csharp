import { adminApi, requireAdmin } from '@/lib/session';
import { allRows, options } from '@/lib/options';
import { pageNumber, type SearchParams } from '@/lib/params';
import type { Invitation, Monastery, SpaceRole, User } from '@/lib/types';
import { date } from '@/lib/utils';
import { WorkflowDialog } from '@/components/workflow-dialog';
import { PageHead, Pager, Empty } from '@/components/page-parts';
import { Card } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableHeader, TableHead, TableRow, TableBody, TableCell } from '@/components/ui/table';
export const metadata = { title: 'ဖိတ်ခေါ်မှုများ' };
export default async function Invitations({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const page = pageNumber((await searchParams).page);
  const [result, users, monasteries, roles, { user: actor }] = await Promise.all([adminApi<Invitation[]>(`/api/v1/admin/invitations?PageNumber=${page}&PageSize=10`), allRows<User>('/api/User'), allRows<Monastery>('/api/v1/monasteries'), allRows<SpaceRole>('/api/v1/space-roles'), requireAdmin()]);
  return <><PageHead title="ဖိတ်ခေါ်မှုများ" description="ကျောင်းတိုက်အဖွဲ့ဝင် ဖိတ်ခေါ်မှုများကို စီမံရန်" action={<WorkflowDialog operation="invitation.create" title="အဖွဲ့ဝင်အသစ် ဖိတ်ခေါ်ရန်" fields={[{ name: 'monasterySpaceId', label: 'ကျောင်းတိုက်', type: 'select', required: true, options: options(monasteries, m => m.monasteryName || `#${m.id}`) }, { name: 'userId', label: 'အသုံးပြုသူ', type: 'select', required: true, options: options(users.filter(u => u.id !== actor.id), u => `${u.name} · ${u.phoneNumber}`) }, { name: 'spaceRoleId', label: 'ကျောင်းတိုက်အခန်းကဏ္ဍ', type: 'select', required: true, options: options(roles.filter(r => r.name !== 'Owner'), r => r.name) }]} />} /><p className="notice">ဖိတ်ခေါ်ခံရသူကသာ လက်ခံ/ငြင်းပယ်နိုင်သည်။ Admin သည် စောင့်ဆိုင်းဆဲဖိတ်ခေါ်မှုကို ရုပ်သိမ်းနိုင်သည်။</p><Card className="data-panel">{result.data.length ? <Table><TableHeader><TableRow><TableHead>ဖိတ်ခေါ်ခံရသူ</TableHead><TableHead>ကျောင်းတိုက်</TableHead><TableHead>အခန်းကဏ္ဍ</TableHead><TableHead>အခြေအနေ</TableHead><TableHead>ရက်စွဲ</TableHead><TableHead>လုပ်ဆောင်ချက်</TableHead></TableRow></TableHeader><TableBody>{result.data.map(i => <TableRow key={i.id}><TableCell>{i.userName}</TableCell><TableCell>{i.monasteryName}</TableCell><TableCell>{i.spaceRoleName}</TableCell><TableCell><Badge variant="secondary">{['စောင့်ဆိုင်းဆဲ', 'လက်ခံပြီး', 'ငြင်းပယ် / ရုပ်သိမ်းပြီး'][i.status]}</Badge></TableCell><TableCell>{date(i.createdAt)}</TableCell><TableCell>{i.status === 0 && <div className="row-actions">{i.invitedUserId === actor.id && <WorkflowDialog operation="invitation.respond" id={i.id} title="ဖိတ်ခေါ်မှု တုံ့ပြန်ရန်" fields={[{ name: 'status', label: 'ဆုံးဖြတ်ချက်', type: 'select', required: true, options: [{ value: '1', label: 'လက်ခံမည်' }, { value: '2', label: 'ငြင်းပယ်မည်' }] }]} />}<WorkflowDialog operation="invitation.revoke" id={i.id} title={`ဖိတ်ခေါ်မှု #${i.id} ရုပ်သိမ်းရန်`} label="ရုပ်သိမ်းရန်" destructive /></div>}</TableCell></TableRow>)}</TableBody></Table> : <Empty />}<Pager path="/invitations" pagination={result.pagination} /></Card></>;
}
