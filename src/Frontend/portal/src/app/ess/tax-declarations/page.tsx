"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import { AppShell } from "@/components/shell/app-shell";
import { Topbar } from "@/components/shell/topbar";
import { Button } from "@/components/ui/button";
import { useTaxSimulation, useAnalyzeProof } from "@/lib/hooks/use-ess";
import { formatCurrency } from "@/lib/utils/format";
import { debounce } from "@/lib/utils/debounce";
import { useDropzone } from "react-dropzone";

export default function TaxSimulatorPage() {
  const [section80C, setSection80C] = useState(150000);
  const [section80D, setSection80D] = useState(25000);
  const [monthlyRent, setMonthlyRent] = useState(20000);
  const [isMetro, setIsMetro] = useState(true);
  const [lastAnalysis, setLastAnalysis] = useState<any>(null);

  const { mutate: simulate, data: result, isPending } = useTaxSimulation();
  const { mutate: analyze, isPending: isAnalyzing } = useAnalyzeProof();

  // Dropzone handling
  const onDrop = useCallback((acceptedFiles: any[]) => {
    if (acceptedFiles.length > 0) {
      analyze(acceptedFiles[0] as any, {
        onSuccess: (data: any) => {
          setLastAnalysis(data);
          // Auto-update the correct slider based on AI category detection
          if (data && typeof data.extractedAmount === "number") {
            if (data.category === "80C") setSection80C(data.extractedAmount);
            if (data.category === "80D") setSection80D(data.extractedAmount);
            if (data.category === "HRA") setMonthlyRent(data.extractedAmount);
          }
        }
      });
    }
  }, [analyze]);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({ 
    onDrop,
    accept: { 
      "application/pdf": [".pdf"], 
      "image/jpeg": [".jpg", ".jpeg"],
      "image/png": [".png"] 
    },
    multiple: false
  });

  // Debounced simulation call
  const debouncedSimulate = useMemo(
    () => debounce((params: any) => {
      simulate(params);
    }, 500),
    [simulate]
  );

  useEffect(() => {
    debouncedSimulate({
      section80C,
      section80D,
      monthlyRent,
      isMetro,
      financialYear: 2026,
    });
  }, [section80C, section80D, monthlyRent, isMetro, debouncedSimulate]);

  return (
    <AppShell>
      <Topbar title="Tax Projection Simulator" />

      <div className="mt-8 grid grid-cols-1 gap-8 lg:grid-cols-3">
        {/* Input Controls */}
        <div className="lg:col-span-2 space-y-8">
          <section className="rounded-2xl border border-outline bg-surface p-6 shadow-sm">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-lg font-semibold text-on-surface">Investment Declarations</h2>
              
              {/* AI Dropzone Trigger */}
              <div {...getRootProps()} className={`cursor-pointer rounded-lg border-2 border-dashed px-4 py-2 transition-colors ${isDragActive ? "border-primary bg-primary/5" : "border-outline hover:border-primary"}`}>
                <input {...getInputProps()} />
                <div className="flex items-center gap-2 text-sm font-medium text-primary">
                  <span className="material-symbols-outlined text-[20px]">
                    {isAnalyzing ? "sync" : "upload_file"}
                  </span>
                  {isAnalyzing ? "Analyzing Proof..." : "Drop Proof for AI Auto-Fill"}
                </div>
              </div>
            </div>

            {lastAnalysis && (
              <div className="mb-6 rounded-xl bg-surface-container-low p-3 flex items-center justify-between border border-outline">
                <div className="flex items-center gap-3">
                  <span className="material-symbols-outlined text-green-600">verified</span>
                  <div>
                    <div className="text-xs font-bold text-on-surface">AI Extraction Success</div>
                    <div className="text-[10px] text-on-surface-variant">Detected {lastAnalysis.category} for {formatCurrency(lastAnalysis.extractedAmount ?? 0)}</div>
                  </div>
                </div>
                <div className="text-[10px] font-bold px-2 py-1 bg-green-100 text-green-700 rounded-full">
                  {Math.round(lastAnalysis.confidence * 100)}% Confidence
                </div>
              </div>
            )}
            
            <div className="space-y-6">
              <SliderInput 
                label="Section 80C (PPF, LIC, ELSS)" 
                value={section80C} 
                max={150000} 
                onChange={setSection80C} 
                helpText="Maximum deduction limit: ₹1,50,000"
                isAiFilled={lastAnalysis?.category === "80C"}
              />

              <SliderInput 
                label="Section 80D (Health Insurance)" 
                value={section80D} 
                max={100000} 
                onChange={setSection80D} 
                helpText="Includes self, spouse, children and parents"
                isAiFilled={lastAnalysis?.category === "80D"}
              />

              <SliderInput 
                label="Monthly Rent Paid" 
                value={monthlyRent} 
                max={100000} 
                onChange={setMonthlyRent} 
                helpText="Used for HRA exemption calculation"
                isAiFilled={lastAnalysis?.category === "HRA"}
              />

              <div className="flex items-center justify-between">
                <div>
                  <div className="font-medium text-on-surface">Metro City Residence</div>
                  <div className="text-sm text-on-surface-variant">Higher HRA exemption for Metros</div>
                </div>
                <input 
                  type="checkbox" 
                  checked={isMetro} 
                  onChange={(e) => setIsMetro(e.target.checked)}
                  className="h-5 w-5 rounded border-outline text-primary focus:ring-primary"
                />
              </div>
            </div>
          </section>

          <div className="rounded-xl bg-secondary-container/20 p-4 text-secondary text-sm border border-secondary/20">
            <span className="material-symbols-outlined align-middle mr-2 text-[18px]">info</span>
            This is a simulation based on your projected annual income. Final tax liability may vary based on actual investments verified by HR.
          </div>
        </div>

        {/* Results Side Panel */}
        <div className="space-y-6">
          <section className="rounded-2xl border border-outline bg-surface p-6 shadow-sm sticky top-24">
            <h2 className="text-lg font-semibold text-on-surface mb-6">Tax Comparison</h2>
            
            <div className="space-y-4">
              <ResultRow label="Old Regime" value={result?.oldRegimeTax} active={result?.recommendedRegime === "Old Regime"} />
              <ResultRow label="New Regime" value={result?.newRegimeTax} active={result?.recommendedRegime === "New Regime"} />
              
              <div className="pt-4 border-t border-outline">
                <div className="text-sm text-on-surface-variant">Potential Savings</div>
                <div className="text-3xl font-bold text-primary mt-1">
                  {formatCurrency(result?.difference ?? 0)}
                </div>
                <div className="mt-4 rounded-xl bg-primary-container/10 p-4 text-sm text-primary font-medium border border-primary/20">
                  {isPending ? "Calculating..." : result?.reason}
                </div>
              </div>

              <Button variant="primary" className="w-full mt-6">
                Submit Declaration
              </Button>
            </div>
          </section>
        </div>
      </div>
    </AppShell>
  );
}

function SliderInput({ label, value, max, onChange, helpText, isAiFilled }: { label: string, value: number, max: number, onChange: (v: number) => void, helpText: string, isAiFilled?: boolean }) {
  return (
    <div className={`space-y-2 rounded-xl p-2 transition-colors ${isAiFilled ? "bg-green-50/50 ring-1 ring-green-100" : ""}`}>
      <div className="flex justify-between items-center">
        <label className="font-medium text-on-surface flex items-center gap-2">
          {label}
          {isAiFilled && <span className="material-symbols-outlined text-[14px] text-green-600">auto_awesome</span>}
        </label>
        <span className={`font-bold ${isAiFilled ? "text-green-600" : "text-primary"}`}>{formatCurrency(value)}</span>
      </div>
      <input 
        type="range" 
        min={0} 
        max={max} 
        step={500}
        value={value} 
        onChange={(e) => onChange(parseInt(e.target.value))}
        className="w-full h-2 bg-surface-container-highest rounded-lg appearance-none cursor-pointer accent-primary"
      />
      <div className="text-xs text-on-surface-variant">{helpText}</div>
    </div>
  );
}

function ResultRow({ label, value, active }: { label: string, value?: number, active: boolean }) {
  return (
    <div className={`flex justify-between items-center p-3 rounded-xl border ${active ? "border-primary bg-primary-container/5" : "border-outline bg-surface"}`}>
      <div className="text-sm font-medium text-on-surface">{label}</div>
      <div className={`font-bold ${active ? "text-primary" : "text-on-surface"}`}>
        {value !== undefined ? formatCurrency(value) : "..."}
      </div>
    </div>
  );
}

