import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { Providers } from "./providers";
import "./globals.css";

/**
 * Inter is the system typeface for the Monolith Precision design system —
 * see docs/design/stitch/monolith_precision/DESIGN.md.
 *
 * `next/font/google` self-hosts the font files at build time so we don't pay
 * for runtime DNS / TLS to Google Fonts on every page load.
 */
const inter = Inter({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
  variable: "--font-inter",
  display: "swap",
});

export const metadata: Metadata = {
  title: {
    default: "Karamchari",
    template: "%s · Karamchari",
  },
  description: "Multi-tenant Employee Management System",
  robots: { index: false, follow: false },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html
      lang="en"
      className={`${inter.variable} dark`}
      suppressHydrationWarning
    >
      <head>
        {/*
          Material Symbols — used for nav icons in the Stitch port. Loaded once
          here so individual pages can drop <span class="material-symbols-outlined">
          without per-page link tags.
        */}
        <link
          rel="stylesheet"
          href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:wght,FILL@100..700,0..1&display=swap"
        />
      </head>
      <body className="min-h-screen bg-background text-on-background font-sans text-body antialiased">
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
