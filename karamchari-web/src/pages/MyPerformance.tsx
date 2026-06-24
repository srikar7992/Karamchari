import { SignalCard, NarrativePlate, RiskCard, InstitutionalTable, CaseTimeline } from '@/components/institutional'
import type { TableColumn, TimelineEntry } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EMP',
  archetype: 'dashboard',
  signals: 3,
  risks: 1,
  narratives: 1,
  maps: 0,
  bindu: false,
}

// --- Data ---

interface Goal { goal: string; category: string; progress: number; due: string; status: string }
const GOALS: Goal[] = [
  { goal: 'Reduce Zone Downtime by 15%', category: 'Operational',  progress: 80,  due: '2026-09-30', status: 'On Track' },
  { goal: 'Complete 4 Learning Modules', category: 'Development',  progress: 25,  due: '2026-12-31', status: 'At Risk' },
  { goal: 'Lead Q2 Cross-Zone Audit',    category: 'Leadership',   progress: 100, due: '2026-06-30', status: 'Completed' },
]

interface Review { period: string; rating: string; reviewer: string; date: string }
const REVIEWS: Review[] = [
  { period: 'FY2025',     rating: 'Exceeds Expectations', reviewer: 'Ananya Rajesh', date: '2026-01-15' },
  { period: 'Mid-FY2025', rating: 'Meets Expectations',   reviewer: 'Ananya Rajesh', date: '2025-07-10' },
]

const FEEDBACK: TimelineEntry[] = [
  { ts: '2026-06-12T10:30:00Z', tag: 'POSITIVE', tone: 'good',    body: 'Ananya Rajesh — Commended for Q2 audit leadership — decisive and well-documented' },
  { ts: '2026-05-22T09:00:00Z', tag: 'PEER',     tone: 'neutral', body: 'Priya Sharma — Peer recognition: cross-zone knowledge transfer in May' },
  { ts: '2026-04-15T11:00:00Z', tag: 'COACHING', tone: 'caution', body: 'Ananya Rajesh — Coaching note: learning module lag against FY2026 target' },
]

// --- Helpers ---

const goalStatusColor = (s: string) =>
  s === 'Completed' ? 'text-sthira-forest' :
  s === 'At Risk'   ? 'text-pita-caution'  :
  'text-primary'

// --- Page ---

export function MyPerformance() {
  const goalCols: TableColumn<Goal>[] = [
    { key: 'goal',     header: 'Goal' },
    { key: 'category', header: 'Category' },
    { key: 'due',      header: 'Due' },
    {
      key: 'status',
      header: 'Status',
      render: (r) => <span className={`font-mono-label text-mono-label uppercase ${goalStatusColor(r.status)}`}>{r.status}</span>,
    },
    {
      key: 'progress',
      header: 'Progress',
      align: 'right',
      render: (r) => (
        <div className="flex items-center gap-2 justify-end">
          <div className="w-16 h-1 bg-surface-variant">
            <div className="h-1 bg-primary" style={{ width: `${r.progress}%` }} />
          </div>
          <span className="font-tabular-data text-tabular-data text-on-surface-variant w-8">{r.progress}%</span>
        </div>
      ),
    },
  ]
  const reviewCols: TableColumn<Review>[] = [
    { key: 'period',   header: 'Period' },
    { key: 'rating',   header: 'Rating' },
    { key: 'reviewer', header: 'Reviewer' },
    { key: 'date',     header: 'Date' },
  ]

  return (
    <>
      <header className="mb-10 border-b border-outline-variant pb-8">
        <h2 className="font-serif font-light text-[28px] md:text-[44px] leading-[1.1] text-primary">
          My Performance
        </h2>
        <p className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mt-3">
          EMP-88271 · FY2026 · Mid-Year Cycle Open
        </p>
      </header>

      <section className="mb-10">
        <div className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-3 gap-grid-12">
          <SignalCard
            label="Goal Completion"
            value={`${GOALS.filter((g) => g.status === 'Completed').length}/${GOALS.length}`}
            bar={{ pct: 67, tone: 'bg-sthira-forest' }}
            cornerTick
          />
          <SignalCard label="Last Review" value="Exceeds" note="FY2025 Annual · Jan 2026" />
          <SignalCard label="Feedback Received" value={String(FEEDBACK.length)} unit="items" note="Q2 2026" />
        </div>
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-grid-12">
        <div className="lg:col-span-2 space-y-10">
          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Goals — FY2026
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={goalCols} rows={GOALS} rowKey={(r) => r.goal} />
            </div>
          </section>

          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Review History
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={reviewCols} rows={REVIEWS} rowKey={(r) => r.period} />
            </div>
          </section>

          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Feedback
            </h3>
            <CaseTimeline entries={FEEDBACK} />
          </section>
        </div>

        <div className="space-y-8">
          <RiskCard
            severity="elevated"
            title="Learning Gap"
            value="1 / 4"
            body="Modules completed against FY2026 development target. Mid-year check-in scheduled Aug 2026."
          />
          <NarrativePlate kicker="Development Summary" source="PERF-INSIGHT-01">
            Strong operational delivery in FY2025. The open development goal is the primary
            risk for the FY2026 rating. Mid-year review window closes September 30.
          </NarrativePlate>
        </div>
      </div>
    </>
  )
}
