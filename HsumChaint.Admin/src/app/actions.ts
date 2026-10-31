'use server';
import { redirect } from 'next/navigation';
import { api, ApiError } from '@/lib/api';
import { saveSession, clearSession } from '@/lib/session';
import type { Login } from '@/lib/types';
export type LoginState = { error: string; phoneNumber?: string };
export async function loginAction(_previous: LoginState, form: FormData): Promise<LoginState> {
  const phoneNumber = String(form.get('phoneNumber') ?? '').trim();
  const password = String(form.get('password') ?? '');
  if (!phoneNumber || !password || phoneNumber.length > 30 || password.length > 256) return { error: 'ဖုန်းနံပါတ်နှင့် စကားဝှက် မှန်ကန်စွာ ဖြည့်ပါ။', phoneNumber: phoneNumber.slice(0, 30) };
  try {
    const result = await api<Login>('/api/v1/Auth/login', undefined, { phoneNumber, password });
    await saveSession(result.data);
  } catch (error) {
    return { error: error instanceof ApiError ? error.message : 'ဝင်ရောက်၍မရပါ။ Server configuration ကို စစ်ဆေးပါ။', phoneNumber };
  }
  redirect('/dashboard');
}
export async function logoutAction() { await clearSession(); redirect('/login'); }
