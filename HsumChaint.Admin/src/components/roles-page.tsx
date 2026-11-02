import { ShieldCheck, LockKeyhole } from 'lucide-react';
import { adminApi } from '@/lib/session';
import { allRows, options } from '@/lib/options';
import type { SpaceRole } from '@/lib/types';
import { PageHead, Empty, Pager } from './page-parts';
import { WorkflowDialog } from './workflow-dialog';
import { pageNumber, type SearchParams } from '@/lib/params';
import type { Field } from '@/lib/workflows';
import { Card, CardHeader, CardTitle, CardContent } from './ui/card';
import { Badge } from './ui/badge';
export async function RolesPage({ params, space = false }: { params: SearchParams; space?: boolean }) {
  const page = pageNumber(params.page), path = space ? '/space-roles' : '/roles', operation = space ? 'spaceRole' : 'role';
  const [result, permissions] = await Promise.all([adminApi<SpaceRole[]>(`/api/v1${path}?PageNumber=${page}&PageSize=12`), allRows<{ id: number; name: string }>(space ? '/api/v1/space-permissions' : '/api/v1/permissions')]);
  const fields = (role?: SpaceRole): Field[] => [{ name: 'name', label: 'အခန်းကဏ္ဍအမည်', required: true, value: role?.name }, ...(space ? [{ name: 'description', label: 'ဖော်ပြချက်', type: 'textarea' as const, value: role?.description || '' }] : []), { name: 'permissionIds', label: 'ခွင့်ပြုချက်များ', type: 'permissions', options: options(permissions, p => p.name), selected: role?.permissions.map(p => p.id) }];
  return <><PageHead title={space ? 'ကျောင်းတိုက်အခန်းကဏ္ဍများ' : 'အခန်းကဏ္ဍနှင့် ခွင့်ပြုချက်'} description="အခန်းကဏ္ဍတစ်ခုချင်းစီ၏ ခွင့်ပြုချက်များကို စီမံရန်" action={<WorkflowDialog operation={`${operation}.create`} title="အခန်းကဏ္ဍအသစ် ဖန်တီးရန်" fields={fields()} />} /><div className="notice"><LockKeyhole size={17} />{space ? 'Owner အခန်းကဏ္ဍကို အားလျော့စေခြင်းနှင့် ဖျက်ခြင်း မပြုလုပ်နိုင်ပါ။' : 'Admin အခန်းကဏ္ဍကို အားလျော့စေခြင်းနှင့် ဖျက်ခြင်း မပြုလုပ်နိုင်ပါ။'}</div>{result.data.length ? <div className="role-grid">{result.data.map(role => {
    const protectedRole = space ? role.name === 'Owner' : role.id === 1 || role.name === 'Admin';
    return <Card key={role.id} className="role-card"><CardHeader><span className="role-icon"><ShieldCheck size={22} /></span><CardTitle>{role.name}</CardTitle><p className="muted">{role.description || `ခွင့်ပြုချက် ${role.permissions.length} ခု`}</p></CardHeader><CardContent><div className="permission-chips">{role.permissions.map(permission => <Badge key={permission.id} variant="secondary">{permission.name}</Badge>)}</div><div className="row-actions mt-5"><WorkflowDialog operation={`${operation}.update`} id={role.id} title={`အခန်းကဏ္ဍ ${role.name} ပြင်ရန်`} label="ပြင်ရန်" fields={fields(role)} disabled={protectedRole} /><WorkflowDialog operation={`${operation}.delete`} id={role.id} title={`အခန်းကဏ္ဍ ${role.name} ဖျက်ရန်`} label="ဖျက်ရန်" destructive disabled={protectedRole} /></div></CardContent></Card>;
  })}</div> : <Empty />}<Pager path={path} pagination={result.pagination} /></>;
}
