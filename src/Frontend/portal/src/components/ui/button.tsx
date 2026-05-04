import * as React from "react";
import { cn } from "@/lib/utils/cn";

/**
 * Three-variant Button per DESIGN.md:
 *   - primary  : solid white bg, black text, no shadow
 *   - secondary: transparent, 1px ghost border
 *   - ghost    : no chrome, surfaces a hover state
 */

type Variant = "primary" | "secondary" | "ghost";
type Size = "sm" | "md";

const variantStyles: Record<Variant, string> = {
  primary:
    "bg-primary text-on-primary hover:bg-primary-container active:bg-primary-container",
  secondary:
    "border bg-transparent text-on-surface hover:bg-surface-container-low",
  ghost:
    "bg-transparent text-on-surface hover:bg-surface-container-low",
};

const sizeStyles: Record<Size, string> = {
  sm: "h-8 px-3 text-body",
  md: "h-9 px-4 text-body",
};

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  function Button(
    { className, variant = "secondary", size = "md", type = "button", ...props },
    ref,
  ) {
    return (
      <button
        ref={ref}
        type={type}
        className={cn(
          "inline-flex items-center justify-center gap-2 rounded font-medium transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary disabled:pointer-events-none disabled:opacity-50",
          variantStyles[variant],
          sizeStyles[size],
          className,
        )}
        {...props}
      />
    );
  },
);
