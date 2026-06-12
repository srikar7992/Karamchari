import { PageHeader } from '@/components/shell/PageHeader'
import { Icon } from '@/components/shell/Icon'
import { RUN_SEQUENCE, STATE_LABELS, type RunState } from '@/lib/payroll-lifecycle'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'command-center',
  signals: 4,
  risks: 3,
  narratives: 0,
  maps: 1,
  bindu: false,
}

// The cockpit reads the run's lifecycle; the decisive acts live on the Run Console.
const CURRENT_STATE: RunState = 'calculated'

const steps = RUN_SEQUENCE.map((s, i) => {
  const currentIdx = RUN_SEQUENCE.indexOf(CURRENT_STATE)
  return {
    no: String(i + 1).padStart(2, '0'),
    label: STATE_LABELS[s],
    meta: i < currentIdx ? 'SEALED' : i === currentIdx ? 'CURRENT' : 'AHEAD',
    state: i < currentIdx ? 'done' : i === currentIdx ? 'active' : 'pending',
  }
})

const summary = [
  { label: 'Total Gross Disbursement', value: 'USD 4,285,910.45', delta: '+ 2.4% vs prev cycle', icon: 'arrow_upward', tone: 'text-yantra-indigo' },
  { label: 'Total Net Pay', value: 'USD 2,981,402.10', delta: '+ 1.8% vs prev cycle', icon: 'arrow_upward', tone: 'text-yantra-indigo' },
  { label: 'Statutory Tax Withheld', value: 'USD 945,210.00', delta: '0.0% variance expected', icon: 'horizontal_rule', tone: 'text-on-surface-variant' },
  { label: 'Voluntary Deductions', value: 'USD 359,298.35', delta: '+ 8.4% investigation req.', icon: 'arrow_upward', tone: 'text-rakta-critical' },
]

const anomalies = [
  { id: 'ID-8842', amount: 'USD 4,200.00', tone: 'bg-rakta-critical', note: 'Missing routing number for new hire (J. Doe).' },
  { id: 'ID-8845', amount: 'USD 850.00', tone: 'bg-pita-caution', note: 'Unapproved overtime hours submitted post-lock.' },
  { id: 'ID-8850', amount: 'USD 12,400.00', tone: 'bg-pita-caution', note: 'Bonus payout exceeds standard deviation threshold.' },
]

export function PayrollCockpit() {
  return (
    <>
      <PageHeader
        kicker="Payroll Cockpit / Pay Period 2026-06"
        title="Command Center"
        sub="Operational overview for the current pay cycle. Figures are provisional until the run locks; decisive acts live on the Run Console."
        right={
          <div className="flex gap-4">
            <button className="px-6 py-3 border border-outline-variant bg-surface text-primary font-mono-label text-mono-label hover:bg-surface-container-high transition-colors">
              DOWNLOAD LEDGER
            </button>
            <a
              href="#/payroll-run"
              className="px-6 py-3 border border-outline-variant bg-surface text-primary font-mono-label text-mono-label hover:bg-surface-container-high transition-colors"
            >
              OPEN RUN CONSOLE
            </a>
          </div>
        }
      />

      {/* Cycle Progress */}
      <section className="mb-12">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
          1. Cycle Progress
        </h3>
        <div className="flex flex-col md:flex-row border border-outline-variant bg-surface">
          {steps.map((s, i) => (
            <div
              key={s.no}
              className={`flex-1 p-6 relative ${i < steps.length - 1 ? 'border-b md:border-b-0 md:border-r border-outline-variant' : ''} ${
                s.state === 'active' ? 'bg-surface-container-low' : ''
              } ${s.state === 'pending' ? 'opacity-50' : ''}`}
            >
              {s.state === 'active' && <div className="absolute inset-x-0 top-0 h-[2px] bg-tamra-copper" />}
              <div className="flex justify-between items-start mb-8">
                <span
                  className={`font-mono-label text-mono-label ${s.state === 'active' ? 'text-tamra-copper' : 'text-on-surface-variant'}`}
                >
                  {s.no}
                </span>
                <div
                  className={`w-2 h-2 rounded-full ${s.state === 'active' ? 'bg-tamra-copper animate-pulse' : 'bg-outline-variant'}`}
                />
              </div>
              <div
                className={`font-body-standard text-body-standard ${s.state === 'active' ? 'text-primary font-medium' : 'text-on-surface-variant'}`}
              >
                {s.label}
              </div>
              <div
                className={`font-mono-label text-mono-label mt-1 ${s.state === 'active' ? 'text-tamra-copper' : 'text-outline-variant'}`}
              >
                {s.meta}
              </div>
            </div>
          ))}
        </div>
      </section>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-6">
        {/* Reconciliation Summary */}
        <section className="col-span-1 md:col-span-8 flex flex-col">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
            2. Reconciliation Summary
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-px bg-outline-variant border border-outline-variant flex-1">
            {summary.map((card) => (
              <div key={card.label} className="bg-surface p-8 flex flex-col justify-between">
                <div className="font-body-standard text-body-standard text-on-surface-variant mb-12">
                  {card.label}
                </div>
                <div>
                  <div className="font-mono text-primary text-[20px] mb-2 leading-none">{card.value}</div>
                  <div className="flex items-center gap-2">
                    <Icon name={card.icon} className={`!text-[14px] ${card.tone}`} />
                    <span className={`font-mono-label text-mono-label ${card.tone}`}>{card.delta}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* Right column */}
        <div className="col-span-1 md:col-span-4 flex flex-col gap-6">
          {/* Resolve Queue */}
          <section className="flex-1 flex flex-col">
            <div className="flex justify-between items-end mb-4">
              <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest">
                3. Resolve Queue
              </h3>
              <span className="font-mono-label text-mono-label text-rakta-critical">3 BLOCKS</span>
            </div>
            <div className="border border-outline-variant bg-surface flex-1 overflow-hidden flex flex-col">
              <div className="overflow-y-auto">
                {anomalies.map((a, i) => (
                  <div
                    key={a.id}
                    className={`p-4 hover:bg-surface-container-low transition-colors cursor-pointer ${
                      i < anomalies.length - 1 ? 'border-b border-outline-variant' : ''
                    }`}
                  >
                    <div className="flex justify-between items-start mb-2">
                      <div className="flex items-center gap-2">
                        <div className={`w-2 h-2 rounded-full ${a.tone}`} />
                        <span className="font-mono-label text-mono-label text-primary">{a.id}</span>
                      </div>
                      <span className="font-mono-label text-mono-label text-on-surface-variant">{a.amount}</span>
                    </div>
                    <div className="font-tabular-data text-tabular-data text-on-surface-variant">{a.note}</div>
                  </div>
                ))}
              </div>
            </div>
          </section>

          {/* Disbursement Readout */}
          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
              4. Disbursement Readout
            </h3>
            <div className="border border-outline-variant bg-surface p-6">
              <div className="flex justify-between items-center mb-6">
                <span className="font-tabular-data text-tabular-data text-primary">Bank File Generation</span>
                <span className="font-mono-label text-mono-label text-on-surface-variant">AWAITING PUB</span>
              </div>
              <div className="w-full h-1 bg-surface-container-high mb-6 overflow-hidden">
                <div className="h-full bg-outline-variant w-0" />
              </div>
              <div className="space-y-4">
                <div className="flex justify-between items-center pb-4 border-b border-outline-variant">
                  <span className="font-tabular-data text-tabular-data text-on-surface-variant">Provider</span>
                  <span className="font-mono-label text-mono-label text-primary">INSTITUTIONAL CLEARING</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="font-tabular-data text-tabular-data text-on-surface-variant">
                    Settlement Date
                  </span>
                  <span className="font-mono-label text-mono-label text-primary">2026-06-30</span>
                </div>
              </div>
            </div>
          </section>
        </div>
      </div>
    </>
  )
}
