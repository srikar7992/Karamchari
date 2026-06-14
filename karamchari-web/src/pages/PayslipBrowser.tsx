import {
  Surface, Label, Code,
  ConstitutionalArticle, SpineRail, useScrollSpy,
  SignalStrip,
  type Signal,
} from '@/components/observatory/surfaces'
import type { PageClassification } from '@/lib/doctrine'

// Payslip is not a document. It is the institution acknowledging that an
// obligation has been fulfilled — the Settlement outcome of the
// Attendance → Payroll → Payslip lifecycle. Read-only: the authority act
// already happened upstream (PAY-AUTH-04), so there is no copper action here.
export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EMP',
  archetype: 'record-detail',
  signals: 3,
  risks: 0,
  narratives: 1,
  maps: 0,
  bindu: false,
}

// ---- Data -------------------------------------------------------------------

const SPINE_IDS = ['settlement', 'earnings', 'deductions', 'authority', 'lineage'] as const
const SPINE_NODES = [
  { id: 'settlement', label: 'I · SETTLEMENT' },
  { id: 'earnings',   label: 'II · EARNINGS' },
  { id: 'deductions', label: 'III · DEDUCTIONS' },
  { id: 'authority',  label: 'IV · AUTHORITY' },
  { id: 'lineage',    label: 'V · LINEAGE' },
]

const SIGNALS: Signal[] = [
  { id: 's1', label: 'Net Settled',   value: '₹1,42,180', unit: '', delta: 'disbursed', dir: 'up',   spark: [138,139,140,141,141,142] },
  { id: 's2', label: 'Gross Earnings', value: '₹1,86,000', unit: '', delta: '+0 m/m',    dir: 'up',   spark: [186,186,186,186,186,186] },
  { id: 's3', label: 'Deductions',    value: '₹43,820',   unit: '', delta: 'statutory',  dir: 'down', spark: [42,43,43,44,44,44] },
]

interface Line { component: string; ref: string; amount: string }

const EARNINGS: Line[] = [
  { component: 'Basic',              ref: 'PAY-BASIC',  amount: '₹93,000' },
  { component: 'House Rent Allow.',  ref: 'PAY-HRA',    amount: '₹46,500' },
  { component: 'Special Allowance',  ref: 'PAY-SPL',    amount: '₹38,200' },
  { component: 'Shift Differential', ref: 'PAY-SHIFT',  amount: '₹5,300'  },
  { component: 'Attendance Incentive', ref: 'PAY-ATT-INC', amount: '₹3,000' },
]

const DEDUCTIONS: Line[] = [
  { component: 'Provident Fund',     ref: 'STAT-PF',    amount: '₹11,160' },
  { component: 'Professional Tax',   ref: 'STAT-PT',    amount: '₹200'    },
  { component: 'Income Tax (TDS)',   ref: 'STAT-TDS',   amount: '₹29,460' },
  { component: 'Health Premium',     ref: 'BEN-HEALTH', amount: '₹3,000'  },
]

interface AuthorityField { k: string; v: string }
const AUTHORITY: AuthorityField[] = [
  { k: 'RUN',        v: 'RUN-2026-06' },
  { k: 'AUTHORITY',  v: 'PAY-AUTH-04' },
  { k: 'WORKFLOW',   v: 'WF-V4' },
  { k: 'PUBLISHED',  v: '2026-06-14 · 14:31 IST' },
  { k: 'PUBLISHED BY', v: 'Ananya R. · L7' },
  { k: 'DISBURSAL',  v: 'NEFT · HDFC ••4821' },
]

interface Stage { id: string; label: string; detail: string; ref: string }
const LINEAGE: Stage[] = [
  { id: 'attendance', label: 'Attendance Cycle',  detail: '21 working days recorded · zero unresolved exceptions', ref: 'SRF-OPS-031' },
  { id: 'validated',  label: 'Validated Payroll', detail: 'Period closed · variances cleared by period close',     ref: 'PERIOD-CLOSE' },
  { id: 'published',  label: 'Published Run',     detail: 'RUN-2026-06 authorised and committed to ledger',         ref: 'PAY-AUTH-04' },
  { id: 'settlement', label: 'Settlement',        detail: 'Obligation fulfilled · disbursed to operator',           ref: 'SETTLED' },
]

// ---- Helpers ----------------------------------------------------------------

function LineTable({ lines, total, totalLabel }: { lines: Line[]; total: string; totalLabel: string }) {
  return (
    <Surface pad="0px" style={{ marginTop: 24, overflow: 'hidden' }}>
      <div style={{
        display: 'grid', gridTemplateColumns: '1fr 120px 120px', gap: 12, padding: '10px 20px',
        borderBottom: '1px solid var(--obs-hair)',
      }}>
        {['COMPONENT', 'REF', 'AMOUNT'].map((h, i) => (
          <div key={h} className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9, textAlign: i === 2 ? 'right' : 'left' }}>{h}</div>
        ))}
      </div>
      {lines.map((l) => (
        <div key={l.component} style={{
          display: 'grid', gridTemplateColumns: '1fr 120px 120px', gap: 12, padding: '11px 20px',
          borderBottom: '1px solid var(--obs-hair)', alignItems: 'baseline',
        }}>
          <span className="voice-mono" style={{ color: 'var(--obs-fg)', textTransform: 'none' as const }}>{l.component}</span>
          <Code style={{ color: 'var(--obs-mute)' }}>{l.ref}</Code>
          <span className="tnum" style={{ textAlign: 'right', color: 'var(--obs-fg)', fontSize: 13 }}>{l.amount}</span>
        </div>
      ))}
      <div style={{
        display: 'grid', gridTemplateColumns: '1fr 120px', gap: 12, padding: '12px 20px',
        background: 'var(--obs-sunken)',
      }}>
        <span className="voice-mono" style={{ color: 'var(--obs-fg)' }}>{totalLabel}</span>
        <span className="tnum" style={{ textAlign: 'right', color: 'var(--obs-fg)', fontSize: 15, fontWeight: 600 }}>{total}</span>
      </div>
    </Surface>
  )
}

// ---- Page -------------------------------------------------------------------

export function PayslipBrowser() {
  const activeIdx = useScrollSpy([...SPINE_IDS])

  return (
    <div className="obs obs-canvas" style={{ minHeight: '100vh', padding: '32px 24px 80px' }}>
      {/* Header */}
      <div style={{ maxWidth: 960, margin: '0 auto 40px' }}>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginBottom: 6 }}>
          PAYSLIP · SETTLEMENT · JUNE 2026
        </div>
        <h1 className="voice-serif" style={{
          fontSize: 40, fontWeight: 400, color: 'var(--obs-fg)', margin: 0, lineHeight: 1.1,
        }}>
          Payroll Settlement
        </h1>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginTop: 8 }}>
          EMP-88271 · settled under PAY-AUTH-04 · obligation fulfilled
        </div>
      </div>

      <div style={{ maxWidth: 960, margin: '0 auto', display: 'grid', gridTemplateColumns: '164px 1fr', gap: 48 }}>
        <SpineRail nodes={SPINE_NODES} active={activeIdx} />

        <div style={{ minWidth: 0 }}>
          {/* Signals */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12, marginBottom: 40 }}>
            {SIGNALS.map((sig) => <SignalStrip key={sig.id} sig={sig} />)}
          </div>

          {/* Chapter I: Settlement */}
          <section id="settlement" style={{ scrollMarginTop: 96 }}>
            <ConstitutionalArticle title="I · Settlement" code="PAY-SETTLE-01">
              A payslip is not a document the institution hands over. It is the institution
              acknowledging that an obligation it incurred — a month of recorded labour — has
              been met in full. The numbers are evidence; the settlement is the act. This page
              records that the obligation to EMP-88271 for June 2026 is discharged.
            </ConstitutionalArticle>

            {/* Settlement notice — sealed, ink, immutable acknowledgement */}
            <Surface variant="ink" pad="24px 28px" style={{ marginTop: 24, position: 'relative', overflow: 'hidden' }}>
              <span style={{ position: 'absolute', left: 0, top: 16, bottom: 16, width: 3, background: 'var(--obs-copper)' }} />
              <Label color="var(--obs-copper-soft)" style={{ letterSpacing: '0.2em', marginBottom: 14 }}>Settlement completed</Label>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 20 }}>
                {[
                  { k: 'Period', v: 'June 2026' },
                  { k: 'Settled For', v: 'EMP-88271' },
                  { k: 'Published Under', v: 'PAY-AUTH-04' },
                ].map((f) => (
                  <div key={f.k}>
                    <div className="voice-mono" style={{ fontSize: 9.5, color: 'rgba(242,237,227,0.46)', marginBottom: 6 }}>{f.k}</div>
                    <div className="voice-code" style={{ fontSize: 15, color: '#f2ede3' }}>{f.v}</div>
                  </div>
                ))}
              </div>
              <div className="voice-mono" style={{ marginTop: 18, paddingTop: 14, borderTop: '1px solid rgba(242,237,227,0.13)', color: 'rgba(242,237,227,0.7)', fontSize: 10 }}>
                STATUS · SETTLED · committed to ledger · immutable
              </div>
            </Surface>
          </section>

          {/* Chapter II: Earnings */}
          <section id="earnings" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="II · Earnings" code="PAY-EARN-01">
              What the institution committed to pay, by component. Each line cites the pay element
              that governs it — earnings are not arbitrary figures but the resolution of standing
              compensation rules against this period's recorded service.
            </ConstitutionalArticle>
            <LineTable lines={EARNINGS} total="₹1,86,000" totalLabel="GROSS EARNINGS" />
          </section>

          {/* Chapter III: Deductions */}
          <section id="deductions" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="III · Deductions" code="PAY-DED-01">
              What the institution withheld on the operator's behalf — statutory obligations and
              elected benefits. Every deduction names its statute or scheme; nothing is withheld
              without a citation.
            </ConstitutionalArticle>
            <LineTable lines={DEDUCTIONS} total="₹43,820" totalLabel="TOTAL DEDUCTIONS" />
          </section>

          {/* Chapter IV: Authority */}
          <section id="authority" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="IV · Authority" code="PAY-AUTH-04">
              Every payroll product shows the money. Few show the authority that produced it. This
              settlement exists because a specific run was authorised by a specific officer under a
              specific workflow version. That provenance is the institution's memory, and it is
              immutable.
            </ConstitutionalArticle>

            <Surface pad="0px" style={{ marginTop: 24, overflow: 'hidden' }}>
              {AUTHORITY.map((f, i) => (
                <div key={f.k} style={{
                  display: 'grid', gridTemplateColumns: '160px 1fr', gap: 16, padding: '12px 20px',
                  borderBottom: i < AUTHORITY.length - 1 ? '1px solid var(--obs-hair)' : 'none',
                  alignItems: 'baseline',
                }}>
                  <div className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9.5 }}>{f.k}</div>
                  <Code style={{ color: f.k === 'AUTHORITY' || f.k === 'RUN' ? 'var(--obs-copper)' : 'var(--obs-fg)' }}>{f.v}</Code>
                </div>
              ))}
            </Surface>
          </section>

          {/* Chapter V: Lineage */}
          <section id="lineage" style={{ scrollMarginTop: 96, marginTop: 64 }}>
            <ConstitutionalArticle title="V · Lineage" code="PAY-LINEAGE-01">
              This settlement is the end of a chain, not an isolated event. It can be traced back
              through the run that produced it, the period close that validated it, and the
              attendance cycle that began it. The chain is the proof: remove the amounts and the
              lineage still explains why this payslip exists.
            </ConstitutionalArticle>

            {/* The lifecycle chain — Attendance → Validated → Published → Settlement */}
            <div style={{ marginTop: 24, position: 'relative', paddingLeft: 8 }}>
              <div style={{ position: 'absolute', left: 13, top: 10, bottom: 10, width: 1, background: 'var(--obs-hair-2)' }} />
              {LINEAGE.map((s, i) => {
                const terminal = i === LINEAGE.length - 1
                return (
                  <div key={s.id} style={{ position: 'relative', display: 'flex', gap: 20, alignItems: 'flex-start', paddingBottom: i < LINEAGE.length - 1 ? 22 : 0 }}>
                    <span style={{
                      width: 11, height: 11, marginTop: 3, flexShrink: 0, transform: 'rotate(45deg)',
                      background: terminal ? 'var(--obs-copper)' : 'var(--obs-surf)',
                      border: `1.5px solid ${terminal ? 'var(--obs-copper)' : 'var(--obs-hair-strong)'}`,
                      boxShadow: terminal ? '0 0 0 4px rgba(177,106,60,0.2)' : undefined,
                      zIndex: 1,
                    }} />
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ display: 'flex', gap: 12, alignItems: 'baseline', flexWrap: 'wrap' }}>
                        <span className="voice-serif" style={{ fontSize: 17, color: 'var(--obs-fg)' }}>{s.label}</span>
                        <Code style={{ color: terminal ? 'var(--obs-copper)' : 'var(--obs-mute)' }}>{s.ref}</Code>
                      </div>
                      <div className="voice-mono" style={{ color: 'var(--obs-text-2)', marginTop: 4, textTransform: 'none' as const, letterSpacing: 0 }}>
                        {s.detail}
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>

            {/* Ledger metadata */}
            <div style={{
              display: 'flex', gap: 32, flexWrap: 'wrap', marginTop: 40,
              paddingTop: 16, borderTop: '1px solid var(--obs-hair)',
            }}>
              {[
                { k: 'SURFACE',    v: 'PayslipSettlement' },
                { k: 'PERSONA',    v: 'EMP' },
                { k: 'RUN',        v: 'RUN-2026-06' },
                { k: 'PROJECTION', v: 'SettlementProjection v3' },
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
