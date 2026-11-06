import Link from 'next/link';
import { Search, SlidersHorizontal } from 'lucide-react';
import { adminApi } from '@/lib/session';
import { statuses, statusLabels, type Donation } from '@/lib/types';
import { pageNumber, scalar, validDate, type SearchParams } from '@/lib/params';
import { PageHead, Pager } from '@/components/page-parts';
import { DonationsTable } from '@/components/donations-table';
import { Card } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button, buttonVariants } from '@/components/ui/button';
import { DonationCreate } from '@/components/donation-create';
import { DateInput } from '@/components/ui/date-input';
export const metadata = { title: 'အလှူမှတ်တမ်းများ' };
export default async function Donations({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const raw = await searchParams;
  const page = pageNumber(raw.page), donor = (scalar(raw.donor) || scalar(raw.search)).slice(0, 100), monastery = scalar(raw.monastery).slice(0, 100);
  const status = statuses.includes(scalar(raw.status)) ? scalar(raw.status) : '';
  const from = validDate(scalar(raw.from)), to = validDate(scalar(raw.to));
  const invalidRange = !!(from && to && from > to);
  const query = new URLSearchParams({ PageNumber: String(page), PageSize: '10' });
  if (donor) query.set('Search', donor);
  if (monastery) query.set('Monastery', monastery);
  if (status) query.set('Status', status);
  // Date controls represent calendar days in Myanmar, including the entire final day.
  if (from && !invalidRange) query.set('FromDate', new Date(`${from}T00:00:00+06:30`).toISOString());
  if (to && !invalidRange) query.set('ToDate', new Date(`${to}T23:59:59.999+06:30`).toISOString());
  const result = await adminApi<Donation[]>(`/api/v1/admin/donations?${query}`);
  const rows = result.data, pagination = result.pagination;
  return <><PageHead title="အလှူမှတ်တမ်းများ" description="ကျောင်းတိုက်အားလုံး၏ အလှူမှတ်တမ်းနှင့် လက်ရှိအခြေအနေများ" action={<div className="head-actions"><DonationCreate /><a className={buttonVariants({ variant: 'outline', size: 'sm' })} href={`/donations/export?${query}`}>CSV ထုတ်ရန်</a></div>} /><Card className="data-panel"><div className="panel-heading"><div><h2>အလှူစာရင်း</h2><p>စနစ်တစ်ခုလုံးရှိ အလှူမှတ်တမ်းများ</p></div><SlidersHorizontal size={18} className="muted" /></div><form className="filter-toolbar" action="/donations"><div className="search-field"><Search size={16} /><Input aria-label="အလှူရှင်အမည် ရှာရန်" name="donor" defaultValue={donor} placeholder="အလှူရှင်အမည်…" maxLength={100} /></div><div className="search-field"><Search size={16} /><Input aria-label="ကျောင်းတိုက်အမည် ရှာရန်" name="monastery" defaultValue={monastery} placeholder="ကျောင်းတိုက်အမည်…" maxLength={100} /></div><select aria-label="အလှူအခြေအနေ" name="status" defaultValue={status} className="select"><option value="">အခြေအနေ အားလုံး</option>{statuses.map((s, i) => <option key={s} value={s}>{statusLabels[i]}</option>)}</select><div className="date-filter"><label htmlFor="from">မှ</label><DateInput id="from" name="from" value={from} /></div><div className="date-filter"><label htmlFor="to">အထိ</label><DateInput id="to" name="to" value={to} /></div><Button type="submit">ရှာရန်</Button><Link className={buttonVariants({ variant: 'outline', className: 'clear-filter' })} href="/donations">ရှင်းရန်</Link></form>{invalidRange && <p role="alert" className="error-message mx-5">အစရက်သည် အဆုံးရက်ထက် စောရပါမည်။ ရက်စွဲစစ်ထုတ်မှု မသုံးထားပါ။</p>}<DonationsTable rows={rows} actions page={page} /><Pager pagination={pagination} path="/donations" params={{ donor, monastery, status, from, to }} /></Card></>;
}
