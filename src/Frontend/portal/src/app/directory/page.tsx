"use client";

import { useState } from "react";
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
import { AddEmployeeModal } from "@/components/ui/add-employee-modal";
import { useEmployees } from "@/lib/hooks/use-employees";
import type { ApiError } from "@/lib/api/errors";

export default function DirectoryPage() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const employeesQuery = useEmployees();

  return (
    <AppShell>
      <Topbar
        title="Directory"
        subtitle="Employees registered for this tenant. Live data from the BFF, RLS-filtered."
      >
        <Button variant="secondary" size="sm">
          <span className="material-symbols-outlined text-[16px]">filter_list</span>
          Filter
        </Button>
        <Button variant="primary" size="sm" onClick={() => setIsModalOpen(true)}>
          <span className="material-symbols-outlined text-[16px]">add</span>
          Onboard Employee
        </Button>
      </Topbar>

      <section className="px-margin py-8">
        <div className="rounded border bg-surface-container-low">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[30%]">Name</TableHead>
                <TableHead>Employee Number</TableHead>
                <TableHead className="w-[150px]">Hired On</TableHead>
                <TableHead className="w-[120px]">Status</TableHead>
                <TableHead className="w-[260px] font-mono text-mono">ID</TableHead>
              </TableRow>
            </TableHeader>

            <TableBody>
              {employeesQuery.isPending ? <SkeletonRows rows={5} /> : null}

              {employeesQuery.isError ? (
                <ErrorRow error={employeesQuery.error as ApiError} />
              ) : null}

              {employeesQuery.isSuccess && employeesQuery.data.length === 0 ? (
                <EmptyRow />
              ) : null}

              {employeesQuery.isSuccess && employeesQuery.data.length > 0
                ? employeesQuery.data.map((e) => (
                    <TableRow key={e.id}>
                      <TableCell className="font-medium text-on-surface">
                        <div>{e.legalName}</div>
                        {e.workEmail && (
                          <div className="text-sm text-on-surface-variant font-normal">{e.workEmail}</div>
                        )}
                      </TableCell>
                      <TableCell className="text-on-surface-variant">
                        {e.employeeNumber}
                      </TableCell>
                      <TableCell className="text-on-surface-variant">
                        {e.hiredOn}
                      </TableCell>
                      <TableCell>
                        <StatusDot
                          status={e.status === "Active" ? "active" : "inactive"}
                          label={e.status}
                        />
                      </TableCell>
                      <TableCell className="font-mono text-mono text-on-surface-variant">
                        {e.id}
                      </TableCell>
                    </TableRow>
                  ))
                : null}
            </TableBody>
          </Table>
        </div>

        <p className="mt-3 text-body text-on-surface-variant">
          {employeesQuery.isSuccess
            ? `${employeesQuery.data.length} employees`
            : "—"}
        </p>
      </section>

      <AddEmployeeModal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} />
    </AppShell>
  );
}

function SkeletonRows({ rows }: { rows: number }) {
  return (
    <>
      {Array.from({ length: rows }).map((_, i) => (
        <TableRow key={`skeleton-${i}`}>
          <TableCell colSpan={5}>
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
      <TableCell colSpan={5} className="py-12 text-center">
        <p className="text-body text-on-surface-variant">
          No employees yet. Use <span className="text-on-surface">Onboard Employee</span> to add
          the first one.
        </p>
      </TableCell>
    </TableRow>
  );
}

function ErrorRow({ error }: { error: ApiError | undefined }) {
  return (
    <TableRow>
      <TableCell colSpan={5} className="py-10">
        <div className="flex flex-col items-start gap-2 rounded border border-error/30 bg-error-container/10 p-4">
          <p className="text-body font-medium text-error">
            {error?.displayMessage ?? "Failed to load employees."}
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
