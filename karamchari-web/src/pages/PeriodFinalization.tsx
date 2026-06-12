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

type Phase = 'open' | 'reconciling' | 'finalized'

const PHASES: { id: Phase; label: string; meta: string }[] = [
  { id: 'open', label: 'Open', meta: 'CLOSED 2026-06-01' },
  { id: 'reconciling', label: 'Reconciling', meta: 'IN PROGRESS' },
  { id: 'finalized', label: 'Finalized', meta: 'FEEDS PAYROLL DRAFT' },
]

interface Regularization {
  id: string
  operator: string
  day: string
  issue: string
  proposed: string
}

const REGULARIZATIONS: Regularization[] = [
  { id: 'REG-2026-0644', operator: 'Asha V. · EMP-7721-C', day: 'May 27', issue: 'Missing checkout', proposed: 'Full day — supervisor attested 17:55 exit' },
  { id: 'REG-2026-0651', operator: 'Vikram S. · EMP-8102-B', day: 'May 29', issue: 'Geo-fence drift at check-in', proposed: 'Full day — device GPS variance within tolerance' },
]

const SEED_LEDGER: TimelineEntry[] = [
  {
    ts: '2026-06-01T00:00:04Z',
    tag: 'CUTOFF',
    tone: 'neutral',
    body: <>Period <span className="font-mono text-[11px]">2026-05</span> closed to new punches by schedule ATT-PERIOD-STD. 21 working days, 1,247 operators.</>,
  },
  {
    ts: '2026-06-02T03:15:40Z',
    tag: 'RECON',
    tone: 'good',
    body: <>Auto-reconciliation matched 25,914 of 26,187 day-records (98.96%). 273 routed to regularization, 271 since resolved.</>,
  },
  {
    ts: '2026-06-11T14:22:09Z',
    tag: 'ESCALATE',
    tone: 'caution',
    body: <>2 regularizations pending past SLA reminder. Finalization gate holds until queue clears.</>,
  },
]

export function PeriodFinalization() {
  const [phase, setPhase] = useState<Phase>('reconciling')
  const [ledger, setLedger] = useState<TimelineEntry[]>(SEED_LEDGER)
  const [applied, setApplied] = useState<Set<string>>(new Set())

  const pending = REGULARIZATIONS.filter((r) => !applied.has(r.id))
  const gated = pending.length > 0
  const phaseIdx = PHASES.findIndex((p) => p.id === phase)

  const apply = (id: string) => {
    setApplied((s) => new Set(s).add(id))
    setLedger((l) => [
      ...l,
      {
        ts: new Date().toISOString(),
        tag: 'REG_APPLIED',
        tone: 'good',
        body: <><span className="font-mono text-[11px]">{id}</span> applied, actor: Ananya R., reason: proposed correction accepted as attested.</>,
      },
    ])
  }

  const finalize = () => {
    if (gated || phase === 'finalized') return
    setLedger((l) => [
      ...l,
      {
        ts: new Date().toISOString(),
        tag: 'FINALIZED',
        tone: 'copper',
        body: <>Period 2026-05 finalized, actor: Ananya R., reason: all 26,187 day-records reconciled. Attendance truth sealed; payroll draft PR-2026-06-A may now consume it. No reopening path exists.</>,
      },
    ])
    setPhase('finalized')
  }

  const regColumns: TableColumn<Regularization>[] = [
    { key: 'id', header: 'Ref', render: (r) => <span className="font-mono text-[11px]">{r.id}</span> },
    { key: 'operator', header: 'Operator', render: (r) => <span className="text-primary">{r.operator}</span> },
    { key: 'day', header: 'Day' },
    { key: 'issue', header: 'Issue' },
    { key: 'proposed', header: 'Proposed Correction', render: (r) => <span className="text-on-surface-variant">{r.proposed}</span> },
    {
      key: 'act',
      header: '',
      align: 'right',
      render: (r) => (
        <button
          onClick={() => apply(r.id)}
          className="px-3 py-1 border border-outline-variant font-mono-label text-mono-label uppercase text-on-surface-variant hover:bg-surface-container-low hover:text-primary transition-colors"
        >
          Apply
        </button>
      ),
    },
  ]

  return (
    <>
      {/* State header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Attendance Period <span className="font-mono">2026-05</span> · 21 Working Days
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Period Finalization
          </h2>
        </div>
        <div className="flex items-center gap-3 font-mono text-[12px] text-on-surface-variant uppercase tracking-widest">
          <span className={`bindu ${phase === 'finalized' ? 'good' : 'neutral'}`} />
          State: <span className="text-primary">{PHASES[phaseIdx].label}</span>
        </div>
      </div>

      {/* Phase topology */}
      <section className="mb-10">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
          1. Period Lifecycle
        </h3>
        <div className="flex flex-col md:flex-row border border-outline-variant bg-surface">
          {PHASES.map((p, i) => {
            const done = i < phaseIdx
            const active = i === phaseIdx
            return (
              <div
                key={p.id}
                className={`flex-1 p-5 relative ${i < PHASES.length - 1 ? 'border-b md:border-b-0 md:border-r border-outline-variant' : ''} ${
                  active ? 'bg-surface-container-low' : ''
                } ${!done && !active ? 'opacity-50' : ''}`}
              >
                {active && <div className="absolute inset-x-0 top-0 h-[2px] bg-tamra-copper" />}
                <div className="flex justify-between items-start gap-2 mb-6">
                  <span className={`font-mono-label text-mono-label ${active ? 'text-tamra-copper' : 'text-on-surface-variant'}`}>
                    {String(i + 1).padStart(2, '0')}
                  </span>
                  {p.id === 'finalized' && (
                    <span className="font-mono text-[9px] text-on-surface-variant uppercase tracking-widest whitespace-nowrap">No return</span>
                  )}
                </div>
                <div className={`font-body-standard text-body-standard ${active ? 'text-primary font-medium' : 'text-on-surface-variant'}`}>
                  {p.label}
                </div>
                <div className={`font-mono-label text-mono-label mt-1 ${active ? 'text-tamra-copper' : 'text-outline-variant'}`}>
                  {done ? 'SEALED' : active ? 'CURRENT' : p.meta}
                </div>
              </div>
            )
          })}
        </div>
      </section>

      {/* Health row */}
      <section className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-grid-12 mb-10">
        <SignalCard label="Day-Records Reconciled" value="98.96" unit="%" note="25,914 auto-matched of 26,187" bindu="good" bar={{ pct: 98.96 }} />
        <SignalCard
          label="Regularizations Pending"
          value={String(pending.length)}
          note={gated ? 'Holding the finalization gate' : 'Queue clear'}
          bindu={gated ? 'caution' : 'good'}
        />
        <SignalCard label="Exception Days" value="273" note="271 resolved through regularization flow" bindu="neutral" />
        <SignalCard label="Payroll Dependency" value="PR-06-A" note="Payroll draft consumes this period on finalize" bindu="neutral" />
      </section>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-grid-12 mb-10">
        {/* Pending queue */}
        <section className="md:col-span-8">
          <div className="flex justify-between items-end mb-4">
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest">
              2. Regularization Queue
            </h3>
            <span className={`font-mono-label text-mono-label ${gated ? 'text-pita-caution' : 'text-on-surface-variant'}`}>
              {pending.length} PENDING
            </span>
          </div>
          <div className="bg-surface-container-lowest hairline-all p-6">
            {pending.length > 0 ? (
              <InstitutionalTable columns={regColumns} rows={pending} rowKey={(r) => r.id} />
            ) : (
              <p className="font-tabular-data text-tabular-data text-on-surface-variant py-4">
                Queue clear. Every working day of every operator is accounted for.
              </p>
            )}
          </div>
        </section>

        {/* Finalization controls */}
        <section className="md:col-span-4">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
            3. Finalization
          </h3>
          <div className="bg-surface-container-lowest hairline-all p-6">
            {gated && phase !== 'finalized' && (
              <RiskCard
                className="mb-6"
                severity="elevated"
                title="Gate Held"
                body={`${pending.length} regularization${pending.length === 1 ? '' : 's'} pending. Finalizing seals attendance truth for payroll; unresolved days would freeze wrong.`}
              />
            )}
            {phase !== 'finalized' ? (
              <BinduButton
                onClick={finalize}
                disabled={gated}
                className="w-full disabled:opacity-40 disabled:cursor-not-allowed py-3"
              >
                Finalize Period
              </BinduButton>
            ) : (
              <div className="py-3 text-center border border-outline-variant font-mono-label text-mono-label uppercase tracking-widest text-on-surface-variant">
                Period Sealed · 2026-05
              </div>
            )}
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-6">
              Finalization is irreversible. Post-seal corrections enter the next period as adjustment acts with full provenance.
            </p>
          </div>
        </section>
      </div>

      {/* Period ledger */}
      <section className="bg-surface-container-lowest hairline-all p-6">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-6 uppercase tracking-widest">
          4. Period Ledger
        </h3>
        <CaseTimeline entries={ledger} />
      </section>

      <LedgerStrip
        className="mt-8"
        entries={[
          { label: 'Period', value: '2026-05 · ATT-PERIOD-STD' },
          { label: 'Records', value: '26,187 day-records · 1,247 operators' },
          { label: 'Downstream', value: 'PR-2026-06-A awaits seal' },
        ]}
      />
    </>
  )
}
