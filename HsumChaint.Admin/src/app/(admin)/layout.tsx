import { AdminShell } from '@/components/admin-shell';
import { requireAdmin } from '@/lib/session';
export const dynamic = 'force-dynamic';
export default async function AdminLayout({ children }: { children: React.ReactNode }) {
  const { user } = await requireAdmin();
  return <AdminShell name={user.name || 'Admin'}>{children}</AdminShell>;
}
