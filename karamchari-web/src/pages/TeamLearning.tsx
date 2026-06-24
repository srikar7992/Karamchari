import { SignalCard, NarrativePlate, RiskCard, InstitutionalTable } from '@/components/institutional'
import type { TableColumn } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'registry',
  signals: 4,
  risks: 1,
  narratives: 1,
  maps: 0,
  bindu: false,
}

// --- Data ---

interface MemberTraining {
  name: string
  role: string
  compliance: number
  complianceTotal: number
  development: number
  developmentTotal: number
  certExpiring: boolean
}
const TRAINING: MemberTraining[] = [
  { name: 'Asha Venkatesan',   role: 'Zone Operator',      compliance: 2, complianceTotal: 3, development: 1, developmentTotal: 4, certExpiring: true  },
  { name: 'Rajiv Mohan',        role: 'Zone Operator',      compliance: 3, complianceTotal: 3, development: 0, developmentTotal: 2, certExpiring: false },
  { name: 'Priya Sharma',        role: 'Sr. Zone Operator',  compliance: 3, complianceTotal: 3, development: 2, developmentTotal: 3, certExpiring: false },
  { name: 'Kiran Nair',          role: 'Shift Lead',         compliance: 3, complianceTotal: 3, development: 3, developmentTotal: 4, certExpiring: false },
  { name: 'Deepika Rao',         role: 'Zone Operator',      compliance: 0, complianceTotal: 3, development: 0, developmentTotal: 2, certExpiring: false },
  { name: 'Suresh Kumar',        role: 'Zone Operator',      compliance: 2, complianceTotal: 3, development: 1, developmentTotal: 2, certExpiring: false },
  { name: 'Meena Pillai',        role: 'Operations Analyst', compliance: 3, complianceTotal: 3, development: 2, developmentTotal: 5, certExpiring: false },
  { name: 'Arun Krishnamurthy',  role: 'Shift Lead',         compliance: 3, complianceTotal: 3, development: 4, developmentTotal: 4, certExpiring: true  },
]

interface SkillGap { skill: string; required: number; proficient: number; gap: number }
const SKILL_GAPS: SkillGap[] = [
  { skill: 'Advanced Zone Safety Protocol',  required: 8, proficient: 5, gap: 3 },
  { skill: 'Operations Data Analysis',       required: 5, proficient: 3, gap: 2 },
  { skill: 'Shift Leadership Certification', required: 4, proficient: 2, gap: 2 },
  { skill: 'Cross-Zone Coordination',        required: 6, proficient: 5, gap: 1 },
]

// --- Page ---

export function TeamLearning() {
  const trainingCols: TableColumn<MemberTraining>[] = [
    { key: 'name', header: 'Member' },
    { key: 'role', header: 'Role' },
    {
      key: 'compliance',
      header: 'Compliance',
      align: 'right',
      render: (r) => {
        const pct = Math.round((r.compliance / r.complianceTotal) * 100)
        const color = pct === 100 ? 'text-sthira-forest' : pct === 0 ? 'text-rakta-critical' : 'text-pita-caution'
        return (
          <div className="flex items-center gap-2 justify-end">
            <div className="w-12 h-1 bg-surface-variant">
              <div className="h-1 bg-primary" style={{ width: `${pct}%` }} />
            </div>
            <span className={`font-tabular-data text-tabular-data ${color} w-14 text-right`}>
              {r.compliance}/{r.complianceTotal}
            </span>
          </div>
        )
      },
    },
    {
      key: 'development',
      header: 'Development',
      align: 'right',
      render: (r) => (
        <span className="font-tabular-data text-tabular-data text-on-surface-variant">
          {r.development}/{r.developmentTotal}
        </span>
      ),
    },
    {
      key: 'certExpiring',
      header: 'Cert Alert',
      render: (r) => r.certExpiring
        ? <span className="font-mono-label text-mono-label uppercase text-pita-caution">Expiring</span>
        : <span className="font-mono-label text-mono-label uppercase text-on-surface-variant">—</span>,
    },
  ]
  const gapCols: TableColumn<SkillGap>[] = [
    { key: 'skill',      header: 'Skill' },
    { key: 'required',   header: 'Required',   align: 'right' },
    { key: 'proficient', header: 'Proficient', align: 'right' },
    {
      key: 'gap',
      header: 'Gap',
      align: 'right',
      render: (r) => (
        <span className={`font-tabular-data text-tabular-data ${r.gap > 1 ? 'text-pita-caution' : 'text-on-surface-variant'}`}>
          {r.gap}
        </span>
      ),
    },
  ]

  const complianceCompleted = TRAINING.filter((m) => m.compliance === m.complianceTotal).length
  const nonCompliantCount   = TRAINING.filter((m) => m.compliance < m.complianceTotal).length
  const certExpiringCount   = TRAINING.filter((m) => m.certExpiring).length
  const avgDevPct = Math.round(
    TRAINING.reduce((a, m) => a + m.development / m.developmentTotal, 0) / TRAINING.length * 100
  )

  return (
    <>
      <header className="mb-10 border-b border-outline-variant pb-8">
        <h2 className="font-serif font-light text-[28px] md:text-[44px] leading-[1.1] text-primary">
          Team Learning
        </h2>
        <p className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mt-3">
          MGR-22001 · Q2 2026 · Compliance Cycle · Skill Period FY2026
        </p>
      </header>

      <section className="mb-10">
        <div className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-4 gap-grid-12">
          <SignalCard
            label="Compliance Complete"
            value={String(complianceCompleted)}
            unit={`of ${TRAINING.length}`}
            bar={{ pct: Math.round((complianceCompleted / TRAINING.length) * 100), tone: 'bg-sthira-forest' }}
            cornerTick
          />
          <SignalCard label="Non-Compliant" value={String(nonCompliantCount)} note="Mandatory training incomplete" bindu={nonCompliantCount > 0 ? 'caution' : 'good'} />
          <SignalCard label="Avg Dev Progress" value={`${avgDevPct}%`} note="Development track YTD" />
          <SignalCard label="Certs Expiring" value={String(certExpiringCount)} unit="within 45 days" note="Renewal required" />
        </div>
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-grid-12">
        <div className="lg:col-span-2 space-y-10">
          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Training Status — By Member
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={trainingCols} rows={TRAINING} rowKey={(r) => r.name} />
            </div>
          </section>

          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Skill Gap Register
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={gapCols} rows={SKILL_GAPS} rowKey={(r) => r.skill} />
            </div>
          </section>
        </div>

        <div className="space-y-8">
          <RiskCard
            severity="elevated"
            title="Mandatory Training Overdue"
            value={String(nonCompliantCount)}
            body="Deepika Rao (0/3 compliance modules) and 2 others are behind the Q2 deadline of 2026-07-31. Escalation to HR if not resolved within 14 days."
          />
          <NarrativePlate kicker="Learning Summary" source="LMS-TEAM-001">
            Team compliance rate is {Math.round((complianceCompleted / TRAINING.length) * 100)}% — below the 100% target for Q2 2026.
            Two certifications expire within 45 days. Skill gaps in Advanced Zone Safety and
            Data Analysis are the highest-priority items for the next development cycle.
          </NarrativePlate>
        </div>
      </div>
    </>
  )
}
