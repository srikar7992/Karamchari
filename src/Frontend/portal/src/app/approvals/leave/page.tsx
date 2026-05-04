"use client";

import { AppShell } from "@/components/shell/app-shell";
import { Topbar } from "@/components/shell/topbar";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { 
  usePendingLeaveRequests, 
  useApproveLeaveRequest, 
  useRejectLeaveRequest, 
  useLeavePolicies 
} from "@/lib/hooks/use-time";
import { useQueryClient } from "@tanstack/react-query";

/**
 * Manager Approval Dashboard.
 * Focused triage inbox for leave requests using Optimistic UI updates.
 */
export default function ManagerApprovalPage() {
  const { data: requests, isLoading } = usePendingLeaveRequests();
  const { data: policies } = useLeavePolicies();
  
  const approve = useApproveLeaveRequest();
  const reject = useRejectLeaveRequest();
  const queryClient = useQueryClient();

  /**
   * Approves a request with Optimistic UI.
   */
  const handleApprove = async (id: string) => {
    // 1. Snapshot previous state
    const previous = queryClient.getQueryData(["leave-requests", "pending"]);
    
    // 2. Optimistically remove from list
    queryClient.setQueryData(["leave-requests", "pending"], (old: any) => 
      old?.filter((r: any) => r.id !== id)
    );

    try {
      await approve.mutateAsync(id);
    } catch (err) {
      // 3. Rollback if API fails
      queryClient.setQueryData(["leave-requests", "pending"], previous);
      alert("Failed to approve leave request.");
    }
  };

  /**
   * Rejects a request with Optimistic UI.
   */
  const handleReject = async (id: string) => {
    const previous = queryClient.getQueryData(["leave-requests", "pending"]);
    
    queryClient.setQueryData(["leave-requests", "pending"], (old: any) => 
      old?.filter((r: any) => r.id !== id)
    );

    try {
      await reject.mutateAsync(id);
    } catch (err) {
      queryClient.setQueryData(["leave-requests", "pending"], previous);
      alert("Failed to reject leave request.");
    }
  };

  return (
    <AppShell>
      <Topbar title="Approval Inbox">
        <div className="flex items-center gap-2 text-on-surface-variant bg-surface-container px-3 py-1.5 rounded-full border border-outline-variant">
           <span className="material-symbols-outlined text-[18px] text-primary">notification_important</span>
           <span className="text-caps font-bold tracking-widest">{requests?.length || 0} Pending</span>
        </div>
      </Topbar>

      <section className="mt-8 max-w-4xl mx-auto space-y-4 animate-fade-in">
        {isLoading ? (
          [1, 2, 3].map((i) => (
            <div key={i} className="h-40 bg-surface-container-low rounded-lg animate-pulse border border-outline-variant" />
          ))
        ) : (
          requests?.map((request) => {
            const policy = policies?.find((p) => p.id === request.policyId);
            return (
              <Card key={request.id} className="p-0 border-outline-variant bg-surface-container-low overflow-hidden flex flex-col md:flex-row hover:border-primary/20 transition-colors group">
                {/* Info Zone */}
                <div className="p-6 flex-1 border-b md:border-b-0 md:border-r border-outline-variant">
                  <div className="flex items-center justify-between mb-6">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-lg bg-surface-bright flex items-center justify-center text-primary border border-outline-variant group-hover:border-primary/40 transition-colors">
                         <span className="material-symbols-outlined">person</span>
                      </div>
                      <div>
                        <div className="text-body font-bold text-on-surface tracking-tight">Employee Name</div>
                        <div className="text-caps text-on-surface-variant group-hover:text-primary transition-colors">
                          {policy?.name || "Leave Request"}
                        </div>
                      </div>
                    </div>
                    <div className="text-right">
                       <div className="text-h1 font-bold text-primary tracking-tighter leading-none">{request.actualDays}</div>
                       <div className="text-caps text-on-surface-variant font-black">Days</div>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-8 py-5 border-t border-outline-variant/30">
                    <div>
                      <div className="text-caps text-on-surface-variant/50 mb-1">Start Date</div>
                      <div className="text-body font-medium text-on-surface">
                        {new Date(request.startDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })}
                      </div>
                    </div>
                    <div>
                      <div className="text-caps text-on-surface-variant/50 mb-1">End Date</div>
                      <div className="text-body font-medium text-on-surface">
                        {new Date(request.endDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })}
                      </div>
                    </div>
                  </div>

                  {request.reason && (
                    <div className="mt-4 p-4 bg-surface-container rounded-lg italic text-on-surface-variant text-sm border border-outline-variant/20 relative overflow-hidden">
                      <div className="absolute top-0 left-0 w-1 h-full bg-primary/20" />
                      "{request.reason}"
                    </div>
                  )}
                </div>

                {/* Action Zone */}
                <div className="w-full md:w-56 bg-surface-container/50 p-6 flex flex-col justify-center gap-3 bg-surface-container-low border-l border-outline-variant/10">
                   <Button 
                    variant="primary" 
                    className="w-full h-11 bg-primary text-on-primary hover:bg-primary-container transition-all shadow-lg shadow-white/5 font-bold"
                    onClick={() => handleApprove(request.id)}
                    disabled={approve.isPending || reject.isPending}
                   >
                     Approve
                   </Button>
                   <Button 
                    variant="secondary" 
                    className="w-full h-11 border-outline-variant text-on-surface-variant hover:bg-error/10 hover:text-error hover:border-error/20 transition-all font-bold"
                    onClick={() => handleReject(request.id)}
                    disabled={approve.isPending || reject.isPending}
                   >
                     Reject
                   </Button>
                </div>
              </Card>
            );
          })
        )}

        {requests?.length === 0 && !isLoading && (
          <div className="py-40 text-center flex flex-col items-center gap-6 animate-fade-in">
            <div className="w-20 h-20 rounded-full bg-surface-container flex items-center justify-center text-on-surface-variant/20 border-2 border-dashed border-outline-variant/30">
               <span className="material-symbols-outlined text-5xl">task_alt</span>
            </div>
            <div className="space-y-1">
              <h3 className="text-display font-bold text-on-surface tracking-tighter">Inbox Zero</h3>
              <p className="text-body-lg text-on-surface-variant">You've processed all pending leave requests.</p>
            </div>
          </div>
        )}
      </section>
    </AppShell>
  );
}
