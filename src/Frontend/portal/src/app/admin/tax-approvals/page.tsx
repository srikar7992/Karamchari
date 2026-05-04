"use client";

import { useState, useEffect, useCallback } from "react";
import { AppShell } from "@/components/shell/app-shell";
import { Topbar } from "@/components/shell/topbar";
import { Button } from "@/components/ui/button";
import { 
  usePendingDeclarations, 
  useApproveDeclaration, 
  useRejectDeclaration, 
  getDeclarationDocumentUrl 
} from "@/lib/hooks/use-admin";
import { formatCurrency } from "@/lib/utils/format";

export default function TaxApprovalsPage() {
  const { data: queue, isLoading } = usePendingDeclarations();
  const { mutate: approve, isPending: isApproving } = useApproveDeclaration();
  const { mutate: reject, isPending: isRejecting } = useRejectDeclaration();

  const [currentIndex, setCurrentIndex] = useState(0);
  const current = queue?.[currentIndex];

  const handleNext = useCallback(() => {
    if (queue && currentIndex < queue.length - 1) {
      setCurrentIndex(prev => prev + 1);
    } else if (queue && queue.length > 0) {
      // If we finished the current batch, we might want to refresh
      // but for now just stay at the end or show empty state
    }
  }, [queue, currentIndex]);

  const handleApprove = () => {
    if (!current) return;
    const amount = (document.getElementById("approvedAmount") as HTMLInputElement)?.valueAsNumber;
    approve({ id: current.id, amount }, {
      onSuccess: () => {
        // Since we invalidate the query, the queue will refresh.
        // We might want to just move to next and let background refresh happen.
        handleNext();
      }
    });
  };

  const handleReject = () => {
    if (!current) return;
    const reason = prompt("Enter rejection reason (e.g. Invalid document, Outside FY):");
    if (reason) {
      reject({ id: current.id, reason }, {
        onSuccess: () => handleNext()
      });
    }
  };

  // Keyboard Shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) return;

      if (e.key.toLowerCase() === "a") handleApprove();
      if (e.key.toLowerCase() === "r") handleReject();
      if (e.key === "ArrowRight") handleNext();
      if (e.key === "ArrowLeft") setCurrentIndex(prev => Math.max(0, prev - 1));
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [current, handleApprove, handleReject, handleNext]);

  if (isLoading) return <AppShell><div className="p-8 text-center">Loading queue...</div></AppShell>;
  
  if (!current) return (
    <AppShell>
      <Topbar title="Tax Approvals" />
      <div className="flex flex-col items-center justify-center h-[60vh] text-on-surface-variant">
        <span className="material-symbols-outlined text-6xl mb-4 text-primary/20">task_alt</span>
        <h2 className="text-xl font-medium">All caught up!</h2>
        <p>No pending declarations for review.</p>
      </div>
    </AppShell>
  );

  return (
    <AppShell>
      <Topbar title="Tax Proof Verification">
        <div className="flex items-center gap-4 text-sm font-medium text-on-surface-variant">
          <span>{currentIndex + 1} / {queue.length} items</span>
          <div className="flex gap-1">
            <Button variant="ghost" size="sm" onClick={() => setCurrentIndex(prev => Math.max(0, prev - 1))} disabled={currentIndex === 0}>
              <span className="material-symbols-outlined">chevron_left</span>
            </Button>
            <Button variant="ghost" size="sm" onClick={handleNext} disabled={currentIndex === queue.length - 1}>
              <span className="material-symbols-outlined">chevron_right</span>
            </Button>
          </div>
        </div>
      </Topbar>

      <div className="flex h-[calc(100vh-120px)] -m-margin border-t border-outline">
        {/* Left: Document Preview */}
        <div className="w-2/3 bg-surface-container-low relative">
          <iframe 
            src={getDeclarationDocumentUrl(current.id)} 
            className="w-full h-full border-none"
            title="Proof Document"
          />
          {/* Zoom/Rotate controls would go here */}
        </div>

        {/* Right: Verification Pane */}
        <div className="w-1/3 bg-surface border-l border-outline overflow-y-auto">
          <div className="p-6 space-y-8">
            <section>
              <h3 className="text-xs font-bold text-primary uppercase tracking-wider mb-4">Employee Information</h3>
              <div className="space-y-1">
                <div className="text-lg font-semibold text-on-surface">Employee ID: {current.employeeId}</div>
                <div className="text-sm text-on-surface-variant">Submitted on {new Date(current.submittedAt).toLocaleDateString()}</div>
              </div>
            </section>

            <section className="space-y-6">
              <h3 className="text-xs font-bold text-primary uppercase tracking-wider">Verification Details</h3>
              
              <div className="rounded-xl border border-outline p-4 bg-surface-container-highest/30">
                <div className="text-sm text-on-surface-variant mb-1">Declared Category</div>
                <div className="text-xl font-bold text-on-surface">{current.category}</div>
              </div>

              <div className="space-y-4">
                <div className="space-y-2">
                  <label className="text-sm font-medium text-on-surface">Approved Amount (INR)</label>
                  <input 
                    id="approvedAmount"
                    type="number" 
                    defaultValue={current.claimedAmount}
                    className="w-full h-12 bg-surface border border-outline rounded-xl px-4 text-xl font-bold text-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                  />
                  <div className="text-xs text-on-surface-variant">Employee Claimed: {formatCurrency(current.claimedAmount)}</div>
                </div>
              </div>
            </section>

            <div className="pt-8 grid grid-cols-2 gap-4 sticky bottom-0 bg-surface pb-6 border-t border-outline/10">
              <Button 
                variant="primary" 
                className="bg-green-600 hover:bg-green-700 text-white border-none h-14"
                onClick={handleApprove}
                disabled={isApproving || isRejecting}
              >
                <span className="material-symbols-outlined">check_circle</span>
                Approve (A)
              </Button>
              <Button 
                variant="secondary" 
                className="text-red-600 border-red-200 hover:bg-red-50 h-14"
                onClick={handleReject}
                disabled={isApproving || isRejecting}
              >
                <span className="material-symbols-outlined">cancel</span>
                Reject (R)
              </Button>
            </div>
            
            <div className="text-[10px] text-center text-on-surface-variant italic">
              Tip: Use Keyboard shortcuts "A" to Approve and "R" to Reject.
            </div>
          </div>
        </div>
      </div>
    </AppShell>
  );
}
