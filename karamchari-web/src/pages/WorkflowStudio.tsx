import { useState } from 'react'
import {
  Surface, Label, Code,
  ConstitutionalArticle, SpineRail, useScrollSpy,
  SignalStrip, RiskSurface, AuthoritySurface,
  type Signal, type Risk,
} from '@/components/observatory/surfaces'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'SYS',
  archetype: 'builder',
  signals: 3,
  risks: 1,
  narratives: 1,
  maps: 0,
  bindu: true,
}

// ---- Data -------------------------------------------------------------------

const SPINE_IDS = ['composition', 'progression', 'contest', 'authority', 'record'] as const
const SPINE_NODES = [
  { id: 'composition', label: 'I · COMPOSITION' },
  { id: 'progression',  label: 'II · PROGRESSION' },
  { id: 'contest',      label: 'III · CONTEST' },
  { id: 'authority',    label: 'IV · AUTHORITY' },
  { id: 'record',       label: 'V · RECORD' },
]

const SIGNALS: Signal[] = [
  { id: 's1', label: 'Definition Shape', value: '6',   unit: 'steps · 2 branches', delta: 'all paths terminate', dir: 'up',   spark: [6,6,6,6,6,6] },
  { id: 's2', label: 'Crystallinity',    value: '4/6', unit: 'stages sealed',       delta: '+2 vs v3',            dir: 'up',   spark: [2,2,3,3,4,4] },
  { id: 's3', label: 'In-Flight',        value: '214', unit: 'under v3',            delta: 'grandfathered GF-02', dir: 'down', spark: [180,195,205,210,213,214] },
]

// A workflow stage carries a crystallinity c in [0,1] — how settled the law is.
// c=0: unformed (open, dashed, irregular). c=1: enacted (compact, solid, sealed).
interface Stage {
  id: string
  code: string
  kind: 'entry' | 'approval' | 'condition' | 'notify' | 'terminal'
  label: string
  c: number
  cx: number
  cy: number
}

const STAGES: Stage[] = [
  { id: 'submit', code: 'WF-ENTRY',   kind: 'entry',     label: 'Submit via ESS',     c: 0.06, cx: 240, cy: 66  },
  { id: 'mgr',    code: 'WF-APPR-01', kind: 'approval',  label: 'Manager Approval',   c: 0.42, cx: 240, cy: 180 },
  { id: 'cond',   code: 'WF-COND-01', kind: 'condition', label: 'Duration > 3 days?', c: 0.50, cx: 240, cy: 296 },
  { id: 'hr',     code: 'WF-APPR-02', kind: 'approval',  label: 'HR Partner Review',  c: 0.70, cx: 122, cy: 410 },
  { id: 'notify', code: 'WF-NOTIFY',  kind: 'notify',    label: 'Notify Requester',   c: 0.82, cx: 350, cy: 410 },
  { id: 'close',  code: 'WF-TERM',    kind: 'terminal',  label: 'Close',              c: 1.00, cx: 240, cy: 524 },
]

interface Edge { from: string; to: string; settled: boolean; label?: string }
const EDGES: Edge[] = [
  { from: 'submit', to: 'mgr',    settled: true },
  { from: 'mgr',    to: 'cond',   settled: true },
  { from: 'cond',   to: 'hr',     settled: false, label: '> 3d' },
  { from: 'cond',   to: 'notify', settled: false, label: '≤ 3d' },
  { from: 'hr',     to: 'notify', settled: true },
  { from: 'notify', to: 'close',  settled: true },
]

const STAGE_PROPS: Record<string, { label: string; value: string }[]> = {
  submit: [
    { label: 'Trigger', value: 'LeaveRequestCreated' },
    { label: 'Channel', value: 'ESS / Mobile' },
    { label: 'Validation', value: 'Balance + blackout check' },
  ],
  mgr: [
    { label: 'Approver', value: 'Direct manager (role)' },
    { label: 'SLA', value: '48 h' },
    { label: 'On Breach', value: 'Escalate: HR partner pool' },
  ],
  cond: [
    { label: 'Expression', value: 'request.days > 3' },
    { label: 'True Path', value: 'HR Partner Review' },
    { label: 'False Path', value: 'Notify Requester' },
  ],
  hr: [
    { label: 'Approver', value: 'HR partner (zone)' },
    { label: 'SLA', value: '24 h' },
    { label: 'On Breach', value: 'No fallback — WF-RULE-007' },
  ],
  notify: [
    { label: 'Channels', value: 'In-app + email' },
    { label: 'Template', value: 'NT-LEAVE-DECISION-02' },
  ],
  close: [
    { label: 'Effects', value: 'Balance debit · calendar block' },
    { label: 'Ledger', value: 'Sutra entry, immutable' },
  ],
}

const FALLBACK_RISK: Risk = {
  id: 'WF-RULE-007',
  level: 'elevated',
  title: 'Branch defines no fallback approver',
  detail:
    "The 'duration > 3 days' branch routes to HR Partner Review with no explicit fallback. On SLA breach the engine escalates to the HR partner pool by default — the definition should state this, not inherit it. Unsettled law is governed by the engine, not the institution.",
  exposure: 'Silent escalation',
  horizon: 'On publish',
  owner: 'Workflow owner',
}

interface VersionEntry { ver: string; date: string; event: string; actor: string; cite?: string }
const RECORD: VersionEntry[] = [
  { ver: 'v1', date: '2024-08-12', event: 'CREATED',   actor: 'S. Menon', cite: 'WF-INIT' },
  { ver: 'v2', date: '2025-03-04', event: 'AMENDED',   actor: 'S. Menon', cite: 'WF-RULE-003' },
  { ver: 'v3', date: '2025-11-02', event: 'PUBLISHED', actor: 'S. Menon', cite: 'WF-PUBLISH-01' },
  { ver: 'v4', date: '2026-06-13', event: 'DRAFTED',   actor: 'A. Rao' },
]

// ---- Geometry ---------------------------------------------------------------

const COPPER = '#b16a3c'
const MUTE = 'var(--obs-mute)'

// Six-vertex form. Irregularity (jitter) decays to zero as the law crystallizes;
// radius contracts. Draft = a broad wobbly cloud; enacted = a compact regular crystal.
const JIT = [0.30, -0.20, 0.38, -0.28, 0.16, -0.34]
function crystal(cx: number, cy: number, c: number): [number, number][] {
  const baseR = 50 - c * 24
  return Array.from({ length: 6 }, (_, i) => {
    const ang = (i / 6) * Math.PI * 2 - Math.PI / 2
    const r = baseR * (1 + JIT[i] * (1 - c))
    return [cx + r * Math.cos(ang), cy + r * Math.sin(ang)] as [number, number]
  })
}
function ptsStr(p: [number, number][]) {
  return p.map((q) => `${q[0].toFixed(1)},${q[1].toFixed(1)}`).join(' ')
}
// Condition fork: a diamond, never crystallises — the decision is where law stays open.
function diamond(cx: number, cy: number, r: number) {
  return `${cx},${cy - r} ${cx + r},${cy} ${cx},${cy + r} ${cx - r},${cy}`
}

function stageById(id: string) {
  return STAGES.find((s) => s.id === id)!
}

// ---- Composition field SVG --------------------------------------------------

function CompositionField({ activeId, onStage }: { activeId: string | null; onStage: (id: string | null) => void }) {
  return (
    <svg
      viewBox="0 0 480 600"
      style={{ width: '100%', height: 'auto', maxHeight: 620, display: 'block', color: 'var(--obs-fg)' }}
      aria-label="Composition field — stage geometry encodes how settled the law is"
    >
      <defs>
        <pattern id="wf-grid" width="34" height="34" patternUnits="userSpaceOnUse">
          <path d="M 34 0 L 0 0 0 34" fill="none" stroke="currentColor" strokeOpacity="0.05" strokeWidth="0.5" />
        </pattern>
      </defs>
      <rect width="480" height="600" fill="url(#wf-grid)" />

      {/* Authority axis — top unformed, bottom enacted */}
      <line x1="40" y1="50" x2="40" y2="540" stroke="currentColor" strokeOpacity="0.16" strokeWidth="1" />
      <text x="40" y="40" textAnchor="middle" fontFamily="JetBrains Mono, monospace" fontSize="8" letterSpacing="1.5" fill="currentColor" fillOpacity="0.45">UNFORMED</text>
      <text x="40" y="558" textAnchor="middle" fontFamily="JetBrains Mono, monospace" fontSize="8" letterSpacing="1.5" fill={COPPER}>ENACTED</text>

      {/* Edges — settled trunk thickens as authority accumulates; branches stay dashed */}
      {EDGES.map((e, i) => {
        const a = stageById(e.from)
        const b = stageById(e.to)
        const w = 1.6 + ((a.c + b.c) / 2) * 3.4
        const color = e.settled ? COPPER : MUTE
        const mx = (a.cx + b.cx) / 2
        const my = (a.cy + b.cy) / 2
        return (
          <g key={i}>
            <line
              x1={a.cx} y1={a.cy} x2={b.cx} y2={b.cy}
              stroke={color}
              strokeWidth={w}
              strokeOpacity={e.settled ? 0.72 : 0.42}
              strokeDasharray={e.settled ? undefined : '5 5'}
            />
            {e.label && (
              <text x={mx + 8} y={my} fontFamily="JetBrains Mono, monospace" fontSize="8" fill="currentColor" fillOpacity="0.5">
                {e.label}
              </text>
            )}
          </g>
        )
      })}

      {/* Stages */}
      {STAGES.map((s) => {
        const active = activeId === s.id
        const sealed = s.c >= 0.6
        const stroke = sealed ? COPPER : MUTE
        const fillOpacity = (active ? s.c * 0.6 + 0.16 : s.c * 0.5)
        const strokeWidth = (1.6 + s.c * 2.4) + (active ? 1.2 : 0)
        const dash = s.c >= 0.85 ? undefined : `${(1 - s.c) * 5 + 1.5} ${(1 - s.c) * 5 + 1.5}`

        const shape =
          s.kind === 'condition' ? (
            <polygon
              points={diamond(s.cx, s.cy, 34)}
              fill={MUTE} fillOpacity={active ? 0.16 : 0.08}
              stroke={MUTE} strokeWidth={active ? 2.4 : 1.6}
              strokeDasharray="5 4" strokeLinejoin="miter"
            />
          ) : (
            <polygon
              points={ptsStr(crystal(s.cx, s.cy, s.c))}
              fill={COPPER} fillOpacity={fillOpacity}
              stroke={stroke} strokeWidth={strokeWidth}
              strokeDasharray={dash} strokeLinejoin="miter"
              style={{ transition: 'fill-opacity 180ms, stroke-width 180ms' }}
            />
          )

        return (
          <g key={s.id} style={{ cursor: 'pointer' }} onClick={() => onStage(active ? null : s.id)}>
            {shape}
            {/* enacted seal — copper diamond + authority halo once law has crystallised */}
            {s.c >= 0.99 && (
              <>
                <polygon points={diamond(s.cx, s.cy, 22)} fill="none" stroke={COPPER} strokeWidth="1.4" strokeOpacity="0.5" />
                <polygon points={diamond(s.cx, s.cy, 11)} fill={COPPER} stroke="#fff" strokeWidth="1.4" />
              </>
            )}
            <text x={s.cx} y={s.cy + (s.kind === 'condition' ? 1 : 4)} textAnchor="middle"
              fontFamily="JetBrains Mono, monospace" fontSize="9.5" letterSpacing="1.5" fontWeight={sealed ? 600 : 400}
              fill={sealed && s.kind !== 'condition' ? '#fff' : 'currentColor'}
              fillOpacity={sealed && s.kind !== 'condition' ? 1 : 0.66}>
              {s.code}
            </text>
            <text x={s.cx} y={s.cy + (s.kind === 'condition' ? 52 : 60)} textAnchor="middle"
              fontFamily="JetBrains Mono, monospace" fontSize="8.5"
              fill="currentColor" fillOpacity="0.55">
              {s.label.toUpperCase()}
            </text>
          </g>
        )
      })}

      <rect width="480" height="600" fill="none" stroke="currentColor" strokeOpacity="0.14" strokeWidth="1" />
    </svg>
  )
}

// ---- Stage detail (shown on click) ------------------------------------------

function StageDetail({ stage }: { stage: Stage }) {
  const sealed = stage.c >= 0.6
  const color = sealed ? COPPER : MUTE
  return (
    <div className="cham" style={{
      background: 'rgba(var(--obs-line),0.025)',
      border: '1px solid rgba(var(--obs-line),0.12)',
      padding: '16px 20px', marginTop: 12,
    }}>
      <div style={{ display: 'flex', gap: 12, alignItems: 'baseline', flexWrap: 'wrap', marginBottom: 12 }}>
        <Code style={{ color }}>{stage.code}</Code>
        <Label style={{ color, textTransform: 'uppercase' as const }}>{stage.kind}</Label>
        <span className="voice-mono" style={{ color: 'var(--obs-fg)' }}>{stage.label}</span>
        <span className="voice-mono" style={{ color: 'var(--obs-mute)', marginLeft: 'auto' }}>
          {stage.c >= 0.99 ? 'ENACTED' : stage.c >= 0.6 ? 'SEALED' : stage.kind === 'condition' ? 'OPEN' : 'FORMING'}
        </span>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
        {STAGE_PROPS[stage.id].map((p) => (
          <div key={p.label} style={{
            display: 'flex', justifyContent: 'space-between', gap: 16,
            paddingBottom: 6, borderBottom: '1px solid rgba(var(--obs-line),0.07)',
          }}>
            <span className="voice-mono" style={{ color: 'var(--obs-mute)' }}>{p.label}</span>
            <Code style={{ color: 'var(--obs-text-2)' }}>{p.value}</Code>
          </div>
        ))}
      </div>
    </div>
  )
}

// ---- Stage index row (supporting evidence beside the field) -----------------

function stageState(s: Stage) {
  return s.c >= 0.99 ? 'ENACTED' : s.c >= 0.6 ? 'SEALED' : s.kind === 'condition' ? 'OPEN' : s.c > 0.2 ? 'FORMING' : 'DRAFT'
}

function StageRow({ stage, active, onSelect }: { stage: Stage; active: boolean; onSelect: (id: string | null) => void }) {
  const sealed = stage.c >= 0.6
  const color = sealed ? COPPER : MUTE
  return (
    <button
      onClick={() => onSelect(active ? null : stage.id)}
      className="cham-sm"
      style={{
        display: 'block', width: '100%', textAlign: 'left', cursor: 'pointer',
        border: active ? '1px solid rgba(177,106,60,0.55)' : '1px solid rgba(var(--obs-line),0.12)',
        background: active ? 'rgba(177,106,60,0.06)' : 'rgba(var(--obs-line),0.015)',
        padding: '9px 12px',
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: 8 }}>
        <Code style={{ color }}>{stage.code}</Code>
        <span className="voice-mono" style={{ color, fontSize: 8.5, letterSpacing: '0.12em' }}>{stageState(stage)}</span>
      </div>
      <div className="voice-mono" style={{ color: 'var(--obs-fg)', fontSize: 11, marginTop: 3 }}>{stage.label}</div>
      <div style={{ height: 3, background: 'rgba(var(--obs-line),0.08)', marginTop: 7, position: 'relative' }}>
        <div style={{ position: 'absolute', left: 0, top: 0, bottom: 0, width: `${stage.c * 100}%`, background: sealed ? COPPER : MUTE, opacity: sealed ? 0.85 : 0.45 }} />
      </div>
    </button>
  )
}

// ---- Crystallinity legend ---------------------------------------------------

function LegendCrystal({ c }: { c: number }) {
  const sealed = c >= 0.6
  const dash = c >= 0.85 ? undefined : `${(1 - c) * 4 + 1.2} ${(1 - c) * 4 + 1.2}`
  return (
    <svg width="40" height="40" viewBox="0 0 40 40" aria-hidden="true">
      <polygon
        points={ptsStr(crystal(20, 20, c).map(([x, y]) => [(x - (20 - 22)) * 0.6 + 4, (y - (20 - 22)) * 0.6 + 4] as [number, number]))}
        fill={COPPER} fillOpacity={c * 0.42}
        stroke={sealed ? COPPER : MUTE} strokeWidth={1 + c * 1.4}
        strokeDasharray={dash} strokeLinejoin="miter"
      />
    </svg>
  )
}

// ---- Page -------------------------------------------------------------------

export function WorkflowStudio() {
  const activeIdx = useScrollSpy([...SPINE_IDS])
  const [activeStage, setActiveStage] = useState<string | null>('mgr')

  return (
    <div className="obs obs-canvas" style={{ minHeight: '100vh', padding: '32px 24px 80px' }}>
      {/* Header */}
      <div style={{ maxWidth: 1080, margin: '0 auto 40px' }}>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginBottom: 6 }}>
          WORKFLOW · LAW COMPOSITION · LEAVE-STD-01
        </div>
        <h1 className="voice-serif" style={{
          fontSize: 40, fontWeight: 400, color: 'var(--obs-fg)', margin: 0, lineHeight: 1.1,
        }}>
          Composition of Law
        </h1>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginTop: 8 }}>
          Draft v4 · supersedes v3 · 214 in-flight grandfathered
        </div>
      </div>

      {/* Authority banner — where authority lives, legible in one second */}
      <div style={{ maxWidth: 1080, margin: '0 auto 28px' }}>
        <div className="cham-sm" style={{
          position: 'relative', background: 'rgba(177,106,60,0.06)',
          border: '1px solid rgba(177,106,60,0.3)', padding: '13px 18px 13px 24px',
          display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 16, flexWrap: 'wrap',
        }}>
          <span style={{ position: 'absolute', left: 0, top: 6, bottom: 6, width: 3, background: COPPER }} />
          <div style={{ display: 'flex', alignItems: 'center', gap: 13 }}>
            <span style={{ width: 12, height: 12, transform: 'rotate(45deg)', background: COPPER, boxShadow: '0 0 0 3px rgba(177,106,60,0.2)', flexShrink: 0 }} />
            <div>
              <div className="voice-mono" style={{ color: COPPER, letterSpacing: '0.16em' }}>AWAITING AUTHORITY</div>
              <div className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9, marginTop: 3 }}>
                LEAVE-STD-01 v4 · publication makes it law · §IV
              </div>
            </div>
          </div>
          <span className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9 }}>214 in-flight grandfathered · GF-02</span>
        </div>
      </div>

      <div style={{ maxWidth: 1080, margin: '0 auto', display: 'grid', gridTemplateColumns: '164px 1fr', gap: 48 }}>
        {/* Spine */}
        <SpineRail nodes={SPINE_NODES} active={activeIdx} />

        {/* Content */}
        <div style={{ minWidth: 0 }}>

          {/* Signals */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12, marginBottom: 40 }}>
            {SIGNALS.map((sig) => <SignalStrip key={sig.id} sig={sig} />)}
          </div>

          {/* Chapter I: Composition */}
          <section id="composition" style={{ scrollMarginTop: 96 }}>
            <ConstitutionalArticle title="I · Composition" code="WF-LAW-01">
              A workflow is not a flowchart. It is institutional law in the act of being written.
              Each stage carries a settledness the eye reads before the label: an entry is broad and
              unformed, an approval narrows it, a terminal is a compact, sealed crystal — law enacted.
              Authority accumulates downward. This field, not the table, is the artifact.
            </ConstitutionalArticle>

            {/* The law field dominates; the index is supporting evidence */}
            <div className="wf-field-grid" style={{
              display: 'grid', gridTemplateColumns: 'minmax(0,1.7fr) minmax(212px,1fr)',
              gap: 20, marginTop: 24, alignItems: 'start',
            }}>
              <Surface pad="0px" style={{ overflow: 'hidden', border: '1.5px solid rgba(var(--obs-line),0.22)' }}>
                <CompositionField activeId={activeStage} onStage={setActiveStage} />
              </Surface>

              <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginBottom: 2 }}>STAGE INDEX · 6</div>
                {STAGES.map((s) => (
                  <StageRow key={s.id} stage={s} active={activeStage === s.id} onSelect={setActiveStage} />
                ))}
              </div>
            </div>

            {activeStage && (() => {
              const s = STAGES.find((s) => s.id === activeStage)
              return s ? <StageDetail stage={s} /> : null
            })()}

            {/* Crystallinity legend */}
            <div style={{ display: 'flex', gap: 28, marginTop: 20, flexWrap: 'wrap', alignItems: 'center' }}>
              {([
                { c: 0.08, label: 'Unformed — draft, open' },
                { c: 0.5,  label: 'Forming — under review' },
                { c: 1.0,  label: 'Enacted — sealed law' },
              ] as const).map((item) => (
                <div key={item.label} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <LegendCrystal c={item.c} />
                  <span className="voice-mono" style={{ color: 'var(--obs-mute)' }}>{item.label}</span>
                </div>
              ))}
            </div>
          </section>

          {/* Chapter II: Progression */}
          <section id="progression" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="II · Progression" code="WF-LAW-02">
              Law does not appear enacted. It crystallises through states, and each state binds
              more than the last. A draft is open to anyone; an approved definition is constrained;
              a published one is settled and governs every new instance. The progression is
              irreversible — amendment requires a new version, never an edit in place.
            </ConstitutionalArticle>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 12, marginTop: 24 }}>
              {([
                { state: 'DRAFT',     c: 0.10, gloss: 'Open. Broad. Exploratory. Binds nothing.' },
                { state: 'APPROVED',  c: 0.55, gloss: 'Reviewed. Constrained. Awaiting authority to enact.' },
                { state: 'PUBLISHED', c: 1.00, gloss: 'Settled. Crystallised. Governs every new request.' },
              ] as const).map((p) => {
                const sealed = p.c >= 0.6
                const color = p.c >= 0.99 ? COPPER : sealed ? COPPER : MUTE
                return (
                  <Surface key={p.state} style={{ display: 'flex', gap: 20, alignItems: 'center' }}>
                    <LegendCrystal c={p.c} />
                    <div style={{ width: 110, flexShrink: 0 }}>
                      <div className="voice-mono" style={{ color, letterSpacing: '0.14em' }}>{p.state}</div>
                      <div className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 8.5, marginTop: 3 }}>
                        crystallinity {Math.round(p.c * 100)}%
                      </div>
                    </div>
                    <p className="voice-serif" style={{ color: 'var(--obs-text-2)', fontSize: 14, margin: 0, lineHeight: 1.55 }}>
                      {p.gloss}
                    </p>
                  </Surface>
                )
              })}
            </div>
          </section>

          {/* Chapter III: Contest */}
          <section id="contest" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="III · Contest" code="WF-COND-01">
              Where the law branches, it is least settled. The condition node never crystallises —
              it is the open question the institution has not resolved. One branch routes to HR
              review, the other notifies directly. The danger is not the branch itself but the
              clause it leaves unwritten: what governs when the branch breaches its SLA.
            </ConstitutionalArticle>

            <div style={{ marginTop: 24 }}>
              <RiskSurface risk={FALLBACK_RISK} />
            </div>
          </section>

          {/* Chapter IV: Authority */}
          <section id="authority" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="IV · Authority" code="WF-PUBLISH-01">
              Publication is the act that turns a draft into law. It is irreversible: the moment v4
              is published it governs every new leave request, and the 214 instances already in
              flight complete under v3 — no instance changes rules mid-journey (GF-02). This is the
              one consequential act on this surface. It is held, not clicked.
            </ConstitutionalArticle>

            <div style={{ marginTop: 24 }}>
              <AuthoritySurface
                run="LEAVE-STD-01 v4"
                authorityCode="WF-PUBLISH-01"
                buttonLabel="Publish Workflow"
                publishedLabel="Workflow authority"
                fields={[
                  { k: 'Becomes Law', v: 'v4', sub: 'new leave requests' },
                  { k: 'In-Flight', v: '214', sub: 'complete under v3 · GF-02' },
                  { k: 'Supersedes', v: 'v3', sub: 'active since 2025-11-02' },
                ]}
              />
            </div>
          </section>

          {/* Chapter V: Record */}
          <section id="record" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="V · Record" code="WF-LEDGER-01">
              Every version is law that once governed, and the record keeps them all. Versions are
              appended, never overwritten — the institution can always read which law ruled on any
              given day. This is the lineage of the workflow, immutable and in order.
            </ConstitutionalArticle>

            <div style={{ marginTop: 24 }}>
              {RECORD.map((entry, i) => (
                <div key={i} style={{
                  display: 'grid', gridTemplateColumns: '52px 110px 1fr 120px 120px',
                  gap: 12, padding: '10px 0',
                  borderBottom: '1px solid rgba(var(--obs-line),0.07)',
                  alignItems: 'baseline',
                }}>
                  <Code style={{ color: entry.event === 'DRAFTED' ? 'var(--obs-mute)' : 'var(--obs-copper)' }}>{entry.ver}</Code>
                  <Code style={{ color: 'var(--obs-mute)' }}>{entry.date}</Code>
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
                { k: 'SURFACE',  v: 'WorkflowStudio' },
                { k: 'PERSONA',  v: 'SYS' },
                { k: 'ENGINE',   v: 'PRAVAHA v3' },
                { k: 'ARTIFACT', v: 'LEAVE-STD-01 · draft v4' },
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
