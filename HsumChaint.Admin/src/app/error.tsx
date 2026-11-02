'use client';
import { Button } from '@/components/ui/button';
export default function ErrorPage({ reset }: { reset: () => void }) {
  return <main className="full-error"><h1>စနစ်ကို ချိတ်ဆက်၍ မရသေးပါ</h1><p>C# API လည်ပတ်မှုနှင့် server configuration ကို စစ်ဆေးပါ။</p><Button onClick={reset}>ပြန်လည်ကြိုးစားရန်</Button><a href="/login">အကောင့်ဝင်ရန်</a></main>;
}
