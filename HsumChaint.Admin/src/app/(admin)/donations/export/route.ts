import { allRows } from '@/lib/options';
import type { Donation } from '@/lib/types';
import { donationTypes, statuses } from '@/lib/types';
import { ApiError } from '@/lib/api';
export async function GET(request: Request) {
  const incoming = new URL(request.url).searchParams;
  const query = new URLSearchParams();
  for (const key of ['Search', 'Status', 'FromDate', 'ToDate']) { const value = incoming.get(key); if (value) query.set(key, value); }
  try {
    const rows = await allRows<Donation>(`/api/v1/admin/donations?${query}`);
    const cell = (value: unknown) => { let text = String(value ?? ''); if (/^[=+\-@\t\r]/.test(text)) text = `'${text}`; return `"${text.replaceAll('"', '""')}"`; };
    const data = [['ID', 'Donor', 'Monastery', 'Type', 'Amount MMK', 'Quantity', 'Status', 'Created UTC', 'Pickup UTC', 'Dropoff UTC', 'Note'], ...rows.map(d => [d.id, d.donorName, d.monasteryName, d.customDonationType || donationTypes[d.donationType], d.amount, d.quantity, statuses[d.status], d.createdAt, d.pickupTime, d.dropoffTime, d.note])];
    return new Response('\uFEFF' + data.map(row => row.map(cell).join(',')).join('\r\n'), { headers: { 'Content-Type': 'text/csv; charset=utf-8', 'Content-Disposition': 'attachment; filename="HsumChaint-donations.csv"', 'Cache-Control': 'private, no-store' } });
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    return new Response('Export failed. Please retry after checking API access.', { status: error.status, headers: { 'Cache-Control': 'no-store' } });
  }
}
