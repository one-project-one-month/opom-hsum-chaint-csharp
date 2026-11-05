'use client';
import { useState } from 'react';

function display(value: string) {
  const match = value.match(/^(\d{4})-(\d{2})-(\d{2})(.*)$/);
  return match ? `${match[3]}/${match[2]}/${match[1]}${(match[4] || '').replace(/^T/, ' ')}` : value;
}
function iso(value: string, withTime: boolean) {
  const pattern = withTime ? /^(\d{2})\/(\d{2})\/(\d{4})[ T](\d{2}):(\d{2})$/ : /^(\d{2})\/(\d{2})\/(\d{4})$/;
  const match = value.trim().match(pattern); if (!match) return '';
  const result = `${match[3]}-${match[2]}-${match[1]}` + (withTime ? `T${match[4]}:${match[5]}` : '');
  const date = new Date(`${result}${withTime ? ':00+06:30' : 'T00:00:00Z'}`);
  return Number.isNaN(date.getTime()) ? '' : result;
}
export function DateInput({ name, value = '', required, withTime = false, id }: { name: string; value?: string | number; required?: boolean; withTime?: boolean; id?: string }) {
  const [shown, setShown] = useState(display(String(value)));
  const parsed = iso(shown, withTime);
  return <><input type="hidden" name={name} value={parsed} /><input id={id} className="date-text-input" inputMode="numeric" value={shown} required={required} pattern={withTime ? '\\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2}' : '\\d{2}/\\d{2}/\\d{4}'} placeholder={withTime ? 'DD/MM/YYYY HH:mm' : 'DD/MM/YYYY'} aria-label={withTime ? 'DD/MM/YYYY HH:mm' : 'DD/MM/YYYY'} onChange={event => setShown(event.target.value)} /></>;
}
