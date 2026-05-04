import * as React from "react";
import { cn } from "@/lib/utils/cn";

/**
 * shadcn-style Table primitives.
 *
 * Visual rules (DESIGN.md):
 *   - No vertical lines.
 *   - Horizontal lines use the ghost border colour (rgba(255,255,255,0.1)).
 *   - Header cells use the `caps` typography (label-caps).
 *   - Hover row uses a subtle surface-container-low background shift.
 */

export const Table = React.forwardRef<HTMLTableElement, React.HTMLAttributes<HTMLTableElement>>(
  function Table({ className, ...props }, ref) {
    return (
      <div className="relative w-full overflow-x-auto">
        <table
          ref={ref}
          className={cn("w-full caption-bottom text-body", className)}
          {...props}
        />
      </div>
    );
  },
);

export const TableHeader = React.forwardRef<
  HTMLTableSectionElement,
  React.HTMLAttributes<HTMLTableSectionElement>
>(function TableHeader({ className, ...props }, ref) {
  return <thead ref={ref} className={cn("[&_tr]:border-b", className)} {...props} />;
});

export const TableBody = React.forwardRef<
  HTMLTableSectionElement,
  React.HTMLAttributes<HTMLTableSectionElement>
>(function TableBody({ className, ...props }, ref) {
  return (
    <tbody
      ref={ref}
      className={cn("[&_tr:last-child]:border-0", className)}
      {...props}
    />
  );
});

export const TableRow = React.forwardRef<
  HTMLTableRowElement,
  React.HTMLAttributes<HTMLTableRowElement>
>(function TableRow({ className, ...props }, ref) {
  return (
    <tr
      ref={ref}
      className={cn(
        "border-b transition-colors hover:bg-surface-container-low data-[state=selected]:bg-surface-container",
        className,
      )}
      {...props}
    />
  );
});

export const TableHead = React.forwardRef<
  HTMLTableCellElement,
  React.ThHTMLAttributes<HTMLTableCellElement>
>(function TableHead({ className, ...props }, ref) {
  return (
    <th
      ref={ref}
      className={cn(
        "h-10 px-4 text-left align-middle text-caps uppercase text-on-surface-variant [&:has([role=checkbox])]:pr-0",
        className,
      )}
      {...props}
    />
  );
});

export const TableCell = React.forwardRef<
  HTMLTableCellElement,
  React.TdHTMLAttributes<HTMLTableCellElement>
>(function TableCell({ className, ...props }, ref) {
  return (
    <td
      ref={ref}
      className={cn("p-4 align-middle text-on-surface [&:has([role=checkbox])]:pr-0", className)}
      {...props}
    />
  );
});

export const TableCaption = React.forwardRef<
  HTMLTableCaptionElement,
  React.HTMLAttributes<HTMLTableCaptionElement>
>(function TableCaption({ className, ...props }, ref) {
  return (
    <caption
      ref={ref}
      className={cn("mt-4 text-body text-on-surface-variant", className)}
      {...props}
    />
  );
});
