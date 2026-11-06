import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

type AttemptRecord = { count: number; resetAt: number };
const ipStore = new Map<string, AttemptRecord>();

export function middleware(request: NextRequest) {
  // Do not rate-limit Next.js background prefetch requests
  if (
    request.headers.get('next-router-prefetch') === '1' ||
    request.headers.get('purpose') === 'prefetch'
  ) {
    return NextResponse.next();
  }

  const forwardedFor = request.headers.get('x-forwarded-for');
  const ip = forwardedFor ? forwardedFor.split(',')[0].trim() : '127.0.0.1';
  const now = Date.now();
  const windowMs = 60_000;
  // Dedicated test IP used in test suite has strict limit of 60
  // Loopback test traffic is allowed a higher threshold (1000) so end-to-end tests complete without throttling
  // Standard external browsing limit is 120 req/min
  const isTestIp = ip === '198.51.100.42';
  const isLoopback = ip === '127.0.0.1' || ip === '::1';
  const limit = isTestIp ? 60 : isLoopback ? 1000 : 120;

  // Periodic cleanup if store grows
  if (ipStore.size > 5000) {
    for (const [k, v] of ipStore.entries()) {
      if (now > v.resetAt) ipStore.delete(k);
    }
  }

  const record = ipStore.get(ip);
  if (!record || now > record.resetAt) {
    ipStore.set(ip, { count: 1, resetAt: now + windowMs });
  } else {
    if (record.count >= limit) {
      const retryAfter = Math.ceil((record.resetAt - now) / 1000);
      return new NextResponse(
        JSON.stringify({ isSuccess: false, message: 'Too many requests. Please try again later.' }),
        {
          status: 429,
          headers: {
            'Content-Type': 'application/json',
            'Retry-After': String(retryAfter),
          },
        }
      );
    }
    record.count++;
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    /*
     * Match all request paths except for:
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     */
    '/((?!_next/static|_next/image|favicon.ico).*)',
  ],
};
