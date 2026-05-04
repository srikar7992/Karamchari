import { cn } from "@/lib/utils/cn";

/**
 * 8px semantic dot per DESIGN.md "Status Indicators" — a high-signal,
 * low-noise alternative to coloured pill badges.
 */

type Status = "active" | "inactive" | "warning" | "error";

const statusStyles: Record<Status, string> = {
  active: "bg-success",
  inactive: "bg-outline-variant",
  warning: "bg-warning",
  error: "bg-error",
};

export function StatusDot({
  status,
  className,
  label,
}: {
  status: Status;
  label?: string;
  className?: string;
}) {
  return (
    <span className={cn("inline-flex items-center gap-2", className)}>
      <span
        aria-hidden="true"
        className={cn("inline-block h-2 w-2 rounded-full", statusStyles[status])}
      />
      {label ? <span className="text-body text-on-surface">{label}</span> : null}
    </span>
  );
}
