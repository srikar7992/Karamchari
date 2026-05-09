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
import {
  usePayrollCockpit,
  useReconciliationJobs,
  useRunReconciliation,
  useAnomalies,
  useResolveAnomaly,
} from "@/lib/hooks/use-payroll-phase1a";
import type { PayrollAnomalyDto } from "@/lib/types/payroll-phase1a";

function SeverityBadge({ severity }: { severity: string }) {
  const cls =
    severity === "Critical"
      ? "bg-error/20 text-error border border-error/30"
      : severity === "Warning"
        ? "bg-tertiary-container/30 text-tertiary border border-tertiary/30"
        : "bg-surface-variant text-on-surface-variant border border-outline/20";
  return (
    <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${cls}`}>
      {severity}
    </span>
  );
}

function MetricCard({
  label,
  value,
  urgent,
}: {
  label: string;
  value: number;
  urgent?: boolean;
}) {
  return (
    <div
      className={`rounded-xl border p-4 ${urgent && value > 0 ? "border-error/40 bg-error/5" : "border-outline/20 bg-surface-variant/30"}`}
    >
      <p className="text-xs text-on-surface-variant">{label}</p>
      <p
        className={`mt-1 text-3xl font-bold tabular-nums ${urgent && value > 0 ? "text-error" : "text-on-surface"}`}
      >
        {value}
      </p>
    </div>
  );
}

export default function PayrollCockpitPage() {
  const [reconcilePeriod, setReconcilePeriod] = useState("");
  const { data: cockpit, isLoading: loadingCockpit } = usePayrollCockpit();
  const { data: jobs } = useReconciliationJobs();
  const { data: anomalies } = useAnomalies({ resolved: false });
  const runReconciliation = useRunReconciliation();

  function handleReconcile() {
    if (!reconcilePeriod) return;
    runReconciliation.mutate(reconcilePeriod, {
      onSuccess: () => setReconcilePeriod(""),
    });
  }

  return (
    <AppShell>
      <Topbar title="Payroll Operations Cockpit" />
      <div className="space-y-8 p-6">
        {/* Summary metrics */}
        <section>
          <h2 className="mb-3 text-sm font-semibold text-on-surface-variant uppercase tracking-wide">
            Live Status
          </h2>
          {loadingCockpit ? (
            <p className="text-sm text-on-surface-variant">Loading...</p>
          ) : cockpit ? (
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <MetricCard label="Active Runs" value={cockpit.activeRuns} />
              <MetricCard label="Blocked Runs" value={cockpit.blockedRuns} urgent />
              <MetricCard label="Pending Corrections" value={cockpit.pendingCorrections} urgent />
              <MetricCard label="Pending Approvals" value={cockpit.pendingApprovals} urgent />
              <MetricCard label="Pending FnF" value={cockpit.pendingFnF} />
              <MetricCard label="Failed Disbursements" value={cockpit.failedDisbursements} urgent />
              <MetricCard label="Open Anomalies" value={cockpit.openAnomalies} urgent />
              <MetricCard label="Pending Arrears" value={cockpit.pendingArrears} />
            </div>
          ) : null}
        </section>

        {/* Anomalies */}
        <section>
          <h2 className="mb-3 text-sm font-semibold text-on-surface-variant uppercase tracking-wide">
            Open Anomalies
          </h2>
          <AnomalyTable anomalies={anomalies?.items ?? []} />
        </section>

        {/* Reconciliation */}
        <section>
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-semibold text-on-surface-variant uppercase tracking-wide">
              Reconciliation Jobs
            </h2>
            <div className="flex gap-2">
              <input
                type="text"
                placeholder="Period (e.g. May-2025)"
                value={reconcilePeriod}
                onChange={(e) => setReconcilePeriod(e.target.value)}
                className="rounded-lg border border-outline/40 bg-surface px-3 py-1.5 text-sm text-on-surface focus:outline-none focus:ring-1 focus:ring-primary"
              />
              <Button
                size="sm"
                onClick={handleReconcile}
                disabled={!reconcilePeriod || runReconciliation.isPending}
              >
                Run Reconciliation
              </Button>
            </div>
          </div>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Period</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Employees</TableHead>
                <TableHead>Anomaly Score</TableHead>
                <TableHead>Critical</TableHead>
                <TableHead>Warning</TableHead>
                <TableHead>Started</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {(jobs?.items ?? []).map((job) => (
                <TableRow key={job.id}>
                  <TableCell className="font-medium">{job.periodName}</TableCell>
                  <TableCell>{job.status}</TableCell>
                  <TableCell className="tabular-nums">{job.employeeCount}</TableCell>
                  <TableCell>
                    <span
                      className={`tabular-nums font-semibold ${job.anomalyScore > 50 ? "text-error" : job.anomalyScore > 20 ? "text-tertiary" : "text-on-surface"}`}
                    >
                      {job.anomalyScore.toFixed(1)}
                    </span>
                  </TableCell>
                  <TableCell className="tabular-nums text-error">{job.criticalCount}</TableCell>
                  <TableCell className="tabular-nums text-tertiary">{job.warningCount}</TableCell>
                  <TableCell className="text-on-surface-variant text-xs">
                    {new Date(job.startedAtUtc).toLocaleString()}
                  </TableCell>
                </TableRow>
              ))}
              {(jobs?.items ?? []).length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-on-surface-variant py-8">
                    No reconciliation jobs
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </section>
      </div>
    </AppShell>
  );
}

function AnomalyTable({ anomalies }: { anomalies: PayrollAnomalyDto[] }) {
  const resolveAnomaly = useResolveAnomaly(anomalies[0]?.id ?? "");

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Type</TableHead>
          <TableHead>Severity</TableHead>
          <TableHead>Period</TableHead>
          <TableHead>Description</TableHead>
          <TableHead>Employee</TableHead>
          <TableHead></TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {anomalies.map((a) => (
          <AnomalyRow key={a.id} anomaly={a} />
        ))}
        {anomalies.length === 0 && (
          <TableRow>
            <TableCell colSpan={6} className="text-center text-on-surface-variant py-8">
              No open anomalies
            </TableCell>
          </TableRow>
        )}
      </TableBody>
    </Table>
  );
}

function AnomalyRow({ anomaly }: { anomaly: PayrollAnomalyDto }) {
  const resolve = useResolveAnomaly(anomaly.id);
  return (
    <TableRow>
      <TableCell className="font-medium">{anomaly.anomalyType}</TableCell>
      <TableCell>
        <SeverityBadge severity={anomaly.severity} />
      </TableCell>
      <TableCell>{anomaly.periodName}</TableCell>
      <TableCell className="max-w-xs truncate text-on-surface-variant text-sm">
        {anomaly.description}
      </TableCell>
      <TableCell className="text-xs text-on-surface-variant">
        {anomaly.employeeId ?? "-"}
      </TableCell>
      <TableCell>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => resolve.mutate("Manually reviewed")}
          disabled={resolve.isPending}
        >
          Resolve
        </Button>
      </TableCell>
    </TableRow>
  );
}
