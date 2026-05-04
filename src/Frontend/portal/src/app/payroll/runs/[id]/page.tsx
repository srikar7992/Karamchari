"use client";

import { use } from "react";
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
  usePayrollRunSummary,
  usePayrollRunDetails,
  useLockPayrollRun,
} from "@/lib/hooks/use-payroll";
import { formatCurrency } from "@/lib/utils/format";
import { useRouter } from "next/navigation";

export default function PayrollRunDetailsPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();
  
  const { data: summary, isLoading: isLoadingSummary } = usePayrollRunSummary(id);
  const { data: details, isLoading: isLoadingDetails } = usePayrollRunDetails(id);
  const { mutate: lockRun, isPending: isLocking } = useLockPayrollRun();

  const handleLock = () => {
    if (confirm("Are you sure you want to lock this payroll run? This will finalize all calculations and trigger payslip generation.")) {
      lockRun(id, {
        onSuccess: () => {
          router.push("/payroll");
        }
      });
    }
  };

  const renderStatusBadge = (status: string) => {
    const baseClasses = "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold border";
    switch (status) {
      case "PendingReview":
        return <span className={`${baseClasses} bg-amber-100 text-amber-800 border-amber-200`}>Pending Review</span>;
      case "Finalized":
        return <span className={`${baseClasses} bg-green-100 text-green-800 border-green-200`}>Finalized</span>;
      case "Calculating":
        return (
          <span className={`${baseClasses} bg-blue-100 text-blue-800 border-blue-200 animate-pulse`}>
            Calculating...
          </span>
        );
      default:
        return <span className={`${baseClasses} bg-gray-100 text-gray-800 border-gray-200`}>{status}</span>;
    }
  };

  if (isLoadingSummary) return <AppShell>Loading...</AppShell>;

  return (
    <AppShell>
      <Topbar title={`Payroll: ${summary?.periodName ?? "Run Overview"}`}>
        <div className="flex items-center gap-3">
          {renderStatusBadge(summary?.currentState ?? "Draft")}
          <Button 
            variant="primary" 
            size="sm" 
            disabled={summary?.currentState !== "PendingReview" || isLocking}
            onClick={handleLock}
          >
            {isLocking ? "Locking..." : "Lock & Publish Payslips"}
          </Button>
        </div>
      </Topbar>

      <div className="mt-8 grid grid-cols-1 gap-6 md:grid-cols-4">
        {/* KPI Cards */}
        <KpiCard 
          title="Total Outflow" 
          value={formatCurrency(summary?.totalNet ?? 0)} 
          icon="payments"
          color="blue"
        />
        <KpiCard 
          title="Gross Salary" 
          value={formatCurrency(summary?.totalGross ?? 0)} 
          icon="account_balance_wallet"
          color="gray"
        />
        <KpiCard 
          title="Total Taxes (TDS)" 
          value={formatCurrency(summary?.totalTax ?? 0)} 
          icon="receipt_long"
          color="amber"
        />
        <KpiCard 
          title="Employees" 
          value={summary?.totalEmployeesToProcess?.toString() ?? "0"} 
          icon="groups"
          color="indigo"
        />
      </div>

      <section className="mt-10">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-on-surface">Employee Breakdown</h2>
          <div className="text-sm text-on-surface-variant">
            {details?.length} employees processed
          </div>
        </div>

        <div className="overflow-hidden rounded-xl border border-outline bg-surface shadow-sm">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Employee ID</TableHead>
                <TableHead className="text-right">Gross</TableHead>
                <TableHead className="text-right">Deductions</TableHead>
                <TableHead className="text-right">Net Pay</TableHead>
                <TableHead>Variance</TableHead>
                <TableHead className="w-[50px]"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoadingDetails ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center py-10">Loading details...</TableCell>
                </TableRow>
              ) : details?.map((row: any) => (
                <TableRow key={row.employeeId}>
                  <TableCell className="font-medium">{row.employeeId.substring(0, 8)}</TableCell>
                  <TableCell className="text-right">{formatCurrency(row.monthlyGross)}</TableCell>
                  <TableCell className="text-right text-error">{formatCurrency(row.monthlyGross - row.netPay)}</TableCell>
                  <TableCell className="text-right font-semibold">{formatCurrency(row.netPay)}</TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <span className={`text-sm ${row.variancePercentage > 0 ? "text-green-600" : row.variancePercentage < 0 ? "text-error" : ""}`}>
                        {row.variancePercentage > 0 ? "+" : ""}{(row.variancePercentage * 100).toFixed(1)}%
                      </span>
                      {row.isAnomaly && (
                        <span className="material-symbols-outlined text-amber-500 text-[18px]" title="High variance detected">
                          warning
                        </span>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <Button variant="ghost" size="sm">
                      <span className="material-symbols-outlined text-[18px]">chevron_right</span>
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </section>
    </AppShell>
  );
}

function KpiCard({ title, value, icon, color }: { title: string, value: string, icon: string, color: string }) {
  const colorMap: Record<string, string> = {
    blue: "bg-blue-50 text-blue-600",
    amber: "bg-amber-50 text-amber-600",
    indigo: "bg-indigo-50 text-indigo-600",
    gray: "bg-gray-50 text-gray-600",
  };

  return (
    <div className="rounded-2xl border border-outline bg-surface p-6 shadow-sm transition-hover hover:shadow-md">
      <div className="flex items-center justify-between mb-4">
        <div className={`rounded-xl p-2 ${colorMap[color]}`}>
          <span className="material-symbols-outlined">{icon}</span>
        </div>
      </div>
      <div className="text-sm font-medium text-on-surface-variant">{title}</div>
      <div className="mt-1 text-2xl font-bold text-on-surface">{value}</div>
    </div>
  );
}
