"use client";

import { useState, useEffect } from "react";
import { AppShell } from "@/components/shell/app-shell";
import { Topbar } from "@/components/shell/topbar";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useLeaveBalances, useLeavePolicies, useCalculateLeave, useSubmitLeaveRequest, useMyLeaves } from "@/lib/hooks/use-time";
import { LeaveRequestStatus } from "@/lib/types/time";

/**
 * Employee Leave Self-Service Page.
 * Allows employees to track their leave balances and submit new requests.
 */
export default function LeavePage() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const { data: balances, isLoading: isBalancesLoading } = useLeaveBalances();
  const { data: policies } = useLeavePolicies();
  const { data: myLeaves } = useMyLeaves();

  return (
    <AppShell>
      <Topbar title="Time Off">
        <Button variant="primary" size="sm" onClick={() => setIsModalOpen(true)}>
          <span className="material-symbols-outlined text-[16px]">add</span>
          Request Time Off
        </Button>
      </Topbar>

      {/* KPI Cards: Balances Overview */}
      <div className="mt-6 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 animate-fade-in">
        {isBalancesLoading ? (
          [1, 2, 3, 4].map((i) => (
            <div key={i} className="h-32 bg-surface-container-low animate-pulse rounded-lg border border-outline-variant" />
          ))
        ) : (
          balances?.map((balance) => {
            const policy = policies?.find((p) => p.id === balance.policyId);
            const percent = (balance.usedBalance / balance.totalEntitlement) * 100;
            return (
              <Card key={balance.id} className="p-5 border-outline-variant bg-surface-container-low hover:bg-surface-container transition-colors cursor-default group">
                <div className="text-caps text-on-surface-variant mb-2 group-hover:text-primary transition-colors">
                  {policy?.name || "Leave Balance"}
                </div>
                <div className="flex items-baseline gap-1.5">
                  <span className="text-display font-bold text-on-surface leading-none tracking-tighter">
                    {balance.remainingBalance}
                  </span>
                  <span className="text-body text-on-surface-variant">days left</span>
                </div>
                <div className="mt-5 h-1 w-full bg-surface-container-highest rounded-full overflow-hidden">
                  <div
                    className="h-full bg-primary transition-all duration-1000 ease-out"
                    style={{ width: `${Math.min(100, percent)}%` }}
                  />
                </div>
                <div className="mt-2 flex justify-between text-[10px] font-bold uppercase tracking-widest text-on-surface-variant/60">
                   <span>{balance.usedBalance} used</span>
                   <span>{balance.totalEntitlement} total</span>
                </div>
              </Card>
            );
          })
        )}
      </div>

      {/* Requests History */}
      <section className="mt-12 animate-fade-in fade-in-up">
        <div className="flex items-center gap-2 mb-4">
          <span className="material-symbols-outlined text-on-surface-variant">history</span>
          <h3 className="text-h2 font-medium text-on-surface">Request History</h3>
        </div>
        <div className="overflow-hidden rounded-xl border border-outline bg-surface shadow-sm">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Leave Type</TableHead>
                <TableHead>Dates</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Submitted On</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {myLeaves && myLeaves.length > 0 ? (
                myLeaves.map((leave) => {
                  const policy = policies?.find((p) => p.id === leave.policyId);
                  const statusLabel = {
                    [LeaveRequestStatus.Pending]: 'Pending',
                    [LeaveRequestStatus.Approved]: 'Approved',
                    [LeaveRequestStatus.Rejected]: 'Rejected',
                    [LeaveRequestStatus.Cancelled]: 'Cancelled',
                  }[leave.status] ?? 'Unknown';
                  return (
                    <TableRow key={leave.id}>
                      <TableCell>{policy?.name ?? 'Leave'}</TableCell>
                      <TableCell>
                        {new Date(leave.startDate).toLocaleDateString()} – {new Date(leave.endDate).toLocaleDateString()}
                      </TableCell>
                      <TableCell>{statusLabel}</TableCell>
                      <TableCell>{new Date(leave.requestedOnUtc).toLocaleDateString()}</TableCell>
                    </TableRow>
                  );
                })
              ) : (
                <TableRow>
                  <TableCell colSpan={4} className="py-24 text-center text-on-surface-variant italic">
                    <div className="flex flex-col items-center gap-2">
                      <span className="material-symbols-outlined text-4xl text-outline-variant">event_busy</span>
                      <p>No leave requests found. Click "Request Time Off" to begin.</p>
                    </div>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      </section>

      {isModalOpen && (
        <RequestLeaveModal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} />
      )}
    </AppShell>
  );
}

/**
 * Modal for submitting a new leave request.
 * Integrates with the dry-run calculation endpoint for real-time feedback.
 */
function RequestLeaveModal({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const { data: policies } = useLeavePolicies();
  const calculate = useCalculateLeave();
  const submit = useSubmitLeaveRequest();

  const [policyId, setPolicyId] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [reason, setReason] = useState("");
  const [calculatedDays, setCalculatedDays] = useState<number | null>(null);

  // Auto-calculate as user changes dates
  useEffect(() => {
    if (policyId && startDate && endDate) {
      calculate.mutate({ policyId, startDate, endDate }, {
        onSuccess: (data) => setCalculatedDays(data.actualDays)
      });
    }
  }, [policyId, startDate, endDate, calculate.mutate]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await submit.mutateAsync({ policyId, startDate, endDate, reason });
      onClose();
    } catch (err: any) {
      // In a real app, we'd use a toast notification here
      alert(err.message || "Failed to submit request.");
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-fade-in">
      <div className="relative w-full max-w-lg rounded-2xl bg-surface border border-outline p-8 shadow-2xl overflow-hidden">
        <div className="mb-8 flex items-center justify-between">
          <h2 className="text-h1 font-bold text-on-surface tracking-tight">Request Time Off</h2>
          <button onClick={onClose} className="text-on-surface-variant hover:text-primary transition-colors p-1">
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="space-y-1.5">
            <label className="text-caps text-on-surface-variant">Leave Type</label>
            <select
              required
              className="w-full h-11 px-4 bg-surface-container-low border border-outline rounded text-body text-on-surface focus:border-primary focus:outline-none appearance-none cursor-pointer"
              value={policyId}
              onChange={(e) => setPolicyId(e.target.value)}
            >
              <option value="">Select a policy...</option>
              {policies?.map((p) => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <label className="text-caps text-on-surface-variant">Start Date</label>
              <input
                type="date"
                required
                className="w-full h-11 px-4 bg-surface-container-low border border-outline rounded text-body text-on-surface focus:border-primary focus:outline-none"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
              />
            </div>
            <div className="space-y-1.5">
              <label className="text-caps text-on-surface-variant">End Date</label>
              <input
                type="date"
                required
                className="w-full h-11 px-4 bg-surface-container-low border border-outline rounded text-body text-on-surface focus:border-primary focus:outline-none"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
              />
            </div>
          </div>

          {calculatedDays !== null && (
            <div className="p-4 bg-surface-bright/10 border border-primary/20 rounded-lg flex items-center justify-between animate-fade-in">
              <div className="flex items-center gap-2">
                <span className="material-symbols-outlined text-primary text-[20px]">info</span>
                <span className="text-body text-on-surface-variant">Estimated deduction:</span>
              </div>
              <span className="text-h2 font-bold text-primary">{calculatedDays} {calculatedDays === 1 ? "Day" : "Days"}</span>
            </div>
          )}

          <div className="space-y-1.5">
            <label className="text-caps text-on-surface-variant">Reason (Optional)</label>
            <textarea
              rows={3}
              placeholder="Provide a brief reason for your leave..."
              className="w-full p-4 bg-surface-container-low border border-outline rounded text-body text-on-surface focus:border-primary focus:outline-none resize-none"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
            />
          </div>

          <div className="mt-10 flex justify-end gap-3 pt-4 border-t border-outline-variant">
            <Button variant="secondary" onClick={onClose} type="button">
              Cancel
            </Button>
            <Button 
              variant="primary" 
              type="submit" 
              disabled={submit.isPending || (calculatedDays !== null && calculatedDays <= 0)}
            >
              {submit.isPending ? (
                <span className="flex items-center gap-2 italic">
                   <span className="material-symbols-outlined animate-spin text-[16px]">refresh</span>
                   Submitting...
                </span>
              ) : "Submit Request"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
