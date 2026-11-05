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
  const total = Math.max(1, pagination.totalPages), current = Math.min(pagination.pageNumber, total);
  const pages = [...new Set([1, current - 1, current, current + 1, total].filter(page => page > 0 && page <= total))].sort((a, b) => a - b);
  return <nav className="pagination" aria-label="စာမျက်နှာများ"><span>စုစုပေါင်း {pagination.totalCount.toLocaleString()} ခု · စာမျက်နှာ {current} / {total}</span><div><Link aria-disabled={!pagination.hasPreviousPage} tabIndex={pagination.hasPreviousPage ? undefined : -1} aria-label="ယခင်စာမျက်နှာ" className={buttonVariants({ variant: 'outline', size: 'icon', className: !pagination.hasPreviousPage ? 'pager-disabled' : '' })} href={pagination.hasPreviousPage ? href(current - 1) : '#'}><ChevronLeft /></Link>{pages.map((page, index) => <span className="pager-slot" key={page}>{index > 0 && page - pages[index - 1] > 1 && <span className="pager-ellipsis">…</span>}<Link aria-current={page === current ? 'page' : undefined} className={buttonVariants({ variant: page === current ? 'default' : 'outline', size: 'icon' })} href={href(page)}>{page}</Link></span>)}<Link aria-disabled={!pagination.hasNextPage} tabIndex={pagination.hasNextPage ? undefined : -1} aria-label="နောက်စာမျက်နှာ" className={buttonVariants({ variant: 'outline', size: 'icon', className: !pagination.hasNextPage ? 'pager-disabled' : '' })} href={pagination.hasNextPage ? href(current + 1) : '#'}><ChevronRight /></Link></div></nav>;
}
