'use client';
import { Button } from '@/components/ui/button';
import { Unplug } from 'lucide-react';
export default function ErrorPage({ reset }: { reset: () => void }) {
  return <div className="empty-state"><Unplug size={34} /><h2>စာရင်းရယူ၍ မရသေးပါ</h2><p>C# API ချိတ်ဆက်မှုနှင့် အကောင့်ခွင့်ပြုချက်ကို စစ်ဆေးပြီး ပြန်လည်ကြိုးစားပါ။</p><Button onClick={reset}>ပြန်လည်ကြိုးစားရန်</Button></div>;
}
