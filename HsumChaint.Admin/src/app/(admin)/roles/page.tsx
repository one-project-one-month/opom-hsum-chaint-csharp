import { RolesPage } from '@/components/roles-page';
import type { SearchParams } from '@/lib/params';
export const metadata = { title: 'အခန်းကဏ္ဍနှင့် ခွင့်ပြုချက်' };
export default async function Roles({ searchParams }: { searchParams: Promise<SearchParams> }) { return <RolesPage params={await searchParams} />; }
