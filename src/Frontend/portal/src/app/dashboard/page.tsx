import { AppShell } from "@/components/shell/app-shell";
import { Topbar } from "@/components/shell/topbar";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { StatusDot } from "@/components/ui/status-dot";

/**
 * Static port of the Stitch dashboard mock. Numbers are placeholders for now —
 * we'll wire them to real metrics once the backend has KPI endpoints. Live data
 * for departments lives on /directory.
 */
export default function DashboardPage() {
  return (
    <AppShell>
      <Topbar
        title="Dashboard"
        subtitle="Operational snapshot for the active tenant."
      />

      <section className="grid gap-gutter px-margin py-8 sm:grid-cols-2 lg:grid-cols-4">
        <Kpi label="Headcount" value="248" delta="+3 this week" trend="up" />
        <Kpi label="Active departments" value="12" delta="—" trend="flat" />
        <Kpi label="Open requisitions" value="7" delta="+2 vs last week" trend="up" />
        <Kpi label="Pending approvals" value="3" delta="-1 vs last week" trend="down" />
      </section>

      <section className="grid gap-gutter px-margin pb-8 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Recent activity</CardTitle>
            <CardDescription>
              Last 5 events emitted by the bus outbox.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ul className="divide-y divide-outline-variant/40 text-body">
              <ActivityRow when="2 min ago" actor="srikar" verb="hired" subject="Anita Verma" />
              <ActivityRow when="14 min ago" actor="hr-bot" verb="created department" subject="Mathematics" />
              <ActivityRow when="1 h ago" actor="srikar" verb="terminated" subject="Pranav Iyer" />
              <ActivityRow when="3 h ago" actor="hr-bot" verb="created department" subject="Computer Science" />
              <ActivityRow when="yesterday" actor="srikar" verb="updated" subject="Faculty handbook" />
            </ul>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>System status</CardTitle>
            <CardDescription>Health of the backbone components.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <StatusRow label="Karamchari API" status="active" detail="200 OK · p95 18ms" />
            <StatusRow label="Azure SQL (RLS)" status="active" detail="Session context propagated" />
            <StatusRow label="Service Bus relay" status="warning" detail="Backlog 4 messages" />
            <StatusRow label="Outbox dispatcher" status="active" detail="Idle · 0 in flight" />
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
