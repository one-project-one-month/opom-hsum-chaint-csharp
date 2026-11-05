import type { Metadata } from 'next';
import '@fontsource/noto-sans-myanmar/400.css';
import '@fontsource/noto-sans-myanmar/600.css';
import '@fontsource/noto-sans-myanmar/700.css';
import './globals.css';
import { LanguageProvider, LocalizedContent } from '@/components/localization';
export const metadata: Metadata = { title: { default: 'ဆွမ်းဂျိုင့် Admin', template: '%s · ဆွမ်းဂျိုင့်' }, description: 'HsumChaint system administration', robots: { index: false, follow: false } };
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return <html lang="my"><body><LanguageProvider><LocalizedContent>{children}</LocalizedContent></LanguageProvider></body></html>;
}
