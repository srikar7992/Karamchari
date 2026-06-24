import { SignalCard, NarrativePlate, InstitutionalTable } from '@/components/institutional'
import type { TableColumn } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EMP',
  archetype: 'registry',
  signals: 3,
  risks: 0,
  narratives: 1,
  maps: 0,
  bindu: false,
}

// --- Data ---

interface Payslip { period: string; gross: string; deductions: string; net: string; run: string; status: string }
const PAYSLIPS: Payslip[] = [
  { period: 'Jun 2026', gross: '₹1,86,000', deductions: '₹43,820', net: '₹1,42,180', run: 'RUN-2026-06', status: 'Settled' },
  { period: 'May 2026', gross: '₹1,86,000', deductions: '₹43,820', net: '₹1,42,180', run: 'RUN-2026-05', status: 'Settled' },
  { period: 'Apr 2026', gross: '₹1,83,500', deductions: '₹43,200', net: '₹1,40,300', run: 'RUN-2026-04', status: 'Settled' },
  { period: 'Mar 2026', gross: '₹1,83,500', deductions: '₹43,200', net: '₹1,40,300', run: 'RUN-2026-03', status: 'Settled' },
]

interface TaxDoc { name: string; fy: string; type: string; issued: string }
const TAX_DOCS: TaxDoc[] = [
  { name: 'Form 16 (Part A)', fy: 'FY2025-26', type: 'TDS Certificate',      issued: '2026-06-15' },
  { name: 'Form 16 (Part B)', fy: 'FY2025-26', type: 'Salary Certificate',   issued: '2026-06-15' },
  { name: 'Form 26AS',        fy: 'FY2025-26', type: 'Tax Credit Statement', issued: '2026-06-10' },
]

interface SalaryRevision { effective: string; gross: string; change: string; reason: string }
const SALARY_HISTORY: SalaryRevision[] = [
  { effective: 'Apr 2026', gross: '₹1,86,000', change: '+₹2,500', reason: 'Annual Increment' },
  { effective: 'Apr 2025', gross: '₹1,83,500', change: '+₹8,000', reason: 'Performance Revision' },
  { effective: 'Apr 2024', gross: '₹1,75,500', change: '+₹6,000', reason: 'Annual Increment' },
]

// --- Page ---

export function MyPayroll() {
  const payslipCols: TableColumn<Payslip>[] = [
    { key: 'period',     header: 'Period' },
    { key: 'gross',      header: 'Gross',      align: 'right' },
    { key: 'deductions', header: 'Deductions', align: 'right' },
    { key: 'net',        header: 'Net',         align: 'right' },
    { key: 'run',        header: 'Run' },
    {
      key: 'status',
      header: 'Status',
      render: (r) => <span className="font-mono-label text-mono-label uppercase text-sthira-forest">{r.status}</span>,
    },
  ]
  const taxCols: TableColumn<TaxDoc>[] = [
    { key: 'name',   header: 'Document' },
    { key: 'fy',     header: 'Financial Year' },
    { key: 'type',   header: 'Type' },
    { key: 'issued', header: 'Issued' },
  ]
  const salaryCols: TableColumn<SalaryRevision>[] = [
    { key: 'effective', header: 'Effective' },
    { key: 'gross',     header: 'Gross CTC', align: 'right' },
    {
      key: 'change',
      header: 'Change',
      align: 'right',
      render: (r) => <span className="text-sthira-forest font-tabular-data text-tabular-data">{r.change}</span>,
    },
    { key: 'reason', header: 'Reason' },
  ]

  return (
    <>
      <header className="mb-10 border-b border-outline-variant pb-8">
        <h2 className="font-serif font-light text-[28px] md:text-[44px] leading-[1.1] text-primary">
          My Payroll
        </h2>
        <p className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mt-3">
          EMP-88271 · FY2025-26 · Compensation Record
        </p>
      </header>

      <section className="mb-10">
        <div className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-3 gap-grid-12">
          <SignalCard label="YTD Gross"      value="₹7,38,000" note="Apr–Jun 2026"           cornerTick />
          <SignalCard label="YTD Tax Withheld" value="₹88,380" note="TDS deposited" />
          <SignalCard label="Monthly Net"    value="₹1,42,180" note="Jun 2026 settlement"    bindu="good" />
        </div>
      </section>

      <div className="space-y-10">
        <section>
          <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
            Payslips
          </h3>
          <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
            <InstitutionalTable columns={payslipCols} rows={PAYSLIPS} rowKey={(r) => r.period} />
          </div>
        </section>

        <section>
          <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
            Tax Documents
          </h3>
          <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
            <InstitutionalTable columns={taxCols} rows={TAX_DOCS} rowKey={(r) => r.name} />
          </div>
        </section>

        <section>
          <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
            Salary History
          </h3>
          <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
            <InstitutionalTable columns={salaryCols} rows={SALARY_HISTORY} rowKey={(r) => r.effective} />
          </div>
        </section>

        <NarrativePlate kicker="Compensation Record" source="PAY-RECORD-01">
          All payslips from the RUN-2026 series are settled. Form 16 for FY2025-26 was
          issued June 2026. The salary history records every authorised revision with
          its originating event — the chain is immutable.
        </NarrativePlate>
      </div>
    </>
  )
}
