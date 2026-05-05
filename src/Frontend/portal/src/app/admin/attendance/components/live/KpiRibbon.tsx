"use client";

import React from "react";
import { useAttendanceKpis } from "@/features/attendance/hooks/useAttendance";
import { colors } from "@/design-system/tokens/colors";
import { Users, CheckCircle, Clock, AlertCircle } from "lucide-react";

/**
 * High-performance KPI ribbon for attendance monitoring.
 */
export const KpiRibbon = () => {
  const { data, isLoading } = useAttendanceKpis();

  const kpis = [
    {
      label: "Total Employees",
      value: data?.total ?? 0,
      icon: <Users className="w-5 h-5" />,
      color: colors.info.DEFAULT,
    },
    {
      label: "Present",
      value: data?.present ?? 0,
      icon: <CheckCircle className="w-5 h-5" />,
      color: colors.success.DEFAULT,
    },
    {
      label: "Late Arrivals",
      value: data?.late ?? 0,
      icon: <Clock className="w-5 h-5" />,
      color: colors.warning.DEFAULT,
    },
    {
      label: "Absent",
      value: data?.absent ?? 0,
      icon: <AlertCircle className="w-5 h-5" />,
      color: colors.danger.DEFAULT,
    },
  ];

  return (
    <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
      {kpis.map((kpi) => (
        <div
          key={kpi.label}
          className="relative overflow-hidden p-6 rounded-2xl border border-white/10 bg-white/5 backdrop-blur-xl transition-all duration-300 hover:bg-white/10"
        >
          {/* Accent Glow */}
          <div 
            className="absolute -top-4 -right-4 w-16 h-16 blur-2xl opacity-20"
            style={{ backgroundColor: kpi.color }}
          />

          <div className="flex items-center gap-4">
            <div 
              className="p-3 rounded-xl"
              style={{ backgroundColor: `${kpi.color}15`, color: kpi.color }}
            >
              {kpi.icon}
            </div>
            <div>
              <p className="text-xs font-medium text-white/50 uppercase tracking-wider">
                {kpi.label}
              </p>
              <p className="text-2xl font-bold text-white mt-0.5">
                {isLoading ? "..." : kpi.value.toLocaleString()}
              </p>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};
