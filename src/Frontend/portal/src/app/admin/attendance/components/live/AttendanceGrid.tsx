"use client";

import React, { useState, useEffect, useMemo } from "react";
import { List } from "react-window";
import { useLiveAttendance } from "@/features/attendance/hooks/useAttendance";
import { LiveAttendanceDto } from "@/features/attendance/types";
import { Badge } from "@/design-system/components/Badge";
import { format } from "date-fns";
import { attendanceHub } from "@/lib/signalr/AttendanceHubClient";

/**
 * High-performance virtualized attendance grid.
 * Implements a "Recent Activity" pinned section to handle frequent updates without constant re-sorting.
 */
export const AttendanceGrid = () => {
  const { data: attendanceMap, isLoading } = useLiveAttendance();
  const [recentIds, setRecentIds] = useState<string[]>([]);

  // Capture recent updates to pin them to the top
  useEffect(() => {
    attendanceHub.onAttendanceUpdated((update) => {
      setRecentIds((prev) => {
        const next = [update.employeeId, ...prev.filter(id => id !== update.employeeId)];
        return next.slice(0, 5); // Keep top 5 recent
      });
    });
  }, []);

  // Compute the stable list (excluding recent)
  const stableList = useMemo(() => {
    if (!attendanceMap) return [];
    return Object.values(attendanceMap)
      .filter(emp => !recentIds.includes(emp.employeeId))
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [attendanceMap, recentIds]);

  // Combined list for the virtualized grid
  const recentList = useMemo(() => {
    if (!attendanceMap) return [];
    return recentIds.map(id => attendanceMap[id]).filter((emp): emp is LiveAttendanceDto => !!emp);
  }, [attendanceMap, recentIds]);

  const fullList = useMemo(() => [...recentList, ...stableList], [recentList, stableList]);

  if (isLoading) return <div className="p-12 text-center text-white/40">Loading employees...</div>;

  const Row = ({ index, style }: { index: number; style: React.CSSProperties }) => {
    const emp = fullList[index];
    const isRecent = index < recentList.length;

    if (!emp) return null;

    return (
      <div 
        style={style} 
        className={`flex items-center px-6 border-b border-white/5 transition-colors duration-300 ${
          isRecent ? "bg-primary/5" : ""
        }`}
      >
        <div className="w-1/3 flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-white/10 flex items-center justify-center text-xs font-bold text-white/60">
            {emp.name.charAt(0)}
          </div>
          <span className="font-medium text-white/90">{emp.name}</span>
          {isRecent && <span className="text-[10px] uppercase font-bold text-primary animate-pulse">Live</span>}
        </div>

        <div className="w-1/6">
          <Badge 
            variant={
              emp.status === "Present" ? "success" : 
              emp.status === "Late" ? "warning" : 
              emp.status === "Absent" ? "danger" : "neutral"
            }
          >
            {emp.status}
          </Badge>
        </div>

        <div className="w-1/4 text-sm text-white/60">
          {emp.lastCheckIn ? format(new Date(emp.lastCheckIn), "hh:mm a") : "--:--"}
        </div>

        <div className="w-1/4 text-sm text-white/60">
          {emp.lastCheckOut ? format(new Date(emp.lastCheckOut), "hh:mm a") : "--:--"}
        </div>
      </div>
    );
  };

  return (
    <div className="rounded-2xl border border-white/10 bg-white/5 backdrop-blur-xl overflow-hidden shadow-2xl">
      <div className="flex items-center px-6 py-4 bg-white/5 border-b border-white/10 text-xs font-bold text-white/40 uppercase tracking-widest">
        <div className="w-1/3">Employee</div>
        <div className="w-1/6">Status</div>
        <div className="w-1/4">Last In</div>
        <div className="w-1/4">Last Out</div>
      </div>
      
      <div style={{ height: 600 }}>
        <List
          rowCount={fullList.length}
          rowHeight={64}
          rowComponent={Row as any}
          rowProps={{}}
        />
      </div>
    </div>
  );
};
