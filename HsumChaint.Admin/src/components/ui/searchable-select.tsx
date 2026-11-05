'use client';
import { useEffect, useId, useRef, useState } from 'react';
import { Check, ChevronDown, Search, X } from 'lucide-react';
import type { Option } from '@/lib/workflows';

export function SearchableSelect({ name, options, value = '', required, label }: { name: string; options: Option[]; value?: string | number; required?: boolean; label: string }) {
  const initial = String(value);
  const [selected, setSelected] = useState(initial);
  const [query, setQuery] = useState('');
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(0);
  const root = useRef<HTMLDivElement>(null);
  const listId = useId();
  const filtered = options.filter(option => option.label.toLocaleLowerCase().includes(query.trim().toLocaleLowerCase()));
  const current = options.find(option => option.value === selected);
  useEffect(() => {
    const close = (event: MouseEvent) => { if (!root.current?.contains(event.target as Node)) setOpen(false); };
    document.addEventListener('mousedown', close); return () => document.removeEventListener('mousedown', close);
  }, []);
  const choose = (option: Option) => { setSelected(option.value); setQuery(''); setOpen(false); };
  return <div className="combobox" ref={root}>
    <input type="hidden" name={name} value={selected} required={required} />
    <button type="button" className="combobox-trigger" aria-expanded={open} aria-controls={listId} onClick={() => setOpen(!open)}><span className={current ? '' : 'muted'}>{current?.label || 'ရွေးချယ်ပါ'}</span><ChevronDown size={16} /></button>
    {open && <div className="combobox-popover"><div className="combobox-search"><Search size={15} /><input autoFocus value={query} aria-label={`${label} ရှာရန်`} placeholder={`${label} ရှာရန်…`} onChange={event => { setQuery(event.target.value); setActive(0); }} onKeyDown={event => {
      if (event.key === 'ArrowDown') { event.preventDefault(); setActive(index => Math.min(index + 1, filtered.length - 1)); }
      if (event.key === 'ArrowUp') { event.preventDefault(); setActive(index => Math.max(index - 1, 0)); }
      if (event.key === 'Enter' && filtered[active]) { event.preventDefault(); choose(filtered[active]); }
      if (event.key === 'Escape') setOpen(false);
    }} />{query && <button type="button" aria-label="ရှာဖွေစာ ရှင်းရန်" onClick={() => setQuery('')}><X size={14} /></button>}</div>
      <div id={listId} role="listbox" className="combobox-options">{filtered.length ? filtered.map((option, index) => <button type="button" role="option" aria-selected={option.value === selected} className={index === active ? 'is-active' : ''} key={option.value} onMouseEnter={() => setActive(index)} onClick={() => choose(option)}><span>{option.label}</span>{option.value === selected && <Check size={15} />}</button>) : <p>ကိုက်ညီသော ရလဒ် မရှိပါ။</p>}</div>
    </div>}
  </div>;
}
