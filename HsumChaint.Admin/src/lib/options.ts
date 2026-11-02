import 'server-only';
import { adminApi } from './session';
import type { Option } from './workflows';
export async function allRows<T>(path: string): Promise<T[]> {
  const rows: T[] = [];
  for (let page = 1; page <= 100; page++) {
    const result = await adminApi<T[]>(`${path}${path.includes('?') ? '&' : '?'}PageNumber=${page}&PageSize=100`);
    rows.push(...result.data);
    if (!result.pagination?.hasNextPage) return rows;
  }
  throw new Error('The lookup exceeds 10,000 records. Narrow the data before loading.');
}
export function options<T extends { id: number }>(rows: T[], label: (row: T) => string): Option[] { return rows.map(row => ({ value: String(row.id), label: label(row) })); }
