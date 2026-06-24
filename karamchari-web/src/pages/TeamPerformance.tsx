import { SignalCard, NarrativePlate, RiskCard, InstitutionalTable, CaseTimeline, BinduButton } from '@/components/institutional'
import type { TableColumn, TimelineEntry } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'approval',
  signals: 5,
  risks: 2,
  narratives: 1,
  maps: 0,
  bindu: true,
}

// --- Data ---

interface GoalSummary { name: string; role: string; onTrack: number; atRisk: number; completed: number }
const GOAL_SUMMARY: GoalSummary[] = [
  { name: 'Asha Venkatesan',   role: 'Zone Operator',      onTrack: 2, atRisk: 1, completed: 1 },
  { name: 'Rajiv Mohan',        role: 'Zone Operator',      onTrack: 1, atRisk: 0, completed: 2 },
  { name: 'Priya Sharma',        role: 'Sr. Zone Operator',  onTrack: 3, atRisk: 0, completed: 1 },
  { name: 'Kiran Nair',          role: 'Shift Lead',         onTrack: 2, atRisk: 1, completed: 2 },
  { name: 'Deepika Rao',         role: 'Zone Operator',      onTrack: 0, atRisk: 2, completed: 0 },
  { name: 'Suresh Kumar',        role: 'Zone Operator',      onTrack: 2, atRisk: 0, completed: 1 },
  { name: 'Meena Pillai',        role: 'Operations Analyst', onTrack: 3, atRisk: 1, completed: 1 },
  { name: 'Arun Krishnamurthy',  role: 'Shift Lead',         onTrack: 2, atRisk: 0, completed: 3 },
]

interface PendingReview { empId: string; name: string; period: string; type: string; due: string; status: string }
const PENDING_REVIEWS: PendingReview[] = [
  { empId: 'EMP-88521', name: 'Deepika Rao',    period: 'Q2 2026', type: 'Probation Review', due: '2026-07-01', status: 'Overdue' },
  { empId: 'EMP-88271', name: 'Asha Venkatesan', period: 'Mid-FY2026', type: 'Mid-Year',       due: '2026-07-15', status: 'Pending' },
  { empId: 'EMP-88350', name: 'Rajiv Mohan',      period: 'Mid-FY2026', type: 'Mid-Year',       due: '2026-07-15', status: 'Pending' },
  { empId: 'EMP-88412', name: 'Priya Sharma',      period: 'Mid-FY2026', type: 'Mid-Year',       due: '2026-07-15', status: 'Pending' },
]

const REVIEW_EVENTS: TimelineEntry[] = [
  { ts: '2026-06-20T10:00:00Z', tag: 'COMPLETED', tone: 'good',    body: 'Kiran Nair — FY2025 Annual Review finalized. Rating: Exceeds Expectations.' },
  { ts: '2026-06-18T14:30:00Z', tag: 'COMPLETED', tone: 'good',    body: 'Arun Krishnamurthy — FY2025 Annual Review finalized. Rating: Outstanding.' },
  { ts: '2026-06-15T09:00:00Z', tag: 'FLAGGED',   tone: 'caution', body: 'Deepika Rao — Probation review overdue. Auto-escalation triggered at 48h.' },
  { ts: '2026-06-10T11:00:00Z', tag: 'STARTED',   tone: 'neutral', body: 'Mid-FY2026 review cycle opened. 6 assessments pending manager action.' },
]

// --- Helpers ---

const reviewStatusColor = (s: string) =>
  s === 'Overdue'   ? 'text-rakta-critical' :
  s === 'Completed' ? 'text-sthira-forest'  :
  'text-pita-caution'

// --- Page ---

export function TeamPerformance() {
  const goalCols: TableColumn<GoalSummary>[] = [
    { key: 'name',      header: 'Member' },
    { key: 'role',      header: 'Role' },
    {
      key: 'completed',
      header: 'Completed',
      align: 'right',
      render: (r) => <span className="font-tabular-data text-tabular-data text-sthira-forest">{r.completed}</span>,
    },
    {
      key: 'onTrack',
      header: 'On Track',
      align: 'right',
      render: (r) => <span className="font-tabular-data text-tabular-data text-primary">{r.onTrack}</span>,
    },
    {
      key: 'atRisk',
      header: 'At Risk',
      align: 'right',
      render: (r) => (
        <span className={`font-tabular-data text-tabular-data ${r.atRisk > 0 ? 'text-pita-caution' : 'text-on-surface-variant'}`}>
          {r.atRisk}
        </span>
      ),
    },
  ]
  const reviewCols: TableColumn<PendingReview>[] = [
    { key: 'empId',  header: 'ID' },
    { key: 'name',   header: 'Name' },
    { key: 'period', header: 'Period' },
    { key: 'type',   header: 'Type' },
    { key: 'due',    header: 'Due' },
    {
      key: 'status',
      header: 'Status',
      render: (r) => (
        <span className={`font-mono-label text-mono-label uppercase ${reviewStatusColor(r.status)}`}>{r.status}</span>
      ),
    },
  ]

  const atRiskTotal   = GOAL_SUMMARY.reduce((a, g) => a + g.atRisk, 0)
  const completedTotal = GOAL_SUMMARY.reduce((a, g) => a + g.completed, 0)
  const onTrackTotal  = GOAL_SUMMARY.reduce((a, g) => a + g.onTrack, 0)
  const overdueCount  = PENDING_REVIEWS.filter((r) => r.status === 'Overdue').length

  return (
    <>
      <header className="mb-10 border-b border-outline-variant pb-8 flex items-end justify-between gap-6 flex-wrap">
        <div>
          <h2 className="font-serif font-light text-[28px] md:text-[44px] leading-[1.1] text-primary">
            Team Performance
          </h2>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mt-3">
            MGR-22001 · Mid-FY2026 · Review Cycle Open
          </p>
        </div>
        <BinduButton>Open Review Round</BinduButton>
      </header>

      <section className="mb-10">
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-grid-12">
          <SignalCard label="Reviews Pending"   value={String(PENDING_REVIEWS.length)} unit="actions"    cornerTick />
          <SignalCard label="Overdue"           value={String(overdueCount)}           note="Requires immediate action" bindu={overdueCount > 0 ? 'caution' : 'good'} />
          <SignalCard label="Goals Completed"   value={String(completedTotal)}         unit="total"      note="Across team" />
          <SignalCard label="Goals On Track"    value={String(onTrackTotal)}           unit="total" />
          <SignalCard label="Goals At Risk"     value={String(atRiskTotal)}            unit="total"      note="Below target pace" />
        </div>
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-grid-12">
        <div className="lg:col-span-2 space-y-10">
          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Pending Reviews
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={reviewCols} rows={PENDING_REVIEWS} rowKey={(r) => r.empId} />
            </div>
          </section>

          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Goal Status — By Member
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={goalCols} rows={GOAL_SUMMARY} rowKey={(r) => r.name} />
            </div>
          </section>

          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Review Activity
            </h3>
            <CaseTimeline entries={REVIEW_EVENTS} />
          </section>
        </div>

        <div className="space-y-8">
          <RiskCard
            severity="critical"
            title="Overdue Probation Review"
            value={String(overdueCount)}
            body="Deepika Rao's probation assessment passed the 2026-07-01 deadline. HR policy requires completion within 5 days or automatic hold is triggered."
          />
          <RiskCard
            severity="elevated"
            title="Goal At-Risk Count"
            value={String(atRiskTotal)}
            body="5 individual goals across 3 team members are tracking below completion pace for FY2026 mid-year target. Coaching sessions recommended."
          />
          <NarrativePlate kicker="Performance Cycle" source="PERF-TEAM-001">
            Mid-FY2026 review window is open until July 31. One probation review is
            overdue and requires immediate action. Team goal completion rate of{' '}
            {Math.round((completedTotal / (completedTotal + onTrackTotal + atRiskTotal)) * 100)}% is on
            track for the cohort average.
          </NarrativePlate>
        </div>
      </div>
    </>
  )
}
