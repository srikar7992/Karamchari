"use client";

import React from "react";
import { KpiRibbon } from "./components/live/KpiRibbon";
import { AttendanceGrid } from "./components/live/AttendanceGrid";
import { DlqPanel } from "./components/triage/DlqPanel";
import { FraudPanel } from "./components/fraud/FraudPanel";
import { ReprocessPanel } from "./components/reprocess/ReprocessPanel";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Activity, Inbox, ShieldAlert, RotateCcw } from "lucide-react";

/**
 * The IoT Workforce Command Center - Operational Dashboard.
 * Optimized for high-throughput biometric attendance monitoring and triage.
 */
export default function AttendanceDashboardPage() {
  return (
    <div className="min-h-screen bg-[#090e1a] text-white p-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-extrabold tracking-tight bg-gradient-to-r from-white to-white/40 bg-clip-text text-transparent">
          Workforce Command Center
        </h1>
        <p className="text-white/40 mt-1 font-medium">
          Real-time biometric ingestion & operational triage control plane.
        </p>
      </div>

      <KpiRibbon />

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8">
        {/* Main Feed */}
        <div className="lg:col-span-8">
          <div className="flex items-center gap-2 mb-4 px-1">
            <Activity className="w-4 h-4 text-primary" />
            <h2 className="text-sm font-bold uppercase tracking-widest text-white/60">Live Presence Feed</h2>
          </div>
          <AttendanceGrid />
        </div>

        {/* Action Sidebar */}
        <div className="lg:col-span-4 space-y-8">
          <Tabs defaultValue="triage" className="w-full">
            <TabsList className="grid w-full grid-cols-2 bg-white/5 border border-white/10 p-1 rounded-xl mb-6">
              <TabsTrigger value="triage" className="flex items-center gap-2 py-2 text-xs font-bold rounded-lg data-[state=active]:bg-white/10 data-[state=active]:text-white">
                <Inbox className="w-3.5 h-3.5" /> Triage
              </TabsTrigger>
              <TabsTrigger value="fraud" className="flex items-center gap-2 py-2 text-xs font-bold rounded-lg data-[state=active]:bg-white/10 data-[state=active]:text-white">
                <ShieldAlert className="w-3.5 h-3.5" /> Fraud
              </TabsTrigger>
            </TabsList>
            
            <TabsContent value="triage" className="mt-0">
              <DlqPanel />
            </TabsContent>
            
            <TabsContent value="fraud" className="mt-0">
              <FraudPanel />
            </TabsContent>
          </Tabs>

          <div className="border-t border-white/5 pt-8">
            <div className="flex items-center gap-2 mb-4 px-1">
              <RotateCcw className="w-4 h-4 text-primary" />
              <h2 className="text-sm font-bold uppercase tracking-widest text-white/60">System Operations</h2>
            </div>
            <ReprocessPanel />
          </div>
        </div>
      </div>
    </div>
  );
}
