import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from '@/components/ui/table';
import { DonationDetails } from './donation-details';
import { Empty, Status } from './page-parts';
import type { Donation } from '@/lib/types';
import { donationTypes } from '@/lib/types';
import { date, number } from '@/lib/utils';
import { DonationActions } from './donation-actions';
export function DonationsTable({ rows, actions = false, page = 1, pageSize = 10 }: { rows: Donation[]; actions?: boolean; page?: number; pageSize?: number }) {
  if (!rows.length) return <Empty />;
  return <Table><TableHeader><TableRow><TableHead className="number-column">No.</TableHead><TableHead>အလှူရှင်</TableHead><TableHead>ကျောင်းတိုက်</TableHead><TableHead>အလှူအမျိုးအစား</TableHead><TableHead>ပမာဏ</TableHead><TableHead>အခြေအနေ</TableHead><TableHead>မှတ်တမ်းရက်</TableHead><TableHead><span className="sr-only">အသေးစိတ်</span></TableHead>{actions && <TableHead>လုပ်ဆောင်ချက်များ</TableHead>}</TableRow></TableHeader><TableBody>{rows.map((row, index) => <TableRow key={row.id}><TableCell className="number-column numeric">{(page - 1) * pageSize + index + 1}</TableCell><TableCell><span className="cell-title">{row.donorName || 'အမည်မဖော်လိုသူ'}</span></TableCell><TableCell>{row.monasteryName || (row.monasterySpaceId ? `ကျောင်းတိုက် ${row.monasterySpaceId}` : '—')}</TableCell><TableCell>{row.customDonationType || donationTypes[row.donationType] || '—'}</TableCell><TableCell className="numeric">{row.amount != null ? `${number(row.amount)} ကျပ်` : row.quantity != null ? number(row.quantity) : '—'}</TableCell><TableCell><Status status={row.status} /></TableCell><TableCell className="whitespace-nowrap muted">{date(row.createdAt)}</TableCell><TableCell><DonationDetails donation={row} /></TableCell>{actions && <TableCell><DonationActions donation={row} /></TableCell>}</TableRow>)}</TableBody></Table>;
}
