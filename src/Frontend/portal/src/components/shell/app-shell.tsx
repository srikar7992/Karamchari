import type { ReactNode } from "react";
import { Sidebar } from "./sidebar";

/**
 * App shell wrapper used by every authenticated page. Sidebar on the left,
 * page content (which owns its own Topbar) on the right.
 */
export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-screen">
      <Sidebar />
      <main className="flex flex-1 flex-col">{children}</main>
    </div>
  );
}
