import Link from 'next/link';
import { WorkflowDialog } from '@/components/workflow-dialog';
import { monasteryFields } from '@/lib/form-fields';
import { Search } from 'lucide-react';
import { adminApi } from '@/lib/session';
import type { Monastery } from '@/lib/types';
import { pageNumber, scalar, type SearchParams } from '@/lib/params';
import { PageHead, Pager, Empty } from '@/components/page-parts';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Table, TableHeader, TableBody, TableHead, TableRow, TableCell } from '@/components/ui/table';
export const metadata = { title: 'ကျောင်းတိုက်များ' };
export default async function Monasteries({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const raw = await searchParams, page = pageNumber(raw.page), search = scalar(raw.search).slice(0, 100);
  const query = new URLSearchParams({ PageNumber: String(page), PageSize: '10', ...(search ? { Keyword: search } : {}) });
  const result = await adminApi<Monastery[]>(`/api/v1/monasteries${search ? '/search' : ''}?${query}`);
  return <><PageHead title="ကျောင်းတိုက်များ" description="ဆွမ်းဂျိုင့်တွင် မှတ်ပုံတင်ထားသော ကျောင်းတိုက်များ" action={<WorkflowDialog operation="monastery.create" title="ကျောင်းတိုက်အသစ် ဖန်တီးရန်" description="ဖန်တီးသူ Admin ကို Owner အဖြစ် သတ်မှတ်ပါမည်။" fields={monasteryFields} />} /><Card className="data-panel"><form className="filter-toolbar" action="/monasteries"><div className="search-field"><Search size={16} /><Input aria-label="ကျောင်းတိုက် ရှာရန်" name="search" defaultValue={search} placeholder="ကျောင်းတိုက် ရှာရန်…" maxLength={100} /></div><Button>ရှာရန်</Button></form>{result.data.length ? <Table><TableHeader><TableRow><TableHead>ကျောင်းတိုက်</TableHead><TableHead>လိပ်စာ</TableHead><TableHead>ဖော်ပြချက်</TableHead></TableRow></TableHeader><TableBody>{result.data.map(m => <TableRow key={m.id}><TableCell><Link className="cell-title text-brand" href={`/monasteries/${m.id}`}>{m.monasteryName || '—'}</Link><span className="cell-sub">#{m.id}</span></TableCell><TableCell className="max-w-80 whitespace-normal">{m.address || '—'}</TableCell><TableCell className="max-w-96 whitespace-normal muted">{m.description || '—'}</TableCell></TableRow>)}</TableBody></Table> : <Empty />}<Pager path="/monasteries" pagination={result.pagination} params={{ search }} /></Card></>;
}
