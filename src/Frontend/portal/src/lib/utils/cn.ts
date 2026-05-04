import clsx, { type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

/**
 * Conditional className composer that resolves Tailwind conflicts (e.g. `p-4 p-6`
 * collapses to `p-6`). Used by every component primitive.
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}
