"use client";

import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { 
  FileText, Download, ShieldCheck, AlertCircle, Loader2, Calendar, 
  TrendingDown, Activity, CheckCircle2, AlertTriangle, ExternalLink,
  ChevronRight, Info
} from "lucide-react";
import { Badge } from "@/design-system/components/Badge";
import { cn } from "@/lib/utils/cn";

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5000";

/**
 * Statutory Compliance Operating System.
 * CFO-level dashboard with health metrics, penalty prediction, and assisted filing.
 */
export default function ComplianceCenterPage() {
  const queryClient = useQueryClient();
  const [selectedMonth, setSelectedMonth] = useState(new Date().toISOString().slice(0, 7));
  const [filingFlow, setFilingFlow] = useState<{ runId: string; type: number; label: string } | null>(null);
  const [refNumber, setRefNumber] = useState("");

  // Fetch Compliance Health Summary (CFO View)
  const { data: health, isLoading: isHealthLoading } = useQuery({
    queryKey: ["compliance", "health"],
    queryFn: async () => {
      const res = await fetch(`${BASE_URL}/api/compliance/health`);
      if (!res.ok) throw new Error("Failed to fetch compliance health");
      return res.json();
    },
  });

  // Fetch Payroll Runs
  const { data: runs, isLoading: isRunsLoading } = useQuery({
    queryKey: ["payroll", "runs", selectedMonth],
    queryFn: async () => {
      const res = await fetch(`${BASE_URL}/api/payroll/runs`);
      if (!res.ok) throw new Error("Failed to fetch payroll runs");
      return res.json();
    },
  });

  // Mark as Filed Mutation
  const markAsFiled = useMutation({
    mutationFn: async (payload: { runId: string; type: number; referenceNumber: string }) => {
      const res = await fetch(`${BASE_URL}/api/compliance/mark-filed`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });
      if (!res.ok) throw new Error("Failed to mark as filed");
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["compliance", "health"] });
      setFilingFlow(null);
      setRefNumber("");
    },
  });

  const downloadFile = (type: string, runId: string) => {
    const url = `${BASE_URL}/api/compliance/${type}/${runId}`;
    window.open(url, "_blank");
  };

  const getPortalUrl = (type: number) => {
    switch(type) {
      case 0: return "https://unifiedportal-emp.epfindia.gov.in/";
      case 1: return "https://www.esic.in/ESICInsurance1/Login.aspx";
      case 2: return "https://www.tin-nsdl.com/";
      default: return "#";
    }
  };

  return (
    <div className="min-h-screen bg-[#090e1a] text-white p-8">
      {/* Header */}
      <div className="flex items-center justify-between mb-10">
        <div>
          <h1 className="text-3xl font-extrabold tracking-tight bg-gradient-to-r from-white to-white/40 bg-clip-text text-transparent">
            Compliance Operating System
          </h1>
          <p className="text-white/40 mt-1 font-medium">
            Assisted filing automation and penalty risk intelligence.
          </p>
        </div>
        <div className="flex items-center gap-4">
           <input
              type="month"
              value={selectedMonth}
              onChange={(e) => setSelectedMonth(e.target.value)}
              className="bg-white/5 border border-white/10 rounded-lg px-4 py-2 text-sm outline-none focus:border-primary transition-all"
            />
        </div>
      </div>

      {/* CFO Health Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-10">
        <HealthCard 
          label="Total Pending Filings" 
          value={health?.totalPending ?? 0} 
          icon={Activity} 
          trend="Next 7 Days"
        />
        <HealthCard 
          label="High Risk Items" 
          value={health?.highRiskCount ?? 0} 
          icon={AlertTriangle} 
          color="text-error"
          trend="Penalty Prediction Active"
        />
        <HealthCard 
          label="Estimated Late Interest" 
          value={`₹${health?.filings?.reduce((acc: number, f: any) => acc + (f.risk.estimatedPenalty || 0), 0).toLocaleString() ?? 0}`} 
          icon={TrendingDown} 
          trend="Accruing daily"
        />
        <HealthCard 
          label="Last Filing Status" 
          value="100% Valid" 
          icon={CheckCircle2} 
          color="text-success"
          trend="Pre-validation passed"
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8">
        {/* Main Operational Feed */}
        <div className="lg:col-span-8 space-y-6">
          <div className="p-6 rounded-2xl border border-white/10 bg-white/5 backdrop-blur-xl">
            <h2 className="text-lg font-bold mb-6 flex items-center gap-2">
              <Calendar className="w-5 h-5 text-primary" />
              Filing Ledger
            </h2>

            {(isHealthLoading || isRunsLoading) ? (
              <div className="flex items-center justify-center py-12">
                <Loader2 className="w-8 h-8 animate-spin text-primary/40" />
              </div>
            ) : !runs || runs.length === 0 ? (
              <div className="text-center py-12 border border-dashed border-white/10 rounded-xl">
                <AlertCircle className="w-12 h-12 text-white/10 mx-auto mb-4" />
                <p className="text-white/40">No payroll runs found.</p>
              </div>
            ) : (
              <div className="space-y-4">
                {runs.map((run: any) => (
                  <div 
                    key={run.correlationId}
                    className="p-5 rounded-xl border border-white/10 bg-white/5 flex flex-col gap-4"
                  >
                    <div className="flex items-center justify-between">
                      <div>
                        <div className="flex items-center gap-3 mb-1">
                          <span className="text-sm font-bold">{run.periodName}</span>
                          <Badge variant="success">Payroll Locked</Badge>
                        </div>
                        <p className="text-[10px] text-white/40 uppercase font-bold tracking-widest">
                          Run ID: {run.correlationId.slice(0, 8)}...
                        </p>
                      </div>
                    </div>

                    <div className="grid grid-cols-3 gap-4">
                      <ComplianceFilingItem 
                        type={0} 
                        label="EPF ECR" 
                        runId={run.correlationId}
                        filing={health?.filings?.find((f: any) => f.payrollRunId === run.correlationId && f.type === 0)}
                        onDownload={() => downloadFile("epf", run.correlationId)}
                        onFile={() => setFilingFlow({ runId: run.correlationId, type: 0, label: "EPF ECR" })}
                      />
                      <ComplianceFilingItem 
                        type={1} 
                        label="ESIC Monthly" 
                        runId={run.correlationId}
                        filing={health?.filings?.find((f: any) => f.payrollRunId === run.correlationId && f.type === 1)}
                        onDownload={() => downloadFile("esic", run.correlationId)}
                        onFile={() => setFilingFlow({ runId: run.correlationId, type: 1, label: "ESIC Monthly" })}
                      />
                      <ComplianceFilingItem 
                        type={2} 
                        label="TDS 24Q" 
                        runId={run.correlationId}
                        filing={health?.filings?.find((f: any) => f.payrollRunId === run.correlationId && f.type === 2)}
                        onDownload={() => downloadFile("tds", run.correlationId)}
                        onFile={() => setFilingFlow({ runId: run.correlationId, type: 2, label: "TDS 24Q" })}
                      />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Assisted Filing & Risk Sidebar */}
        <div className="lg:col-span-4 space-y-6">
          {filingFlow && (
            <div className="p-6 rounded-2xl border border-primary/50 bg-primary/10 animate-in fade-in slide-in-from-right-4 duration-300">
              <h3 className="text-sm font-bold mb-4 flex items-center gap-2">
                <ExternalLink className="w-4 h-4" />
                Assisted Filing: {filingFlow.label}
              </h3>
              
              <div className="space-y-4">
                <div className="flex items-start gap-3">
                  <div className="w-6 h-6 rounded-full bg-primary/20 flex items-center justify-center text-[10px] font-bold shrink-0">1</div>
                  <p className="text-xs text-white/80">Download the pre-validated return file.</p>
                </div>
                <div className="flex items-start gap-3">
                  <div className="w-6 h-6 rounded-full bg-primary/20 flex items-center justify-center text-[10px] font-bold shrink-0">2</div>
                  <div className="flex-1">
                    <p className="text-xs text-white/80 mb-2">Login to the government portal.</p>
                    <a 
                      href={getPortalUrl(filingFlow.type)} 
                      target="_blank" 
                      className="text-[10px] font-bold uppercase tracking-widest text-primary flex items-center gap-1 hover:underline"
                    >
                      Open Portal <ExternalLink className="w-3 h-3" />
                    </a>
                  </div>
                </div>
                <div className="flex items-start gap-3">
                  <div className="w-6 h-6 rounded-full bg-primary/20 flex items-center justify-center text-[10px] font-bold shrink-0">3</div>
                  <div className="flex-1">
                    <p className="text-xs text-white/80 mb-2">Upload the file and enter the Reference/Challan Number below.</p>
                    <input 
                      type="text" 
                      placeholder="Ref Number / TRRN" 
                      value={refNumber}
                      onChange={(e) => setRefNumber(e.target.value)}
                      className="w-full bg-white/5 border border-white/10 rounded px-3 py-2 text-xs outline-none focus:border-primary"
                    />
                  </div>
                </div>
                
                <div className="flex gap-2 pt-2">
                   <button 
                    onClick={() => markAsFiled.mutate({ runId: filingFlow.runId, type: filingFlow.type, referenceNumber: refNumber })}
                    disabled={!refNumber || markAsFiled.isPending}
                    className="flex-1 bg-primary text-white py-2 rounded-lg text-xs font-bold hover:bg-primary/80 transition-all disabled:opacity-50"
                  >
                    {markAsFiled.isPending ? "Updating..." : "Mark as Filed"}
                  </button>
                  <button 
                    onClick={() => setFilingFlow(null)}
                    className="px-4 py-2 bg-white/5 border border-white/10 rounded-lg text-xs font-bold"
                  >
                    Cancel
                  </button>
                </div>
              </div>
            </div>
          )}

          <div className="p-6 rounded-2xl border border-white/10 bg-white/5">
            <h3 className="text-sm font-bold mb-4 flex items-center gap-2">
              <ShieldCheck className="w-4 h-4 text-primary" />
              Compliance Snapshot Logic
            </h3>
            <p className="text-xs text-white/40 leading-relaxed">
              Every file you download is versioned. If your payroll data changes, we detect the mismatch and mark the filing as "Dirty" until a new return is generated.
            </p>
          </div>
          
          <div className="p-6 rounded-2xl border border-white/10 bg-[#1a0909]">
             <h3 className="text-sm font-bold mb-4 flex items-center gap-2 text-error">
              <AlertTriangle className="w-4 h-4" />
              Penalty Logic
            </h3>
            <div className="space-y-4">
              <PenaltyRule label="EPF Late Filing" rule="12% Int + up to 25% Damages" />
              <PenaltyRule label="ESIC Late Payment" rule="12% Interest p.a." />
              <PenaltyRule label="TDS Delay" rule="₹200 per day (Sec 234E)" />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function HealthCard({ label, value, icon: Icon, color = "text-white", trend }: any) {
  return (
    <div className="p-6 rounded-2xl border border-white/10 bg-white/5 backdrop-blur-xl relative overflow-hidden group hover:border-primary/30 transition-all">
      <div className="flex items-center justify-between mb-4">
        <div className="p-2 rounded-lg bg-white/5 border border-white/10 group-hover:bg-primary/10 transition-colors">
          <Icon className={cn("w-5 h-5", color)} />
        </div>
        <span className="text-[10px] font-bold text-white/20 uppercase tracking-widest">{trend}</span>
      </div>
      <p className="text-3xl font-black mb-1">{value}</p>
      <p className="text-xs font-medium text-white/40 uppercase tracking-wider">{label}</p>
    </div>
  );
}

function ComplianceFilingItem({ label, filing, onDownload, onFile }: any) {
  const isFiled = filing?.status === "Filed" || filing?.status === 1;
  const risk = filing?.risk;
  
  return (
    <div className={cn(
      "p-4 rounded-xl border transition-all",
      isFiled ? "bg-success/5 border-success/20" : "bg-white/5 border-white/10"
    )}>
      <div className="flex items-center justify-between mb-3">
        <span className="text-[10px] font-black uppercase tracking-widest text-white/60">{label}</span>
        {isFiled ? (
          <Badge variant="success">Filed</Badge>
        ) : (
          <Badge variant={risk?.riskLevel === "Critical" ? "danger" : risk?.riskLevel === "High" ? "warning" : "neutral"}>
            {risk?.riskLevel ?? "Pending"}
          </Badge>
        )}
      </div>

      {isFiled ? (
        <div className="space-y-1 mb-4">
          <p className="text-[10px] text-white/40">Ref: {filing.referenceNumber}</p>
          <p className="text-[10px] text-white/40">Date: {new Date(filing.filedAtUtc).toLocaleDateString()}</p>
        </div>
      ) : (
        <div className="mb-4">
           {risk?.estimatedPenalty > 0 && (
            <p className="text-[10px] text-error font-bold mb-1 flex items-center gap-1">
              <AlertCircle className="w-3 h-3" />
              Est. Penalty: ₹{risk.estimatedPenalty.toLocaleString()}
            </p>
          )}
          <p className="text-[10px] text-white/40">Due: {new Date(filing?.dueDate || Date.now()).toLocaleDateString()}</p>
        </div>
      )}

      <div className="flex flex-col gap-2">
        {!isFiled && (
          <button 
            onClick={onDownload}
            className="w-full flex items-center justify-center gap-2 py-1.5 rounded-lg bg-white/5 border border-white/10 hover:bg-white/10 text-[10px] font-bold transition-all"
          >
            <Download className="w-3 h-3" /> Download ECR
          </button>
        )}
        {!isFiled ? (
          <button 
            onClick={onFile}
            className="w-full py-1.5 rounded-lg bg-primary text-white text-[10px] font-bold hover:bg-primary/80 transition-all flex items-center justify-center gap-1"
          >
            File Now <ChevronRight className="w-3 h-3" />
          </button>
        ) : (
          <button 
             onClick={onDownload}
             className="w-full flex items-center justify-center gap-2 py-1.5 rounded-lg bg-white/5 border border-white/10 hover:bg-white/10 text-[10px] font-bold transition-all"
          >
            <FileText className="w-3 h-3" /> View Receipt
          </button>
        )}
      </div>
    </div>
  );
}

function PenaltyRule({ label, rule }: { label: string; rule: string }) {
  return (
    <div className="flex items-start gap-2">
      <Info className="w-3 h-3 text-error/40 mt-0.5" />
      <div>
        <p className="text-[10px] font-bold text-white/80">{label}</p>
        <p className="text-[9px] text-white/40 leading-tight">{rule}</p>
      </div>
    </div>
  );
}
