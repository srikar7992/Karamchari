import {
  Surface, Label, Code,
  ConstitutionalArticle, SpineRail, useScrollSpy,
  SignalStrip,
  type Signal,
} from '@/components/observatory/surfaces'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EXEC',
  archetype: 'topology',
  signals: 3,
  risks: 0,
  narratives: 1,
  maps: 0,
  bindu: false,
}

// ---- Data -------------------------------------------------------------------

const SPINE_IDS = ['field', 'critical', 'bench', 'gaps', 'record'] as const

const SPINE_NODES = [
  { id: 'field',    label: 'I · FIELD MAP' },
  { id: 'critical', label: 'II · CRITICAL ROLES' },
  { id: 'bench',    label: 'III · BENCH' },
  { id: 'gaps',     label: 'IV · READINESS GAPS' },
  { id: 'record',   label: 'V · RECORD' },
]

const SIGNALS: Signal[] = [
  { id: 'sh', label: 'Succession Health', value: '62%',  unit: '',      delta: '−8% vs Q1',    dir: 'down', spark: [74,71,68,66,63,62] },
  { id: 'bd', label: 'Bench Depth',       value: '3.4',  unit: 'avg',   delta: '+0.2 vs Q1',   dir: 'up',   spark: [2.9,3.0,3.1,3.2,3.3,3.4] },
  { id: 'vc', label: 'Vacancies',         value: '1',    unit: 'active', delta: 'CFO open 62d', dir: 'down', spark: [0,0,0,0,0,1] },
]

interface Successor {
  initials: string
  name: string
  readiness: number
  angle: number   // degrees, 0 = right
}

interface Role {
  id: string
  title: string
  tone: 'good' | 'warn' | 'danger' | 'vacant'
  cx: number
  cy: number
  fieldRadius: number
  incumbentRadius: number
  successors: Successor[]
}

// Each role is a gravity well. fieldRadius = institutional weight of the position.
// incumbentRadius = holder's institutional strength (tenure × performance × authority).
// Successors orbit at distance inversely proportional to readiness.
// Vacant roles have incumbentRadius = 0 and a dashed field ring.
const ROLES: Role[] = [
  {
    id: 'VP-ENG', title: 'VP Engineering',
    tone: 'danger',
    cx: 180, cy: 205,
    fieldRadius: 88, incumbentRadius: 36,
    successors: [
      { initials: 'MK', name: 'Marcus Kim',       readiness: 0.92, angle: -55 },
      { initials: 'EL', name: 'Elena Rodriguez',  readiness: 0.78, angle:  25 },
      { initials: 'DP', name: 'David Patel',      readiness: 0.65, angle: 110 },
    ],
  },
  {
    id: 'CTO', title: 'Chief Technology Officer',
    tone: 'warn',
    cx: 482, cy: 158,
    fieldRadius: 80, incumbentRadius: 44,
    successors: [
      { initials: 'AR', name: 'Amira Rashid',  readiness: 0.55, angle:  50 },
      { initials: 'JS', name: 'James Santos',  readiness: 0.40, angle: 150 },
    ],
  },
  {
    id: 'HEAD-OPS', title: 'Head of Operations',
    tone: 'good',
    cx: 548, cy: 338,
    fieldRadius: 82, incumbentRadius: 46,
    successors: [
      { initials: 'PR', name: 'Priya Rajan',   readiness: 0.88, angle: -20 },
      { initials: 'NK', name: 'Nikhil Kapur',  readiness: 0.82, angle: 200 },
    ],
  },
  {
    id: 'CFO', title: 'Chief Financial Officer',
    tone: 'vacant',
    cx: 105, cy: 348,
    fieldRadius: 70, incumbentRadius: 0,
    successors: [
      { initials: 'TM', name: 'Tomas Müller', readiness: 0.45, angle: 0 },
    ],
  },
  {
    id: 'CPO', title: 'Chief People Officer',
    tone: 'warn',
    cx: 342, cy: 82,
    fieldRadius: 65, incumbentRadius: 36,
    successors: [
      { initials: 'SL', name: 'Sara Lindqvist', readiness: 0.60, angle: -90 },
    ],
  },
]

interface BenchEntry {
  initials: string
  name: string
  role: string
  targetRole: string
  readiness: number
  horizon: string
  lawCite: string
}

const BENCH: BenchEntry[] = [
  { initials: 'MK', name: 'Marcus Kim',      role: 'Dir. Cloud Infrastructure', targetRole: 'VP Engineering', readiness: 0.92, horizon: 'Ready Now',  lawCite: 'SUCC-BENCH-01' },
  { initials: 'EL', name: 'Elena Rodriguez', role: 'Dir. Platform Engineering',  targetRole: 'VP Engineering', readiness: 0.78, horizon: '1–2 Years',  lawCite: 'SUCC-BENCH-01' },
  { initials: 'PR', name: 'Priya Rajan',     role: 'Head of APAC Operations',    targetRole: 'Head of Ops',    readiness: 0.88, horizon: 'Ready Now',  lawCite: 'SUCC-BENCH-01' },
  { initials: 'NK', name: 'Nikhil Kapur',    role: 'Dir. Supply Chain',          targetRole: 'Head of Ops',    readiness: 0.82, horizon: '6 Months',   lawCite: 'SUCC-BENCH-01' },
  { initials: 'DP', name: 'David Patel',     role: 'Sr. Engineering Manager',    targetRole: 'VP Engineering', readiness: 0.65, horizon: '3–5 Years',  lawCite: 'SUCC-BENCH-01' },
  { initials: 'SL', name: 'Sara Lindqvist',  role: 'Head of People Operations',  targetRole: 'CPO',            readiness: 0.60, horizon: '2–3 Years',  lawCite: 'SUCC-BENCH-01' },
  { initials: 'AR', name: 'Amira Rashid',    role: 'VP Product Engineering',     targetRole: 'CTO',            readiness: 0.55, horizon: '3–5 Years',  lawCite: 'SUCC-BENCH-01' },
  { initials: 'TM', name: 'Tomas Müller',    role: 'VP Finance',                 targetRole: 'CFO',            readiness: 0.45, horizon: '3–5 Years',  lawCite: 'SUCC-BENCH-01' },
]

interface Gap {
  initials: string
  name: string
  gap: string
  detail: string
  action: string
  lawCite: string
}

const GAPS: Gap[] = [
  { initials: 'EL', name: 'Elena Rodriguez', gap: 'Executive Board Communication', detail: 'Communication style overly technical for C-suite alignment. Board presentation experience absent.', action: 'Assign executive coach (comm. focus). Present Q4 Tech Roadmap to Board.', lawCite: 'SUCC-DEV-02' },
  { initials: 'DP', name: 'David Patel', gap: 'Strategic Budget Allocation', detail: 'No experience managing portfolios over $5M. CFO-level financial exposure required.', action: 'Co-own FY27 Engineering CapEx with incumbent. Shadow CFO budget review cycle.', lawCite: 'SUCC-DEV-02' },
  { initials: 'TM', name: 'Tomas Müller', gap: 'CFO Scope — External Reporting', detail: 'VP Finance scope does not include SEC reporting or Board audit committee. Institutional gap vs. CFO mandate.', action: 'Interim CFO assignment for 6-month period. Board audit committee observer status.', lawCite: 'SUCC-DEV-02' },
  { initials: 'AR', name: 'Amira Rashid', gap: 'Org-Scale Architecture Vision', detail: 'Product engineering depth present. Company-wide technical strategy and vendor governance absent.', action: 'Assign to Vendor Governance Council. Co-author FY27 Technology Strategy with CTO.', lawCite: 'SUCC-DEV-02' },
]

interface AuditEntry {
  date: string
  event: string
  detail: string
  cite?: string
}

const RECORD: AuditEntry[] = [
  { date: '2026-06-01', event: 'BENCH_ASSESSMENT',   detail: 'Q2 Succession Review completed. Succession Health index declined to 62%.', cite: 'SUCC-AUDIT-01' },
  { date: '2026-05-14', event: 'VACANCY_DECLARED',   detail: 'CFO vacancy activated following incumbent departure. Pipeline depth: 1 candidate at 45%.', cite: 'SUCC-VAC-01' },
  { date: '2026-04-11', event: 'READINESS_UPDATE',   detail: 'Marcus Kim upgraded to 92% following cross-functional rotation completion.' },
  { date: '2026-03-15', event: 'DEV_ACTION',         detail: 'Elena Rodriguez — executive communication coach assigned. 6-month engagement.', cite: 'SUCC-DEV-02' },
  { date: '2026-01-20', event: 'PLAN_APPROVED',      detail: 'FY26 Succession Plan ratified by Compensation & Governance Committee.', cite: 'SUCC-PLAN-01' },
]

// ---- Helpers ----------------------------------------------------------------

const TONE_COLOR = {
  good:   '#2d5d4b',
  warn:   '#9c842a',
  danger: '#b16a3c',
  vacant: '#b16a3c',
}

function readinessTone(r: number): string {
  return r >= 0.80 ? '#2d5d4b' : r >= 0.60 ? '#9c842a' : '#b16a3c'
}

function readinessHorizonTone(h: string): string {
  return h === 'Ready Now' ? '#2d5d4b' : h.includes('6 M') || h.includes('1–2') ? '#9c842a' : '#b16a3c'
}

// Orbit radius: incumbentRadius + gap + (1 - readiness) × inner band.
// Close successor = high readiness = close to incumbent.
// Distant successor = low readiness = near field boundary.
function orbitPos(
  cx: number, cy: number,
  incR: number, fieldR: number,
  readiness: number, angleDeg: number
): { x: number; y: number } {
  const band = Math.max(fieldR - incR - 22, 8)
  const r = incR + 14 + (1 - readiness) * band
  const rad = (angleDeg * Math.PI) / 180
  return { x: cx + r * Math.cos(rad), y: cy + r * Math.sin(rad) }
}

// ---- Influence Field SVG ----------------------------------------------------

function InfluenceFieldMap() {
  return (
    <svg
      viewBox="0 0 680 442"
      style={{ width: '100%', height: 'auto', display: 'block', color: 'var(--obs-fg)' }}
      aria-label="Succession influence field — role gravity wells. Field size = institutional weight. Incumbent circle = holder strength. Orbit distance = successor readiness. Dashed ring = vacancy."
    >
      <defs>
        <pattern id="suc-grid" width="34" height="34" patternUnits="userSpaceOnUse">
          <path d="M 34 0 L 0 0 0 34" fill="none" stroke="currentColor" strokeOpacity="0.045" strokeWidth="0.5" />
        </pattern>
      </defs>
      <rect width="680" height="442" fill="url(#suc-grid)" />

      {ROLES.map((role) => {
        const color = TONE_COLOR[role.tone]
        const isVacant = role.tone === 'vacant'
        const isDanger = role.tone === 'danger'

        return (
          <g key={role.id}>
            {/* Outer influence field ring */}
            <circle
              cx={role.cx} cy={role.cy}
              r={role.fieldRadius}
              fill={color}
              fillOpacity={isDanger ? 0.07 : isVacant ? 0.03 : 0.06}
              stroke={color}
              strokeWidth={isDanger ? 1.8 : 1}
              strokeOpacity={isVacant ? 0.35 : isDanger ? 0.6 : 0.35}
              strokeDasharray={isVacant ? '7,4' : 'none'}
            />

            {/* Incumbent circle — encodes institutional strength of current holder */}
            {role.incumbentRadius > 0 && (
              <circle
                cx={role.cx} cy={role.cy}
                r={role.incumbentRadius}
                fill={color}
                fillOpacity={isDanger ? 0.22 : 0.16}
                stroke={color}
                strokeWidth={isDanger ? 2.5 : 1.5}
                strokeOpacity={isDanger ? 0.8 : 0.55}
              />
            )}

            {/* Vacancy vacuum marker — three concentric dashed rings contracting inward */}
            {isVacant && (
              <>
                <circle cx={role.cx} cy={role.cy} r={22}
                  fill="none" stroke={color} strokeWidth="1" strokeOpacity="0.35" strokeDasharray="4,4" />
                <circle cx={role.cx} cy={role.cy} r={12}
                  fill="none" stroke={color} strokeWidth="1" strokeOpacity="0.25" strokeDasharray="3,3" />
                <circle cx={role.cx} cy={role.cy} r={4}
                  fill={color} fillOpacity="0.25" />
              </>
            )}

            {/* Orbit lines — gossamer threads from center to each successor */}
            {role.successors.map((s, i) => {
              const pos = orbitPos(role.cx, role.cy, role.incumbentRadius, role.fieldRadius, s.readiness, s.angle)
              return (
                <line key={i}
                  x1={role.cx} y1={role.cy}
                  x2={pos.x}   y2={pos.y}
                  stroke="currentColor" strokeOpacity="0.1" strokeWidth="0.8"
                />
              )
            })}

            {/* Successor nodes — orbit the role at distances encoding readiness */}
            {role.successors.map((s, i) => {
              const pos = orbitPos(role.cx, role.cy, role.incumbentRadius, role.fieldRadius, s.readiness, s.angle)
              const nodeColor = readinessTone(s.readiness)
              const nodeR = 6 + s.readiness * 4.5
              return (
                <g key={i}>
                  <circle cx={pos.x} cy={pos.y} r={nodeR}
                    fill={nodeColor} fillOpacity="0.72"
                    stroke={nodeColor} strokeWidth="1.2" strokeOpacity="0.9"
                  />
                  <text x={pos.x} y={pos.y + 3}
                    textAnchor="middle"
                    fontFamily="JetBrains Mono, monospace"
                    fontSize="6" fontWeight="500"
                    fill="rgba(255,255,255,0.85)">
                    {s.initials}
                  </text>
                </g>
              )
            })}

            {/* Role identifier — below field ring */}
            <text
              x={role.cx}
              y={role.cy + role.fieldRadius + 14}
              textAnchor="middle"
              fontFamily="JetBrains Mono, monospace"
              fontSize="8" letterSpacing="1"
              fill={color} fillOpacity="0.7"
            >
              {role.id}
            </text>
            {isVacant && (
              <text
                x={role.cx}
                y={role.cy + role.fieldRadius + 24}
                textAnchor="middle"
                fontFamily="JetBrains Mono, monospace"
                fontSize="7" letterSpacing="0.5"
                fill={color} fillOpacity="0.5"
              >
                VACANT
              </text>
            )}
          </g>
        )
      })}

      <rect width="680" height="442" fill="none" stroke="currentColor" strokeOpacity="0.14" strokeWidth="1" />
    </svg>
  )
}

// ---- Role card for Chapter II -----------------------------------------------

function CriticalRoleCard({ role }: { role: Role }) {
  const color = TONE_COLOR[role.tone]
  const isVacant = role.tone === 'vacant'
  const label = isVacant ? 'VACANT' : 'CRITICAL SUCCESSION RISK'
  const nearest = role.successors.length
    ? role.successors.reduce((a, b) => a.readiness > b.readiness ? a : b)
    : null
  return (
    <Surface risk={role.tone === 'danger' || isVacant} style={{ borderLeft: `3px solid ${color}` }}>
      <div style={{ display: 'flex', gap: 24, alignItems: 'flex-start' }}>
        {/* Field geometry thumbnail */}
        <svg width="60" height="60" viewBox="-1 -1 62 62" style={{ flexShrink: 0 }}>
          <circle cx="30" cy="30" r="28"
            fill={color} fillOpacity={isVacant ? 0.05 : 0.10}
            stroke={color} strokeWidth="1.5" strokeOpacity="0.5"
            strokeDasharray={isVacant ? '5,3' : 'none'}
          />
          {role.incumbentRadius > 0 && (
            <circle cx="30" cy="30"
              r={role.incumbentRadius * 0.32}
              fill={color} fillOpacity="0.35"
              stroke={color} strokeWidth="1" strokeOpacity="0.7"
            />
          )}
          {isVacant && (
            <circle cx="30" cy="30" r="4"
              fill={color} fillOpacity="0.3" />
          )}
          {role.successors.slice(0, 2).map((s, i) => {
            const angle = (s.angle * Math.PI) / 180
            const r = 14 + (1 - s.readiness) * 10
            return (
              <circle key={i}
                cx={30 + r * Math.cos(angle)} cy={30 + r * Math.sin(angle)}
                r={3 + s.readiness * 2}
                fill={readinessTone(s.readiness)} fillOpacity="0.75"
              />
            )
          })}
        </svg>

        <div style={{ flex: 1 }}>
          <div style={{ display: 'flex', gap: 16, alignItems: 'baseline', marginBottom: 6, flexWrap: 'wrap' }}>
            <Code style={{ color }}>{role.id}</Code>
            <Label style={{ color, textTransform: 'uppercase' as const }}>{label}</Label>
          </div>
          <div className="voice-serif" style={{ fontSize: 16, color: 'var(--obs-fg)', marginBottom: 8 }}>
            {role.title}
          </div>
          <div style={{ display: 'flex', gap: 24, flexWrap: 'wrap' }}>
            <div>
              <div className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9 }}>BENCH DEPTH</div>
              <div className="voice-mono" style={{ color, marginTop: 2 }}>
                {role.successors.length} candidate{role.successors.length !== 1 ? 's' : ''}
              </div>
            </div>
            {nearest && (
              <div>
                <div className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9 }}>NEAREST SUCCESSOR</div>
                <div className="voice-mono" style={{ color: readinessTone(nearest.readiness), marginTop: 2 }}>
                  {nearest.initials} · {Math.round(nearest.readiness * 100)}% ready
                </div>
              </div>
            )}
            {isVacant && (
              <div>
                <div className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9 }}>VACANCY</div>
                <div className="voice-mono" style={{ color, marginTop: 2 }}>62 days open</div>
              </div>
            )}
          </div>
        </div>
      </div>
    </Surface>
  )
}

// ---- Page -------------------------------------------------------------------

export function SuccessionWorkspace() {
  const activeIdx = useScrollSpy([...SPINE_IDS])

  return (
    <div className="obs obs-canvas" style={{ minHeight: '100vh', padding: '32px 24px 80px' }}>
      {/* Header */}
      <div style={{ maxWidth: 960, margin: '0 auto 40px' }}>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginBottom: 6 }}>
          SUCCESSION · TOPOLOGY · 2026-06-13
        </div>
        <h1 className="voice-serif" style={{
          fontSize: 40, fontWeight: 400, color: 'var(--obs-fg)', margin: 0, lineHeight: 1.1,
        }}>
          Succession Influence Fields
        </h1>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginTop: 8 }}>
          5 critical roles · 1 vacancy · succession health 62%
        </div>
      </div>

      <div style={{ maxWidth: 960, margin: '0 auto', display: 'grid', gridTemplateColumns: '164px 1fr', gap: 48 }}>
        {/* Spine */}
        <SpineRail nodes={SPINE_NODES} active={activeIdx} />

        {/* Content */}
        <div style={{ minWidth: 0 }}>

          {/* Signals */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12, marginBottom: 40 }}>
            {SIGNALS.map((sig) => <SignalStrip key={sig.id} sig={sig} />)}
          </div>

          {/* Chapter I: Field Map */}
          <section id="field" style={{ scrollMarginTop: 96 }}>
            <ConstitutionalArticle title="I · Field Map" code="SUCC-FIELD-01">
              Every role exerts a field proportional to its institutional weight. The incumbent shapes
              that field through tenure, performance, and authority. Successors orbit at distances that
              encode readiness — close means capable now, distant means years away. A vacant role
              becomes a vacuum: the field persists, but nothing holds it. Geometry precedes language here.
            </ConstitutionalArticle>

            <Surface pad="0px" style={{ overflow: 'hidden', border: '1px solid rgba(var(--obs-line),0.12)', marginTop: 24 }}>
              <InfluenceFieldMap />
            </Surface>

            {/* Field legend */}
            <div style={{ display: 'flex', gap: 32, marginTop: 20, flexWrap: 'wrap' }}>
              {[
                { label: 'Field ring — role institutional weight' },
                { label: 'Inner circle — incumbent strength' },
                { label: 'Orbit distance — successor readiness (close = ready)' },
                { label: 'Dashed ring — vacancy' },
              ].map((item) => (
                <div key={item.label} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <div style={{ width: 8, height: 8, borderRadius: '50%', background: 'rgba(var(--obs-line),0.3)', flexShrink: 0 }} />
                  <span className="voice-mono" style={{ color: 'var(--obs-mute)' }}>{item.label}</span>
                </div>
              ))}
            </div>

            {/* Successor readiness scale */}
            <div style={{ display: 'flex', gap: 24, marginTop: 12, flexWrap: 'wrap' }}>
              {[
                { color: '#2d5d4b', label: 'Node: ≥ 80% ready (forest)' },
                { color: '#9c842a', label: 'Node: 60–79% (developing)' },
                { color: '#b16a3c', label: 'Node: < 60% (pipeline)' },
              ].map((item) => (
                <div key={item.label} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <div style={{ width: 10, height: 10, borderRadius: '50%', background: item.color, opacity: 0.75, flexShrink: 0 }} />
                  <span className="voice-mono" style={{ color: 'var(--obs-mute)' }}>{item.label}</span>
                </div>
              ))}
            </div>
          </section>

          {/* Chapter II: Critical Roles */}
          <section id="critical" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="II · Critical Roles" code="SUCC-RISK-01">
              A role is critical not because of its title but because of the vacuum it would leave.
              Two conditions define institutional succession risk: a field with a weakened incumbent,
              and a field without one. Both are visible in the geometry before they appear in a report.
            </ConstitutionalArticle>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 12, marginTop: 24 }}>
              {ROLES.filter((r) => r.tone === 'danger' || r.tone === 'vacant').map((role) => (
                <CriticalRoleCard key={role.id} role={role} />
              ))}
            </div>
          </section>

          {/* Chapter III: Bench */}
          <section id="bench" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="III · Bench" code="SUCC-BENCH-01">
              The bench is not a list of candidates. It is a measurement of the institution's
              ability to replace itself. Each entry carries a readiness score, a time horizon,
              and a target role. Readiness below 60% is pipeline, not bench. The distinction matters
              when a vacancy activates.
            </ConstitutionalArticle>

            <Surface pad="0px" style={{ marginTop: 24, overflow: 'hidden' }}>
              {/* Header */}
              <div style={{
                display: 'grid', gridTemplateColumns: '40px 1fr 100px 100px 80px',
                gap: 12, padding: '10px 20px',
                borderBottom: '1px solid rgba(var(--obs-line),0.10)',
              }}>
                {['', 'SUCCESSOR', 'TARGET ROLE', 'HORIZON', 'READINESS'].map((h, i) => (
                  <div key={i} className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9 }}>{h}</div>
                ))}
              </div>
              {BENCH.map((entry, i) => {
                const toneColor = readinessTone(entry.readiness)
                const horizonColor = readinessHorizonTone(entry.horizon)
                return (
                  <div key={i} style={{
                    display: 'grid', gridTemplateColumns: '40px 1fr 100px 100px 80px',
                    gap: 12, padding: '11px 20px',
                    borderBottom: '1px solid rgba(var(--obs-line),0.06)',
                    alignItems: 'center',
                  }}>
                    {/* Initials badge */}
                    <div style={{
                      width: 28, height: 28,
                      background: toneColor, color: '#fff',
                      display: 'flex', alignItems: 'center', justifyContent: 'center',
                      fontFamily: 'JetBrains Mono', fontSize: 8, fontWeight: 600,
                      opacity: 0.85,
                    }}>
                      {entry.initials}
                    </div>
                    <div>
                      <div className="voice-mono" style={{ color: 'var(--obs-fg)' }}>{entry.name}</div>
                      <div className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9, marginTop: 2 }}>{entry.role}</div>
                    </div>
                    <Code style={{ color: 'var(--obs-mute)' }}>{entry.targetRole}</Code>
                    <div className="voice-mono" style={{ color: horizonColor }}>{entry.horizon}</div>
                    <div style={{ fontFamily: 'JetBrains Mono', fontSize: 13, fontWeight: 300, color: toneColor }}>
                      {Math.round(entry.readiness * 100)}%
                    </div>
                  </div>
                )
              })}
            </Surface>
          </section>

          {/* Chapter IV: Readiness Gaps */}
          <section id="gaps" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="IV · Readiness Gaps" code="SUCC-DEV-02">
              Readiness is not time — it is capability distance. A gap is the specific institutional
              experience the successor lacks before the field can close around them. Every gap here
              has a defined action. Actions without timelines are intentions, not plans.
            </ConstitutionalArticle>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 16, marginTop: 24 }}>
              {GAPS.map((g, i) => (
                <Surface key={i}>
                  <div style={{ display: 'flex', gap: 16, alignItems: 'flex-start' }}>
                    <div style={{
                      width: 28, height: 28,
                      background: '#9c842a', color: '#fff',
                      display: 'flex', alignItems: 'center', justifyContent: 'center',
                      fontFamily: 'JetBrains Mono', fontSize: 8, fontWeight: 600,
                      flexShrink: 0, opacity: 0.85,
                    }}>
                      {g.initials}
                    </div>
                    <div style={{ flex: 1 }}>
                      <div style={{ display: 'flex', gap: 16, alignItems: 'baseline', marginBottom: 6, flexWrap: 'wrap' }}>
                        <span className="voice-mono" style={{ color: 'var(--obs-fg)' }}>{g.name}</span>
                        <Label style={{ color: '#9c842a' }}>{g.gap}</Label>
                      </div>
                      <p className="voice-serif" style={{ fontSize: 14, color: 'var(--obs-text-2)', margin: '0 0 10px', lineHeight: 1.6 }}>
                        {g.detail}
                      </p>
                      <div style={{
                        padding: '10px 14px',
                        background: 'rgba(156,132,42,0.06)',
                        borderLeft: '2px solid rgba(156,132,42,0.4)',
                      }}>
                        <span className="voice-mono" style={{ color: '#9c842a', fontSize: 9 }}>ACTION · </span>
                        <span className="voice-mono" style={{ color: 'var(--obs-text-2)', fontSize: 10 }}>{g.action}</span>
                      </div>
                      <Code style={{ color: 'var(--obs-mute)', display: 'block', marginTop: 8 }}>{g.lawCite}</Code>
                    </div>
                  </div>
                </Surface>
              ))}
            </div>
          </section>

          {/* Chapter V: Record */}
          <section id="record" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="V · Record" code="SUCC-AUDIT-01">
              The succession record is the institution's account of its own continuity planning.
              Bench assessments, vacancy declarations, development actions, and plan approvals are
              appended in time order. The record is immutable. Accountability lives here.
            </ConstitutionalArticle>

            <div style={{ marginTop: 24 }}>
              {RECORD.map((entry, i) => (
                <div key={i} style={{
                  display: 'grid', gridTemplateColumns: '100px 160px 1fr 130px',
                  gap: 12, padding: '10px 0',
                  borderBottom: '1px solid rgba(var(--obs-line),0.07)',
                  alignItems: 'baseline',
                }}>
                  <Code style={{ color: 'var(--obs-mute)' }}>{entry.date}</Code>
                  <Code style={{ color: 'var(--obs-copper)' }}>{entry.event}</Code>
                  <span className="voice-mono" style={{ color: 'var(--obs-text-2)', fontSize: 10.5 }}>{entry.detail}</span>
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
                { k: 'SURFACE',    v: 'SuccessionInfluenceFields' },
                { k: 'PERSONA',    v: 'EXEC' },
                { k: 'AS-OF',      v: '2026-06-13T00:00Z' },
                { k: 'PROJECTION', v: 'SuccessionProjection v14' },
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
