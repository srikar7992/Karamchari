"use client";

import { AppShell } from "@/components/shell/app-shell";
import { Topbar } from "@/components/shell/topbar";
import { Button } from "@/components/ui/button";
import { StatusDot } from "@/components/ui/status-dot";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useDepartments } from "@/lib/hooks/use-departments";
import type { ApiError } from "@/lib/api/errors";

export default function DirectoryPage() {
  const departmentsQuery = useDepartments();

  return (
    <AppShell>
      <Topbar
        title="Directory"
        subtitle="Departments registered for this tenant. Live data from the BFF, RLS-filtered."
      >
        <Button variant="secondary" size="sm">
          <span className="material-symbols-outlined text-[16px]">filter_list</span>
          Filter
        </Button>
        <Button variant="primary" size="sm">
          <span className="material-symbols-outlined text-[16px]">add</span>
          New department
        </Button>
      </Topbar>

      <section className="px-margin py-8">
        <div className="rounded border bg-surface-container-low">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[40%]">Name</TableHead>
                <TableHead>Description</TableHead>
                <TableHead className="w-[120px]">Status</TableHead>
                <TableHead className="w-[260px] font-mono text-mono">ID</TableHead>
              </TableRow>
            </TableHeader>

            <TableBody>
              {departmentsQuery.isPending ? <SkeletonRows rows={5} /> : null}

              {departmentsQuery.isError ? (
                <ErrorRow error={departmentsQuery.error as ApiError} />
              ) : null}

              {departmentsQuery.isSuccess && departmentsQuery.data.length === 0 ? (
                <EmptyRow />
              ) : null}

              {departmentsQuery.isSuccess && departmentsQuery.data.length > 0
                ? departmentsQuery.data.map((d) => (
                    <TableRow key={d.id}>
                      <TableCell className="font-medium text-on-surface">{d.name}</TableCell>
                      <TableCell className="text-on-surface-variant">
                        {d.description ?? <span className="text-outline">—</span>}
                      </TableCell>
                      <TableCell>
                        <StatusDot
                          status={d.isActive ? "active" : "inactive"}
                          label={d.isActive ? "Active" : "Inactive"}
                        />
                      </TableCell>
                      <TableCell className="font-mono text-mono text-on-surface-variant">
                        {d.id}
                      </TableCell>
                    </TableRow>
                  ))
                : null}
            </TableBody>
          </Table>
        </div>

        <p className="mt-3 text-body text-on-surface-variant">
          {departmentsQuery.isSuccess
            ? `${departmentsQuery.data.length} departments`
            : "—"}
        </p>
      </section>
    </AppShell>
  );
}

function SkeletonRows({ rows }: { rows: number }) {
  return (
    <>
      {Array.from({ length: rows }).map((_, i) => (
        <TableRow key={`skeleton-${i}`}>
          <TableCell colSpan={4}>
            <div className="h-4 w-full animate-pulse rounded bg-surface-container-high/50" />
          </TableCell>
        </TableRow>
      ))}
    </>
  );
}

function EmptyRow() {
  return (
    <TableRow>
      <TableCell colSpan={4} className="py-12 text-center">
        <p className="text-body text-on-surface-variant">
          No departments yet. Use <span className="text-on-surface">New department</span> to create
          the first one.
        </p>
      </TableCell>
    </TableRow>
  );
}

function ErrorRow({ error }: { error: ApiError | undefined }) {
  return (
    <TableRow>
      <TableCell colSpan={4} className="py-10">
        <div className="flex flex-col items-start gap-2 rounded border border-error/30 bg-error-container/10 p-4">
          <p className="text-body font-medium text-error">
            {error?.displayMessage ?? "Failed to load departments."}
          </p>
          {error?.status ? (
            <p className="font-mono text-mono text-on-surface-variant">
              HTTP {error.status}
              {error.reason ? ` · ${error.reason}` : ""}
            </p>
          ) : null}
        </div>
      </TableCell>
    </TableRow>
  );
}
