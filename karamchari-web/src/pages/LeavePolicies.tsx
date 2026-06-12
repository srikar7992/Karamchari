import { SignalCard, InstitutionalTable, BinduButton, LedgerStrip, type TableColumn } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'SYS',
  archetype: 'administration',
  signals: 1,
  risks: 0,
  narratives: 0,
  maps: 0,
  bindu: true,
}

interface LeaveType {
  code: string
  name: string
  accrual: string
  carryForward: string
  certificate: string
  laws: string
  active: boolean
}

const LEAVE_TYPES: LeaveType[] = [
  { code: 'EL', name: 'Earned Leave', accrual: '1.75 d/month', carryForward: '≤ 30 d (LV-CF-01)', certificate: 'Not required', laws: 'LV-CF-01 · LV-SLA-02', active: true },
  { code: 'SL', name: 'Sick Leave', accrual: '0.83 d/month', carryForward: 'None', certificate: '≥ 3 days (LV-CERT-03)', laws: 'LV-CERT-03 · LV-SLA-02', active: true },
  { code: 'CL', name: 'Casual Leave', accrual: '7 d/year, lump', carryForward: 'None', certificate: 'Not required', laws: 'LV-SLA-02', active: true },
  { code: 'ML', name: 'Maternity Leave', accrual: 'Statutory grant', carryForward: 'N/A', certificate: 'Statutory documents', laws: 'LV-SLA-02', active: true },
  { code: 'LWP', name: 'Leave Without Pay', accrual: 'None', carryForward: 'N/A', certificate: 'Case review', laws: 'LV-SLA-02', active: true },
]

const columns: TableColumn<LeaveType>[] = [
  { key: 'code', header: 'Code', render: (t) => <span className="font-mono text-[12px] text-primary">{t.code}</span> },
  { key: 'name', header: 'Leave Type', render: (t) => <span className="text-primary">{t.name}</span> },
  { key: 'accrual', header: 'Accrual', render: (t) => <span className="font-mono text-[11px]">{t.accrual}</span> },
  { key: 'carryForward', header: 'Carry-Forward', render: (t) => <span className="font-mono text-[11px]">{t.carryForward}</span> },
  { key: 'certificate', header: 'Certificate', render: (t) => <span className="font-mono text-[11px]">{t.certificate}</span> },
  { key: 'laws', header: 'Laws', render: (t) => <span className="font-mono text-[10px] text-on-surface-variant">{t.laws}</span> },
  {
    key: 'status',
    header: 'Status',
    render: (t) => (
      <span className="flex items-center gap-2">
        <span className={`bindu ${t.active ? 'good' : 'neutral'}`} />
        <span className="font-mono text-[10px] uppercase tracking-wider">{t.active ? 'Active' : 'Retired'}</span>
      </span>
    ),
  },
]

export function LeavePolicies() {
  return (
    <>
      {/* Entity header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Leave Governance · Tenant-wide
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Leave Policies
          </h2>
        </div>
        <span className="font-mono text-[12px] text-on-surface-variant uppercase tracking-widest">
          5 Types · Policy Set v4 Active
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-grid-12 mb-10">
        {/* The register */}
        <section className="md:col-span-8">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
            1. Policy Register
          </h3>
          <div className="bg-surface-container-lowest hairline-all p-6">
            <InstitutionalTable columns={columns} rows={LEAVE_TYPES} rowKey={(t) => t.code} />
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-6">
              Every row cites the laws it implements (design/LAWBOOK.md). A policy change that
              alters a cited law is a law amendment first, a configuration change second.
            </p>
          </div>
        </section>

        {/* Draft + activation */}
        <div className="md:col-span-4 flex flex-col gap-grid-12">
          <SignalCard
            label="Draft Pending"
            value="v5"
            note="Changes SL certificate threshold under LV-CERT-03; all other types unchanged"
            bindu="caution"
          />
          <section className="bg-surface-container-lowest hairline-all p-6 flex-1">
            <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-5 uppercase tracking-widest">
              2. Draft v5 — SL Revision
            </h3>
            <div className="space-y-4 font-tabular-data text-tabular-data">
              {[
                { label: 'Certificate From', value: '3 days → 2 days' },
                { label: 'Law Affected', value: 'LV-CERT-03' },
                { label: 'Accrual', value: 'Unchanged' },
                { label: 'Applies From', value: 'Next leave period' },
              ].map((f) => (
                <div key={f.label} className="flex justify-between items-center pb-3 hairline-b">
                  <span className="text-on-surface-variant text-[12px]">{f.label}</span>
                  <span className="font-mono text-[12px] text-primary">{f.value}</span>
                </div>
              ))}
            </div>
            <div className="flex flex-col gap-3 mt-6">
              <BinduButton className="py-3">Activate Policy Version</BinduButton>
              <button className="py-2.5 bg-transparent border border-outline-variant text-on-surface-variant font-mono-label text-mono-label uppercase tracking-widest hover:bg-surface-container-low hover:text-primary transition-colors">
                Save Draft
              </button>
            </div>
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-4">
              Activation amends LV-CERT-03 to v2. Requests submitted under v4 are evaluated
              under v4; the lawbook records the amendment.
            </p>
          </section>
        </div>
      </div>

      {/* Effect note */}
      <div className="bg-ivory-2 hairline-all p-4 mb-8">
        <p className="font-tabular-data text-tabular-data text-on-surface">
          Policies on this register govern accrual, carry-forward (LV-CF-01), certificate gates
          (LV-CERT-03), and approval SLAs (LV-SLA-02) for all 1,247 operators across every zone.
        </p>
      </div>

      <LedgerStrip
        entries={[
          { label: 'Register', value: 'Leave.PolicySet' },
          { label: 'Active', value: 'v4 since 2026-01-01 · activated by S. Menon' },
          { label: 'Draft', value: 'v5 · edited 2026-06-11T16:20:44Z' },
        ]}
      />
    </>
  )
}
