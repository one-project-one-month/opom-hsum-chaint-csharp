import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';
export function cn(...inputs: ClassValue[]) { return twMerge(clsx(inputs)); }
export function date(value?: string | null) {
  if (!value) return '—';
  // C# stores UTC DateTime; unspecified JSON timestamps must also be treated as UTC.
  const utc = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : `${value}Z`;
  return new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'Asia/Yangon' }).format(new Date(utc));
}
export function number(value: number) { return new Intl.NumberFormat('en-US').format(value); }
