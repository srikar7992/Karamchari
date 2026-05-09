"use client";

import { useState } from "react";
import { AppShell } from "@/components/shell/app-shell";
import { Topbar } from "@/components/shell/topbar";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  useRunSimulation,
  useSimulation,
  useDiscardSimulation,
} from "@/lib/hooks/use-payroll-phase1a";
import type { SimulationType, PayrollSimulationDto } from "@/lib/types/payroll-phase1a";

const SIM_TYPES: SimulationType[] = [
  "DryRunPayroll",
  "RevisionImpact",
  "TaxImpact",
  "ArrearImpact",
  "ComplianceImpact",
  "BudgetForecast",
];

function DeltaCell({ value }: { value: number }) {
  const formatted = Math.abs(value).toLocaleString("en-IN", {
    style: "currency",
    currency: "INR",
  });
  if (value === 0) return <span className="tabular-nums text-on-surface-variant">-</span>;
  return (
    <span className={`tabular-nums font-medium ${value > 0 ? "text-primary" : "text-error"}`}>
      {value > 0 ? "+" : "-"}
      {formatted}
    </span>
  );
}

function SimulationResultPanel({
  simId,
  onDiscard,
}: {
  simId: string;
  onDiscard: () => void;
}) {
  const { data: sim } = useSimulation(simId);
  const discard = useDiscardSimulation(simId);

  if (!sim) return null;

  const totalGrossDelta = sim.totalSimulatedGross - sim.totalPreviousGross;

  return (
    <div className="space-y-6">
      {/* Status bar */}
      <div className="flex items-center justify-between rounded-xl border border-outline/20 bg-surface-variant/20 px-5 py-4">
        <div className="space-y-0.5">
          <p className="text-sm font-medium text-on-surface">
            {sim.simulationType} &bull; {sim.periodName}
          </p>
          <p className="text-xs text-on-surface-variant">
            {sim.totalEmployees} employees &bull; expires{" "}
            {new Date(sim.expiresAtUtc).toLocaleTimeString()}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <div className="text-right">
            <p className="text-xs text-on-surface-variant">Total Gross Impact</p>
            <DeltaCell value={totalGrossDelta} />
          </div>
          {sim.status !== "Discarded" && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                discard.mutate(undefined, { onSuccess: onDiscard });
              }}
              disabled={discard.isPending}
            >
              Discard
            </Button>
          )}
        </div>
      </div>

      {sim.status === "Running" && (
        <div className="flex items-center gap-3 text-sm text-on-surface-variant">
          <span className="relative flex h-2.5 w-2.5">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-primary opacity-75" />
            <span className="relative inline-flex h-2.5 w-2.5 rounded-full bg-primary" />
          </span>
          Simulation running...
        </div>
      )}

      {sim.status === "Completed" && sim.employeeResults.length > 0 && (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Employee</TableHead>
              <TableHead className="text-right">Prev Gross</TableHead>
              <TableHead className="text-right">Sim Gross</TableHead>
              <TableHead className="text-right">Gross Delta</TableHead>
              <TableHead className="text-right">Net Delta</TableHead>
              <TableHead className="text-right">TDS Delta</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {sim.employeeResults.map((r) => (
              <TableRow key={r.employeeId}>
                <TableCell className="font-medium">{r.employeeName}</TableCell>
                <TableCell className="tabular-nums text-right text-on-surface-variant text-sm">
                  {r.previousGross.toLocaleString("en-IN", { style: "currency", currency: "INR" })}
                </TableCell>
                <TableCell className="tabular-nums text-right text-sm">
                  {r.simulatedGross.toLocaleString("en-IN", { style: "currency", currency: "INR" })}
                </TableCell>
                <TableCell className="text-right">
                  <DeltaCell value={r.grossDelta} />
                </TableCell>
                <TableCell className="text-right">
                  <DeltaCell value={r.netDelta} />
                </TableCell>
                <TableCell className="text-right">
                  <DeltaCell value={r.simulatedTds - r.previousTds} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}

export default function SimulationPage() {
  const runSim = useRunSimulation();
  const [activeSimId, setActiveSimId] = useState<string | null>(null);
  const [form, setForm] = useState({
    simulationType: "DryRunPayroll" as SimulationType,
    periodName: "",
    globalCTCAdjustmentPercent: "",
  });

  function handleRun(e: React.FormEvent) {
    e.preventDefault();
    runSim.mutate(
      {
        simulationType: form.simulationType,
        periodName: form.periodName,
        globalCTCAdjustmentPercent: form.globalCTCAdjustmentPercent
          ? parseFloat(form.globalCTCAdjustmentPercent)
          : undefined,
      },
      {
        onSuccess: (sim: PayrollSimulationDto) => setActiveSimId(sim.id),
      },
    );
  }

  return (
    <AppShell>
      <Topbar title="Payroll Simulation" />
      <div className="p-6 space-y-8">
        {/* Config panel */}
        <form
          onSubmit={handleRun}
          className="rounded-xl border border-outline/20 bg-surface-variant/10 p-5 space-y-4"
        >
          <h2 className="text-sm font-semibold text-on-surface-variant uppercase tracking-wide">
            Configure Simulation
          </h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div>
              <label className="mb-1 block text-xs text-on-surface-variant">Simulation Type</label>
              <select
                value={form.simulationType}
                onChange={(e) =>
                  setForm({ ...form, simulationType: e.target.value as SimulationType })
                }
                className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
              >
                {SIM_TYPES.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-xs text-on-surface-variant">Period</label>
              <input
                required
                placeholder="e.g. May-2025"
                value={form.periodName}
                onChange={(e) => setForm({ ...form, periodName: e.target.value })}
                className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs text-on-surface-variant">
                Global CTC Adjustment (%)
              </label>
              <input
                type="number"
                step="0.1"
                placeholder="e.g. 10 for 10%"
                value={form.globalCTCAdjustmentPercent}
                onChange={(e) =>
                  setForm({ ...form, globalCTCAdjustmentPercent: e.target.value })
                }
                className="w-full rounded-lg border border-outline/40 bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
          </div>
          <div className="flex items-center justify-between">
            <p className="text-xs text-on-surface-variant">
              Simulations never mutate real payroll state and expire after 24 hours.
            </p>
            <Button type="submit" disabled={runSim.isPending}>
              {runSim.isPending ? "Running..." : "Run Simulation"}
            </Button>
          </div>
        </form>

        {/* Results */}
        {activeSimId && (
          <SimulationResultPanel
            simId={activeSimId}
            onDiscard={() => setActiveSimId(null)}
          />
        )}
      </div>
    </AppShell>
  );
}
