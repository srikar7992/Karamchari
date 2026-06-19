'use client';

import { useParams, useRouter } from 'next/navigation';
import { AppShell } from '@/components/shell/app-shell';
import { Topbar } from '@/components/shell/topbar';
import { StatusDot } from '@/components/ui/status-dot';
import { Button } from '@/components/ui/button';
import { useEmployee, useEmployeeHistory } from '@/lib/hooks/use-employees';
import type { ApiError } from '@/lib/api/errors';

function fmtDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

function fmtDateTime(iso: string): string {
  return (
    new Date(iso).toLocaleString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }) + ' UTC'
  );
}

function truncateId(value: string): string {
  if (/^[0-9a-f]{8}-/i.test(value)) return value.slice(0, 8) + '…';
  return value;
}

function humanizeType(type: string): string {
  return type.replace(/([A-Z])/g, ' $1').trim();
}

function toStatusDot(s: string): 'active' | 'inactive' | 'warning' | 'error' {
  switch (s) {
    case 'Active': return 'active';
    case 'Terminated': return 'error';
    case 'OnLeave': return 'warning';
    default: return 'inactive';
  }
}

export default function EmployeeProfilePage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();

  const profile = useEmployee(id);
  const history = useEmployeeHistory(id);

  const e = profile.data;

  return (
    <AppShell>
      <Topbar
        title={e?.legalName ?? (profile.isPending ? 'Loading…' : 'Employee')}
        subtitle={e?.employeeNumber}
      >
        <Button variant="secondary" size="sm" onClick={() => router.back()}>
          ← Directory
        </Button>
      </Topbar>

      <section className="px-margin py-8 space-y-8">
        {/* Profile card */}
        {profile.isPending ? (
          <ProfileSkeleton />
        ) : profile.isError ? (
          <ProfileError
            error={profile.error as ApiError}
            onRetry={() => profile.refetch()}
          />
        ) : e ? (
          <div className="rounded border bg-surface-container-low p-6">
            <div className="mb-6 flex items-start justify-between">
              <div>
                <h2 className="text-h2 font-semibold text-on-surface">{e.legalName}</h2>
                <p className="font-mono text-mono text-on-surface-variant">{e.employeeNumber}</p>
              </div>
              <StatusDot status={toStatusDot(e.status)} label={e.status} />
            </div>
            <dl className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
              <Field label="Work Email" value={e.workEmail ?? '—'} />
              <Field label="Hired On" value={fmtDate(e.hiredOn)} />
              <Field label="Time Zone" value={e.timeZoneId ?? '—'} />
            </dl>
          </div>
        ) : null}

        {/* Change history */}
        <div>
          <h3 className="mb-4 text-caps uppercase tracking-wider text-on-surface-variant">
            Change History
          </h3>

          {history.isPending ? (
            <HistorySkeleton />
          ) : history.isError ? (
            <HistoryError
              error={history.error as ApiError}
              onRetry={() => history.refetch()}
            />
          ) : (history.data?.length ?? 0) === 0 ? (
            <p className="text-body text-on-surface-variant">No history recorded.</p>
          ) : (
            <ol className="space-y-3">
              {[...history.data!]
                .sort(
                  (a, b) =>
                    new Date(b.loggedAt).getTime() - new Date(a.loggedAt).getTime(),
                )
                .map((evt, i) => (
                  <li key={i} className="rounded border bg-surface-container-low p-4">
                    <div className="flex items-start justify-between gap-4">
                      <span className="text-body font-medium text-on-surface">
                        {humanizeType(evt.type)}
                      </span>
                      <span className="shrink-0 font-mono text-mono text-on-surface-variant">
                        {fmtDateTime(evt.loggedAt)}
                      </span>
                    </div>
                    <div className="mt-2 flex items-center gap-2 font-mono text-mono text-on-surface-variant">
                      <span title={evt.previous ?? undefined}>
                        {evt.previous ? truncateId(evt.previous) : '—'}
                      </span>
                      <span className="shrink-0">→</span>
                      <span title={evt.new ?? undefined}>
                        {evt.new ? truncateId(evt.new) : '—'}
                      </span>
                    </div>
                    <div className="mt-1 text-body text-on-surface-variant">
                      Effective {fmtDate(evt.effectiveFrom)}
                      {evt.changedBy ? ` · by ${truncateId(evt.changedBy)}` : ''}
                    </div>
                  </li>
                ))}
            </ol>
          )}
        </div>
      </section>
    </AppShell>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-caps uppercase text-on-surface-variant">{label}</dt>
      <dd className="mt-1 text-body text-on-surface">{value}</dd>
    </div>
  );
}

function ProfileSkeleton() {
  return (
    <div className="rounded border bg-surface-container-low p-6 space-y-4">
      <div className="h-6 w-48 animate-pulse rounded bg-surface-container-high/50" />
      <div className="h-4 w-32 animate-pulse rounded bg-surface-container-high/50" />
      <div className="grid grid-cols-3 gap-6 pt-2">
        {[1, 2, 3].map((i) => (
          <div key={i} className="space-y-2">
            <div className="h-3 w-16 animate-pulse rounded bg-surface-container-high/50" />
            <div className="h-4 w-24 animate-pulse rounded bg-surface-container-high/50" />
          </div>
        ))}
      </div>
    </div>
  );
}

function ProfileError({
  error,
  onRetry,
}: {
  error: ApiError | undefined;
  onRetry: () => void;
}) {
  return (
    <div className="rounded border border-error/30 bg-error-container/10 p-6 flex flex-col gap-3">
      <p className="text-body font-medium text-error">
        {error?.displayMessage ?? 'Failed to load employee profile.'}
      </p>
      <Button variant="secondary" size="sm" onClick={onRetry}>
        Retry
      </Button>
    </div>
  );
}

function HistorySkeleton() {
  return (
    <div className="space-y-3">
      {[1, 2, 3].map((i) => (
        <div
          key={i}
          className="h-20 animate-pulse rounded border bg-surface-container-low"
        />
      ))}
    </div>
  );
}

function HistoryError({
  error,
  onRetry,
}: {
  error: ApiError | undefined;
  onRetry: () => void;
}) {
  return (
    <div className="rounded border border-error/30 bg-error-container/10 p-4 flex items-center gap-4">
      <p className="text-body text-error flex-1">
        {error?.displayMessage ?? 'Failed to load change history.'}
      </p>
      <Button variant="secondary" size="sm" onClick={onRetry}>
        Retry
      </Button>
    </div>
  );
}
