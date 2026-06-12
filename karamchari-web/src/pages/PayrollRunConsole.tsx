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
  RUN_SEQUENCE,
  STATE_LABELS,
  FORWARD_VERBS,
  IRREVERSIBLE_FROM,
  nextForward,
  canTransition,
  transition,
  type RunState,
} from '@/lib/payroll-lifecycle'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'command-center',
  signals: 4,
  risks: 1,
  narratives: 0,
  maps: 1,
  bindu: true,
}

interface Anomaly {
  id: string
  severity: 'critical' | 'caution'
  amount: string
  note: string
}

const ANOMALIES: Anomaly[] = [
  { id: 'ANM-8842', severity: 'critical', amount: 'USD 4,200.00', note: 'Missing routing number for new hire (J. Doe). Blocks lock.' },
  { id: 'ANM-8845', severity: 'caution', amount: 'USD 850.00', note: 'Unapproved overtime hours submitted post-cutoff.' },
  { id: 'ANM-8850', severity: 'caution', amount: 'USD 12,400.00', note: 'Bonus payout exceeds standard deviation threshold.' },
]

// Sutra entries that precede this session: the run was created, the first
// calculation attempt failed mid-batch, and the engine resumed from checkpoint.
const SEED_LEDGER: TimelineEntry[] = [
  {
    ts: '2026-06-10T04:00:11Z',
    tag: 'CREATED',
    tone: 'neutral',
    body: <>Run <span className="font-mono text-[11px]">PR-2026-06-A</span> opened in Draft by schedule PAY-CYCLE-STD. Scope: 1,247 operators.</>,
  },
  {
    ts: '2026-06-10T04:18:47Z',
    tag: 'CALC_FAIL',
    tone: 'critical',
    body: <>Calculation halted at batch 31/52: statutory table v2026.2 unreachable. Run held in Draft; no partial results committed.</>,
  },
  {
    ts: '2026-06-10T05:02:03Z',
    tag: 'CALC_RESUME',
    tone: 'good',
    body: <>Engine resumed from checkpoint batch 31 after table mirror recovered. Batches 31–52 completed. Draft → Calculated, actor: payroll-engine, reason: full recalculation verified against checkpoint hashes.</>,
  },
]

const TRANSITION_REASONS: Partial<Record<RunState, string>> = {
  validated: 'All 1,247 operator records within tolerance. Variance +2.4% gross, explained by approved increments.',
  locked: 'Anomaly queue clear of blocking items. Figures frozen; corrections now route to Corrections Console.',
  approved: 'Authorized under payroll authority PAY-AUTH-04. Net disbursement USD 2,981,402.10.',
  published: 'Bank file BF-2026-06-A released to Institutional Clearing. Settlement 2026-06-30.',
}

const ACTOR = 'Ananya R.'

export function PayrollRunConsole() {
  const [state, setState] = useState<RunState>('calculated')
  const [ledger, setLedger] = useState<TimelineEntry[]>(SEED_LEDGER)
  const [resolved, setResolved] = useState<Set<string>>(new Set())

  const openAnomalies = ANOMALIES.filter((a) => !resolved.has(a.id))
  const blockingOpen = openAnomalies.filter((a) => a.severity === 'critical').length
  const next = nextForward(state)
  const verb = FORWARD_VERBS[state]
  const lockGated = state === 'validated' && blockingOpen > 0
  const stateIdx = RUN_SEQUENCE.indexOf(state)
  const reopenable = state !== 'draft' && canTransition(state, 'draft')

  const advance = () => {
    if (!next || lockGated) return
    const rec = transition(state, next, ACTOR, TRANSITION_REASONS[next] ?? '')
    setLedger((l) => [
      ...l,
      {
        ts: rec.ts,
        tag: STATE_LABELS[next].toUpperCase(),
        tone: next === 'published' ? 'copper' : 'good',
        body: <>{STATE_LABELS[rec.from]} → {STATE_LABELS[rec.to]}, actor: {rec.actor}, reason: {rec.reason}</>,
      },
    ])
    setState(next)
  }

  const reopen = () => {
    if (!canTransition(state, 'draft')) return
    const rec = transition(state, 'draft', ACTOR, 'Reopened for correction before lock. Calculated figures discarded.')
    setLedger((l) => [
      ...l,
      { ts: rec.ts, tag: 'REOPENED', tone: 'caution', body: <>{STATE_LABELS[rec.from]} → Draft, actor: {rec.actor}, reason: {rec.reason}</> },
    ])
    setState('draft')
  }

  const resolve = (id: string) => setResolved((s) => new Set(s).add(id))

  const anomalyColumns: TableColumn<Anomaly>[] = [
    {
      key: 'id',
      header: 'Ref',
      render: (a) => (
        <span className="flex items-center gap-2">
          <span className={`bindu ${a.severity === 'critical' ? 'rakta-critical' : 'caution'}`} />
          <span className="font-mono text-[11px]">{a.id}</span>
        </span>
      ),
    },
    { key: 'note', header: 'Finding', render: (a) => <span className="text-primary">{a.note}</span> },
    { key: 'amount', header: 'Exposure', align: 'right', render: (a) => <span className="font-mono text-[12px]">{a.amount}</span> },
    {
      key: 'act',
      header: '',
      align: 'right',
      render: (a) => (
        <button
          onClick={() => resolve(a.id)}
          className="px-3 py-1 border border-outline-variant font-mono-label text-mono-label uppercase text-on-surface-variant hover:bg-surface-container-low hover:text-primary transition-colors"
        >
          Resolve
        </button>
      ),
    },
  ]

  return (
    <>
      {/* State header — the phase is always knowable at a glance */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Pay Period 2026-06 · Run <span className="font-mono">PR-2026-06-A</span>
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Payroll Run Console
          </h2>
        </div>
        <div className="flex items-center gap-3 font-mono text-[12px] text-on-surface-variant uppercase tracking-widest">
          <span className={`bindu ${state === 'published' ? 'good' : 'neutral'}`} />
          State: <span className="text-primary">{STATE_LABELS[state]}</span>
        </div>
      </div>

      {/* Lifecycle topology — authority as progression through states */}
      <section className="mb-10">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
          1. Run Lifecycle
        </h3>
        <div className="flex flex-col lg:flex-row border border-outline-variant bg-surface">
          {RUN_SEQUENCE.map((s, i) => {
            const done = i < stateIdx
            const active = i === stateIdx
            return (
              <div
                key={s}
                className={`flex-1 p-5 relative ${i < RUN_SEQUENCE.length - 1 ? 'border-b lg:border-b-0 lg:border-r border-outline-variant' : ''} ${
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
      </section>

      {/* Health row */}
      <section className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-grid-12 mb-10">
        <SignalCard label="Operators in Scope" value="1,247" note="52 calculation batches, all committed" bindu="good" />
        <SignalCard
          label="Exceptions Open"
          value={String(openAnomalies.length)}
          note={blockingOpen > 0 ? `${blockingOpen} blocking lock` : 'None blocking'}
          bindu={blockingOpen > 0 ? 'rakta-critical' : openAnomalies.length > 0 ? 'caution' : 'good'}
        />
        <SignalCard label="Gross Variance" value="+2.4" unit="% vs prior" note="Explained: approved increments cycle 2026-06" bindu="neutral" />
        <SignalCard label="Calc Duration" value="44:16" unit="min" note="Includes checkpoint resume after batch 31 halt" bindu="neutral" />
      </section>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-grid-12 mb-10">
        {/* Anomaly queue */}
        <section className="md:col-span-7">
          <div className="flex justify-between items-end mb-4">
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest">
              2. Anomaly Queue
            </h3>
            <span className={`font-mono-label text-mono-label ${blockingOpen > 0 ? 'text-rakta-critical' : 'text-on-surface-variant'}`}>
              {openAnomalies.length} OPEN
            </span>
          </div>
          <div className="bg-surface-container-lowest hairline-all p-6">
            {openAnomalies.length > 0 ? (
              <InstitutionalTable columns={anomalyColumns} rows={openAnomalies} rowKey={(a) => a.id} />
            ) : (
              <p className="font-tabular-data text-tabular-data text-on-surface-variant py-4">
                Queue clear. All findings resolved and recorded.
              </p>
            )}
          </div>
        </section>

        {/* Run controls */}
        <section className="md:col-span-5">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
            3. Run Controls
          </h3>
          <div className="bg-surface-container-lowest hairline-all p-6">
            {lockGated && (
              <RiskCard
                className="mb-6"
                severity="critical"
                title="Lock Gated"
                body={`${blockingOpen} blocking ${blockingOpen === 1 ? 'anomaly' : 'anomalies'} open. Locking freezes figures; resolve the queue first.`}
              />
            )}
            <div className="flex flex-col gap-3">
              {verb ? (
                <BinduButton
                  onClick={advance}
                  disabled={lockGated}
                  className="disabled:opacity-40 disabled:cursor-not-allowed py-3"
                >
                  {verb}
                </BinduButton>
              ) : (
                <div className="py-3 text-center border border-outline-variant font-mono-label text-mono-label uppercase tracking-widest text-on-surface-variant">
                  Run Archived · Sutra Ledger Sealed
                </div>
              )}
              {reopenable && (
                <button
                  onClick={reopen}
                  className="py-3 bg-transparent border border-outline-variant text-on-surface-variant font-mono-label text-mono-label uppercase tracking-widest hover:bg-surface-container-low hover:text-primary transition-colors"
                >
                  Reopen for Correction
                </button>
              )}
            </div>
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-6">
              {stateIdx >= RUN_SEQUENCE.indexOf(IRREVERSIBLE_FROM)
                ? 'Past the lock boundary. No backward transition exists; corrections route to the Corrections Console as new acts.'
                : 'Before the lock boundary. The run may reopen to Draft; after Locked, no return path exists in the lifecycle model.'}
            </p>
          </div>
        </section>
      </div>

      {/* Run ledger */}
      <section className="bg-surface-container-lowest hairline-all p-6">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-6 uppercase tracking-widest">
          4. Run Ledger
        </h3>
        <CaseTimeline entries={ledger} />
      </section>

      <LedgerStrip
        className="mt-8"
        entries={[
          { label: 'Run', value: 'PR-2026-06-A' },
          { label: 'Lifecycle', value: 'payroll-lifecycle v1 · 6 states, forward-sealed' },
          { label: 'Engine', value: 'KARMA calc v3.2' },
        ]}
      />
    </>
  )
}
