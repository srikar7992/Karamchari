import { Icon } from '@/components/shell/Icon'
import {
  SignalCard,
  RiskCard,
  NarrativePlate,
  InstitutionalTable,
  CaseTimeline,
  LedgerStrip,
  type TableColumn,
  type TimelineEntry,
} from '@/components/institutional'
import { STATE_LABELS, type OfferState } from '@/lib/offer-lifecycle'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'record-detail',
  signals: 3,
  risks: 1,
  narratives: 1,
  maps: 0,
  bindu: false,
}

// The dossier is evidence; the decisive act (offer) lives on Offer Management,
// the same way the Payroll Cockpit reads and the Run Console acts.

const OFFER_STATE: OfferState = 'approved'

interface Application {
  ref: string
  requisition: string
  role: string
  date: string
  outcome: string
  current: boolean
}

const APPLICATIONS: Application[] = [
  { ref: 'APP-2026-0871', requisition: 'REQ-ENG-014', role: 'Senior Field Operations Engineer', date: '2026-04-28', outcome: 'In process — offer stage', current: true },
  { ref: 'APP-2025-2204', requisition: 'REQ-ENG-009', role: 'Field Operations Engineer II', date: '2025-09-12', outcome: 'Declined offer — compensation', current: false },
]

interface Interview {
  stage: string
  panel: string
  date: string
  score: string
  verdict: 'advance' | 'hold'
}

const INTERVIEWS: Interview[] = [
  { stage: 'Screening', panel: 'R. Kapoor (Talent)', date: '2026-05-04', score: '4.0 / 5', verdict: 'advance' },
  { stage: 'Technical Panel', panel: 'M. Vance · E. Rostova', date: '2026-05-12', score: '4.5 / 5', verdict: 'advance' },
  { stage: 'Operations Case', panel: 'Ananya R. (Hiring Mgr)', date: '2026-05-19', score: '4.2 / 5', verdict: 'advance' },
]

const AUDIT: TimelineEntry[] = [
  {
    ts: '2026-06-09T11:42:00Z',
    tag: 'OFR_APPROVED',
    tone: 'good',
    body: <>Offer <span className="font-mono text-[11px]">OFR-2026-114</span> approved under OFF-AUTH-01 by Ananya R. (hiring authority, REQ-ENG-014). Compensation within band P50–P75; no CHRO endorsement required.</>,
  },
  {
    ts: '2026-06-06T09:10:00Z',
    tag: 'OFR_DRAFTED',
    tone: 'neutral',
    body: <>Offer drafted from template OFR-TPL v3 on workflow RECRUIT-STD-01 v2. Under GF-02 this offer completes on v3 terms regardless of later template changes.</>,
  },
  {
    ts: '2026-05-19T16:05:00Z',
    tag: 'PANEL_DONE',
    tone: 'good',
    body: 'Final interview complete. Composite score 4.23 across three stages; all panels recommended advance.',
  },
  {
    ts: '2026-04-28T08:30:00Z',
    tag: 'APPLIED',
    tone: 'neutral',
    body: 'Application received against REQ-ENG-014 via referral (E. Rostova). Candidate record opened.',
  },
]

const applicationColumns: TableColumn<Application>[] = [
  { key: 'ref', header: 'Application', render: (a) => <span className="font-mono text-[11px] text-primary">{a.ref}</span> },
  { key: 'requisition', header: 'Requisition', render: (a) => <span className="font-mono text-[11px]">{a.requisition}</span> },
  { key: 'role', header: 'Role', render: (a) => <span className="text-primary">{a.role}</span> },
  { key: 'date', header: 'Applied', render: (a) => <span className="font-mono text-[11px]">{a.date}</span> },
  {
    key: 'outcome',
    header: 'Outcome',
    render: (a) => (
      <span className="flex items-center gap-2">
        <span className={`bindu ${a.current ? 'good' : 'neutral'}`} />
        <span className="text-on-surface-variant">{a.outcome}</span>
      </span>
    ),
  },
]

const interviewColumns: TableColumn<Interview>[] = [
  { key: 'stage', header: 'Stage', render: (i) => <span className="text-primary">{i.stage}</span> },
  { key: 'panel', header: 'Panel', render: (i) => <span className="text-on-surface-variant">{i.panel}</span> },
  { key: 'date', header: 'Date', render: (i) => <span className="font-mono text-[11px]">{i.date}</span> },
  { key: 'score', header: 'Score', align: 'right', render: (i) => <span className="font-mono text-[12px]">{i.score}</span> },
  {
    key: 'verdict',
    header: 'Verdict',
    render: (i) => (
      <span className="flex items-center gap-2">
        <span className={`bindu ${i.verdict === 'advance' ? 'good' : 'caution'}`} />
        <span className="font-mono text-[10px] uppercase tracking-wider">{i.verdict}</span>
      </span>
    ),
  },
]

export function CandidateDetail() {
  return (
    <>
      {/* Dossier header */}
      <div className="mb-12">
        <div className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-4 flex items-center">
          <Icon name="arrow_back" className="!text-[14px] mr-2" />
          <a href="#/candidates" className="hover:text-primary transition-colors cursor-pointer">Candidate Pipeline</a>
          <span className="mx-2">/</span> Candidate Dossier
        </div>
        <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
          <div>
            <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight mb-2">
              Dev Sharma
            </h2>
            <p className="font-pull-quote text-pull-quote text-on-surface-variant italic">
              Senior Field Operations Engineer · Candidate
            </p>
          </div>
          <a
            href="#/offer-management"
            className="px-[22px] py-[14px] bg-transparent border border-outline-variant text-primary font-tabular-data text-tabular-data hover:bg-surface-container-low transition-colors flex items-center self-start md:self-auto"
          >
            <Icon name="history_edu" className="!text-[18px] mr-2" /> Open Offer Management
          </a>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-grid-12 mb-10">
        {/* Identity rail */}
        <div className="lg:col-span-4">
          <div className="bg-surface-container-lowest hairline-all corner-tick relative p-6 h-full flex flex-col">
            <div className="flex justify-between items-start mb-6">
              <div className="font-mono-label text-mono-label text-on-surface-variant uppercase">Identity</div>
              <div className="flex items-center gap-2 font-mono-label text-mono-label text-on-surface-variant">
                <span className="bindu neutral" /> Pre-Institutional
              </div>
            </div>
            <div className="aspect-square w-32 bg-ivory-2 hairline-all mb-6 flex items-center justify-center">
              <span className="font-serif font-light text-[28px] md:text-[44px] text-graphite tracking-tight">DS</span>
            </div>
            <div className="space-y-4 font-tabular-data text-tabular-data">
              <div>
                <div className="text-on-surface-variant text-[12px] mb-1">Candidate ID</div>
                <div className="text-primary font-medium font-mono text-[13px]">CND-2026-0871</div>
              </div>
              <div>
                <div className="text-on-surface-variant text-[12px] mb-1">Requisition</div>
                <div className="text-primary font-mono text-[13px]">REQ-ENG-014</div>
              </div>
              <div>
                <div className="text-on-surface-variant text-[12px] mb-1">Source</div>
                <div className="text-primary">Referral · E. Rostova</div>
              </div>
              <div>
                <div className="text-on-surface-variant text-[12px] mb-1">Location</div>
                <div className="text-primary flex items-center">
                  <Icon name="location_on" className="!text-[16px] mr-1 text-on-surface-variant" />
                  Pune Node (Field)
                </div>
              </div>
            </div>
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-6 pt-4 hairline-t">
              No employee identity exists for this person. Under HIRE-ID-01 one is created only by the
              hire act on an accepted offer — never by editing this record.
            </p>
          </div>
        </div>

        {/* Signals + offer/workflow state */}
        <div className="lg:col-span-8 flex flex-col gap-grid-12">
          <div className="grid grid-cols-1 xl:grid-cols-3 gap-grid-12">
            <SignalCard label="Composite Score" value="4.23" unit="/ 5" note="Three stages, all advance" bindu="good" />
            <SignalCard label="Days in Pipeline" value="45" note="Median for grade: 38 days" bindu="caution" />
            <SignalCard label="Offer State" value={STATE_LABELS[OFFER_STATE]} note="OFR-2026-114 · template v3" bindu="neutral" />
          </div>

          <div className="bg-surface-container-low hairline-all p-6 flex-1">
            <div className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-6 border-b border-outline-variant pb-2">
              Workflow State
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-6 font-tabular-data text-tabular-data">
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Definition</div>
                <div className="text-primary font-mono text-[13px]">RECRUIT-STD-01 v2</div>
              </div>
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Current Node</div>
                <div className="text-primary">Offer Extension</div>
              </div>
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Authority</div>
                <div className="text-primary font-mono text-[13px]">OFF-AUTH-01</div>
              </div>
            </div>
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-6">
              This case started on v2 and completes on v2 (GF-02), whatever the studio publishes
              meanwhile. The offer act and its authority are recorded on the offer ledger, not here.
            </p>
          </div>

          <RiskCard
            severity="elevated"
            title="Reference Check Outstanding"
            body="Two of three references verified. RECRUIT-STD-01 v2 blocks the hire act until verification completes; the offer may still be extended. Due 2026-06-16."
          />
        </div>
      </div>

      {/* Application history */}
      <section className="bg-surface-container-lowest hairline-all p-6 mb-10">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-6 uppercase tracking-widest border-b border-outline-variant pb-2">
          Application History
        </h3>
        <InstitutionalTable columns={applicationColumns} rows={APPLICATIONS} rowKey={(a) => a.ref} />
      </section>

      {/* Interviews & feedback */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-grid-12 mb-10">
        <section className="bg-surface-container-lowest hairline-all p-6">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-6 uppercase tracking-widest border-b border-outline-variant pb-2">
            Interview Record
          </h3>
          <InstitutionalTable columns={interviewColumns} rows={INTERVIEWS} rowKey={(i) => i.stage} />
        </section>

        <NarrativePlate source="Buddhi · panel feedback synthesis, 2026-05-20">
          "Three panels converge on the same read: strong field judgment, calm under coverage pressure,
          gaps in statutory paperwork that training closes in a quarter. The prior decline was about
          compensation, not fit — the band has since moved. The risk is not capability; it is losing
          forty-five days of pipeline to a competing offer."
        </NarrativePlate>
      </div>

      {/* Audit timeline */}
      <section className="bg-surface-container-lowest hairline-all p-6">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-6 uppercase tracking-widest border-b border-outline-variant pb-2">
          Audit Timeline
        </h3>
        <CaseTimeline entries={AUDIT} />
      </section>

      <LedgerStrip
        className="mt-8"
        entries={[
          { label: 'Record', value: 'CND-2026-0871' },
          { label: 'Workflow', value: 'RECRUIT-STD-01 v2' },
          { label: 'Offer', value: `OFR-2026-114 · ${STATE_LABELS[OFFER_STATE]}` },
          { label: 'Last Mutation', value: 'OFR_APPROVED by Ananya R.' },
        ]}
      />
    </>
  )
}
