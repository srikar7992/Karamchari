import { cn } from "@/lib/utils/cn";

/**
 * Page topbar — title left, action slot right. The title is the page's H1
 * because the sidebar already carries the global brand.
 */

export function Topbar({
  title,
  subtitle,
  children,
  className,
}: {
  title: string;
  subtitle?: string;
  children?: React.ReactNode;
  className?: string;
}) {
  return (
    <header
      className={cn(
        "flex flex-col gap-3 border-b px-margin py-6 md:flex-row md:items-end md:justify-between",
        className,
      )}
    >
      <div>
        <h1 className="text-h1 font-semibold tracking-tight text-on-surface">{title}</h1>
        {subtitle ? (
          <p className="mt-1 text-body text-on-surface-variant">{subtitle}</p>
        ) : null}
      </div>
      {children ? <div className="flex items-center gap-2">{children}</div> : null}
    </header>
  );
}
