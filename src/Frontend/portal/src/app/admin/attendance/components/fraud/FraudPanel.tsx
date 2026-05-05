"use client";

import React from "react";
import { useFraudFlags, useReviewFraud } from "@/features/attendance/hooks/useAttendance";
import { Badge } from "@/design-system/components/Badge";
import { format } from "date-fns";
import { ShieldAlert, Check, X, MessageSquare } from "lucide-react";

/**
 * Fraud Inbox for reviewing detected anomalies.
 * Features inline actions to minimize context switching.
 */
export const FraudPanel = () => {
  const { data: fraudFlags, isLoading } = useFraudFlags();
  const reviewMutation = useReviewFraud();

  if (isLoading) return <div className="p-8 text-white/40">Scanning for anomalies...</div>;
  if (!fraudFlags || fraudFlags.length === 0) {
    return (
      <div className="p-12 text-center rounded-2xl border border-white/5 bg-white/5">
        <p className="text-white/20 text-sm">No fraud alerts detected.</p>
      </div>
    );
  }

  const handleReview = (id: string, status: string) => {
    const notes = prompt("Enter review notes (optional):") || "Reviewed via dashboard";
    reviewMutation.mutate({ id, status, notes });
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 mb-4">
        <ShieldAlert className="w-5 h-5 text-danger" />
        <h2 className="text-lg font-bold text-white">Fraud & Anomalies</h2>
      </div>

      <div className="overflow-hidden rounded-xl border border-white/10 bg-white/5">
        <table className="w-full text-left">
          <thead className="bg-white/5 text-[10px] uppercase font-bold text-white/40">
            <tr>
              <th className="px-4 py-3">Employee</th>
              <th className="px-4 py-3">Type</th>
              <th className="px-4 py-3">Severity</th>
              <th className="px-4 py-3">Detected</th>
              <th className="px-4 py-3 text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/5">
            {fraudFlags.map((flag) => (
              <tr key={flag.id} className="hover:bg-white/5 transition-colors">
                <td className="px-4 py-4 font-medium text-white/90">{flag.employeeName || "Unknown"}</td>
                <td className="px-4 py-4 text-xs text-white/60">{flag.type}</td>
                <td className="px-4 py-4">
                  <Badge 
                    variant={flag.severityScore > 70 ? "danger" : flag.severityScore > 40 ? "warning" : "info"}
                  >
                    {flag.severityScore}
                  </Badge>
                </td>
                <td className="px-4 py-4 text-xs text-white/40">
                  {format(new Date(flag.detectedAtUtc), "MMM d, HH:mm")}
                </td>
                <td className="px-4 py-4 text-right">
                  <div className="flex items-center justify-end gap-2">
                    <button
                      onClick={() => handleReview(flag.id, "Dismissed")}
                      className="p-1.5 rounded-lg hover:bg-white/10 text-white/40 hover:text-white transition-colors"
                      title="Mark as Valid"
                    >
                      <Check className="w-4 h-4" />
                    </button>
                    <button
                      onClick={() => handleReview(flag.id, "Confirmed")}
                      className="p-1.5 rounded-lg hover:bg-white/10 text-danger/40 hover:text-danger transition-colors"
                      title="Confirm Fraud"
                    >
                      <X className="w-4 h-4" />
                    </button>
                    <button
                      className="p-1.5 rounded-lg hover:bg-white/10 text-white/20 hover:text-white transition-colors"
                      title="Add Note"
                    >
                      <MessageSquare className="w-4 h-4" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
