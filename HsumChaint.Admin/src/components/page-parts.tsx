import Link from 'next/link';
import { Inbox, ChevronLeft, ChevronRight } from 'lucide-react';
import { buttonVariants } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { statusLabels } from '@/lib/types';
import type { Pagination } from '@/lib/types';
export function PageHead({ title, description, action }: { title: string; description: string; action?: React.ReactNode }) {
  return <div className="page-head"><div><p className="eyebrow text-brand">HSUM CHAINT / ADMIN</p><h1>{title}</h1><p className="muted">{description}</p></div>{action}</div>;
}
export function Empty() { return <div className="empty-state"><Inbox size={32} /><h3>မှတ်တမ်း မတွေ့ရှိပါ</h3><p>စစ်ထုတ်ထားသည့် အခြေအနေများကို ပြောင်းပြီး ထပ်မံရှာဖွေပါ။</p></div>; }
export function Status({ status }: { status: number }) { return <Badge variant="outline" className={`status-badge status-${status}`}><span />{statusLabels[status] ?? 'မသိရသေး'}</Badge>; }
export function Pager({ pagination, path, params = {} }: { pagination?: Pagination; path: string; params?: Record<string, string> }) {
  if (!pagination) return null;
  const href = (page: number) => `${path}?${new URLSearchParams({ ...params, page: String(page) })}`;
  return <div className="pagination"><span>စုစုပေါင်း {pagination.totalCount.toLocaleString()} ခု · စာမျက်နှာ {pagination.pageNumber} / {Math.max(1, pagination.totalPages)}</span><div>{pagination.hasPreviousPage ? <Link aria-label="ယခင်စာမျက်နှာ" className={buttonVariants({ variant: 'outline', size: 'icon' })} href={href(pagination.pageNumber - 1)}><ChevronLeft /></Link> : null}{pagination.hasNextPage ? <Link aria-label="နောက်စာမျက်နှာ" className={buttonVariants({ variant: 'outline', size: 'icon' })} href={href(pagination.pageNumber + 1)}><ChevronRight /></Link> : null}</div></div>;
}
