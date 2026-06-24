import { SignalCard, NarrativePlate, InstitutionalTable } from '@/components/institutional'
import type { TableColumn } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EMP',
  archetype: 'record-detail',
  signals: 3,
  risks: 0,
  narratives: 1,
  maps: 0,
  bindu: false,
}

// --- Data ---

interface Plan { plan: string; type: string; carrier: string; tier: string; status: string }
const ENROLLMENTS: Plan[] = [
  { plan: 'Group Mediclaim',  type: 'Medical',    carrier: 'Star Health',   tier: 'Family Floater 5L', status: 'Active' },
  { plan: 'Dental Cover',     type: 'Dental',     carrier: 'ICICI Lombard', tier: 'Individual',        status: 'Active' },
  { plan: 'Vision Care',      type: 'Vision',     carrier: 'Star Health',   tier: 'Individual',        status: 'Active' },
  { plan: 'Term Life',        type: 'Life',       carrier: 'LIC Group',     tier: '₹50L Cover',        status: 'Active' },
  { plan: 'NPS (Tier I)',     type: 'Retirement', carrier: 'PFRDA',         tier: '10% Basic',         status: 'Active' },
]

interface Dependent { name: string; relation: string; dob: string; covered: string }
const DEPENDENTS: Dependent[] = [
  { name: 'Ravi Venkatesan',  relation: 'Spouse', dob: '1989-07-22', covered: 'Medical, Life' },
  { name: 'Arjun Venkatesan', relation: 'Child',  dob: '2015-11-10', covered: 'Medical' },
]

interface Deduction { component: string; monthly: string; annual: string; employer: string }
const DEDUCTIONS: Deduction[] = [
  { component: 'Health Premium (Employee)', monthly: '₹3,000',  annual: '₹36,000',   employer: '₹4,500' },
  { component: 'NPS Contribution',          monthly: '₹11,160', annual: '₹1,33,920', employer: '₹11,160' },
  { component: 'Provident Fund',            monthly: '₹11,160', annual: '₹1,33,920', employer: '₹11,160' },
]

// --- Page ---

export function MyBenefits() {
  const planCols: TableColumn<Plan>[] = [
    { key: 'plan',    header: 'Plan' },
    { key: 'type',    header: 'Type' },
    { key: 'carrier', header: 'Carrier' },
    { key: 'tier',    header: 'Tier' },
    {
      key: 'status',
      header: 'Status',
      render: (r) => <span className="font-mono-label text-mono-label uppercase text-sthira-forest">{r.status}</span>,
    },
  ]
  const depCols: TableColumn<Dependent>[] = [
    { key: 'name',     header: 'Name' },
    { key: 'relation', header: 'Relation' },
    { key: 'dob',      header: 'Date of Birth' },
    { key: 'covered',  header: 'Covered Under' },
  ]
  const dedCols: TableColumn<Deduction>[] = [
    { key: 'component', header: 'Component' },
    { key: 'monthly',   header: 'Monthly',         align: 'right' },
    { key: 'annual',    header: 'Annual',           align: 'right' },
    { key: 'employer',  header: 'Employer Match',   align: 'right' },
  ]

  return (
    <>
      <header className="mb-10 border-b border-outline-variant pb-8">
        <h2 className="font-serif font-light text-[28px] md:text-[44px] leading-[1.1] text-primary">
          My Benefits
        </h2>
        <p className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mt-3">
          EMP-88271 · Enrolment Year 2026 · Open Enrollment Oct 2026
        </p>
      </header>

      <section className="mb-10">
        <div className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-3 gap-grid-12">
          <SignalCard label="Total Coverage" value="₹55L"    note="Medical + Life combined"       cornerTick />
          <SignalCard label="Monthly Premium" value="₹3,000" note="Employee contribution" />
          <SignalCard label="Employer Contribution" value="₹26,820" unit="/mo" note="PF + NPS + Health" bindu="good" />
        </div>
      </section>

      <div className="space-y-10">
        <section>
          <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
            Current Enrollments
          </h3>
          <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
            <InstitutionalTable columns={planCols} rows={ENROLLMENTS} rowKey={(r) => r.plan} />
          </div>
        </section>

        <section>
          <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
            Dependents
          </h3>
          <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
            <InstitutionalTable columns={depCols} rows={DEPENDENTS} rowKey={(r) => r.name} />
          </div>
        </section>

        <section>
          <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
            Payroll Deductions
          </h3>
          <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
            <InstitutionalTable columns={dedCols} rows={DEDUCTIONS} rowKey={(r) => r.component} />
          </div>
        </section>

        <NarrativePlate kicker="Benefits Summary" source="BEN-SUMMARY-01">
          Your employer contributes ₹26,820 per month toward your benefits — equivalent to
          17% above your gross salary. Open Enrollment begins October 2026. Life events
          may trigger mid-year elections within 30 days of the qualifying event.
        </NarrativePlate>
      </div>
    </>
  )
}
