import { useState } from 'react'
import {
  Surface, Label, Code,
  ConstitutionalArticle, SpineRail, useScrollSpy,
  SignalStrip,
  type Signal,
} from '@/components/observatory/surfaces'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'topology',
  signals: 6,
  risks: 0,
  narratives: 1,
  maps: 0,
  bindu: false,
}

// ---- Data -------------------------------------------------------------------

const SPINE_IDS = ['territory', 'exposure', 'skill', 'contract', 'record'] as const
const SPINE_NODES = [
  { id: 'territory', label: 'I · TERRITORY' },
  { id: 'exposure',  label: 'II · EXPOSURE' },
  { id: 'skill',     label: 'III · SKILL FIELD' },
  { id: 'contract',  label: 'IV · DEPENDENCY' },
  { id: 'record',    label: 'V · RECORD' },
]

const SIGNALS: Signal[] = [
  { id: 's1', label: 'On Location',  value: '129', unit: '/ 151',  delta: '+4 vs Fri',   dir: 'up',   spark: [112,119,121,125,127,129] },
  { id: 's2', label: 'Absent',       value: '12',  unit: '',       delta: '+2 vs norm',   dir: 'down', spark: [8,9,9,10,11,12] },
  { id: 's3', label: 'Late Arrival', value: '10',  unit: '',       delta: '−2 vs norm',   dir: 'up',   spark: [14,13,12,12,11,10] },
  { id: 's4', label: 'On Break',     value: '22',  unit: '',       delta: 'scheduled',    dir: 'up',   spark: [20,21,22,22,22,22] },
  { id: 's5', label: 'Contractor',   value: '17%', unit: '',       delta: '−1% m/m',      dir: 'up',   spark: [19,19,18,18,17,17] },
  { id: 's6', label: 'Zone D Cov',   value: '68%', unit: 'cov',   delta: '−15% target',  dir: 'down', spark: [82,80,75,72,69,68] },
]

interface Zone {
  id: string
  name: string
  present: number
  total: number
  contractor: number
  skills: string[]
  cx: number
  cy: number
  points: string
}

// Five zones tile a 600×400 SVG with no gaps.
// Voronoi-like irregular partition: shape encodes territory, not just data.
const ZONES: Zone[] = [
  {
    id: 'A', name: 'Manufacturing',
    present: 42, total: 48, contractor: 0.08,
    skills: ['CNC', 'QA'],
    cx: 95, cy: 295,
    points: '0,155 155,155 300,360 300,400 0,400',
  },
  {
    id: 'B', name: 'Assembly',
    present: 31, total: 36, contractor: 0.22,
    skills: ['Assembly', 'QA'],
    cx: 290, cy: 280,
    points: '155,155 320,140 450,175 450,400 300,400 300,360',
  },
  {
    id: 'C', name: 'Quality',
    present: 14, total: 15, contractor: 0.00,
    skills: ['ISO', 'QA'],
    cx: 438, cy: 80,
    points: '280,0 600,0 600,175 450,175 320,140',
  },
  {
    id: 'D', name: 'Logistics',
    present: 19, total: 28, contractor: 0.35,
    skills: ['Forklift', 'WMS'],
    cx: 530, cy: 295,
    points: '450,175 600,175 600,400 450,400',
  },
  {
    id: 'E', name: 'Administration',
    present: 23, total: 24, contractor: 0.04,
    skills: ['HR', 'Finance'],
    cx: 128, cy: 62,
    points: '0,0 280,0 320,140 155,155 0,155',
  },
]

interface SkillGap {
  zone: string
  skill: string
  risk: 'exposure' | 'thin' | 'concentrated'
  detail: string
}

const SKILL_GAPS: SkillGap[] = [
  { zone: 'D', skill: 'Forklift Certified',  risk: 'exposure',     detail: 'Only 4 of 28 certified. 2 absent today — forklift ops suspended under OPS-SAFE-03.' },
  { zone: 'B', skill: 'Assembly Line Lead',  risk: 'thin',         detail: '1 lead covering 31 personnel. Contractor substitution not permitted.' },
  { zone: 'A', skill: 'CNC Operator',        risk: 'concentrated', detail: '80% of CNC hours held by 3 workers. All 3 present today.' },
  { zone: 'C', skill: 'ISO Auditor',         risk: 'thin',         detail: '2 auditors total. Both present. Zero redundancy if either is absent.' },
]

interface ContractorDep {
  zone: string
  name: string
  ratio: number
  firm: string
  limit: number
}

const CONTRACTOR_DEPS: ContractorDep[] = [
  { zone: 'D', name: 'Logistics',     ratio: 0.35, firm: 'SwiftOps Ltd', limit: 0.25 },
  { zone: 'B', name: 'Assembly',      ratio: 0.22, firm: 'TempForce',    limit: 0.25 },
  { zone: 'A', name: 'Manufacturing', ratio: 0.08, firm: 'Internal',     limit: 0.25 },
  { zone: 'E', name: 'Administration', ratio: 0.04, firm: 'Internal',    limit: 0.25 },
  { zone: 'C', name: 'Quality',       ratio: 0.00, firm: 'Internal',     limit: 0.25 },
]

interface AttendanceEntry {
  time: string
  zone: string
  event: string
  actor: string
  cite?: string
}

const RECORD: AttendanceEntry[] = [
  { time: '08:00', zone: 'ALL',  event: 'SHIFT_START',    actor: 'System',           cite: 'SCHED-01' },
  { time: '08:14', zone: 'D',    event: 'FORKLIFT_HOLD',  actor: 'System',           cite: 'OPS-SAFE-03' },
  { time: '09:02', zone: 'B',    event: 'CONT_ALERT',     actor: 'System',           cite: 'CONT-LIMIT-01' },
  { time: '10:30', zone: 'ALL',  event: 'HEADCOUNT_SNAP', actor: 'Geo-system' },
  { time: '13:00', zone: 'ALL',  event: 'BREAK_CYCLE',    actor: 'Shift Scheduler',  cite: 'SCHED-02' },
]

// ---- Helpers ----------------------------------------------------------------

function cov(z: Zone) { return z.present / z.total }
function pct(n: number) { return Math.round(n * 100) + '%' }

function zoneTone(z: Zone): 'good' | 'warn' | 'danger' {
  const c = cov(z)
  return c >= 0.92 ? 'good' : c >= 0.80 ? 'warn' : 'danger'
}

const TONE_COLOR = {
  good:   '#2d5d4b',
  warn:   '#9c842a',
  danger: '#b16a3c',
}

function zoneFillStyle(z: Zone) {
  const color = TONE_COLOR[zoneTone(z)]
  const opacity = zoneTone(z) === 'danger' ? 0.26 : 0.18
  const strokeW = zoneTone(z) === 'danger' ? 2.5 : 1.5
  return { fill: color, fillOpacity: opacity, stroke: color, strokeWidth: strokeW }
}

// ---- Territory SVG ----------------------------------------------------------

function TerritoryMap({ activeZone, onZone }: { activeZone: string | null; onZone: (id: string | null) => void }) {
  return (
    <svg
      viewBox="0 0 600 400"
      style={{ width: '100%', height: 'auto', maxHeight: 420, display: 'block', color: 'var(--obs-fg)' }}
      aria-label="Operational territory map — zone shape encodes coverage"
    >
      <defs>
        <pattern id="atop-grid" width="34" height="34" patternUnits="userSpaceOnUse">
          <path d="M 34 0 L 0 0 0 34" fill="none" stroke="currentColor" strokeOpacity="0.05" strokeWidth="0.5" />
        </pattern>
      </defs>
      <rect width="600" height="400" fill="url(#atop-grid)" />

      {ZONES.map((z) => {
        const f = zoneFillStyle(z)
        const isActive = activeZone === z.id
        const tone = zoneTone(z)
        const textColor = TONE_COLOR[tone]
        return (
          <g key={z.id} style={{ cursor: 'pointer' }} onClick={() => onZone(isActive ? null : z.id)}>
            <polygon
              points={z.points}
              fill={f.fill}
              fillOpacity={isActive ? f.fillOpacity * 2.2 : f.fillOpacity}
              stroke={f.stroke}
              strokeWidth={isActive ? f.strokeWidth + 1 : f.strokeWidth}
              strokeOpacity={isActive ? 0.9 : 0.5}
              strokeLinejoin="miter"
              style={{ transition: 'fill-opacity 180ms, stroke-width 180ms' }}
            />
            {/* Zone ID */}
            <text x={z.cx} y={z.cy - 12} textAnchor="middle"
              fontFamily="JetBrains Mono, monospace" fontSize="10" letterSpacing="3"
              fill={textColor} fillOpacity="0.9">
              {z.id}
            </text>
            {/* Zone name */}
            <text x={z.cx} y={z.cy + 4} textAnchor="middle"
              fontFamily="JetBrains Mono, monospace" fontSize="7.5" letterSpacing="0.5"
              fill="currentColor" fillOpacity="0.55">
              {z.name.toUpperCase()}
            </text>
            {/* Coverage % — largest number, carries most meaning */}
            <text x={z.cx} y={z.cy + 20} textAnchor="middle"
              fontFamily="JetBrains Mono, monospace" fontSize="13" fontWeight="300"
              fill={textColor}>
              {pct(cov(z))}
            </text>
            {/* Present / total */}
            <text x={z.cx} y={z.cy + 33} textAnchor="middle"
              fontFamily="JetBrains Mono, monospace" fontSize="7"
              fill="currentColor" fillOpacity="0.42">
              {z.present}/{z.total}
            </text>
          </g>
        )
      })}

      <rect width="600" height="400" fill="none" stroke="currentColor" strokeOpacity="0.14" strokeWidth="1" />
    </svg>
  )
}

// ---- Zone detail card (shown on click) --------------------------------------

function ZoneDetail({ zone }: { zone: Zone }) {
  const tone = zoneTone(zone)
  const color = TONE_COLOR[tone]
  return (
    <div className="cham" style={{
      background: 'rgba(var(--obs-line),0.025)',
      border: '1px solid rgba(var(--obs-line),0.12)',
      padding: '16px 20px',
      marginTop: 12,
      display: 'flex', gap: 32, alignItems: 'center', flexWrap: 'wrap',
    }}>
      <div>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginBottom: 4 }}>
          ZONE {zone.id} · {zone.name.toUpperCase()}
        </div>
        <span style={{ fontFamily: 'JetBrains Mono', fontSize: 30, fontWeight: 300, color }}>
          {pct(cov(zone))}
        </span>
        <span className="voice-mono" style={{ color: 'var(--obs-mute)', marginLeft: 8 }}>
          {zone.present} / {zone.total}
        </span>
      </div>
      <div>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9 }}>CONTRACTOR</div>
        <div className="voice-mono" style={{ color: zone.contractor > 0.25 ? '#b16a3c' : 'var(--obs-text-2)', marginTop: 2 }}>
          {pct(zone.contractor)}
        </div>
      </div>
      <div>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9 }}>SKILLS</div>
        <div className="voice-mono" style={{ color: 'var(--obs-text-2)', marginTop: 2 }}>
          {zone.skills.join(' · ')}
        </div>
      </div>
    </div>
  )
}

// ---- Page -------------------------------------------------------------------

export function AttendanceOps() {
  const activeIdx = useScrollSpy([...SPINE_IDS])
  const [activeZone, setActiveZone] = useState<string | null>(null)

  return (
    <div className="obs obs-canvas" style={{ minHeight: '100vh', padding: '32px 24px 80px' }}>
      {/* Header */}
      <div style={{ maxWidth: 960, margin: '0 auto 40px' }}>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginBottom: 6 }}>
          ATTENDANCE · TOPOLOGY · 2026-06-13
        </div>
        <h1 className="voice-serif" style={{
          fontSize: 40, fontWeight: 400, color: 'var(--obs-fg)', margin: 0, lineHeight: 1.1,
        }}>
          Operational Territory
        </h1>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginTop: 8 }}>
          129 of 151 on location · Zone D exposure active
        </div>
      </div>

      <div style={{ maxWidth: 960, margin: '0 auto', display: 'grid', gridTemplateColumns: '164px 1fr', gap: 48 }}>
        {/* Spine */}
        <SpineRail nodes={SPINE_NODES} active={activeIdx} />

        {/* Content */}
        <div style={{ minWidth: 0 }}>

          {/* Signal strip — 3 + 3 grid */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12, marginBottom: 40 }}>
            {SIGNALS.map((sig) => <SignalStrip key={sig.id} sig={sig} />)}
          </div>

          {/* Chapter I: Territory */}
          <section id="territory" style={{ scrollMarginTop: 96 }}>
            <ConstitutionalArticle title="I · Territory" code="OPS-TERR-01">
              Territory is not attendance. It is operational reality made visible — which zones are held,
              which are thin, and where the institution is exposed. A zone below 80% coverage is not
              an absence problem. It is a territory problem. Shape carries meaning: tap a zone to read it.
            </ConstitutionalArticle>

            <Surface pad="0px" style={{ overflow: 'hidden', border: '1px solid rgba(var(--obs-line),0.12)', marginTop: 24 }}>
              <TerritoryMap activeZone={activeZone} onZone={setActiveZone} />
            </Surface>

            {activeZone && (() => {
              const z = ZONES.find((z) => z.id === activeZone)
              return z ? <ZoneDetail zone={z} /> : null
            })()}

            {/* Territory legend */}
            <div style={{ display: 'flex', gap: 24, marginTop: 20, flexWrap: 'wrap' }}>
              {([
                { color: '#2d5d4b', label: '≥ 92% — Covered' },
                { color: '#9c842a', label: '80–91% — Thin' },
                { color: '#b16a3c', label: '< 80% — Exposed' },
              ] as const).map((item) => (
                <div key={item.label} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <div style={{
                    width: 10, height: 10,
                    background: item.color, opacity: 0.7,
                    transform: 'rotate(45deg)', flexShrink: 0,
                  }} />
                  <span className="voice-mono" style={{ color: 'var(--obs-mute)' }}>{item.label}</span>
                </div>
              ))}
            </div>
          </section>

          {/* Chapter II: Exposure */}
          <section id="exposure" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="II · Exposure" code="OPS-EXP-02">
              Exposure is not absence — it is structural vulnerability. Zone D has lost forklift
              certification quorum. Not because workers are missing, but because capability was
              never distributed broadly enough. Exposure names that institutional failure.
            </ConstitutionalArticle>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 12, marginTop: 24 }}>
              {[...ZONES].sort((a, b) => cov(a) - cov(b)).map((z) => {
                const tone = zoneTone(z)
                const color = TONE_COLOR[tone]
                return (
                  <Surface key={z.id} style={{ display: 'flex', gap: 20, alignItems: 'center' }}>
                    {/* Coverage indicator bar */}
                    <div style={{ width: 4, alignSelf: 'stretch', background: color, opacity: 0.5, flexShrink: 0 }} />
                    <div style={{ width: 60, textAlign: 'right', flexShrink: 0 }}>
                      <div style={{ fontFamily: 'JetBrains Mono', fontSize: 20, fontWeight: 300, color }}>
                        {pct(cov(z))}
                      </div>
                      <div className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 8 }}>
                        {z.present}/{z.total}
                      </div>
                    </div>
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div className="voice-mono" style={{ color }}>
                        ZONE {z.id} · {z.name.toUpperCase()}
                      </div>
                      <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginTop: 3 }}>
                        {z.total - z.present} absent · {pct(z.contractor)} contractor
                        {tone === 'good' ? ' — covered' : tone === 'danger' ? ' — EXPOSED' : ' — thin'}
                      </div>
                    </div>
                  </Surface>
                )
              })}
            </div>
          </section>

          {/* Chapter III: Skill Field */}
          <section id="skill" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="III · Skill Field" code="CAP-FIELD-01">
              Skill concentration creates single points of failure invisible to headcount.
              A zone may appear fully covered yet hold a skill field that collapses when two
              people are absent. The field map surfaces that risk before it becomes an incident.
            </ConstitutionalArticle>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 12, marginTop: 24 }}>
              {SKILL_GAPS.map((g, i) => {
                const color = g.risk === 'exposure' ? '#b16a3c' : g.risk === 'thin' ? '#9c842a' : 'var(--obs-text-2)'
                return (
                  <Surface key={i}>
                    <div style={{ display: 'flex', gap: 12, alignItems: 'flex-start' }}>
                      <div style={{ width: 3, alignSelf: 'stretch', background: color, opacity: 0.55, flexShrink: 0 }} />
                      <div style={{ flex: 1 }}>
                        <div style={{ display: 'flex', gap: 12, alignItems: 'baseline', flexWrap: 'wrap', marginBottom: 6 }}>
                          <Code style={{ color }}>{g.zone}</Code>
                          <Label style={{ color, textTransform: 'uppercase' as const }}>{g.risk}</Label>
                          <span className="voice-mono" style={{ color: 'var(--obs-fg)' }}>{g.skill}</span>
                        </div>
                        <p className="voice-serif" style={{ color: 'var(--obs-text-2)', fontSize: 14, margin: 0, lineHeight: 1.6 }}>
                          {g.detail}
                        </p>
                      </div>
                    </div>
                  </Surface>
                )
              })}
            </div>
          </section>

          {/* Chapter IV: Contractor Dependency */}
          <section id="contract" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="IV · Dependency" code="CONT-LIMIT-01">
              Contractor dependency above 25% creates operational fragility not visible in
              headcount. Zone D at 35% breaches the institutional limit. This is not a
              staffing problem — it is a structural exposure the institution must resolve,
              governed under CONT-LIMIT-01.
            </ConstitutionalArticle>

            <Surface pad="0px" style={{ marginTop: 24, overflow: 'hidden' }}>
              {/* Header */}
              <div style={{
                display: 'grid', gridTemplateColumns: '60px 1fr 80px 80px 130px',
                gap: 12, padding: '10px 20px',
                borderBottom: '1px solid rgba(var(--obs-line),0.1)',
              }}>
                {['ZONE', 'FIRM', 'RATIO', 'LIMIT', 'STATUS'].map((h) => (
                  <div key={h} className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9 }}>{h}</div>
                ))}
              </div>
              {CONTRACTOR_DEPS.map((dep) => {
                const over = dep.ratio > dep.limit
                return (
                  <div key={dep.zone} style={{
                    display: 'grid', gridTemplateColumns: '60px 1fr 80px 80px 130px',
                    gap: 12, padding: '11px 20px',
                    borderBottom: '1px solid rgba(var(--obs-line),0.06)',
                    background: over ? 'rgba(177,106,60,0.04)' : 'transparent',
                    alignItems: 'center',
                  }}>
                    <Code style={{ color: over ? '#b16a3c' : 'var(--obs-text-2)' }}>ZONE {dep.zone}</Code>
                    <span className="voice-mono" style={{ color: 'var(--obs-text-2)' }}>{dep.firm}</span>
                    <span className="voice-mono" style={{ color: over ? '#b16a3c' : '#2d5d4b', fontWeight: over ? 600 : 400 }}>
                      {pct(dep.ratio)}
                    </span>
                    <span className="voice-mono" style={{ color: 'var(--obs-mute)' }}>{pct(dep.limit)}</span>
                    <div className="voice-mono" style={{ color: over ? '#b16a3c' : '#2d5d4b' }}>
                      {over ? 'BREACH · CONT-LIMIT-01' : 'within limit'}
                    </div>
                  </div>
                )
              })}
            </Surface>
          </section>

          {/* Chapter V: Record */}
          <section id="record" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="V · Record" code="OPS-LOG-01">
              The shift record is the institution's account of this day. Every structural event —
              zone holds, system alerts, headcount snapshots — is appended in time order.
              No entry may be removed or reordered. This is the record that governs.
            </ConstitutionalArticle>

            <div style={{ marginTop: 24 }}>
              {RECORD.map((entry, i) => (
                <div key={i} style={{
                  display: 'grid', gridTemplateColumns: '52px 52px 1fr 140px 120px',
                  gap: 12, padding: '10px 0',
                  borderBottom: '1px solid rgba(var(--obs-line),0.07)',
                  alignItems: 'baseline',
                }}>
                  <Code style={{ color: 'var(--obs-mute)' }}>{entry.time}</Code>
                  <Code style={{ color: 'var(--obs-copper)' }}>{entry.zone}</Code>
                  <span className="voice-mono" style={{ color: 'var(--obs-text-2)', fontSize: 11 }}>{entry.event}</span>
                  <span className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 11 }}>{entry.actor}</span>
                  {entry.cite
                    ? <Code style={{ color: 'var(--obs-copper)', opacity: 0.7 }}>{entry.cite}</Code>
                    : <span />}
                </div>
              ))}
            </div>

            {/* Ledger metadata */}
            <div style={{
              display: 'flex', gap: 32, flexWrap: 'wrap', marginTop: 40,
              paddingTop: 16, borderTop: '1px solid rgba(var(--obs-line),0.12)',
            }}>
              {[
                { k: 'SURFACE',    v: 'AttendanceTopology' },
                { k: 'PERSONA',    v: 'MGR' },
                { k: 'AS-OF',      v: '2026-06-13T08:00Z' },
                { k: 'PROJECTION', v: 'OpsTopologyProjection v8' },
              ].map(({ k, v }) => (
                <div key={k}>
                  <div className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9 }}>{k}</div>
                  <Code style={{ color: 'var(--obs-text-2)', display: 'block', marginTop: 3 }}>{v}</Code>
                </div>
              ))}
            </div>
          </section>
        </div>
      </div>
    </div>
  )
}
