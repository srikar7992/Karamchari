"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils/cn";

interface NavItem {
  href: string;
  label: string;
  icon: string;
}

const navItems: NavItem[] = [
  { href: "/dashboard", label: "Dashboard", icon: "dashboard" },
  { href: "/directory", label: "Directory", icon: "groups" },
  { href: "/payroll", label: "Payroll", icon: "payments" },
  { href: "/reports", label: "Reports", icon: "insights" },
  { href: "/settings", label: "Settings", icon: "settings" },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="hidden w-60 shrink-0 flex-col border-r bg-surface px-4 py-6 md:flex">
      <div className="mb-8 px-2">
        <Link href="/dashboard" className="flex items-center gap-2 text-on-surface">
          <span className="material-symbols-outlined text-[20px]">hub</span>
          <span className="text-body font-semibold tracking-tight">Karamchari</span>
        </Link>
      </div>

      <nav aria-label="Primary" className="flex flex-col gap-0.5">
        {navItems.map((item) => {
          const active =
            pathname === item.href || pathname.startsWith(`${item.href}/`);
          return (
            <Link
              key={item.href}
              href={item.href}
              aria-current={active ? "page" : undefined}
              className={cn(
                "flex h-9 items-center gap-3 rounded px-3 text-body transition-colors",
                active
                  ? "bg-surface-container text-on-surface"
                  : "text-on-surface-variant hover:bg-surface-container-low hover:text-on-surface",
              )}
            >
              <span
                aria-hidden="true"
                className="material-symbols-outlined text-[18px]"
                style={{ fontVariationSettings: active ? '"FILL" 1' : '"FILL" 0' }}
              >
                {item.icon}
              </span>
              <span>{item.label}</span>
            </Link>
          );
        })}
      </nav>

      <div className="mt-auto px-2 pt-6">
        <p className="text-caps uppercase text-on-surface-variant">Tenant</p>
        <p className="mt-1 font-mono text-mono text-on-surface">
          {process.env.NEXT_PUBLIC_DEFAULT_TENANT_ID ?? "sch_oakridge"}
        </p>
      </div>
    </aside>
  );
}
