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

interface BandRecord {
  name: string
  grade: string
  band: string
  bandMin: string
  bandMid: string
  bandMax: string
  currentCtc: string
  position: string
  lastRevision: string
}
const BAND_RECORDS: BandRecord[] = [
  { name: 'Asha Venkatesan',  grade: 'G4', band: 'B4', bandMin: '₹1,50,000', bandMid: '₹1,80,000', bandMax: '₹2,10,000', currentCtc: '₹1,86,000', position: 'Mid',   lastRevision: 'Apr 2026' },
  { name: 'Rajiv Mohan',       grade: 'G4', band: 'B4', bandMin: '₹1,50,000', bandMid: '₹1,80,000', bandMax: '₹2,10,000', currentCtc: '₹1,60,000', position: 'Low',   lastRevision: 'Apr 2026' },
  { name: 'Priya Sharma',       grade: 'G5', band: 'B5', bandMin: '₹1,90,000', bandMid: '₹2,30,000', bandMax: '₹2,70,000', currentCtc: '₹2,35,000', position: 'Mid',   lastRevision: 'Apr 2026' },
  { name: 'Kiran Nair',         grade: 'G6', band: 'B6', bandMin: '₹2,50,000', bandMid: '₹3,10,000', bandMax: '₹3,70,000', currentCtc: '₹3,15,000', position: 'Mid',   lastRevision: 'Apr 2025' },
  { name: 'Deepika Rao',        grade: 'G4', band: 'B4', bandMin: '₹1,50,000', bandMid: '₹1,80,000', bandMax: '₹2,10,000', currentCtc: '₹1,50,000', position: 'Floor', lastRevision: 'Sep 2025' },
  { name: 'Suresh Kumar',       grade: 'G4', band: 'B4', bandMin: '₹1,50,000', bandMid: '₹1,80,000', bandMax: '₹2,10,000', currentCtc: '₹1,72,000', position: 'Low',   lastRevision: 'Apr 2026' },
  { name: 'Meena Pillai',       grade: 'G5', band: 'B5', bandMin: '₹1,90,000', bandMid: '₹2,30,000', bandMax: '₹2,70,000', currentCtc: '₹2,55,000', position: 'High',  lastRevision: 'Apr 2026' },
  { name: 'Arun Krishnamurthy', grade: 'G6', band: 'B6', bandMin: '₹2,50,000', bandMid: '₹3,10,000', bandMax: '₹3,70,000', currentCtc: '₹3,60,000', position: 'High',  lastRevision: 'Apr 2026' },
]

interface PendingRevision { name: string; currentCtc: string; proposedCtc: string; reason: string; status: string }
const PENDING_REVISIONS: PendingRevision[] = [
  { name: 'Kiran Nair',   currentCtc: '₹3,15,000', proposedCtc: '₹3,40,000', reason: 'Promotion to G6 Sr.',   status: 'Pending HR' },
  { name: 'Rajiv Mohan',  currentCtc: '₹1,60,000', proposedCtc: '₹1,75,000', reason: 'Annual increment cycle', status: 'Draft' },
]

// --- Helpers ---

const positionColor = (p: string) =>
  p === 'High'  ? 'text-yantra-indigo' :
  p === 'Floor' ? 'text-rakta-critical':
  p === 'Low'   ? 'text-pita-caution'  :
  'text-on-surface-variant'

// --- Page ---

export function TeamCompensation() {
  const bandCols: TableColumn<BandRecord>[] = [
    { key: 'name',       header: 'Member' },
    { key: 'grade',      header: 'Grade' },
    { key: 'band',       header: 'Band' },
    { key: 'bandMin',    header: 'Band Min',    align: 'right' },
    { key: 'bandMid',    header: 'Band Mid',    align: 'right' },
    { key: 'bandMax',    header: 'Band Max',    align: 'right' },
    { key: 'currentCtc', header: 'Current CTC', align: 'right' },
    {
      key: 'position',
      header: 'Position',
      render: (r) => (
        <span className={`font-mono-label text-mono-label uppercase ${positionColor(r.position)}`}>{r.position}</span>
      ),
    },
    { key: 'lastRevision', header: 'Last Revised' },
  ]
  const revisionCols: TableColumn<PendingRevision>[] = [
    { key: 'name',        header: 'Member' },
    { key: 'currentCtc',  header: 'Current CTC',  align: 'right' },
    { key: 'proposedCtc', header: 'Proposed CTC', align: 'right' },
    { key: 'reason',      header: 'Reason' },
    {
      key: 'status',
      header: 'Status',
      render: (r) => (
        <span className="font-mono-label text-mono-label uppercase text-pita-caution">{r.status}</span>
      ),
    },
  ]

  const totalMonthlyCost = 19_33_000
  const avgCtc = Math.round(totalMonthlyCost / BAND_RECORDS.length)
  const bandOutliers = BAND_RECORDS.filter((r) => r.position === 'Floor' || r.position === 'High').length

  return (
    <>
      <header className="mb-10 border-b border-outline-variant pb-8">
        <h2 className="font-serif font-light text-[28px] md:text-[44px] leading-[1.1] text-primary">
          Team Compensation
        </h2>
        <p className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mt-3">
          MGR-22001 · FY2026 · Band Cycle Apr 2026
        </p>
      </header>

      <section className="mb-10">
        <div className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-4 gap-grid-12">
          <SignalCard
            label="Total Monthly Cost"
            value="₹19.3L"
            note="8 headcount, all-in CTC"
            cornerTick
          />
          <SignalCard label="Average CTC" value={`₹${(avgCtc / 1000).toFixed(0)}K`} unit="/mo" note="Incl. G4–G6 mix" />
          <SignalCard label="Pending Revisions" value={String(PENDING_REVISIONS.length)} note="Awaiting HR sign-off" bindu={PENDING_REVISIONS.length > 0 ? 'caution' : 'good'} />
          <SignalCard label="Band Outliers" value={String(bandOutliers)} note="Floor or High position" />
        </div>
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-grid-12">
        <div className="lg:col-span-2 space-y-10">
          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Band Positions
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={bandCols} rows={BAND_RECORDS} rowKey={(r) => r.name} />
            </div>
          </section>

          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Pending Revisions
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={revisionCols} rows={PENDING_REVISIONS} rowKey={(r) => r.name} />
            </div>
          </section>
        </div>

        <div className="space-y-8">
          <RiskCard
            severity="monitored"
            title="Band Outliers"
            value={String(bandOutliers)}
            body="Deepika Rao is at band floor (probation rate). Arun Krishnamurthy and Meena Pillai are in high position — both eligible for promotion band review at FY end."
          />
          <NarrativePlate kicker="Compensation Summary" source="COMP-TEAM-001">
            Team sits across G4–G6 grades. 2 pending revisions are in the HR queue. One member is at
            band floor (probation). Band ceiling reviews for 2 high-position members are scheduled
            for Q4 FY2026.
          </NarrativePlate>
        </div>
      </div>
    </>
  )
}
