'use client';
import { Eye } from 'lucide-react';
import { Dialog, DialogTrigger, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Status } from './page-parts';
import { donationTypes, type Donation } from '@/lib/types';
import { date, number } from '@/lib/utils';
export function DonationDetails({ donation: d }: { donation: Donation }) {
  return <Dialog><DialogTrigger asChild><Button size="icon" variant="ghost" aria-label={`အလှူ #${d.id} အသေးစိတ်`}><Eye size={17} /></Button></DialogTrigger><DialogContent><DialogHeader><DialogTitle>အလှူမှတ်တမ်း #{d.id}</DialogTitle><DialogDescription>{d.donorName || 'အမည်မဖော်လိုသူ'} · {date(d.createdAt)}</DialogDescription></DialogHeader><Status status={d.status} /><dl className="details-grid"><div><dt>ကျောင်းတိုက်</dt><dd>{d.monasteryName || '—'}</dd></div><div><dt>အလှူအမျိုးအစား</dt><dd>{d.customDonationType || donationTypes[d.donationType]}</dd></div><div><dt>ငွေပမာဏ</dt><dd>{d.amount == null ? '—' : `${number(d.amount)} ကျပ်`}</dd></div><div><dt>အရေအတွက်</dt><dd>{d.quantity == null ? '—' : number(d.quantity)}</dd></div><div><dt>လာယူမည့်ရက်</dt><dd>{date(d.pickupTime)}</dd></div><div><dt>ပို့ဆောင်မည့်ရက်</dt><dd>{date(d.dropoffTime)}</dd></div><div className="col-span-2"><dt>မှတ်ချက်</dt><dd className="whitespace-pre-wrap">{d.note || 'မှတ်ချက်မရှိပါ။'}</dd></div></dl></DialogContent></Dialog>;
}
