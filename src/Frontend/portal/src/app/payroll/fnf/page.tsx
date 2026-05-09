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
  useFnFSettlements,
  useFnFSettlement,
  useInitiateFnF,
  useFnFAction,
} from "@/lib/hooks/use-payroll-phase1a";
import type { FnFExitType, FnFSettlementDto } from "@/lib/types/payroll-phase1a";

function StatusBadge({ status }: { status: string }) {
  const map: Record<string, string> = {
    Draft: "bg-surface-variant text-on-surface-variant border-outline/20",
    PendingApproval: "bg-tertiary-container/30 text-tertiary border-tertiary/30",
    Approved: "bg-primary-container/20 text-primary border-primary/30",
    Disbursed: "bg-secondary-container/30 text-secondary border-secondary/30",
    OnHold: "bg-error/10 text-error border-error/30",
    Cancelled: "bg-surface-variant text-on-surface-variant/60 border-outline/10",
    Reopened: "bg-tertiary-container/20 text-tertiary border-tertiary/20",
  };
  return (
    <span
      className={`inline-flex items-center rounded-full border px-2 py-0.5 text-xs font-medium ${map[status] ?? map.Draft}`}
    >
      {status}
    </span>
  );
}

function DetailPanel({
  settlement,
  onClose,
}: {
  settlement: FnFSettlementDto;
  onClose: () => void;
}) {
  const submit = useFnFAction(settlement.id, "submit");
  const approve = useFnFAction(settlement.id, "approve");
  const disburse = useFnFAction(settlement.id, "disburse");
  const hold = useFnFAction(settlement.id, "hold");
  const releaseHold = useFnFAction(settlement.id, "release-hold");
  const reopen = useFnFAction(settlement.id, "reopen");

  const { data: fresh } = useFnFSettlement(settlement.id);
  const s = fresh ?? settlement;

  return (
    <div className="fixed inset-y-0 right-0 z-50 flex w-full max-w-lg flex-col border-l border-outline/20 bg-surface shadow-2xl">
      <div className="flex items-center justify-between border-b border-outline/20 px-6 py-4">
        <div>
          <h2 className="font-semibold text-on-surface">{s.employeeName}</h2>
          <p className="text-xs text-on-surface-variant">
            {s.exitType} &bull; Last working: {new Date(s.lastWorkingDate).toLocaleDateString()}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <StatusBadge status={s.status} />
          <Button variant="ghost" size="sm" onClick={onClose}>
            Close
          </Button>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">
        {/* Line items */}
        <div>
          <h3 className="mb-2 text-sm font-semibold text-on-surface-variant">Settlement Breakdown</h3>
          <div className="space-y-1.5">
            {s.lineItems.map((item, i) => (
              <div key={i} className="flex items-center justify-between text-sm">
                <span className="text-on-surface">{item.description}</span>
                <span
                  className={`tabular-nums font-medium ${item.isDeduction ? "text-error" : "text-on-surface"}`}
                >
                  {item.isDeduction ? "-" : "+"}
                  {item.signedAmount.toLocaleString("en-IN", { style: "currency", currency: "INR" })}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Totals */}
        <div className="rounded-lg border border-outline/20 bg-surface-variant/20 p-4 space-y-2">
          <div className="flex justify-between text-sm">
            <span className="text-on-surface-variant">Gross Payable</span>
            <span className="tabular-nums font-medium">
              {s.grossPayable.toLocaleString("en-IN", { style: "currency", currency: "INR" })}
            </span>
          </div>
          <div className="flex justify-between text-sm">
            <span className="text-on-surface-variant">Total Deductions</span>
            <span className="tabular-nums font-medium text-error">
              -{s.totalDeductions.toLocaleString("en-IN", { style: "currency", currency: "INR" })}
            </span>
          </div>
          <div className="flex justify-between border-t border-outline/20 pt-2 text-base font-bold">
            <span>Net Payable</span>
            <span
              className={`tabular-nums ${s.netPayable < 0 ? "text-error" : "text-primary"}`}
            >
              {s.netPayable.toLocaleString("en-IN", { style: "currency", currency: "INR" })}
            </span>
          </div>
        </div>
      </div>

      {/* Actions */}
      <div className="border-t border-outline/20 px-6 py-4 flex flex-wrap gap-2">
        {s.status === "Draft" && (
          <Button onClick={() => submit.mutate(undefined)} disabled={submit.isPending} size="sm">
            Submit for Approval
          </Button>
        )}
        {s.status === "PendingApproval" && (
          <Button onClick={() => approve.mutate(undefined)} disabled={approve.isPending} size="sm">
            Approve
          </Button>
        )}
        {s.status === "Approved" && (
          <Button onClick={() => disburse.mutate(undefined)} disabled={disburse.isPending} size="sm">
            Disburse
          </Button>
        )}
        {(s.status === "PendingApproval" || s.status === "Approved") && (
          <Button
            variant="secondary"
            onClick={() => hold.mutate("Legal review required")}
            disabled={hold.isPending}
            size="sm"
          >
            Place on Hold
          </Button>
        )}
        {s.status === "OnHold" && (
          <Button
            variant="secondary"
            onClick={() => releaseHold.mutate(undefined)}
            disabled={releaseHold.isPending}
            size="sm"
          >
            Release Hold
          </Button>
        )}
        {s.status === "Disbursed" && (
          <Button
            variant="secondary"
            onClick={() => reopen.mutate(undefined)}
            disabled={reopen.isPending}
            size="sm"
          >
            Reopen
          </Button>
        )}
      </div>
    </div>
  );
}

function InitiateFnFModal({ onClose }: { onClose: () => void }) {
  const initiate = useInitiateFnF();
  const [form, setForm] = useState({
    employeeId: "",
    employeeName: "",
    exitType: "Resignation" as FnFExitType,
    lastWorkingDate: "",
    exitDate: "",
    remarks: "",
  });

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    initiate.mutate(form, { onSuccess: onClose });
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-md rounded-2xl border border-outline/20 bg-surface p-6 shadow-2xl space-y-4"
      >
        <h2 className="font-semibold text-on-surface">Initiate FnF Settlement</h2>

        <div className="space-y-3">
          <input
            required
            placeholder="Employee ID"
            value={form.employeeId}
            onChange={(e) => setForm({ ...form, employeeId: e.target.value })}
            className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
          />
          <input
            required
            placeholder="Employee Name"
            value={form.employeeName}
            onChange={(e) => setForm({ ...form, employeeName: e.target.value })}
            className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
          />
          <select
            value={form.exitType}
            onChange={(e) => setForm({ ...form, exitType: e.target.value as FnFExitType })}
            className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
          >
            {["Resignation", "Termination", "Absconding", "Retirement", "ContractCompletion"].map(
              (t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ),
            )}
          </select>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-xs text-on-surface-variant">Last Working Date</label>
              <input
                required
                type="date"
                value={form.lastWorkingDate}
                onChange={(e) => setForm({ ...form, lastWorkingDate: e.target.value })}
                className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs text-on-surface-variant">Exit Date</label>
              <input
                required
                type="date"
                value={form.exitDate}
                onChange={(e) => setForm({ ...form, exitDate: e.target.value })}
                className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
          </div>
          <textarea
            placeholder="Remarks (optional)"
            rows={2}
            value={form.remarks}
            onChange={(e) => setForm({ ...form, remarks: e.target.value })}
            className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>

        <div className="flex justify-end gap-2">
          <Button variant="ghost" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" disabled={initiate.isPending}>
            {initiate.isPending ? "Initiating..." : "Initiate"}
          </Button>
        </div>
      </form>
    </div>
  );
}

export default function FnFPage() {
  const [showModal, setShowModal] = useState(false);
  const [selected, setSelected] = useState<FnFSettlementDto | null>(null);
  const { data, isLoading } = useFnFSettlements();

  return (
    <AppShell>
      <Topbar title="Full & Final Settlements">
        <Button size="sm" onClick={() => setShowModal(true)}>
          Initiate FnF
        </Button>
      </Topbar>

      <div className="p-6">
        {isLoading ? (
          <p className="text-sm text-on-surface-variant">Loading...</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Employee</TableHead>
                <TableHead>Exit Type</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Net Payable</TableHead>
                <TableHead>Last Working Day</TableHead>
                <TableHead></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {(data?.items ?? []).map((s) => (
                <TableRow key={s.id} className="cursor-pointer" onClick={() => setSelected(s)}>
                  <TableCell className="font-medium">{s.employeeName}</TableCell>
                  <TableCell>{s.exitType}</TableCell>
                  <TableCell>
                    <StatusBadge status={s.status} />
                  </TableCell>
                  <TableCell
                    className={`tabular-nums font-medium ${s.netPayable < 0 ? "text-error" : ""}`}
                  >
                    {s.netPayable.toLocaleString("en-IN", { style: "currency", currency: "INR" })}
                  </TableCell>
                  <TableCell className="text-on-surface-variant text-sm">
                    {new Date(s.lastWorkingDate).toLocaleDateString()}
                  </TableCell>
                  <TableCell>
                    <Button variant="ghost" size="sm" onClick={(e) => { e.stopPropagation(); setSelected(s); }}>
                      View
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {(data?.items ?? []).length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="text-center text-on-surface-variant py-8">
                    No FnF settlements
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        )}
      </div>

      {showModal && <InitiateFnFModal onClose={() => setShowModal(false)} />}
      {selected && <DetailPanel settlement={selected} onClose={() => setSelected(null)} />}
    </AppShell>
  );
}
