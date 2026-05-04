"use client";

import { useState } from "react";
import { AppShell } from "@/components/shell/app-shell";
import { Topbar } from "@/components/shell/topbar";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { StartRunModal } from "@/components/ui/start-run-modal";
import { usePayrollRuns } from "@/lib/hooks/use-payroll";
import type { ApiError } from "@/lib/api/errors";

import { useRouter } from "next/navigation";

export default function PayrollPage() {
  const router = useRouter();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const { data: runs, isLoading, isError, error } = usePayrollRuns();

  const renderStatus = (state: string) => {
    switch (state) {
      case "Calculating":
        return (
          <span className="inline-flex items-center gap-1.5 rounded-full bg-primary-container/20 px-2 py-0.5 text-xs font-medium text-primary shadow-sm border border-primary/30">
            <span className="relative flex h-2 w-2">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-primary opacity-75"></span>
              <span className="relative inline-flex h-2 w-2 rounded-full bg-primary"></span>
            </span>
            Calculating
          </span>
        );
      case "PendingReview":
        return (
          <span className="inline-flex items-center rounded-full bg-tertiary-container/30 px-2 py-0.5 text-xs font-medium text-tertiary border border-tertiary/30">
            Pending Review
          </span>
        );
      case "Finalized":
        return (
          <span className="inline-flex items-center rounded-full bg-secondary-container/30 px-2 py-0.5 text-xs font-medium text-secondary border border-secondary/30">
            Finalized
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center rounded-full bg-surface-container-high px-2 py-0.5 text-xs font-medium text-on-surface border border-outline">
            {state}
          </span>
        );
    }
  };

  return (
    <AppShell>
      <Topbar title="Payroll Runs">
        <Button variant="primary" size="sm" onClick={() => setIsModalOpen(true)}>
          <span className="material-symbols-outlined text-[16px]">play_arrow</span>
          Start New Run
        </Button>
      </Topbar>

      <section className="mt-6 animate-fade-in fade-in-up">
        {isError ? (
          <div className="rounded-lg bg-error-container/20 p-4 text-error">
            Failed to load payroll runs: {(error as ApiError).message}
          </div>
        ) : (
          <div className="overflow-hidden rounded-xl border border-outline bg-surface shadow-sm">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Period</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Progress</TableHead>
                  <TableHead>Started At</TableHead>
                  <TableHead>Completed At</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  <TableRow>
                    <TableCell colSpan={5} className="py-8 text-center text-on-surface-variant">
                      <div className="flex items-center justify-center gap-2">
                        <span className="material-symbols-outlined animate-spin">refresh</span>
                        Loading payroll runs...
                      </div>
                    </TableCell>
                  </TableRow>
                ) : !runs || runs.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="py-8 text-center text-on-surface-variant">
                      No payroll runs found. Click "Start New Run" to begin.
                    </TableCell>
                  </TableRow>
                ) : (
                  runs.map((run) => {
                    // Safety check to avoid division by zero
                    const percent =
                      run.totalEmployeesToProcess > 0
                        ? Math.round((run.processedEmployees / run.totalEmployeesToProcess) * 100)
                        : 0;

                    return (
                      <TableRow 
                        key={run.correlationId} 
                        className="cursor-pointer hover:bg-surface-container-low transition-colors"
                        onClick={() => router.push(`/payroll/runs/${run.correlationId}`)}
                      >
                        <TableCell className="font-medium">{run.periodName}</TableCell>
                        <TableCell>{renderStatus(run.currentState)}</TableCell>
                        <TableCell>
                          {run.currentState === "Calculating" ? (
                            <div className="flex items-center gap-3">
                              <div className="h-1.5 w-24 overflow-hidden rounded-full bg-surface-container-highest">
                                <div
                                  className="h-full bg-primary transition-all duration-500 ease-out"
                                  style={{ width: `${percent}%` }}
                                ></div>
                              </div>
                              <span className="text-xs font-medium text-primary animate-pulse">{percent}%</span>
                            </div>
                          ) : (
                            <span className="text-sm text-on-surface-variant">
                              {run.totalEmployeesToProcess} Employees
                            </span>
                          )}
                        </TableCell>
                        <TableCell className="text-on-surface-variant">
                          {new Date(run.startedAt).toLocaleString()}
                        </TableCell>
                        <TableCell className="text-on-surface-variant">
                          {run.completedAt ? new Date(run.completedAt).toLocaleString() : "—"}
                        </TableCell>
                      </TableRow>
                    );
                  })
                )}
              </TableBody>
            </Table>
          </div>
        )}
      </section>

      <StartRunModal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} />
    </AppShell>
  );
}
