"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { useState, type ReactNode } from "react";

/**
 * App-wide providers.
 *
 * QueryClient is held in component state so it survives Fast Refresh in dev
 * without losing the cache, and it's instantiated lazily so SSR doesn't share
 * a singleton across requests (each request gets a fresh client).
 *
 * Defaults are conservative for an admin tool:
 *   - staleTime 30s — most directory views are happy with a half-minute window.
 *   - gcTime 5min — keep entries warm during typical session navigation.
 *   - refetchOnWindowFocus false — admin UIs are background-tab heavy; the
 *     constant refetching gets noisy.
 *   - retry 1 — surface real failures fast; 4xx and 5xx are usually informative
 *     and a long retry just slows the UI down.
 */
export function Providers({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30_000,
            gcTime: 5 * 60_000,
            refetchOnWindowFocus: false,
            retry: 1,
          },
          mutations: {
            retry: 0,
          },
        },
      }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      {children}
      {process.env.NODE_ENV !== "production" ? (
        <ReactQueryDevtools initialIsOpen={false} buttonPosition="bottom-right" />
      ) : null}
    </QueryClientProvider>
  );
}
