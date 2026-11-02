import 'server-only';
import type { Envelope } from './types';
export class ApiError extends Error {
  constructor(public status: number, message: string) { super(message); }
}
export async function api<T>(path: string, token?: string, body?: unknown, method?: string): Promise<Envelope<T>> {
  const base = process.env.API_BASE_URL;
  if (!base) throw new ApiError(503, 'API_BASE_URL မသတ်မှတ်ရသေးပါ။');
  let response: Response;
  try {
    response = await fetch(`${base.replace(/\/$/, '')}${path}`, {
      method: method ?? (body === undefined ? 'GET' : 'POST'), cache: 'no-store',
      headers: { Accept: 'application/json', ...(body === undefined ? {} : { 'Content-Type': 'application/json' }), ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      ...(body === undefined ? {} : { body: JSON.stringify(body) }), signal: AbortSignal.timeout(12000),
      redirect: 'error',
    });
  } catch { throw new ApiError(503, 'C# API ကို ချိတ်ဆက်၍မရပါ။ ခဏကြာမှ ပြန်ကြိုးစားပါ။'); }
  if (response.status === 401) throw new ApiError(401, 'ဝင်ရောက်ချိန် သက်တမ်းကုန်ပါပြီ။ ပြန်လည်ဝင်ရောက်ပါ။');
  if (response.status === 403) throw new ApiError(403, 'ဤလုပ်ဆောင်ချက်အတွက် ခွင့်ပြုချက်မရှိပါ။');
  const result = await response.json().catch(() => null) as Envelope<T> | null;
  if (!response.ok || !result || result.isSuccess !== true) {
    const login = path.toLowerCase().includes('/auth/login');
    const message = result?.message;
    const safe = typeof message === 'string' && message.length < 400 && !/exception|application layer|sql|connection|stack|innerexception/i.test(message);
    throw new ApiError(response.ok ? 400 : response.status, login ? 'ဖုန်းနံပါတ် သို့မဟုတ် စကားဝှက်ကို စစ်ဆေးပါ။' : response.status < 500 && safe ? message : 'လုပ်ဆောင်မှု မအောင်မြင်ပါ။ ပြန်လည်ကြိုးစားပါ။');
  }
  return result;
}
