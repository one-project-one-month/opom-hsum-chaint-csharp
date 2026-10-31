import { RolesPage } from '@/components/roles-page';
import type { SearchParams } from '@/lib/params';
export const metadata = { title: 'ကျောင်းတိုက်အခန်းကဏ္ဍများ' };
export default async function SpaceRoles({ searchParams }: { searchParams: Promise<SearchParams> }) { return <RolesPage params={await searchParams} space />; }
