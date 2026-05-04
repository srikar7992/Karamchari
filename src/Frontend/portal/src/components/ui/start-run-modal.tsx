"use client";

import { useState, useEffect } from "react";
import { Button } from "./button";
import { useStartPayrollRun } from "@/lib/hooks/use-payroll";

export function StartRunModal({
  isOpen,
  onClose,
}: {
  isOpen: boolean;
  onClose: () => void;
}) {
  const [periodName, setPeriodName] = useState("");
  const startRun = useStartPayrollRun();

  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    if (isOpen) {
      window.addEventListener("keydown", handleEscape);
    }
    return () => window.removeEventListener("keydown", handleEscape);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    startRun.mutate(periodName, {
      onSuccess: () => {
        setPeriodName("");
        onClose();
      },
    });
  };

  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>) => {
    // Only close if clicking the backdrop itself, not its children
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  return (
    <div 
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm transition-opacity"
      onClick={handleBackdropClick}
    >
      <div className="w-full max-w-md scale-100 rounded-xl bg-surface p-6 shadow-xl transition-transform">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-xl font-semibold text-on-surface">Start New Payroll Run</h2>
          <button onClick={onClose} className="text-on-surface-variant hover:text-on-surface">
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="mb-1 block text-sm font-medium text-on-surface">Period Name</label>
            <input
              type="text"
              required
              placeholder="e.g. March 2027"
              value={periodName}
              onChange={(e) => setPeriodName(e.target.value)}
              className="w-full rounded border border-outline bg-surface-container-low px-3 py-2 text-on-surface focus:border-primary focus:outline-none"
            />
            <p className="mt-1 text-xs text-on-surface-variant">
              This will calculate pay for all active employees.
            </p>
          </div>

          {startRun.isError && (
            <div className="rounded bg-error-container/20 p-3 text-sm text-error">
              Failed to trigger payroll run. Please try again.
            </div>
          )}

          <div className="mt-6 flex justify-end gap-3">
            <Button variant="secondary" onClick={onClose} type="button">
              Cancel
            </Button>
            <Button variant="primary" type="submit" disabled={startRun.isPending || !periodName}>
              {startRun.isPending ? "Starting..." : "Start Run"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
