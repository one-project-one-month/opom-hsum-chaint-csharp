import Link from 'next/link';
import { allRows } from '@/lib/options';
import { scalar, type SearchParams } from '@/lib/params';
import type { Donation } from '@/lib/types';
import { PageHead, Empty } from '@/components/page-parts';
import { DonationDetails } from '@/components/donation-details';
import { Card } from '@/components/ui/card';
import { Button, buttonVariants } from '@/components/ui/button';
export const metadata = { title: 'အလှူပြက္ခဒိန်' };
export default async function Calendar({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const raw = scalar((await searchParams).month);
  // This dynamic Server Component resolves the current month once per request.
  // eslint-disable-next-line react-hooks/purity
  const now = new Date(Date.now() + 390 * 60000).toISOString().slice(0, 7);
  const month = /^(20\d{2}|2100)-(0[1-9]|1[0-2])$/.test(raw) ? raw : now;
  const donations = await allRows<Donation>(`/api/v1/admin/donations?CalendarMonth=${month}`);
  const first = new Date(`${month}-01T00:00:00Z`), days = new Date(Date.UTC(first.getUTCFullYear(), first.getUTCMonth() + 1, 0)).getUTCDate();
  const adjacent = (offset: number) => new Date(Date.UTC(first.getUTCFullYear(), first.getUTCMonth() + offset, 1)).toISOString().slice(0, 7);
  const dayOf = (d: Donation) => { const value = d.pickupTime || d.dropoffTime || d.createdAt; if (!value) return ''; const normalized = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : `${value}Z`; return new Date(new Date(normalized).getTime() + 390 * 60000).toISOString().slice(0, 10); };
  return <><PageHead title="အလှူပြက္ခဒိန်" description="လာယူရက်၊ မရှိလျှင် ပို့ဆောင်ရက်၊ နှစ်ခုလုံးမရှိလျှင် မှတ်တမ်းရက်အလိုက် ပြထားသည်။ မြန်မာစံတော်ချိန်ကို အသုံးပြုထားသည်။" /><Card className="data-panel"><div className="panel-heading"><Link className={buttonVariants({ variant: 'outline', size: 'sm' })} href={`/calendar?month=${adjacent(-1)}`}>ယခင်လ</Link><form action="/calendar" className="head-actions"><input aria-label="လ ရွေးချယ်ရန်" className="select" type="month" name="month" defaultValue={month} min="2000-01" max="2100-12" /><Button size="sm">ကြည့်ရန်</Button></form><Link className={buttonVariants({ variant: 'outline', size: 'sm' })} href={`/calendar?month=${adjacent(1)}`}>နောက်လ</Link></div><div className="calendar-scroll"><div className="calendar-grid">{['တနင်္ဂနွေ', 'တနင်္လာ', 'အင်္ဂါ', 'ဗုဒ္ဓဟူး', 'ကြာသပတေး', 'သောကြာ', 'စနေ'].map(day => <div className="calendar-weekday" key={day}>{day}</div>)}{Array.from({ length: first.getUTCDay() }, (_, i) => <div className="calendar-day muted-day" key={`empty${i}`} />)}{Array.from({ length: days }, (_, i) => {
    const key = `${month}-${String(i + 1).padStart(2, '0')}`, rows = donations.filter(d => dayOf(d) === key);
    return <div className="calendar-day" key={key}><strong>{i + 1}</strong>{rows.map(d => <div className={`calendar-event status-${d.status}`} key={d.id}><span>{d.donorName || `#${d.id}`}<small>{d.monasteryName}</small></span><DonationDetails donation={d} /></div>)}</div>;
  })}</div></div>{!donations.length && <Empty />}</Card></>;
}
