// LAB EXPERIMENT B — Executive Dashboard as Observatory.
// Not production. Outside doctrine jurisdiction (design/EXPERIMENTS.md).
// Question: what if the executive surface were an instrument room — mission
// control / central bank — instead of a KPI card grid? Includes the
// atmospheric-pressure concept: switching watch condition from NOMINAL to
// STRAINED compresses margins and tightens typography. No alarms, no red
// flashing — the room itself feels constrained.

import { useState } from 'react'

interface Instrument {
  name: string
  reading: number // 0..1 needle position
  value: string
  unit: string
  note: string
}

const INSTRUMENTS: Instrument[] = [
  { name: 'Workforce', reading: 0.72, value: '94.1', unit: 'capacity index', note: 'coverage stable, attrition 4.2%' },
  { name: 'Payroll', reading: 0.86, value: '99.4', unit: 'integrity index', note: 'run PR-2026-06-A at Calculated' },
  { name: 'Compliance', reading: 0.63, value: '87.0', unit: 'posture index', note: '2 filings due within 9 days' },
  { name: 'Capability', reading: 0.55, value: '78.3', unit: 'readiness index', note: 'succession gap: 3 critical roles' },
]

interface RiskPoint {
  id: string
  label: string
  likelihood: number // 0..1
  impact: number // 0..1
  tone: 'critical' | 'caution' | 'watch'
}

const RISKS: RiskPoint[] = [
  { id: 'R-114', label: 'Zone East coverage', likelihood: 0.78, impact: 0.62, tone: 'critical' },
  { id: 'R-098', label: 'Architect flight risk', likelihood: 0.55, impact: 0.81, tone: 'critical' },
  { id: 'R-121', label: 'Statutory filing window', likelihood: 0.40, impact: 0.66, tone: 'caution' },
  { id: 'R-103', label: 'Contractor dependency', likelihood: 0.62, impact: 0.38, tone: 'caution' },
  { id: 'R-130', label: 'Overtime drift, west', likelihood: 0.33, impact: 0.25, tone: 'watch' },
]

const RISK_FILL: Record<RiskPoint['tone'], string> = {
  critical: '#7A2B2B',
  caution: '#9C842A',
  watch: '#C9A86A',
}

// Semicircular gauge: needle angle from reading (0 = left, 1 = right).
function Gauge({ reading, size = 150 }: { reading: number; size?: number }) {
  const cx = size / 2
  const cy = size / 2 + 10
  const r = size / 2 - 12
  const angle = Math.PI * (1 - reading)
  const nx = cx + r * 0.82 * Math.cos(angle)
  const ny = cy - r * 0.82 * Math.sin(angle)
  const arc = (from: number, to: number, color: string, width: number) => {
    const x1 = cx + r * Math.cos(Math.PI * (1 - from))
    const y1 = cy - r * Math.sin(Math.PI * (1 - from))
    const x2 = cx + r * Math.cos(Math.PI * (1 - to))
    const y2 = cy - r * Math.sin(Math.PI * (1 - to))
    return <path d={`M ${x1} ${y1} A ${r} ${r} 0 0 1 ${x2} ${y2}`} fill="none" stroke={color} strokeWidth={width} />
  }
  return (
    <svg viewBox={`0 0 ${size} ${size / 2 + 28}`} className="w-full">
      {arc(0, 1, '#2A3D6B', 2)}
      {arc(0, reading, '#C9A86A', 3)}
      {[0, 0.25, 0.5, 0.75, 1].map((t) => {
        const tx1 = cx + (r - 5) * Math.cos(Math.PI * (1 - t))
        const ty1 = cy - (r - 5) * Math.sin(Math.PI * (1 - t))
        const tx2 = cx + (r + 3) * Math.cos(Math.PI * (1 - t))
        const ty2 = cy - (r + 3) * Math.sin(Math.PI * (1 - t))
        return <line key={t} x1={tx1} y1={ty1} x2={tx2} y2={ty2} stroke="#46464b" strokeWidth="1" />
      })}
      <line x1={cx} y1={cy} x2={nx} y2={ny} stroke="#B16A3C" strokeWidth="2.5" strokeLinecap="round" />
      <circle cx={cx} cy={cy} r="4" fill="#B16A3C" />
    </svg>
  )
}

export function Observatory() {
  const [strained, setStrained] = useState(false)

  // Atmospheric pressure: the same instruments, the room compressed.
  const pad = strained ? 'p-4' : 'p-7'
  const gap = strained ? 'gap-3' : 'gap-7'
  const sectionGap = strained ? 'mb-5' : 'mb-12'
  const track = strained ? 'tracking-tight' : 'tracking-tight'
  const titleSize = strained ? 'text-[40px]' : 'text-[56px]'

  return (
    <div className="bg-ink min-h-screen text-ivory">
      <div className={`max-w-[1320px] mx-auto px-6 md:px-10 ${strained ? 'py-6' : 'py-12'}`}>
        {/* Horizon band */}
        <header className={`flex flex-col lg:flex-row lg:items-end justify-between gap-6 ${sectionGap} border-b border-ivory/15 ${strained ? 'pb-4' : 'pb-8'}`}>
          <div>
            <p className="font-mono text-[10px] uppercase tracking-[0.3em] text-ivory/50 mb-4">
              Karamchari · Institutional Observatory · 2026-06-12 · Watch 03
            </p>
            <h1 className={`font-serif font-light ${titleSize} leading-none ${track}`}>
              The Institution, Observed
            </h1>
          </div>
          <div className="flex items-center gap-4">
            <span className="font-mono text-[10px] uppercase tracking-[0.25em] text-ivory/50">Watch condition</span>
            <button
              onClick={() => setStrained(false)}
              className={`px-4 py-2 font-mono text-[10px] uppercase tracking-[0.2em] border ${!strained ? 'border-hiranya-gold text-hiranya-gold' : 'border-ivory/20 text-ivory/40'}`}
            >
              Nominal
            </button>
            <button
              onClick={() => setStrained(true)}
              className={`px-4 py-2 font-mono text-[10px] uppercase tracking-[0.2em] border ${strained ? 'border-tamra-copper text-tamra-copper' : 'border-ivory/20 text-ivory/40'}`}
            >
              Strained
            </button>
          </div>
        </header>

        <div className={`grid grid-cols-1 xl:grid-cols-12 ${gap} ${sectionGap}`}>
          {/* Instrument cluster */}
          <section className="xl:col-span-8">
            <h2 className="font-mono text-[10px] uppercase tracking-[0.3em] text-ivory/50 mb-5">Instrument Cluster</h2>
            <div className={`grid grid-cols-2 lg:grid-cols-4 ${gap}`}>
              {INSTRUMENTS.map((ins) => (
                <div key={ins.name} className={`border border-ivory/15 ${pad} bg-ink-2`}>
                  <div className="font-mono text-[9.5px] uppercase tracking-[0.25em] text-ivory/60 mb-3">{ins.name}</div>
                  <Gauge reading={ins.reading} />
                  <div className="mt-2">
                    <span className="font-serif font-light text-[34px] leading-none">{ins.value}</span>
                    <span className="font-mono text-[9px] uppercase tracking-widest text-ivory/40 ml-2">{ins.unit}</span>
                  </div>
                  {!strained && <p className="font-sans text-[12px] text-ivory/55 mt-3 leading-snug">{ins.note}</p>}
                </div>
              ))}
            </div>

            {/* Risk quadrant */}
            <h2 className={`font-mono text-[10px] uppercase tracking-[0.3em] text-ivory/50 ${strained ? 'mt-5' : 'mt-10'} mb-5`}>
              Risk Field · likelihood × impact
            </h2>
            <div className={`border border-ivory/15 bg-ink-2 ${pad}`}>
              <svg viewBox="0 0 600 300" className="w-full">
                <line x1="40" y1="20" x2="40" y2="260" stroke="#46464b" strokeWidth="1" />
                <line x1="40" y1="260" x2="580" y2="260" stroke="#46464b" strokeWidth="1" />
                <line x1="310" y1="20" x2="310" y2="260" stroke="#2A3D6B" strokeWidth="1" strokeDasharray="3 5" />
                <line x1="40" y1="140" x2="580" y2="140" stroke="#2A3D6B" strokeWidth="1" strokeDasharray="3 5" />
                <text x="44" y="32" fill="#77777b" fontSize="9" fontFamily="JetBrains Mono" letterSpacing="2">IMPACT</text>
                <text x="500" y="276" fill="#77777b" fontSize="9" fontFamily="JetBrains Mono" letterSpacing="2">LIKELIHOOD</text>
                {RISKS.map((rp) => {
                  const x = 40 + rp.likelihood * 540
                  const y = 260 - rp.impact * 240
                  return (
                    <g key={rp.id}>
                      <circle cx={x} cy={y} r={rp.tone === 'critical' ? 9 : 6} fill={RISK_FILL[rp.tone]} opacity="0.9" />
                      <text x={x + 13} y={y + 3} fill="#D9D2C2" fontSize="10" fontFamily="JetBrains Mono">{rp.id} {rp.label}</text>
                    </g>
                  )
                })}
              </svg>
            </div>
          </section>

          {/* Analyst briefing */}
          <aside className="xl:col-span-4">
            <h2 className="font-mono text-[10px] uppercase tracking-[0.3em] text-ivory/50 mb-5">Analyst Briefing · Buddhi</h2>
            <div className={`border-l-2 border-hiranya-gold ${strained ? 'pl-4' : 'pl-7'} space-y-6`}>
              <p className={`font-serif font-light italic ${strained ? 'text-[19px]' : 'text-[23px]'} leading-snug text-ivory/90`}>
                Attendance stabilized this cycle. The improvement originated in eastern logistics teams
                following the shift redesign introduced eleven days ago.
              </p>
              <p className="font-sans text-[14px] leading-relaxed text-ivory/65">
                Current risk remains concentrated in contractor-dependent sites, where coverage is one
                resignation away from breach. The payroll spine is healthy; the constraint this watch is
                capability succession, not operations.
              </p>
              <p className="font-sans text-[14px] leading-relaxed text-ivory/65">
                Two decisions await authority this watch: the Zone East replacement assignment and the
                Q3 retention mandate for the senior architect bench. Neither improves by waiting.
              </p>
              <div className="font-mono text-[9px] uppercase tracking-[0.2em] text-ivory/40 pt-2 border-t border-ivory/15">
                Reasoning from 14 signals, 5 risks · confidence 0.83 · L04 Buddhi
              </div>
            </div>
          </aside>
        </div>

        {/* Telemetry ticker */}
        <footer className="border-t border-ivory/15 pt-3 flex flex-wrap gap-x-8 gap-y-1 font-mono text-[9.5px] uppercase tracking-[0.2em] text-ivory/40">
          <span>SRF-OBS-LAB</span>
          <span>4,281 operators under observation</span>
          <span>payroll PR-2026-06-A calculated</span>
          <span>2 filings in window</span>
          <span className={strained ? 'text-tamra-copper' : 'text-hiranya-gold'}>
            condition {strained ? 'strained — room compressed' : 'nominal'}
          </span>
        </footer>
      </div>
    </div>
  )
}
