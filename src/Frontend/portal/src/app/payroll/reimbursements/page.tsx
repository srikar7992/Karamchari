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
  useReimbursements,
  useSubmitReimbursement,
  useApproveReimbursement,
  useRejectReimbursement,
} from "@/lib/hooks/use-payroll-phase1a";
import type { ReimbursementCategory, ReimbursementClaimDto } from "@/lib/types/payroll-phase1a";

const CATEGORIES: ReimbursementCategory[] = [
  "Travel",
  "Meal",
  "Internet",
  "Fuel",
  "Mobile",
  "Relocation",
  "FlexibleBenefits",
  "Medical",
  "Books",
  "Other",
];

function StatusBadge({ status }: { status: string }) {
  const map: Record<string, string> = {
    Draft: "bg-surface-variant text-on-surface-variant border-outline/20",
    Submitted: "bg-tertiary-container/30 text-tertiary border-tertiary/30",
    UnderReview: "bg-primary-container/20 text-primary border-primary/30",
    FraudFlagged: "bg-error/20 text-error border-error/40",
    Approved: "bg-secondary-container/30 text-secondary border-secondary/30",
    PartiallyApproved: "bg-tertiary-container/20 text-tertiary border-tertiary/20",
    Rejected: "bg-error/10 text-error border-error/20",
    PaidOut: "bg-secondary-container/20 text-secondary border-secondary/20",
    ClawedBack: "bg-surface-variant text-on-surface-variant/60 border-outline/10",
  };
  return (
    <span
      className={`inline-flex items-center rounded-full border px-2 py-0.5 text-xs font-medium ${map[status] ?? map.Submitted}`}
    >
      {status}
    </span>
  );
}

function ApprovePanel({
  claim,
  onClose,
}: {
  claim: ReimbursementClaimDto;
  onClose: () => void;
}) {
  const [approvedAmount, setApprovedAmount] = useState(claim.claimedAmount.toString());
  const [rejectReason, setRejectReason] = useState("");
  const [mode, setMode] = useState<"approve" | "reject">("approve");

  const approve = useApproveReimbursement(claim.id);
  const reject = useRejectReimbursement(claim.id);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-md rounded-2xl border border-outline/20 bg-surface p-6 shadow-2xl space-y-4">
        <div>
          <h2 className="font-semibold text-on-surface">Review Claim</h2>
          <p className="text-xs text-on-surface-variant mt-0.5">
            {claim.employeeName} &bull; {claim.category} &bull;{" "}
            {claim.claimedAmount.toLocaleString("en-IN", { style: "currency", currency: "INR" })}
          </p>
        </div>

        {claim.fraudIndicator !== "None" && (
          <div className="rounded-lg bg-error/10 border border-error/30 px-3 py-2 text-xs text-error">
            Fraud indicator: {claim.fraudIndicator}
          </div>
        )}

        <div className="flex gap-2">
          <button
            onClick={() => setMode("approve")}
            className={`flex-1 rounded-lg border py-2 text-sm font-medium transition-colors ${mode === "approve" ? "border-primary bg-primary text-on-primary" : "border-outline/30 bg-surface text-on-surface hover:bg-surface-variant/30"}`}
          >
            Approve
          </button>
          <button
            onClick={() => setMode("reject")}
            className={`flex-1 rounded-lg border py-2 text-sm font-medium transition-colors ${mode === "reject" ? "border-error bg-error text-on-error" : "border-outline/30 bg-surface text-on-surface hover:bg-surface-variant/30"}`}
          >
            Reject
          </button>
        </div>

        {mode === "approve" && (
          <div>
            <label className="mb-1 block text-xs text-on-surface-variant">Approved Amount</label>
            <input
              type="number"
              step="1"
              value={approvedAmount}
              onChange={(e) => setApprovedAmount(e.target.value)}
              className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
            />
            {parseFloat(approvedAmount) < claim.claimedAmount && (
              <p className="mt-1 text-xs text-tertiary">
                Partial approval: employee will receive less than claimed.
              </p>
            )}
          </div>
        )}

        {mode === "reject" && (
          <div>
            <label className="mb-1 block text-xs text-on-surface-variant">Reason</label>
            <textarea
              rows={3}
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
        )}

        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={onClose}>
            Cancel
          </Button>
          {mode === "approve" ? (
            <Button
              onClick={() =>
                approve.mutate(
                  { approvedAmount: parseFloat(approvedAmount) },
                  { onSuccess: onClose },
                )
              }
              disabled={approve.isPending || !approvedAmount}
            >
              Confirm Approval
            </Button>
          ) : (
            <Button
              onClick={() => reject.mutate(rejectReason, { onSuccess: onClose })}
              disabled={reject.isPending || !rejectReason}
            >
              Confirm Rejection
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}

function SubmitClaimModal({ onClose }: { onClose: () => void }) {
  const submit = useSubmitReimbursement();
  const [form, setForm] = useState({
    employeeId: "",
    employeeName: "",
    category: "Travel" as ReimbursementCategory,
    claimedAmount: "",
    description: "",
    claimDate: new Date().toISOString().split("T")[0] ?? "",
  });

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    submit.mutate(
      {
        ...form,
        claimedAmount: parseFloat(form.claimedAmount),
      },
      { onSuccess: onClose },
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-md rounded-2xl border border-outline/20 bg-surface p-6 shadow-2xl space-y-3"
      >
        <h2 className="font-semibold text-on-surface">Submit Reimbursement Claim</h2>

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
          value={form.category}
          onChange={(e) => setForm({ ...form, category: e.target.value as ReimbursementCategory })}
          className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
        >
          {CATEGORIES.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </select>
        <input
          required
          type="number"
          step="1"
          placeholder="Amount (INR)"
          value={form.claimedAmount}
          onChange={(e) => setForm({ ...form, claimedAmount: e.target.value })}
          className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
        />
        <textarea
          required
          placeholder="Description / purpose"
          rows={2}
          value={form.description}
          onChange={(e) => setForm({ ...form, description: e.target.value })}
          className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
        />
        <div>
          <label className="mb-1 block text-xs text-on-surface-variant">Claim Date</label>
          <input
            required
            type="date"
            value={form.claimDate}
            onChange={(e) => setForm({ ...form, claimDate: e.target.value })}
            className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>

        <div className="flex justify-end gap-2 pt-1">
          <Button variant="ghost" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" disabled={submit.isPending}>
            {submit.isPending ? "Submitting..." : "Submit"}
          </Button>
        </div>
      </form>
    </div>
  );
}

export default function ReimbursementsPage() {
  const [showSubmit, setShowSubmit] = useState(false);
  const [approvingClaim, setApprovingClaim] = useState<ReimbursementClaimDto | null>(null);
  const { data, isLoading } = useReimbursements();

  return (
    <AppShell>
      <Topbar title="Reimbursements">
        <Button size="sm" onClick={() => setShowSubmit(true)}>
          Submit Claim
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
                <TableHead>Category</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="text-right">Claimed</TableHead>
                <TableHead className="text-right">Approved</TableHead>
                <TableHead>Date</TableHead>
                <TableHead></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {(data?.items ?? []).map((c) => (
                <TableRow key={c.id}>
                  <TableCell className="font-medium">{c.employeeName}</TableCell>
                  <TableCell>{c.category}</TableCell>
                  <TableCell>
                    <StatusBadge status={c.status} />
                    {c.fraudIndicator !== "None" && (
                      <span className="ml-2 text-xs text-error">Fraud flag</span>
                    )}
                  </TableCell>
                  <TableCell className="tabular-nums text-right">
                    {c.claimedAmount.toLocaleString("en-IN", {
                      style: "currency",
                      currency: "INR",
                    })}
                  </TableCell>
                  <TableCell className="tabular-nums text-right">
                    {c.approvedAmount > 0
                      ? c.approvedAmount.toLocaleString("en-IN", {
                          style: "currency",
                          currency: "INR",
                        })
                      : "-"}
                  </TableCell>
                  <TableCell className="text-xs text-on-surface-variant">
                    {new Date(c.createdAtUtc).toLocaleDateString()}
                  </TableCell>
                  <TableCell>
                    {(c.status === "Submitted" || c.status === "UnderReview") && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setApprovingClaim(c)}
                      >
                        Review
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
              {(data?.items ?? []).length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-on-surface-variant py-8">
                    No reimbursement claims
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        )}
      </div>

      {showSubmit && <SubmitClaimModal onClose={() => setShowSubmit(false)} />}
      {approvingClaim && (
        <ApprovePanel claim={approvingClaim} onClose={() => setApprovingClaim(null)} />
      )}
    </AppShell>
  );
}
