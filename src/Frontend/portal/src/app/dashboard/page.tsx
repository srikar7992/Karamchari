'use client';

import { AppShell } from "@/components/shell/app-shell";
import { Topbar } from "@/components/shell/topbar";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { StatusDot } from "@/components/ui/status-dot";
import { useAuth } from "@/features/auth/AuthContext";
import { useEmployees } from "@/lib/hooks/use-employees";
import { usePendingLeaveRequests } from "@/lib/hooks/use-time";
import { usePendingTimesheets } from "@/lib/hooks/use-timesheets";
import { usePayrollRuns } from "@/lib/hooks/use-payroll";
import { usePayrollCockpit } from "@/lib/hooks/use-payroll-phase1a";

function timeAgo(iso: string): string {
  const seconds = Math.floor((Date.now() - new Date(iso).getTime()) / 1000);
  if (seconds < 60) return `${seconds}s ago`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
  return `${Math.floor(seconds / 86400)}d ago`;
}

function kpiVal(isLoading: boolean, isError: boolean, value: number | undefined): string {
  if (isLoading) return '…';
  if (isError) return '—';
  return String(value ?? 0);
}

export default function DashboardPage() {
  const { user } = useAuth();

  const employees     = useEmployees();
  const leaveRequests = usePendingLeaveRequests();
  const timesheets    = usePendingTimesheets();
  const payrollRuns   = usePayrollRuns();
  const cockpit       = usePayrollCockpit();

  const activeEmployeeCount  = employees.data?.filter(e => e.status === 'Active').length;
  const pendingApprovalCount = (leaveRequests.data?.length ?? 0) + (timesheets.data?.length ?? 0);
  const isPendingLoading     = leaveRequests.isLoading || timesheets.isLoading;
  const isPendingError       = leaveRequests.isError   || timesheets.isError;
  const cockpitData          = cockpit.data;

  const activityItems = (payrollRuns.data ?? []).slice(0, 5).map(run => {
    const verb =
      run.currentState === 'Calculating'   ? 'started calculating payroll for' :
      run.currentState === 'PendingReview' ? 'completed calculation for'       :
      run.currentState === 'Finalized'     ? 'finalized and published payslips for' :
                                             'updated payroll run for';
    return { when: timeAgo(run.startedAt), actor: 'payroll-engine', verb, subject: run.periodName };
  });

  return (
    <AppShell>
      <Topbar
        title="Dashboard"
        subtitle={user ? `Welcome back, ${user.name}.` : 'Operational snapshot for the active tenant.'}
      />

      <section className="grid gap-gutter px-margin py-8 sm:grid-cols-2 lg:grid-cols-4">
        <Kpi
          label="Headcount"
          value={kpiVal(employees.isLoading, employees.isError, activeEmployeeCount)}
          delta={
            employees.isLoading ? '—' :
            employees.isError   ? 'unavailable' :
            `${activeEmployeeCount ?? 0} active`
          }
          trend="flat"
        />
        <Kpi
          label="Active payroll runs"
          value={kpiVal(cockpit.isLoading, cockpit.isError, cockpitData?.activeRuns)}
          delta={
            cockpit.isLoading ? '—' :
            cockpit.isError   ? 'unavailable' :
            (cockpitData?.blockedRuns ?? 0) > 0 ? `${cockpitData!.blockedRuns} blocked` : 'none blocked'
          }
          trend={(cockpitData?.blockedRuns ?? 0) > 0 ? 'down' : 'flat'}
        />
        <Kpi
          label="Open anomalies"
          value={kpiVal(cockpit.isLoading, cockpit.isError, cockpitData?.openAnomalies)}
          delta={
            cockpit.isLoading ? '—' :
            cockpit.isError   ? 'unavailable' :
            (cockpitData?.openAnomalies ?? 0) > 0 ? 'needs attention' : 'all clear'
          }
          trend={(cockpitData?.openAnomalies ?? 0) > 0 ? 'down' : 'flat'}
        />
        <Kpi
          label="Pending approvals"
          value={kpiVal(isPendingLoading, isPendingError, pendingApprovalCount)}
          delta={
            isPendingLoading ? '—' :
            isPendingError   ? 'unavailable' :
            pendingApprovalCount === 0 ? 'inbox zero' : `${pendingApprovalCount} awaiting action`
          }
          trend={pendingApprovalCount > 0 ? 'up' : 'flat'}
        />
      </section>

      <section className="grid gap-gutter px-margin pb-8 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Recent activity</CardTitle>
            <CardDescription>Latest payroll run events.</CardDescription>
          </CardHeader>
          <CardContent>
            {payrollRuns.isLoading ? (
              <div className="space-y-3">
                {[...Array(3)].map((_, i) => (
                  <div key={i} className="h-10 animate-pulse rounded bg-surface-container" />
                ))}
              </div>
            ) : activityItems.length === 0 ? (
              <p className="py-4 text-center text-body text-on-surface-variant">
                No recent payroll activity.
              </p>
            ) : (
              <ul className="divide-y divide-outline-variant/40 text-body">
                {activityItems.map((item, i) => (
                  <ActivityRow key={i} {...item} />
                ))}
              </ul>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>System status</CardTitle>
            <CardDescription>Payroll operational health.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {cockpit.isLoading ? (
              <div className="space-y-3">
                {[...Array(4)].map((_, i) => (
                  <div key={i} className="h-8 animate-pulse rounded bg-surface-container" />
                ))}
              </div>
            ) : (
              <>
                <StatusRow
                  label="Payroll Engine"
                  status="active"
                  detail={
                    (cockpitData?.activeRuns ?? 0) > 0
                      ? `${cockpitData!.activeRuns} run${cockpitData!.activeRuns > 1 ? 's' : ''} active`
                      : 'No active runs'
                  }
                />
                <StatusRow
                  label="Blocked Runs"
                  status={(cockpitData?.blockedRuns ?? 0) > 0 ? 'warning' : 'active'}
                  detail={
                    (cockpitData?.blockedRuns ?? 0) > 0
                      ? `${cockpitData!.blockedRuns} blocked — needs attention`
                      : '0 blocked'
                  }
                />
                <StatusRow
                  label="Failed Disbursements"
                  status={(cockpitData?.failedDisbursements ?? 0) > 0 ? 'error' : 'active'}
                  detail={
                    (cockpitData?.failedDisbursements ?? 0) > 0
                      ? `${cockpitData!.failedDisbursements} failed · retry needed`
                      : 'All disbursements succeeded'
                  }
                />
                <StatusRow
                  label="Pending Corrections"
                  status={(cockpitData?.pendingCorrections ?? 0) > 0 ? 'warning' : 'active'}
                  detail={`${cockpitData?.pendingCorrections ?? 0} pending review`}
                />
              </>
            )}
          </CardContent>
        </Card>
      </section>
    </AppShell>
  );
}

function Kpi({
  label,
  value,
  delta,
  trend,
}: {
  label: string;
  value: string;
  delta: string;
  trend: "up" | "down" | "flat";
}) {
  const trendIcon =
    trend === "up" ? "trending_up" : trend === "down" ? "trending_down" : "trending_flat";
  return (
    <div className="rounded border bg-surface-container-low p-5">
      <p className="text-caps uppercase text-on-surface-variant">{label}</p>
      <p className="mt-3 text-h1 font-semibold tracking-tight text-on-surface">{value}</p>
      <p className="mt-2 inline-flex items-center gap-1.5 text-body text-on-surface-variant">
        <span className="material-symbols-outlined text-[16px]">{trendIcon}</span>
        {delta}
      </p>
    </div>
  );
}

function ActivityRow({
  when,
  actor,
  verb,
  subject,
}: {
  when: string;
  actor: string;
  verb: string;
  subject: string;
}) {
  return (
    <li className="flex items-baseline justify-between py-3">
      <p className="text-on-surface">
        <span className="font-mono text-mono text-on-surface-variant">{actor}</span>{" "}
        <span className="text-on-surface-variant">{verb}</span>{" "}
        <span className="font-medium">{subject}</span>
      </p>
      <span className="text-body text-on-surface-variant">{when}</span>
    </li>
  );
}

function StatusRow({
  label,
  status,
  detail,
}: {
  label: string;
  status: "active" | "warning" | "error";
  detail: string;
}) {
  return (
    <div className="flex items-center justify-between">
      <StatusDot status={status} label={label} />
      <span className="text-body text-on-surface-variant">{detail}</span>
    </div>
  );
}
