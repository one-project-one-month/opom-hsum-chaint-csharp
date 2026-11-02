import 'server-only';
import { createHash } from 'node:crypto';
import { EncryptJWT, jwtDecrypt, decodeJwt } from 'jose';
import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';
import { cache } from 'react';
import { api, ApiError } from './api';
import type { Login, User } from './types';
const cookieName = 'hsumchaint_admin';
type Session = { accessToken: string; userId: number; roleName: string };
function key() {
  const secret = process.env.SESSION_SECRET;
  if (!secret || secret.length < 32) throw new Error('SESSION_SECRET must contain at least 32 characters.');
  return createHash('sha256').update(secret).digest();
}
export async function saveSession(login: Login) {
  if (login.roleName !== 'Admin') throw new ApiError(403, 'System Admin အကောင့်ဖြင့်သာ ဝင်ရောက်နိုင်ပါသည်။');
  const exp = decodeJwt(login.accessToken).exp;
  if (!exp || exp <= Date.now() / 1000) throw new ApiError(401, 'အကောင့်ကို ပြန်လည်ဝင်ရောက်ပါ။');
  const encrypted = await new EncryptJWT({ accessToken: login.accessToken, userId: login.id, roleName: login.roleName })
    .setProtectedHeader({ alg: 'dir', enc: 'A256GCM' }).setIssuedAt().setExpirationTime(exp).encrypt(key());
  (await cookies()).set(cookieName, encrypted, { httpOnly: true, secure: process.env.NODE_ENV === 'production', sameSite: 'lax', path: '/', expires: new Date(exp * 1000) });
}
export async function readSession(): Promise<Session | null> {
  const value = (await cookies()).get(cookieName)?.value;
  if (!value) return null;
  try {
    const { payload } = await jwtDecrypt(value, key(), { keyManagementAlgorithms: ['dir'], contentEncryptionAlgorithms: ['A256GCM'] });
    if (payload.roleName !== 'Admin' || typeof payload.accessToken !== 'string' || typeof payload.userId !== 'number') return null;
    return { accessToken: payload.accessToken, userId: payload.userId, roleName: 'Admin' };
  } catch { return null; }
}
export async function clearSession() { (await cookies()).delete(cookieName); }
// Check current API identity on each request; encrypted claims alone cannot detect a revoked role.
export const requireAdmin = cache(async () => {
  const session = await readSession();
  if (!session) redirect('/login');
  try {
    const { data: user } = await api<User>(`/api/User/${session.userId}`, session.accessToken);
    if (user.roleName !== 'Admin') redirect('/login?reason=role');
    return { session, user };
  } catch (error) {
    if (error instanceof ApiError && [401, 403].includes(error.status)) redirect('/login?reason=expired');
    throw error;
  }
});
export async function adminApi<T>(path: string) {
  const { session } = await requireAdmin();
  try { return await api<T>(path, session.accessToken); }
  catch (error) {
    if (error instanceof ApiError && error.status === 401) redirect('/login?reason=expired');
    throw error;
  }
}
