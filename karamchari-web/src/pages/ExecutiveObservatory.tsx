import { useState } from 'react'
import {
  Surface, Label, Code, SpineRail, useScrollSpy, ConstitutionalArticle,
  SignalStrip, RiskSurface, AuthoritySurface, nowIST,
  type Signal, type Risk,
} from '@/components/observatory/surfaces'
import type { PageClassification } from '@/lib/doctrine'

// First production surface in the ui3 institutional language (Wave 1).
// Assessment-first Observatory: reasoning leads, signals support. Every surface
// is one information class — no generic cards. Authority is a sealed ceremony,
// not a button. Identifiers (RUN/PAY-AUTH/ZN) are mono visual anchors.
export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EXEC',
  archetype: 'dashboard',
  signals: 3,
  risks: 3,
  narratives: 1,
  maps: 1,
  bindu: true,
}

const SIGNALS: Signal[] = [
  { id: 'SIG-ATT', label: 'Attendance', value: '96.4', unit: '%', delta: '+0.7 pts', dir: 'up', spark: [94.1, 94.8, 95.2, 95.0, 95.6, 96.1, 96.4] },
  { id: 'SIG-PAY', label: 'Payroll Cost', value: '4.21', unit: '₹ Cr', delta: '+2.1%', dir: 'up', spark: [3.92, 3.98, 4.02, 4.05, 4.11, 4.18, 4.21] },
  { id: 'SIG-OVT', label: 'Overtime Load', value: '7.8', unit: '%', delta: '-1.2 pts', dir: 'down', spark: [10.1, 9.6, 9.2, 8.8, 8.4, 8.1, 7.8] },
]

const RISKS: Risk[] = [
  { id: 'RSK-01', level: 'elevated', title: 'Contractor coverage concentration', detail: '41% contractor dependency in eastern logistics. A single vendor exit would expose 1.8% of total coverage.', exposure: '1,842 people', horizon: 'Q3 FY27', owner: 'T. Venkatesh' },
  { id: 'RSK-02', level: 'watch', title: 'Statutory filing window', detail: 'PF & ESI reconciliation for RUN-2026-10 due before authority publication. 3 working days remain.', exposure: '₹38.4 L', horizon: '16 Jun 2026', owner: 'Compliance Office' },
  { id: 'RSK-03', level: 'watch', title: 'Succession depth — plant operations', detail: 'Two of five plant-head roles carry no ready successor. Concentration risk on retirement horizon.', exposure: '2 roles', horizon: 'FY28', owner: 'People Council' },
]

interface Zone { id: string; name: string; hub: string; people: number; coverage: number; risk: 'low' | 'watch' | 'elevated'; contractorPct: number; trend: number }
const ZONES: Zone[] = [
  { id: 'ZN-N', name: 'North Cluster', hub: 'Delhi NCR · Ludhiana', people: 3120, coverage: 97.2, risk: 'low', contractorPct: 18, trend: 0.6 },
  { id: 'ZN-W', name: 'West Cluster', hub: 'Pune · Mumbai · Surat', people: 4180, coverage: 95.8, risk: 'low', contractorPct: 22, trend: 0.3 },
  { id: 'ZN-S', name: 'South Cluster', hub: 'Bengaluru · Chennai', people: 3892, coverage: 96.9, risk: 'low', contractorPct: 20, trend: 0.9 },
  { id: 'ZN-E', name: 'East Cluster', hub: 'Kolkata · Guwahati', people: 1842, coverage: 91.4, risk: 'elevated', contractorPct: 41, trend: 1.8 },
  { id: 'ZN-C', name: 'Central Cluster', hub: 'Nagpur · Indore · Raipur', people: 1468, coverage: 94.1, risk: 'watch', contractorPct: 33, trend: -0.4 },
]

interface LedgerEntry { run: string; state: 'PENDING' | 'APPROVED' | 'AMENDED'; ts: string; actor: string; impact: string; amount: string }
const SEED_LEDGER: LedgerEntry[] = [
  { run: 'RUN-2026-10', state: 'PENDING', ts: '—', actor: '—', impact: '11,240 on-roll', amount: '₹4.21 Cr' },
  { run: 'RUN-2026-09', state: 'APPROVED', ts: '14:31 IST', actor: 'ANANYA R.', impact: '11,196 on-roll', amount: '₹4.12 Cr' },
  { run: 'RUN-2026-08', state: 'APPROVED', ts: '11:08 IST', actor: 'ANANYA R.', impact: '11,142 on-roll', amount: '₹4.05 Cr' },
  { run: 'RUN-2026-07', state: 'APPROVED', ts: '16:52 IST', actor: 'D. MEHRA', impact: '11,090 on-roll', amount: '₹3.98 Cr' },
]

const RISK_TONE: Record<Zone['risk'], string> = { low: 'var(--obs-forest)', watch: 'var(--obs-risk)', elevated: 'var(--obs-risk-hi)' }

function ScreenHead() {
  return (
    <div data-emerge="" style={{ marginBottom: 32 }}>
      <Label color="var(--obs-copper)" style={{ marginBottom: 14 }}>Daily · Pulse</Label>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 24, flexWrap: 'wrap' }}>
        <h1 className="voice-serif" style={{ fontSize: 46, lineHeight: 1.04, letterSpacing: '-0.02em', fontWeight: 400, margin: 0, maxWidth: 620 }}>Institutional Observatory</h1>
        <Code color="var(--obs-mute)" style={{ fontSize: 10.5 }}>AS OF 13 JUN 2026 · 14:31 IST</Code>
      </div>
      <p style={{ fontSize: 15, color: 'var(--obs-text-2)', marginTop: 14, maxWidth: 560, lineHeight: 1.55 }}>
        A standing view of the institution as a living system — observable truth, exposure, reasoning and structure.
      </p>
    </div>
  )
}

function ZoneTopology({ zones, selected, onSelect }: { zones: Zone[]; selected: string; onSelect: (id: string) => void }) {
  const total = zones.reduce((a, z) => a + z.people, 0)
  const sel = zones.find((z) => z.id === selected) ?? zones[0]
  return (
    <Surface pad="24px" emergeDelay={120}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <div style={{ display: 'flex', alignItems: 'baseline', gap: 12 }}>
          <Label>Coverage Topology</Label>
          <span className="tnum" style={{ fontSize: 12.5, color: 'var(--obs-mute)' }}>5 clusters · 96.4% overall</span>
        </div>
        <Code color="var(--obs-mute)">ZN-MAP</Code>
      </div>
      {/* territory strip — width carries headcount, copper edge marks selection, tone carries risk */}
      <div style={{ display: 'flex', gap: 2, height: 92 }}>
        {zones.map((z) => {
          const on = z.id === selected
          return (
            <button
              key={z.id}
              onClick={() => onSelect(z.id)}
              className="cham-sm obs-interactive"
              style={{
                flex: z.people / total, position: 'relative', border: 'none', cursor: 'pointer', textAlign: 'left',
                background: on ? 'var(--obs-sunken)' : 'var(--obs-surf)', boxShadow: `inset 0 -3px 0 ${RISK_TONE[z.risk]}`, padding: '10px 12px',
                outline: on ? '1.5px solid var(--obs-copper)' : '1px solid var(--obs-hair)',
              }}
            >
              <div className="voice-code" style={{ fontSize: 10, color: on ? 'var(--obs-copper)' : 'var(--obs-mute)' }}>{z.id}</div>
              <div className="tnum" style={{ fontSize: 18, fontWeight: 600, letterSpacing: '-0.02em', marginTop: 6 }}>{z.coverage}<span style={{ fontSize: 10, color: 'var(--obs-mute)' }}>%</span></div>
            </button>
          )
        })}
      </div>
      {/* selected territory detail */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 16, marginTop: 16, paddingTop: 16, borderTop: '1px solid var(--obs-hair)' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ width: 6, height: 6, background: RISK_TONE[sel.risk], transform: 'rotate(45deg)', display: 'inline-block' }} />
            <span className="voice-serif" style={{ fontSize: 20, letterSpacing: '-0.01em' }}>{sel.name}</span>
          </div>
          <div style={{ fontSize: 12.5, color: 'var(--obs-mute)', marginTop: 4 }}>{sel.hub}</div>
        </div>
        <div style={{ display: 'flex', gap: 28 }}>
          <ZMeta k="Workforce" v={sel.people.toLocaleString('en-IN')} />
          <ZMeta k="Contractor" v={`${sel.contractorPct}%`} />
          <ZMeta k="Trend" v={`${sel.trend > 0 ? '+' : ''}${sel.trend} pts`} />
        </div>
      </div>
    </Surface>
  )
}
function ZMeta({ k, v }: { k: string; v: string }) {
  return (
    <div style={{ textAlign: 'right' }}>
      <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)', marginBottom: 4 }}>{k}</div>
      <div className="tnum" style={{ fontSize: 15, fontWeight: 600 }}>{v}</div>
    </div>
  )
}

function LedgerSurface({ rows }: { rows: LedgerEntry[] }) {
  const stateColor: Record<string, string> = { APPROVED: 'var(--obs-forest)', PENDING: 'var(--obs-copper)', AMENDED: 'var(--obs-mute)' }
  return (
    <Surface pad="24px" emergeDelay={200}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Label>Institutional Ledger</Label>
        <Code color="var(--obs-mute)">Audit memory</Code>
      </div>
      <div className="voice-mono" style={{ display: 'grid', gridTemplateColumns: '108px 110px minmax(0,1fr) 84px', gap: 14, color: 'var(--obs-mute)', paddingBottom: 10, borderBottom: '1px solid var(--obs-hair)' }}>
        <div>Run</div><div>State</div><div>Authority</div><div style={{ textAlign: 'right' }}>Amount</div>
      </div>
      {rows.map((r) => (
        <div key={r.run} className="obs-interactive" style={{ display: 'grid', gridTemplateColumns: '108px 110px minmax(0,1fr) 84px', gap: 14, alignItems: 'center', padding: '11px 0', borderBottom: '1px solid var(--obs-hair)' }}>
          <Code color="var(--obs-fg)">{r.run}</Code>
          <div style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
            {r.state === 'PENDING' && <span className="obs-pulse" style={{ width: 6, height: 6 }} />}
            <span className="voice-mono" style={{ color: stateColor[r.state], letterSpacing: '0.1em' }}>{r.state}</span>
          </div>
          <span className="voice-code" style={{ fontSize: 11, color: 'var(--obs-text-2)' }}>{r.actor}{r.ts !== '—' ? ` · ${r.ts}` : ''}</span>
          <span className="voice-code tnum" style={{ fontSize: 11.5, textAlign: 'right' }}>{r.amount}</span>
        </div>
      ))}
    </Surface>
  )
}

export function ExecutiveObservatory() {
  const [sel, setSel] = useState('ZN-E')
  const [ledger, setLedger] = useState<LedgerEntry[]>(SEED_LEDGER)
  const spyIds = ['obs-read', 'obs-maps', 'obs-auth', 'obs-signals']
  const active = useScrollSpy(spyIds)

  const onPublished = () => {
    setLedger((l) => [{ ...l[0], state: 'APPROVED', ts: nowIST(), actor: 'ANANYA R.' }, ...l.slice(1)])
  }

  return (
    <div className="obs">
      <div className="obs-canvas" style={{ padding: '4px 4px 48px' }}>
        <ScreenHead />
        <div className="obs-shell" style={{ display: 'grid', gridTemplateColumns: '148px minmax(0,1fr)', gap: 32, alignItems: 'start' }}>
          <SpineRail active={active} nodes={[
            { id: 'obs-read', label: 'Insight' },
            { id: 'obs-maps', label: 'Topology' },
            { id: 'obs-auth', label: 'Authority' },
            { id: 'obs-signals', label: 'Signals' },
          ]} />
          <div style={{ display: 'flex', flexDirection: 'column', gap: 48, minWidth: 0 }}>
            {/* reasoning leads — numbers come later, in support */}
            <div id="obs-read" style={{ scrollMarginTop: 96 }}>
              <ConstitutionalArticle title="Executive Reading" code="NAR-EXE-10" lead emergeDelay={0}
                attribution="Synthesised across 5 clusters · People Council · 13 Jun">
                The institution is operating within tolerance. Attendance has held above the ninety-six
                line for the first time this quarter, carried by the eastern clusters after shift
                redesign. The single live obligation is constitutional: the current run must clear
                statutory pre-check before payroll authority may be exercised.
              </ConstitutionalArticle>
            </div>

            {/* golden-ratio split: evidence (topology + ledger) beside authority + exposure */}
            <div style={{ display: 'grid', gridTemplateColumns: 'minmax(0,1.618fr) minmax(0,1fr)', gap: 24, alignItems: 'start' }} className="obs-split">
              <div id="obs-maps" style={{ display: 'flex', flexDirection: 'column', gap: 16, scrollMarginTop: 96 }}>
                <ZoneTopology zones={ZONES} selected={sel} onSelect={setSel} />
                <LedgerSurface rows={ledger} />
              </div>
              <div id="obs-auth" style={{ display: 'flex', flexDirection: 'column', gap: 16, scrollMarginTop: 96 }}>
                <AuthoritySurface run="RUN-2026-10" onPublished={onPublished} />
                {RISKS.map((r, i) => <RiskSurface key={r.id} risk={r} emergeDelay={160 + i * 40} />)}
              </div>
            </div>

            {/* signals — supporting strip, never the headline */}
            <div id="obs-signals" style={{ scrollMarginTop: 96 }}>
              <Label style={{ marginBottom: 16 }}>Standing signals</Label>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,minmax(0,1fr))', gap: 16 }} className="obs-signals-grid">
                {SIGNALS.map((s, i) => <SignalStrip key={s.id} sig={s} emergeDelay={i * 50} />)}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
