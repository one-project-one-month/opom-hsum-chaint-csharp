'use client';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useState } from 'react';
import { LayoutDashboard, HandHeart, Church, Users, ShieldCheck, Menu, X, LogOut, ChevronRight, CalendarDays, MailPlus, Bell, KeyRound } from 'lucide-react';
import { logoutAction } from '@/app/actions';
import { Button } from '@/components/ui/button';
const items = [
  { href: '/dashboard', label: 'ခြုံငုံသုံးသပ်ချက်', english: 'Overview', icon: LayoutDashboard },
  { href: '/donations', label: 'အလှူမှတ်တမ်းများ', english: 'Donations', icon: HandHeart },
  { href: '/calendar', label: 'အလှူပြက္ခဒိန်', english: 'Calendar', icon: CalendarDays },
  { href: '/monasteries', label: 'ကျောင်းတိုက်များ', english: 'Monasteries', icon: Church },
  { href: '/users', label: 'အသုံးပြုသူများ', english: 'Users', icon: Users },
  { href: '/roles', label: 'အခန်းကဏ္ဍနှင့် ခွင့်ပြုချက်', english: 'Roles & permissions', icon: ShieldCheck },
  { href: '/space-roles', label: 'ကျောင်းတိုက်အခန်းကဏ္ဍ', english: 'Space roles', icon: KeyRound },
  { href: '/invitations', label: 'ဖိတ်ခေါ်မှုများ', english: 'Invitations', icon: MailPlus },
  { href: '/notifications', label: 'အသိပေးချက်များ', english: 'Notifications', icon: Bell },
];
export function AdminShell({ name, children }: { name: string; children: React.ReactNode }) {
  const pathname = usePathname();
  const [open, setOpen] = useState(false);
  const active = items.find(item => pathname.startsWith(item.href));
  return <div className="admin-app"><a className="skip-link" href="#main-content">အကြောင်းအရာသို့ သွားရန်</a>{open && <button aria-label="မီနူး ပိတ်ရန်" className="sidebar-overlay" onClick={() => setOpen(false)} />}
    <aside className={`sidebar ${open ? 'is-open' : ''}`}><div className="sidebar-brand"><Link href="/dashboard" className="brand" onClick={() => setOpen(false)}><span className="brand-mark"><HandHeart /></span><span>ဆွမ်းဂျိုင့်<small>ADMINISTRATION</small></span></Link><Button className="close-menu" variant="ghost" size="icon" aria-label="မီနူး ပိတ်ရန်" onClick={() => setOpen(false)}><X /></Button></div><p className="nav-label">WORKSPACE</p><nav aria-label="အဓိက မီနူး">{items.map(item => <Link key={item.href} href={item.href} onClick={() => setOpen(false)} aria-current={pathname.startsWith(item.href) ? 'page' : undefined} className={`nav-item ${pathname.startsWith(item.href) ? 'active' : ''}`}><item.icon size={19} /><span>{item.label}<small>{item.english}</small></span></Link>)}</nav><div className="sidebar-bottom"><div className="workspace-note"><ShieldCheck size={17} /><span>System administration<small>စနစ်တစ်ခုလုံး စီမံခန့်ခွဲမှု</small></span></div><div className="admin-profile"><span className="avatar">{name.slice(0, 1).toUpperCase()}</span><div><strong>{name}</strong><small>System Admin</small></div><form action={logoutAction}><Button variant="ghost" size="icon" aria-label="အကောင့်ထွက်ရန်" title="အကောင့်ထွက်ရန်"><LogOut size={18} /></Button></form></div></div></aside>
    <div className="main-area"><header className="topbar"><Button className="menu-toggle" variant="outline" size="icon" aria-label="မီနူး ဖွင့်ရန်" aria-expanded={open} onClick={() => setOpen(true)}><Menu /></Button><span className="breadcrumb">Admin <ChevronRight size={14} /><strong>{active?.english}</strong></span><span className="portal-tag"><span />System Admin</span></header><main id="main-content" className="content">{children}</main><footer className="app-footer">ဆွမ်းဂျိုင့် စီမံခန့်ခွဲမှုစနစ်<span>HsumChaint Admin</span></footer></div>
  </div>;
}
