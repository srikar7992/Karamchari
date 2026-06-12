import { useState } from 'react'
import {
  SignalCard,
  RiskCard,
  BinduButton,
  InstitutionalTable,
  CaseTimeline,
  LedgerStrip,
  type TableColumn,
  type TimelineEntry,
} from '@/components/institutional'
import {
  OFFER_SEQUENCE,
  STATE_LABELS,
  FORWARD_VERBS,
  IRREVERSIBLE_FROM,
  nextForward,
  canTransition,
  transition,
  type OfferState,
} from '@/lib/offer-lifecycle'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'approval',
  signals: 3,
  risks: 1,
  narratives: 0,
  maps: 0,
  bindu: true,
}

interface QueuedOffer {
  ref: string
  candidate: string
  requisition: string
  state: string
  note: string
  gated: boolean
}

const OTHER_OFFERS: QueuedOffer[] = [
  { ref: 'OFR-2026-117', candidate: 'L. Fernandes', requisition: 'REQ-OPS-021', state: 'Draft', note: 'Comp at P82 — CHRO endorsement pending per OFF-AUTH-01', gated: true },
  { ref: 'OFR-2026-112', candidate: 'T. Joshi', requisition: 'REQ-ENG-014', state: 'Extended', note: 'Candidate response due 2026-06-18', gated: false },
  { ref: 'OFR-2026-108', candidate: 'P. Nair', requisition: 'REQ-OPS-019', state: 'Declined', note: 'Counter-offer at current employer. Ledger sealed.', gated: false },
]

// Sutra entries preceding this session: drafted on template v3, approved under
// OFF-AUTH-01. This console picks the offer up at Approved.
const SEED_LEDGER: TimelineEntry[] = [
  {
    ts: '2026-06-09T11:42:00Z',
    tag: 'APPROVED',
    tone: 'good',
    body: <>Draft → Approved, actor: Ananya R., reason: Hiring authority for REQ-ENG-014 under OFF-AUTH-01. Compensation at band P64 — no CHRO endorsement required.</>,
  },
  {
    ts: '2026-06-06T09:10:00Z',
    tag: 'DRAFTED',
    tone: 'neutral',
    body: <>Offer <span className="font-mono text-[11px]">OFR-2026-114</span> drafted from template OFR-TPL v3 on workflow RECRUIT-STD-01 v2. Under GF-02 this offer completes on v3 terms.</>,
  },
]

const TRANSITION_REASONS: Partial<Record<OfferState, string>> = {
  approved: 'Re-approved after draft correction under OFF-AUTH-01.',
  extended: 'Offer released to candidate via institutional channel. Validity 10 days; terms sealed as v3.',
  accepted: 'Signed acceptance received and archived as evidence DOC-ACC-2214.',
  hired: 'Employee identity created under HIRE-ID-01 from accepted offer OFR-2026-114 v3, actor Ananya R., authority OFF-AUTH-01. Onboarding case opened.',
}

const ACTOR = 'Ananya R.'

const queueColumns: TableColumn<QueuedOffer>[] = [
  { key: 'ref', header: 'Offer', render: (o) => <span className="font-mono text-[11px] text-primary">{o.ref}</span> },
  { key: 'candidate', header: 'Candidate', render: (o) => <span className="text-primary">{o.candidate}</span> },
  { key: 'requisition', header: 'Requisition', render: (o) => <span className="font-mono text-[11px]">{o.requisition}</span> },
  {
    key: 'state',
    header: 'State',
    render: (o) => (
      <span className="flex items-center gap-2">
        <span className={`bindu ${o.gated ? 'caution' : o.state === 'Declined' ? 'neutral' : 'good'}`} />
        <span className="font-mono text-[10px] uppercase tracking-wider">{o.state}</span>
      </span>
    ),
  },
  { key: 'note', header: 'Position', render: (o) => <span className="text-on-surface-variant">{o.note}</span> },
]

export function OfferManagement() {
  const [state, setState] = useState<OfferState>('approved')
  const [ledger, setLedger] = useState<TimelineEntry[]>(SEED_LEDGER)

  const next = nextForward(state)
  const verb = FORWARD_VERBS[state]
  const stateIdx = OFFER_SEQUENCE.indexOf(state)
  const reopenable = state !== 'draft' && canTransition(state, 'draft')
  const declinable = canTransition(state, 'declined')
  const terminal = state === 'hired' || state === 'declined'

  const advance = () => {
    if (!next) return
    const rec = transition(state, next, ACTOR, TRANSITION_REASONS[next] ?? '')
    setLedger((l) => [
      ...l,
      {
        ts: rec.ts,
        tag: STATE_LABELS[next].toUpperCase(),
        tone: next === 'hired' ? 'copper' : 'good',
        body: <>{STATE_LABELS[rec.from]} → {STATE_LABELS[rec.to]}, actor: {rec.actor}, reason: {rec.reason}</>,
      },
    ])
    setState(next)
  }

  const reopen = () => {
    if (!canTransition(state, 'draft')) return
    const rec = transition(state, 'draft', ACTOR, 'Returned to draft for term correction before extension. Approval discarded; re-approval required under OFF-AUTH-01.')
    setLedger((l) => [
      ...l,
      { ts: rec.ts, tag: 'REOPENED', tone: 'caution', body: <>{STATE_LABELS[rec.from]} → Draft, actor: {rec.actor}, reason: {rec.reason}</> },
    ])
    setState('draft')
  }

  const decline = () => {
    if (!canTransition(state, 'declined')) return
    const rec = transition(state, 'declined', ACTOR, 'Candidate declined in writing. Terms sealed; ledger closed. Re-engagement requires a new offer.')
    setLedger((l) => [
      ...l,
      { ts: rec.ts, tag: 'DECLINED', tone: 'critical', body: <>{STATE_LABELS[rec.from]} → Declined, actor: {rec.actor}, reason: {rec.reason}</> },
    ])
    setState('declined')
  }

  return (
    <>
      {/* State header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Recruitment · Offer <span className="font-mono">OFR-2026-114</span> · Dev Sharma ·{' '}
            <a href="#/candidate-detail" className="underline hover:text-primary transition-colors">Dossier</a>
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Offer Management
          </h2>
        </div>
        <div className="flex items-center gap-3 font-mono text-[12px] text-on-surface-variant uppercase tracking-widest">
          <span className={`bindu ${state === 'hired' ? 'good' : state === 'declined' ? 'neutral' : 'caution'}`} />
          State: <span className="text-primary">{STATE_LABELS[state]}</span>
        </div>
      </div>

      {/* Offer lifecycle — identity creation as progression through states */}
      <section className="mb-10">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
          1. Offer Lifecycle
        </h3>
        <div className="flex flex-col lg:flex-row border border-outline-variant bg-surface">
          {OFFER_SEQUENCE.map((s, i) => {
            const done = i < stateIdx && state !== 'declined'
            const active = s === state
            return (
              <div
                key={s}
                className={`flex-1 p-5 relative ${i < OFFER_SEQUENCE.length - 1 ? 'border-b lg:border-b-0 lg:border-r border-outline-variant' : ''} ${
                  active ? 'bg-surface-container-low' : ''
                } ${!done && !active ? 'opacity-50' : ''}`}
              >
                {active && <div className="absolute inset-x-0 top-0 h-[2px] bg-tamra-copper" />}
                <div className="flex justify-between items-start gap-2 mb-6">
                  <span className={`font-mono-label text-mono-label ${active ? 'text-tamra-copper' : 'text-on-surface-variant'}`}>
                    {String(i + 1).padStart(2, '0')}
                  </span>
                  {s === IRREVERSIBLE_FROM && (
                    <span className="font-mono text-[9px] text-on-surface-variant uppercase tracking-widest whitespace-nowrap">No return</span>
                  )}
                </div>
                <div className={`font-body-standard text-body-standard ${active ? 'text-primary font-medium' : 'text-on-surface-variant'}`}>
                  {STATE_LABELS[s]}
                </div>
                <div className={`font-mono-label text-mono-label mt-1 ${active ? 'text-tamra-copper' : 'text-outline-variant'}`}>
                  {done ? 'SEALED' : active ? 'CURRENT' : 'AHEAD'}
                </div>
              </div>
            )
          })}
        </div>
        {state === 'declined' && (
          <p className="font-mono text-[11px] text-on-surface-variant uppercase tracking-widest mt-3">
            Terminal fork: Extended → Declined. Forward path closed; ledger sealed.
          </p>
        )}
      </section>

      {/* Signals */}
      <section className="grid grid-cols-1 sm:grid-cols-3 gap-grid-12 mb-10">
        <SignalCard label="Offers in Cycle" value="4" note="1 gated on CHRO endorsement" bindu="caution" />
        <SignalCard label="Acceptance Rate (Q2)" value="78" unit="%" note="11 of 14 extended offers accepted" bindu="good" />
        <SignalCard label="Median Days to Accept" value="6.5" note="Validity window: 10 days" bindu="neutral" />
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-grid-12 mb-10">
        {/* Authority & citations */}
        <section className="lg:col-span-7">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
            2. Authority &amp; Citations
          </h3>
          <div className="bg-surface-container-lowest hairline-all p-6">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-6 font-tabular-data text-tabular-data">
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Law</div>
                <div className="text-primary font-mono text-[13px]">OFF-AUTH-01 · HIRE-ID-01</div>
              </div>
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Workflow</div>
                <div className="text-primary font-mono text-[13px]">RECRUIT-STD-01 v2</div>
              </div>
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Offer Version</div>
                <div className="text-primary font-mono text-[13px]">OFR-TPL v3 (grandfathered, GF-02)</div>
              </div>
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Evidence</div>
                <div className="text-primary font-mono text-[13px]">DOC-CMP-1180 · band check P64</div>
              </div>
            </div>
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-6">
              Every act on this console cites its law and lands on the offer ledger with actor and
              authority — the payroll authority model, reused. The hire act is the boundary where a
              candidate becomes an employee: HIRE-ID-01 permits identity creation only here, from an
              accepted offer, never by data entry.
            </p>
          </div>
        </section>

        {/* Controls */}
        <section className="lg:col-span-5">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
            3. Offer Controls
          </h3>
          <div className="bg-surface-container-lowest hairline-all p-6">
            {state === 'accepted' && (
              <RiskCard
                className="mb-6"
                severity="elevated"
                title="Hire Act Pending Reference Check"
                body="RECRUIT-STD-01 v2 requires all references verified before identity creation. Two of three complete; due 2026-06-16. The act below executes against that gate in the engine."
              />
            )}
            <div className="flex flex-col gap-3">
              {verb ? (
                <BinduButton onClick={advance} className="py-3">
                  {verb}
                </BinduButton>
              ) : (
                <div className="py-3 text-center border border-outline-variant font-mono-label text-mono-label uppercase tracking-widest text-on-surface-variant">
                  {state === 'hired' ? 'Identity Created · Ledger Sealed' : 'Offer Declined · Ledger Sealed'}
                </div>
              )}
              {reopenable && (
                <button
                  onClick={reopen}
                  className="py-3 bg-transparent border border-outline-variant text-on-surface-variant font-mono-label text-mono-label uppercase tracking-widest hover:bg-surface-container-low hover:text-primary transition-colors"
                >
                  Return to Draft
                </button>
              )}
              {declinable && (
                <button
                  onClick={decline}
                  className="py-3 bg-transparent border border-outline-variant text-on-surface-variant font-mono-label text-mono-label uppercase tracking-widest hover:bg-surface-container-low hover:text-primary transition-colors"
                >
                  Record Decline
                </button>
              )}
            </div>
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-6">
              {terminal
                ? 'Terminal state. No transition leaves it; re-engagement means a new offer with its own ledger.'
                : stateIdx >= OFFER_SEQUENCE.indexOf(IRREVERSIBLE_FROM)
                  ? 'Past the extension boundary. The candidate holds the terms; the offer cannot be silently rewritten. A changed offer is a new version under GF-02.'
                  : 'Before the extension boundary. The offer may return to Draft; once Extended, no return path exists in the lifecycle model.'}
            </p>
          </div>
        </section>
      </div>

      {/* Offer queue */}
      <section className="bg-surface-container-lowest hairline-all p-6 mb-10">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-6 uppercase tracking-widest">
          4. Offers in Cycle
        </h3>
        <InstitutionalTable columns={queueColumns} rows={OTHER_OFFERS} rowKey={(o) => o.ref} />
      </section>

      {/* Offer ledger */}
      <section className="bg-surface-container-lowest hairline-all p-6">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-6 uppercase tracking-widest">
          5. Offer Ledger
        </h3>
        <CaseTimeline entries={ledger} />
      </section>

      <LedgerStrip
        className="mt-8"
        entries={[
          { label: 'Offer', value: 'OFR-2026-114' },
          { label: 'Lifecycle', value: 'offer-lifecycle v1 · 5 forward states + declined fork' },
          { label: 'Laws', value: 'OFF-AUTH-01 · HIRE-ID-01 · GF-02' },
        ]}
      />
    </>
  )
}
