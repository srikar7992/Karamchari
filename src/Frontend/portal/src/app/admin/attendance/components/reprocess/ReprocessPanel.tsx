"use client";

import React, { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { triggerReprocess, fetchJobProgress } from "@/features/attendance/api/attendanceApi";
import { RefreshCw, Calendar, Users, AlertTriangle, Loader2 } from "lucide-react";

/**
 * Reprocessing Panel for bulk historical attendance corrections.
 * Features job tracking and impact warnings.
 */
export const ReprocessPanel = () => {
  const [fromDate, setFromDate] = useState<string>(new Date().toISOString().split("T")[0] || "");
  const [toDate, setToDate] = useState<string>(new Date().toISOString().split("T")[0] || "");
  const [employeeId, setEmployeeId] = useState("");
  const [activeJobId, setActiveJobId] = useState<string | null>(null);

  const reprocessMutation = useMutation({
    mutationFn: triggerReprocess,
    onSuccess: (data) => {
      setActiveJobId(data.jobId);
    },
  });

  const { data: jobStatus } = useQuery({
    queryKey: ["reprocess", "job", activeJobId],
    queryFn: () => fetchJobProgress(activeJobId!),
    enabled: !!activeJobId,
    refetchInterval: (query) => {
      if (query.state.data?.status === "Completed" || query.state.data?.status === "Failed") {
        return false;
      }
      return 2000;
    },
  });

  const handleTrigger = () => {
    if (!fromDate || !toDate) return;
    if (confirm(`Warning: This will re-evaluate attendance logic for the selected range. This may affect payroll calculations. Proceed?`)) {
      reprocessMutation.mutate({ fromDate, toDate, employeeId: employeeId || undefined });
    }
  };

  return (
    <div className="p-6 rounded-2xl border border-white/10 bg-white/5 backdrop-blur-xl">
      <div className="flex items-center gap-2 mb-6">
        <RefreshCw className="w-5 h-5 text-primary" />
        <h2 className="text-lg font-bold text-white">Bulk Reprocessing</h2>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <div className="space-y-1.5">
          <label className="text-[10px] uppercase font-bold text-white/40 flex items-center gap-1.5">
            <Calendar className="w-3 h-3" /> Start Date
          </label>
          <input
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
            className="w-full bg-white/5 border border-white/10 rounded-lg px-3 py-2 text-sm text-white outline-none focus:border-primary"
          />
        </div>

        <div className="space-y-1.5">
          <label className="text-[10px] uppercase font-bold text-white/40 flex items-center gap-1.5">
            <Calendar className="w-3 h-3" /> End Date
          </label>
          <input
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
            className="w-full bg-white/5 border border-white/10 rounded-lg px-3 py-2 text-sm text-white outline-none focus:border-primary"
          />
        </div>

        <div className="space-y-1.5">
          <label className="text-[10px] uppercase font-bold text-white/40 flex items-center gap-1.5">
            <Users className="w-3 h-3" /> Employee (Optional)
          </label>
          <input
            type="text"
            placeholder="All Employees"
            value={employeeId}
            onChange={(e) => setEmployeeId(e.target.value)}
            className="w-full bg-white/5 border border-white/10 rounded-lg px-3 py-2 text-sm text-white outline-none focus:border-primary"
          />
        </div>
      </div>

      {activeJobId && jobStatus && (
        <div className="mb-6 p-4 rounded-xl bg-primary/10 border border-primary/20 flex items-center justify-between">
          <div className="flex items-center gap-3">
            {jobStatus.status === "Completed" ? (
              <RefreshCw className="w-5 h-5 text-success" />
            ) : jobStatus.status === "Failed" ? (
              <AlertTriangle className="w-5 h-5 text-danger" />
            ) : (
              <Loader2 className="w-5 h-5 text-primary animate-spin" />
            )}
            <div>
              <p className="text-sm font-bold text-white">Job {jobStatus.status}</p>
              <p className="text-xs text-white/60">Processing {jobStatus.progress}% complete</p>
            </div>
          </div>
          <button 
            onClick={() => setActiveJobId(null)}
            className="text-xs text-white/40 hover:text-white"
          >
            Clear
          </button>
        </div>
      )}

      <button
        onClick={handleTrigger}
        disabled={reprocessMutation.isPending || (activeJobId !== null && jobStatus?.status === "Processing")}
        className="w-full py-3 rounded-xl bg-primary text-white font-bold text-sm shadow-lg shadow-primary/20 hover:bg-primary-light transition-all disabled:opacity-50"
      >
        {reprocessMutation.isPending ? "Queuing..." : "Trigger Replay Engine"}
      </button>
    </div>
  );
};
