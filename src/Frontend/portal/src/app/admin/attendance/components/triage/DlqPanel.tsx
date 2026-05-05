"use client";

import React, { useState, useRef, useEffect } from "react";
import { useDlq, useMapDlq } from "@/features/attendance/hooks/useAttendance";
import { useQuery } from "@tanstack/react-query";
import { fetchEmployees } from "@/features/attendance/api/attendanceApi";
import { format } from "date-fns";
import { Check, AlertCircle, Keyboard } from "lucide-react";

/**
 * High-priority Triage Inbox for unmapped biometric punches.
 * Features keyboard-first workflow and optimistic UI.
 */
export const DlqPanel = () => {
  const { data: dlqItems, isLoading: isLoadingDlq } = useDlq();
  const { data: employees } = useQuery({ queryKey: ["employees", "compact"], queryFn: fetchEmployees });
  const mapMutation = useMapDlq();
  
  const [selectedIndex, setSelectedIndex] = useState(0);
  const selectRefs = useRef<(HTMLSelectElement | null)[]>([]);

  // Keyboard Navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!dlqItems || dlqItems.length === 0) return;

      if (e.key === "ArrowDown") {
        e.preventDefault();
        setSelectedIndex((prev) => Math.min(prev + 1, dlqItems.length - 1));
      } else if (e.key === "ArrowUp") {
        e.preventDefault();
        setSelectedIndex((prev) => Math.max(prev - 1, 0));
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [dlqItems]);

  // Auto-focus selected row's select input
  useEffect(() => {
    selectRefs.current[selectedIndex]?.focus();
  }, [selectedIndex]);

  if (isLoadingDlq) return <div className="p-8 text-white/40">Loading triage items...</div>;
  if (!dlqItems || dlqItems.length === 0) {
    return (
      <div className="p-12 text-center rounded-2xl border-2 border-dashed border-white/5 bg-white/5">
        <Check className="w-12 h-12 text-success mx-auto mb-4 opacity-20" />
        <p className="text-white/40 font-medium">Triage Inbox Clean</p>
        <p className="text-white/20 text-xs mt-1">All biometric punches correctly mapped.</p>
      </div>
    );
  }

  const handleMap = (id: string, employeeId: string) => {
    mapMutation.mutate({ id, employeeId });
    // Move to next item automatically
    if (selectedIndex < dlqItems.length - 1) {
      setSelectedIndex(selectedIndex); // Re-focus logic
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <AlertCircle className="w-5 h-5 text-warning" />
          <h2 className="text-lg font-bold text-white">Triage Inbox (DLQ)</h2>
        </div>
        <div className="flex items-center gap-2 px-3 py-1 rounded-full bg-white/5 border border-white/10 text-[10px] text-white/40">
          <Keyboard className="w-3 h-3" />
          <span>Use Arrow Keys & Type to Search</span>
        </div>
      </div>

      <div className="space-y-2">
        {dlqItems.map((item, index) => (
          <div
            key={item.id}
            className={`group p-4 rounded-xl border transition-all duration-200 flex items-center justify-between ${
              selectedIndex === index 
                ? "bg-white/10 border-primary ring-1 ring-primary/50 shadow-lg shadow-primary/10" 
                : "bg-white/5 border-white/10 hover:bg-white/10"
            }`}
          >
            <div className="flex-1">
              <div className="flex items-center gap-3">
                <span className="text-xs font-mono text-white/40">#{item.biometricId}</span>
                <span className="text-sm font-semibold text-white/90">{item.reason}</span>
              </div>
              <div className="text-[10px] text-white/40 mt-1">
                {item.deviceId} • {format(new Date(item.timestampUtc), "MMM d, hh:mm a")}
              </div>
            </div>

            <div className="flex items-center gap-4">
              <select
                ref={(el) => { selectRefs.current[index] = el; }}
                onFocus={() => setSelectedIndex(index)}
                onChange={(e) => handleMap(item.id, e.target.value)}
                className="bg-background-surface border border-white/10 rounded-lg px-3 py-1.5 text-xs text-white/80 focus:border-primary outline-none min-w-[200px]"
                defaultValue=""
              >
                <option value="" disabled>Select Employee...</option>
                {employees?.map((emp) => (
                  <option key={emp.id} value={emp.id}>{emp.name}</option>
                ))}
              </select>

              <button
                disabled={mapMutation.isPending}
                className="p-2 rounded-lg bg-success/20 text-success border border-success/30 hover:bg-success/30 transition-colors disabled:opacity-50"
              >
                <Check className="w-4 h-4" />
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
