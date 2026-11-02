export type SearchParams = Record<string, string | string[] | undefined>;
export function scalar(value: string | string[] | undefined) { return typeof value === 'string' ? value : ''; }
export function pageNumber(value: string | string[] | undefined) { const n = Number(scalar(value)); return Number.isInteger(n) && n > 0 && n <= 1000000 ? n : 1; }
export function validDate(value: string) { return /^\d{4}-\d{2}-\d{2}$/.test(value) && !Number.isNaN(Date.parse(value)) ? value : ''; }
