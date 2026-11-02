import Link from 'next/link';
import { HandHeart, Clock3, Church, Users, ArrowUpRight, CheckCheck, ArrowRight } from 'lucide-react';
import { adminApi } from '@/lib/session';
import type { Summary, Donation } from '@/lib/types';
import { number } from '@/lib/utils';
import { PageHead } from '@/components/page-parts';
import { DonationsTable } from '@/components/donations-table';
import { Card, CardContent } from '@/components/ui/card';
import { buttonVariants } from '@/components/ui/button';
export const metadata = { title: 'ခြုံငုံသုံးသပ်ချက်' };
export default async function Dashboard() {
  const [summary, recent] = await Promise.all([adminApi<Summary>('/api/v1/admin/summary'), adminApi<Donation[]>('/api/v1/admin/donations?PageSize=6')]);
  const data = summary.data;
  const metrics = [
    { label: 'အလှူ စုစုပေါင်း', value: data.donations, icon: HandHeart, href: '/donations', caption: 'မှတ်တမ်းအားလုံး', color: 'orange' },
    { label: 'စိစစ်ရန် စောင့်ဆိုင်းဆဲ', value: data.pendingReview, icon: Clock3, href: '/donations?status=PendingReview', caption: 'Pending review', color: 'amber' },
    { label: 'ကျောင်းတိုက်များ', value: data.monasteries, icon: Church, href: '/monasteries', caption: 'လက်ရှိကျောင်းတိုက်များ', color: 'green' },
    { label: 'အသုံးပြုသူများ', value: data.users, icon: Users, href: '/users', caption: 'လက်ရှိအကောင့်များ', color: 'blue' },
  ];
  const completedPercent = data.donations ? Math.round(data.completed / data.donations * 100) : 0;
  return <><PageHead title="ခြုံငုံသုံးသပ်ချက်" description="ဆွမ်းဂျိုင့်စနစ်၏ လက်ရှိအခြေအနေကို တစ်နေရာတည်းမှ ကြည့်ရှုပါ။" action={<Link className={buttonVariants({ variant: 'outline' })} href="/donations">အလှူစာရင်း ကြည့်ရန်<ArrowUpRight /></Link>} /><div className="welcome-strip"><div><span className="eyebrow">SYSTEM OVERVIEW</span><h2>အလှူတိုင်းကို အလေးထား စီမံခြင်း</h2><p>ကျောင်းတိုက်များနှင့် အလှူရှင်များအကြား ချိတ်ဆက်ပေးသော ဆွမ်းဂျိုင့်</p></div><HandHeart size={70} strokeWidth={1} aria-hidden="true" /></div><div className="metrics">{metrics.map(metric => <Link href={metric.href} key={metric.href}><Card className="metric-card"><CardContent><div className="metric-top"><span className={`metric-icon ${metric.color}`}><metric.icon size={21} /></span><ArrowUpRight size={16} className="muted" /></div><p>{metric.label}</p><strong>{number(metric.value)}</strong><small>{metric.caption}</small></CardContent></Card></Link>)}</div><div className="overview-row"><Card className="progress-panel"><div className="progress-icon"><CheckCheck size={22} /></div><div><h2>ပြီးစီးသည့် အလှူများ</h2><p><strong>{number(data.completed)}</strong> / {number(data.donations)} မှတ်တမ်း</p></div><div className="progress-summary"><strong>{completedPercent}%</strong><div className="progress-track"><span style={{ width: `${completedPercent}%` }} /></div></div></Card><Link href="/roles" className="permission-link"><ShieldLabel /><ArrowRight size={18} /></Link></div><Card className="data-panel"><div className="panel-heading"><div><h2>နောက်ဆုံး အလှူမှတ်တမ်းများ</h2><p>လတ်တလော မှတ်တမ်းတင်ထားသော အလှူများ</p></div><Link href="/donations" className="text-link">အားလုံး ကြည့်ရန် <ArrowRight size={15} /></Link></div><DonationsTable rows={recent.data} /></Card></>;
}
function ShieldLabel() { return <span><strong>အခန်းကဏ္ဍနှင့် ခွင့်ပြုချက်</strong><small>System roles ကို ကြည့်ရှုရန်</small></span>; }
