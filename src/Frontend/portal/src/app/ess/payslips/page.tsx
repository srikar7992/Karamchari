"use client";

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
import { useEssPayslips, useYtdSummary, getPayslipDownloadUrl } from "@/lib/hooks/use-ess";
import { formatCurrency } from "@/lib/utils/format";

export default function EssPayslipsPage() {
  const { data: payslips, isLoading: isLoadingPayslips } = useEssPayslips();
  const { data: ytd, isLoading: isLoadingYtd } = useYtdSummary();

  const handleDownload = (year: number, month: number) => {
    const url = getPayslipDownloadUrl(year, month);
    // Since it's a file stream, we can open it in a new tab or use a hidden anchor
    window.open(url, "_blank");
  };

  return (
    <AppShell>
      <Topbar title="My Payslips" />

      {/* YTD Financial Summary */}
      <div className="mt-8 grid grid-cols-1 gap-6 md:grid-cols-3">
        <YtdCard 
          title="Year-to-Date Gross" 
          value={formatCurrency(ytd?.grossYtd ?? 0)} 
          icon="payments"
          loading={isLoadingYtd}
        />
        <YtdCard 
          title="Year-to-Date Net" 
          value={formatCurrency(ytd?.netYtd ?? 0)} 
          icon="account_balance_wallet"
          loading={isLoadingYtd}
        />
        <YtdCard 
          title="Tax Deducted (YTD)" 
          value={formatCurrency(ytd?.taxYtd ?? 0)} 
          icon="receipt_long"
          loading={isLoadingYtd}
        />
      </div>

      {/* Payslip Timeline */}
      <section className="mt-10">
        <h2 className="mb-4 text-lg font-semibold text-on-surface">Payment History</h2>
        
        <div className="overflow-hidden rounded-xl border border-outline bg-surface shadow-sm">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Month</TableHead>
                <TableHead className="text-right">Gross</TableHead>
                <TableHead className="text-right">Deductions</TableHead>
                <TableHead className="text-right font-bold">Net Pay</TableHead>
                <TableHead className="text-right">Action</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoadingPayslips ? (
                <TableRow>
                  <TableCell colSpan={5} className="py-10 text-center">Loading payslips...</TableCell>
                </TableRow>
              ) : payslips?.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="py-10 text-center">No payslips found for this financial year.</TableCell>
                </TableRow>
              ) : (
                payslips?.map((p) => (
                  <TableRow key={p.id}>
                    <TableCell className="font-medium">{p.periodName}</TableCell>
                    <TableCell className="text-right">{formatCurrency(p.monthlyGross)}</TableCell>
                    <TableCell className="text-right text-error">{formatCurrency(p.monthlyGross - p.netPay)}</TableCell>
                    <TableCell className="text-right font-bold">{formatCurrency(p.netPay)}</TableCell>
                    <TableCell className="text-right">
                      <Button 
                        variant="secondary" 
                        size="sm" 
                        onClick={() => handleDownload(p.year, p.month)}
                      >
                        <span className="material-symbols-outlined text-[18px]">download</span>
                        Download PDF
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      </section>
    </AppShell>
  );
}

function YtdCard({ title, value, icon, loading }: { title: string, value: string, icon: string, loading: boolean }) {
  return (
    <div className="rounded-2xl border border-outline bg-surface p-6 shadow-sm">
      <div className="flex items-center gap-4">
        <div className="rounded-xl bg-primary/10 p-2 text-primary">
          <span className="material-symbols-outlined">{icon}</span>
        </div>
        <div>
          <div className="text-sm font-medium text-on-surface-variant">{title}</div>
          <div className={`mt-1 text-2xl font-bold text-on-surface ${loading ? "animate-pulse" : ""}`}>
            {loading ? "..." : value}
          </div>
        </div>
      </div>
    </div>
  );
}
