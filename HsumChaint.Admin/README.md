# HsumChaint Admin

Next.js App Router + TypeScript system admin portal. Dark green/orange branding follows the supplied HTML reference; actual shadcn-admin UI components are adapted for dynamic SSR and the existing C# API.

## Run

Requires Node.js 22+, the existing C# API configuration, and MySQL. From the backend repository root:

```powershell
dotnet run --project HsumChaint.API --launch-profile http
```

From this directory:

```powershell
npm ci
Copy-Item .env.example .env.local
node -e "console.log(require('crypto').randomBytes(32).toString('hex'))"
```

Set `SESSION_SECRET` in `.env.local` to the generated value. Set `API_BASE_URL` to the reachable C# API origin (default `http://localhost:5174`). `.env.local` is ignored. Neither variable may be exposed through `NEXT_PUBLIC_*`. Keep a consistent, strong session secret across server instances; changing it invalidates sessions.

```powershell
npm run dev
```

Open http://127.0.0.1:3000 and log in with an existing **Admin** phone number/password. The portal does not seed accounts. For production use `npm run build` then `npm start` behind HTTPS, with a trusted HTTPS API origin. Static export is unsupported.

## Connected workflows

| Area | Workflows |
| --- | --- |
| Dashboard | Real global counts, recent donations, completion count, navigation |
| Donations | Create manual donation with account or walk-in donor; batch up to 50 entries; monthly recurring series up to 24 months; accept/reject with note; pickup/dropoff schedule; complete; cancel; delete; detail dialog |
| Donation search | Donor-name/status/date filters, URL-based pagination, CSV export of filtered data |
| Calendar | Month navigation and picker; global records by pickup, otherwise dropoff, otherwise creation day; detail dialog |
| Users | Create with role; edit contact details/system role; deactivate; view details; edit pickup/dropoff and reminder settings; Monk profile edit/delete |
| Monasteries | Search/pagination; create; edit; member list; owner-only deactivation |
| Members | Invite existing user; update space role; remove non-owner member |
| Invitations | Global list; create; revoke pending invitation; accept/reject only invitations addressed to the current user |
| System roles | Create/edit/delete with permission picker; protected Admin role; assigned roles cannot be deleted |
| Space roles | Create/edit/delete with permission picker; protected Owner role; assigned/pending-invitation roles cannot be deleted |
| Notifications | Database announcement for a selected user; global list; mark one/all read; delete one/clear all with confirmation |

All mutations go through authenticated Server Actions with a fixed operation dispatcher and server-side field extraction. Browser components never receive access tokens or call C# directly. Errors preserve entered dialog fields, pending state disables repeated submissions, and successful writes revalidate SSR data. Destructive operations require confirmation in the UI and Server Action.

Admin announcements create **database notifications only** and do not promise push delivery. Donation/invitation services retain their existing notification behavior.

## Rules and API behavior

- Admin session cookies are encrypted, HttpOnly, SameSite=Lax, and Secure in production. Their expiry follows the API access token, currently one day; automatic refresh is not implemented.
- Each SSR request/Server Action checks the current API user role. Admin-specific C# endpoints also check the active database user and role, preventing stale Admin tokens from accessing them after revocation.
- System Admins can manage donations across monasteries without joining as a member. Ordinary donor/member scopes are retained. Admin donor-history and calendar reads also support global scope.
- Completed donations cannot be cancelled/deleted. Other transition rules remain enforced by the C# donation service.
- Creating a monastery assigns the creator as Owner. A System Admin can edit/manage members globally, but may deactivate a monastery only when they are its Owner. Owner membership cannot be removed or changed through member management.
- An Admin cannot deactivate themself or change their own role in the portal. Active monastery owners cannot be deactivated before ownership is resolved. Last-Admin demotion is rejected.
- Invitation responses belong to the invited user; Admins may revoke a pending invitation without impersonating its recipient.
- Admin/user creation is protected by `/api/v1/admin/users`. Public registration now accepts only active public User/Monk/Donor roles, rejecting Admin/custom privileged roles.
- Dates/times in forms and calendar use Myanmar time (UTC+06:30). The recurring donation DTO adds optional `TimeZoneOffsetMinutes`, default zero for existing clients; the portal passes 390. No database schema migration is required.
- Lookup selectors, calendar loads, and CSV export are bounded at 10,000 records and fail explicitly above this size rather than silently truncating. Large deployments should move to asynchronous lookup/export endpoints.
- CSV exports escape quotes and protect spreadsheet cells from formula injection.

## Verification

```powershell
npm run lint
npm run typecheck
npm run build
npx playwright install chromium
npm test
```

Browser tests launch the production build on port 3197 and an isolated fixture API. They cover session security, role denial, SSR HTML, token confinement, form errors/retention, create/batch/recurring donations, review/schedule/complete, roles, user creation/edit, monastery creation/member editing, notification creation/deletion, destructive confirmation, calendar/invitation/profile routes, CSV export, mobile navigation/overflow, logout, tampered cookies, and API-failure UI. Screenshots are written to ignored `test-results/`. A prior build is required; fixtures are never available in the real app.

From the backend root:

```powershell
dotnet test HsumChaint.Tests/HsumChaint.Tests.csproj
```

`AdminApiTests` exercises real authenticated HTTP endpoints with an isolated EF database: cross-monastery donation lifecycle/batch/recurrence, Myanmar date boundaries/month-end clamping, member invitations/acceptance/role changes/removal, owner-only deletion, user protection, duplicate validation, notifications, filters/pagination, and role/account revocation. Existing donor/member/role tests remain in the suite. `StartupTests` verifies public registration rejects Admin while public User registration succeeds.

### Opt-in real MySQL end-to-end run

`npm run test:real` uses the running local MySQL container and real Admin credentials supplied in `REAL_ADMIN_PHONE` and `REAL_ADMIN_PASSWORD`. It creates a uniquely named `hsumchaint_e2e_*` database, streams a clone of the actual local schema/data into it, clears FCM tokens only in that clone, and launches isolated C# / Next.js servers on ports 5187 / 3198. No fixture API or EF InMemory provider is used. It exercises the portal workflows in Chromium and verifies persisted results through the real C# API.

The source database is checksummed before/after; bulk notification mutations run only in the clone. A `finally` block closes the test servers/browser and drops only the generated test database. Reports and failure screenshots go to ignored `test-results/real-db-report.json` and `test-results/real-db-failure.png`. Credentials/tokens are never saved to reports. The script does not read `SECRETS.md`. Optional settings: `REAL_MYSQL_CONTAINER` (default `hsumchaint_csharp-db-1`) and `REAL_MYSQL_DATABASE` (default `hsumchaint_db`).

For databases initialized before `Donation.Delete` was added, apply `HsumChaint.Database/Scripts/002_admin_donation_delete_permission.sql` once before testing. It is idempotent and adds only the missing permission/Admin mapping, without reseeding users or business data. Sign in again afterward so the JWT contains the new permission. A seed consistency regression test guards all required system permissions.

The existing backend restore reports `System.Security.Cryptography.Xml` 8.0.2 vulnerability advisories; resolve those before production rollout.

## Attribution

`src/components/ui/{button,input,card,table,badge,dialog}.tsx` are adapted from [satnaing/shadcn-admin](https://github.com/satnaing/shadcn-admin), inspected at commit `e16c87f213a5ba5e45964e9b67c792105ec74d26`. Its MIT notice is preserved in `licenses/shadcn-admin-LICENSE`. Vite routing and Clerk auth are replaced by Next.js App Router and C# authentication. Domain layouts/workflows are specific to HsumChaint. `@fontsource/noto-sans-myanmar` includes its SIL Open Font License.
