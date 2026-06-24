import { Icon } from '@/components/shell/Icon'
import type { PageId } from '@/lib/nav'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'SYS',
  archetype: 'command-center',
  signals: 4,
  risks: 0,
  narratives: 1,
  maps: 1,
  bindu: true,
}

// Sanskrit name always paired with operational language — DOCTRINE §5a.
const layers = [
  { no: '01', name: 'Pulse', ops: 'Workforce Awareness', principle: 'Spanda', purpose: 'Real-time awareness, adherence monitoring, system load.', status: 'Nominal', tone: 'good', page: 'pulse' as PageId },
  { no: '02', name: 'Ops', ops: 'Daily Execution', principle: 'Karma', purpose: 'Frontline execution, attendance, tasks, geo-verified action.', status: 'Nominal', tone: 'good', page: 'attendance' as PageId },
  { no: '03', name: 'Pravaha', ops: 'Flows & Policies', principle: 'Pravāha', purpose: 'Flow builder and orchestration engine for enterprise policies.', status: 'Nominal', tone: 'good', page: 'roster' as PageId },
  { no: '04', name: 'Buddhi', ops: 'Institutional Intelligence', principle: 'Buddhi', purpose: 'Institutional cognition; anticipatory reasoning.', status: 'Reasoning', tone: 'copper', page: 'executive' as PageId },
  { no: '05', name: 'Sutra', ops: 'Memory & Audit', principle: 'Sutra', purpose: 'Institutional memory; archival of every decision and rule.', status: 'Nominal', tone: 'good', page: 'compliance' as PageId },
  { no: '06', name: 'Topology', ops: 'People & Places', principle: 'Jala', purpose: 'Human operational substrate; roles mapped to physical reality.', status: 'Nominal', tone: 'good', page: 'employee-360' as PageId },
  { no: '07', name: 'Temporal', ops: 'Forecasting & Drift', principle: 'Kāla', purpose: 'Predictive modeling, drift detection, anticipatory control.', status: 'Drift Watch', tone: 'caution', page: 'succession' as PageId },
  { no: '08', name: 'Adaptive', ops: 'Governance & Rules', principle: 'Dharma', purpose: 'Governance at scale; intelligence committed to institutional law.', status: 'Nominal', tone: 'good', page: 'compliance' as PageId },
  { no: '09', name: 'Recovery', ops: 'Resilience & Restoration', principle: 'Sthiti', purpose: 'Systemic resilience; restoration of autonomy after strain.', status: 'Standby', tone: 'neutral', page: 'pulse' as PageId },
]

const loads = [
  { label: 'Operators On Shift', value: '1,284', meta: '94.2% adherence' },
  { label: 'Active Flows', value: '312', meta: '4 awaiting human gate' },
  { label: 'Decisions Archived Today', value: '8,914', meta: 'Sutra ledger · immutable' },
  { label: 'Open Risk Vectors', value: '7', meta: '2 elevated · 0 critical' },
]

export function PulseHome({ onNavigate }: { onNavigate: (id: PageId) => void }) {
  return (
    <>
      {/* Cover */}
      <header className="mb-14 border-b border-outline-variant pb-10">
        <p className="font-mono-label text-mono-label text-tamra-copper uppercase tracking-widest mb-5">
          Layer 01 · Spanda · System Pulse
        </p>
        <h2 className="font-serif font-light text-[48px] md:text-[72px] leading-[0.98] tracking-[-0.02em] text-primary max-w-4xl">
          The institution is <em className="italic text-yantra-indigo">steady</em>.
        </h2>
        <p className="font-serif italic text-[19px] leading-[1.55] text-graphite mt-5 max-w-2xl">
          Nine layers of operational intelligence, one coherent substrate for organized human action. No
          alarms are sounding. None are needed.
        </p>
      </header>

      {/* System load */}
      <section className="mb-14">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-4">
          System Load
        </h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-px bg-outline-variant border border-outline-variant">
          {loads.map((l) => (
            <div key={l.label} className="bg-surface p-6 flex flex-col justify-between min-h-[140px]">
              <div className="font-mono-label text-mono-label text-on-surface-variant uppercase">{l.label}</div>
              <div>
                <div className="font-serif font-light text-[36px] leading-none text-primary tabular-nums">
                  {l.value}
                </div>
                <div className="font-mono-label text-mono-label text-on-surface-variant mt-2">{l.meta}</div>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Nine layers registry */}
      <section className="mb-14">
        <div className="flex justify-between items-baseline mb-4">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest">
            The Nine Layers
          </h3>
          <span className="font-mono-label text-mono-label text-on-surface-variant">
            8 NOMINAL · 1 WATCH · 0 CRISIS
          </span>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-grid-12">
          {layers.map((layer) => (
            <button
              key={layer.no}
              onClick={() => onNavigate(layer.page)}
              className="text-left hairline-all bg-surface-container-lowest p-5 hover:border-yantra-indigo hover:bg-surface transition-all group"
            >
              <div className="flex justify-between items-start mb-4">
                <span className="font-mono-label text-mono-label text-tamra-copper">{layer.no}</span>
                <span className="flex items-center gap-2 font-mono-label text-mono-label text-on-surface-variant uppercase">
                  <span className={`bindu ${layer.tone}`} /> {layer.status}
                </span>
              </div>
              <div className="flex items-baseline gap-3">
                <span className="font-serif font-light text-[24px] text-primary">{layer.name}</span>
                <span className="font-serif italic text-[14px] text-graphite">{layer.principle}</span>
              </div>
              <div className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-2 mt-0.5">
                {layer.ops}
              </div>
              <p className="font-tabular-data text-[13px] text-on-surface-variant leading-relaxed">
                {layer.purpose}
              </p>
              <div className="mt-4 flex items-center gap-1 font-mono-label text-mono-label text-outline uppercase opacity-0 group-hover:opacity-100 transition-opacity">
                Enter Layer <Icon name="arrow_forward" className="!text-[14px]" />
              </div>
            </button>
          ))}
        </div>
      </section>

      {/* Buddhi posture statement */}
      <section className="bg-yantra-indigo text-white p-8 md:p-12 relative overflow-hidden structural-hairline">
        <div
          className="absolute inset-0 opacity-10"
          style={{
            backgroundImage:
              'repeating-linear-gradient(45deg, #ffffff 0, #ffffff 1px, transparent 1px, transparent 10px)',
          }}
        />
        <div className="relative z-10 max-w-3xl">
          <h3 className="font-mono-label text-mono-label text-surface-variant uppercase tracking-widest mb-6 flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-tamra-copper-bright inline-block" />
            Buddhi · Institutional Posture
          </h3>
          <p className="font-serif font-light italic text-[26px] md:text-[32px] leading-[1.25] tracking-[-0.015em]">
            "Attendance adherence has held above 94% for eleven consecutive days. Temporal layer flags mild
            drift in Zone C scheduling. No intervention is mandated; one is suggested."
          </p>
          <button
            onClick={() => onNavigate('executive')}
            className="mt-8 px-6 py-3 bg-tamra-copper text-white font-mono-label text-mono-label uppercase tracking-widest hover:bg-tamra-copper-bright transition-colors"
          >
            Review Suggestion
          </button>
        </div>
      </section>
    </>
  )
}
