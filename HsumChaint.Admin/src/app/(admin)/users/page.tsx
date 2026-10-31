import Link from 'next/link';
import { adminApi, requireAdmin } from '@/lib/session';
import type { Role, User } from '@/lib/types';
import { pageNumber, type SearchParams } from '@/lib/params';
import { allRows, options } from '@/lib/options';
import { userFields } from '@/lib/form-fields';
import { WorkflowDialog } from '@/components/workflow-dialog';
import { PageHead, Pager, Empty } from '@/components/page-parts';
import { Card } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { buttonVariants } from '@/components/ui/button';
import { Table, TableHeader, TableBody, TableHead, TableRow, TableCell } from '@/components/ui/table';
export const metadata = { title: 'အသုံးပြုသူများ' };
export default async function Users({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const page = pageNumber((await searchParams).page);
  const [result, roles, { user: actor }] = await Promise.all([adminApi<User[]>(`/api/User?PageNumber=${page}&PageSize=10`), allRows<Role>('/api/v1/roles'), requireAdmin()]);
  const roleOptions = options(roles, r => r.name);
  return <><PageHead title="အသုံးပြုသူများ" description="စနစ်အတွင်းရှိ အသုံးပြုသူအကောင့်နှင့် အခန်းကဏ္ဍများ" action={<WorkflowDialog operation="user.create" title="အသုံးပြုသူအသစ် ဖန်တီးရန်" fields={userFields(roleOptions)} />} /><Card className="data-panel"><div className="panel-heading"><h2>အသုံးပြုသူစာရင်း</h2></div>{result.data.length ? <Table><TableHeader><TableRow><TableHead>အသုံးပြုသူ</TableHead><TableHead>ဖုန်းနံပါတ်</TableHead><TableHead>အီးမေးလ်</TableHead><TableHead>အခန်းကဏ္ဍ</TableHead><TableHead>လုပ်ဆောင်ချက်များ</TableHead></TableRow></TableHeader><TableBody>{result.data.map(user => <TableRow key={user.id}><TableCell><Link className="cell-title" href={`/users/${user.id}`}>{user.name || '—'}</Link><span className="cell-sub">#{user.id}</span></TableCell><TableCell>{user.phoneNumber}</TableCell><TableCell>{user.email || '—'}</TableCell><TableCell><Badge variant="secondary">{user.roleName}</Badge></TableCell><TableCell><div className="row-actions"><Link className={buttonVariants({ size: 'sm', variant: 'outline' })} href={`/users/${user.id}`}>အသေးစိတ်</Link><WorkflowDialog operation="user.update" id={user.id} title={`အသုံးပြုသူ #${user.id} ပြင်ရန်`} label="ပြင်ရန်" fields={userFields(roleOptions, user)} /><WorkflowDialog operation="user.delete" id={user.id} title={`အသုံးပြုသူ #${user.id} ရပ်ဆိုင်းရန်`} label="ရပ်ဆိုင်း" disabled={user.id === actor.id} destructive /></div></TableCell></TableRow>)}</TableBody></Table> : <Empty />}<Pager path="/users" pagination={result.pagination} /></Card></>;
}
