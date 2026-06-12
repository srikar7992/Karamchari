import { SignalCard, InstitutionalTable, LedgerStrip, type TableColumn } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'SYS',
  archetype: 'administration',
  signals: 1,
  risks: 0,
  narratives: 0,
  maps: 0,
  bindu: false,
}

interface ApprovalRule {
  law: string
  scope: string
  rule: string
  sla: string
  escalation: string
  workflow: string
  active: boolean
}

const RULES: ApprovalRule[] = [
  { law: 'LV-SLA-02', scope: 'Leave requests', rule: 'Single approver: direct manager', sla: '48 h', escalation: 'HR partner pool', workflow: 'LEAVE-STD-01', active: true },
  { law: 'EXP-AUTH-01', scope: 'Expenses ≤ ₹5,000', rule: 'Single approver: budget owner', sla: '72 h', escalation: 'Finance controller', workflow: 'EXP-STD-02', active: true },
  { law: 'EXP-AUTH-01', scope: 'Expenses > ₹5,000', rule: 'Two-step: budget owner + finance review', sla: '96 h', escalation: 'Finance controller', workflow: 'EXP-2STEP-01', active: true },
  { law: 'SWP-SLA-01', scope: 'Shift swaps', rule: 'Supervisor confirms after counterparty accepts', sla: '48 h', escalation: 'Shift coordinator', workflow: 'SWAP-STD-01', active: true },
  { law: 'PAY-AUTH-04', scope: 'Payroll runs', rule: 'Payroll authority approves locked run', sla: 'Period close', escalation: 'None — gate holds', workflow: 'payroll-lifecycle', active: true },
]

const columns: TableColumn<ApprovalRule>[] = [
  { key: 'law', header: 'Law', render: (r) => <span className="font-mono text-[11px] text-primary">{r.law}</span> },
  { key: 'scope', header: 'Scope', render: (r) => <span className="text-primary">{r.scope}</span> },
  { key: 'rule', header: 'Rule', render: (r) => <span className="text-on-surface-variant">{r.rule}</span> },
  { key: 'sla', header: 'SLA', render: (r) => <span className="font-mono text-[11px]">{r.sla}</span> },
  { key: 'escalation', header: 'On Breach', render: (r) => <span className="font-mono text-[11px]">{r.escalation}</span> },
  { key: 'workflow', header: 'Workflow', render: (r) => <span className="font-mono text-[10px] text-on-surface-variant">{r.workflow}</span> },
  {
    key: 'status',
    header: 'Status',
    render: (r) => (
      <span className="flex items-center gap-2">
        <span className={`bindu ${r.active ? 'good' : 'neutral'}`} />
        <span className="font-mono text-[10px] uppercase tracking-wider">{r.active ? 'Active' : 'Retired'}</span>
      </span>
    ),
  },
]

export function ApprovalRules() {
  return (
    <>
      {/* Entity header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Decision Governance · Flows &amp; Policies
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Approval Rules
          </h2>
        </div>
        <span className="font-mono text-[12px] text-on-surface-variant uppercase tracking-widest">
          5 Rules · 4 Laws Implemented
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-grid-12 mb-10">
        {/* The register */}
        <section className="md:col-span-9">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
            1. Rule Register
          </h3>
          <div className="bg-surface-container-lowest hairline-all p-6">
            <InstitutionalTable columns={columns} rows={RULES} rowKey={(r) => `${r.law}-${r.scope}`} />
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-6">
              Rules bind a law to a workflow. The Approvals Inbox displays these rules as policy
              context on every queued decision; changing a rule here changes what approvers see
              and when escalations fire. Rule edits are revisions, never deletions.
            </p>
          </div>
        </section>

        <div className="md:col-span-3">
          <SignalCard
            label="SLA Breaches (30d)"
            value="3"
            note="All escalated per rule; none unresolved"
            bindu="good"
          />
        </div>
      </div>

      {/* Effect note */}
      <div className="bg-ivory-2 hairline-all p-4 mb-8">
        <p className="font-tabular-data text-tabular-data text-on-surface">
          Rules on this register govern routing, SLA timers, and escalation for every approval
          queue in the tenant. Each rule cites its law in design/LAWBOOK.md; the workflow column
          names the PRAVAHA definition that executes it.
        </p>
      </div>

      <LedgerStrip
        entries={[
          { label: 'Register', value: 'Flows.ApprovalRules' },
          { label: 'Engine', value: 'PRAVAHA v3' },
          { label: 'Last Change', value: 'SWP-SLA-01 revised 2026-03-18 · S. Menon' },
        ]}
      />
    </>
  )
}
