import React from "react";
import { colors } from "../tokens/colors";

export interface Column<T> {
  key: string;
  label: string;
  render: (item: T) => React.ReactNode;
  width?: string;
}

interface TableProps<T> {
  columns: Column<T>[];
  data: T[];
  onRowClick?: (item: T) => void;
  className?: string;
  isLoading?: boolean;
}

/**
 * A highly performant, accessible, and premium table component.
 */
export function Table<T extends { id: string | number }>({
  columns,
  data,
  onRowClick,
  className = "",
  isLoading = false,
}: TableProps<T>) {
  return (
    <div className={`overflow-x-auto rounded-xl border border-[${colors.background.border}] bg-[${colors.background.surface}] backdrop-blur-md shadow-2xl ${className}`}>
      <table className="w-full text-left border-collapse">
        <thead>
          <tr className="border-b border-white/5 bg-white/5">
            {columns.map((col) => (
              <th
                key={col.key}
                className="px-6 py-4 text-xs font-semibold uppercase tracking-wider text-white/60"
                style={{ width: col.width }}
              >
                {col.label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-white/5">
          {isLoading ? (
            <tr>
              <td colSpan={columns.length} className="px-6 py-12 text-center text-white/40">
                <div className="animate-pulse">Loading data...</div>
              </td>
            </tr>
          ) : data.length === 0 ? (
            <tr>
              <td colSpan={columns.length} className="px-6 py-12 text-center text-white/40">
                No records found.
              </td>
            </tr>
          ) : (
            data.map((item) => (
              <tr
                key={item.id}
                onClick={() => onRowClick?.(item)}
                className={`group transition-colors duration-150 hover:bg-white/5 ${
                  onRowClick ? "cursor-pointer" : ""
                }`}
              >
                {columns.map((col) => (
                  <td key={col.key} className="px-6 py-4 text-sm text-white/80 group-hover:text-white">
                    {col.render(item)}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}
