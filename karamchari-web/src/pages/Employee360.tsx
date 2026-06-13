import { useState } from 'react'
import {
  Surface, Label, Code,
  ConstitutionalArticle, SpineRail, useScrollSpy,
  SignalStrip, RiskSurface, AuthoritySurface,
  type Signal, type Risk,
} from '@/components/observatory/surfaces'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'record-detail',
  signals: 4,
  risks: 1,
  narratives: 1,
  maps: 0,
  bindu: true,
}

// ---- Data -------------------------------------------------------------------

type HistTone = 'neutral' | 'copper' | 'forest'
const TONE_COLOR: Record<HistTone, string> = {
  neutral: 'var(--obs-mute)',
  copper:  'var(--obs-copper)',
  forest:  'var(--obs-forest)',
}

const SIGNALS: Signal[] = [
  { id: 's-tenure', label: 'Tenure',          value: '4.2',  unit: 'yrs',  ref: 'Current role since Mar 2024', delta: '+0.3',  dir: 'up',   spark: [3.1, 3.4, 3.7, 3.9, 4.0, 4.2] },
  { id: 's-compa',  label: 'Compa-Ratio',      value: '1.12',               ref: 'vs Tier 4 band median',       delta: '+0.04', dir: 'up',   spark: [1.01, 1.04, 1.07, 1.09, 1.11, 1.12] },
  { id: 's-att',    label: 'Attendance Score', value: '98.4', unit: '%',                                        delta: '+1.2%', dir: 'up',   spark: [94, 96, 97, 96.8, 97.5, 98.4] },
  { id: 's-leave',  label: 'Leave Balance',    value: '14',   unit: 'days', ref: '3 expire 31 Dec',             delta: '-3d',   dir: 'down', spark: [22, 20, 18, 17, 15, 14] },
]

const FLIGHT_RISK: Risk = {
  id: 'RET-EMP-001',
  level: 'elevated',
  title: 'Flight Risk — Elevated',
  detail: '4.2 years tenure in current role. Compa-ratio healthy, but peer market velocity for Sr. Architects indicates elevated departure probability. Top drivers: role tenure (0.41), market demand (0.33), manager span (0.12).',
  exposure: '78th percentile',
  horizon: '6–9 months',
  owner: 'M. Vance',
}

const SKILLS = [
  { name: 'Distributed Systems Design',   level: 5, validated: true,  law: 'EMP-CAP-03' },
  { name: 'Cloud Infrastructure (AWS/GCP)', level: 4, validated: true, law: 'EMP-CAP-03' },
  { name: 'Technical Leadership',          level: 3, validated: false, law: null },
  { name: 'Go / Rust Engineering',         level: 4, validated: true,  law: 'EMP-CAP-03' },
]

const HISTORY: Array<{ ts: string; tag: string; tone: HistTone; law: string; body: string }> = [
  { ts: '2026-04-02', tag: 'COMP_REV',   tone: 'neutral', law: 'COMP-REV-11',    body: 'Annual compensation revision applied. Base CHF 178,000 → CHF 185,000. Approved by M. Vance.' },
  { ts: '2026-01-19', tag: 'RECOG',      tone: 'copper',  law: 'RECOG-AWARD-04', body: 'Q1 Infrastructure Excellence Award conferred by CTO for the database migration protocol.' },
  { ts: '2025-08-11', tag: 'SKILL_VAL',  tone: 'forest',  law: 'EMP-CAP-03',     body: 'Skill validated: Distributed Systems Design, level 5. Evidence: migration design review board.' },
  { ts: '2024-03-01', tag: 'POS_ASSIGN', tone: 'neutral', law: 'POS-ASSIGN-01',  body: 'Assigned: Senior Technical Architect, Core Infrastructure. Reports to VP, Infrastructure Operations.' },
  { ts: '2022-02-14', tag: 'ONBOARD',    tone: 'forest',  law: 'ONBOARD-01',     body: 'Onboarding case completed in 11 days. All documents verified; assets issued.' },
]

const SPINE_NODES = [
  { id: 'identity',     label: 'I · Identity' },
  { id: 'service',      label: 'II · Service' },
  { id: 'capability',   label: 'III · Capability' },
  { id: 'recognition',  label: 'IV · Recognition' },
  { id: 'compensation', label: 'V · Compensation' },
  { id: 'authority',    label: 'VI · Authority' },
  { id: 'record',       label: 'VII · Record' },
]
const CHAPTER_IDS = SPINE_NODES.map(n => n.id)

// ---- Local helpers ----------------------------------------------------------

function IField({ label, value, code = false }: { label: string; value: string; code?: boolean }) {
  return (
    <div>
      <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)', marginBottom: 3 }}>{label}</div>
      {code
        ? <Code color="var(--obs-fg)" style={{ fontSize: 12 }}>{value}</Code>
        : <div style={{ fontSize: 13, fontWeight: 500, color: 'var(--obs-fg)' }}>{value}</div>
      }
    </div>
  )
}

function RailNode({ initials, name, role }: { initials: string; name: string; role: string }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
      <div className="cham-sm" style={{ width: 36, height: 36, background: 'var(--obs-sunken)', border: '1px solid var(--obs-hair-2)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
        <span className="voice-mono" style={{ fontSize: 9.5 }}>{initials}</span>
      </div>
      <div>
        <div style={{ fontSize: 13, fontWeight: 500, color: 'var(--obs-fg)' }}>{name}</div>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginTop: 2 }}>{role}</div>
      </div>
    </div>
  )
}

function SkillRow({ name, level, validated, law }: { name: string; level: number; validated: boolean; law: string | null }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', paddingBottom: 12, borderBottom: '1px solid var(--obs-hair)' }}>
      <div>
        <div style={{ fontSize: 13, fontWeight: 500, color: 'var(--obs-fg)' }}>{name}</div>
        {validated && law && <Code color="var(--obs-mute)">{law}</Code>}
      </div>
      <div style={{ display: 'flex', gap: 3, alignItems: 'center' }}>
        {[1, 2, 3, 4, 5].map(n => (
          <span key={n} style={{ width: 20, height: 2, background: n <= level ? 'var(--obs-copper)' : 'var(--obs-hair-2)', display: 'block' }} />
        ))}
        <span className="voice-mono" style={{ color: 'var(--obs-mute)', fontSize: 9.5, marginLeft: 6 }}>{level}/5</span>
      </div>
    </div>
  )
}

function HistEntry({ ts, tag, tone, law, body }: { ts: string; tag: string; tone: HistTone; law: string; body: string }) {
  return (
    <div style={{ display: 'grid', gridTemplateColumns: '112px 1fr', gap: 20, paddingBottom: 18, marginBottom: 18, borderBottom: '1px solid var(--obs-hair)' }}>
      <div>
        <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)' }}>{ts}</div>
        <Code color={TONE_COLOR[tone]} style={{ marginTop: 4, display: 'block' }}>{tag}</Code>
      </div>
      <div>
        <div style={{ fontSize: 13, lineHeight: 1.55, color: 'var(--obs-fg)' }}>{body}</div>
        <Code color="var(--obs-mute)" style={{ marginTop: 5, display: 'block' }}>{law}</Code>
      </div>
    </div>
  )
}

function LedgerMeta({ k, v }: { k: string; v: string }) {
  return (
    <div>
      <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)', marginBottom: 3 }}>{k}</div>
      <Code color="var(--obs-mute)">{v}</Code>
    </div>
  )
}

// ---- Page -------------------------------------------------------------------

export function Employee360() {
  const active = useScrollSpy(CHAPTER_IDS)
  const [mandatePublished, setMandatePublished] = useState(false)

  return (
    <div className="obs obs-canvas" style={{ minHeight: '100vh', padding: '32px 24px 80px' }}>

      {/* Screen head */}
      <div style={{ marginBottom: 40 }}>
        <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)', letterSpacing: '0.16em', marginBottom: 14 }}>
          EMPLOYEE DIRECTORY / EMP-8834-A / INSTITUTIONAL BIOGRAPHY
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 24, flexWrap: 'wrap' }}>
          <div>
            <div className="voice-serif" style={{ fontSize: 44, lineHeight: 1.05, letterSpacing: '-0.022em', color: 'var(--obs-fg)', fontWeight: 400 }}>
              Elena Rostova
            </div>
            <div className="voice-mono" style={{ marginTop: 8, color: 'var(--obs-mute)', letterSpacing: '0.1em' }}>
              Senior Technical Architect · Core Infrastructure · Zurich
            </div>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, paddingBottom: 4 }}>
            <span style={{ width: 7, height: 7, borderRadius: '50%', background: 'var(--obs-forest)' }} />
            <span className="voice-mono" style={{ color: 'var(--obs-forest)' }}>Active · On-roll</span>
          </div>
        </div>
      </div>

      {/* Shell: spine | biography */}
      <div className="obs-shell" style={{ display: 'grid', gridTemplateColumns: '148px minmax(0,1fr)', gap: 48 }}>
        <SpineRail nodes={SPINE_NODES} active={active} />

        <div style={{ display: 'flex', flexDirection: 'column', gap: 64 }}>

          {/* Chapter I — Identity */}
          <section id="identity">
            <ConstitutionalArticle title="Chapter I" code="EMP-ID-01" lead emergeDelay={0}>
              Elena Rostova entered the institution on 14 February 2022 as a systems architect. Four years of
              institutional contribution have compounded into the senior-most individual contributor in Core
              Infrastructure. The institution holds in her the capability anchor for distributed systems design —
              a class of knowledge that cannot be reconstructed inside a fiscal quarter.
            </ConstitutionalArticle>
            <div className="obs-split" style={{ display: 'grid', gridTemplateColumns: '1fr 1.618fr', gap: 20, marginTop: 28 }}>
              <Surface pad="28px" emergeDelay={80}>
                <Label style={{ marginBottom: 18 }}>Identity</Label>
                <div className="cham" style={{ width: 72, height: 72, background: 'var(--obs-sunken)', border: '1px solid var(--obs-hair-2)', display: 'flex', alignItems: 'center', justifyContent: 'center', marginBottom: 22 }}>
                  <span className="voice-serif" style={{ fontSize: 24, color: 'var(--obs-fg)', fontWeight: 300, letterSpacing: '-0.02em' }}>ER</span>
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                  <IField label="Internal ID" value="EMP-8834-A" code />
                  <IField label="Department" value="Core Infrastructure" />
                  <IField label="Location" value="Zurich Node (Hybrid)" />
                  <IField label="Assets Held" value="3 (Laptop, Token, Badge)" />
                  <IField label="Joined" value="14 Feb 2022" />
                </div>
              </Surface>
              <Surface pad="28px" emergeDelay={140}>
                <Label style={{ marginBottom: 18 }}>Chain of Command</Label>
                <RailNode initials="MV" name="Marcus Vance" role="VP, Infrastructure Operations" />
                <div style={{ margin: '16px 0', height: 1, background: 'var(--obs-hair)' }} />
                <div>
                  <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginBottom: 10 }}>Direct Reports · 5</div>
                  <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                    {['MR', 'AL', 'TJ', 'PR', 'KB'].map(ini => (
                      <div key={ini} className="cham-sm" style={{ width: 34, height: 34, background: 'var(--obs-sunken)', border: '1px solid var(--obs-hair-2)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                        <span className="voice-mono" style={{ fontSize: 9.5 }}>{ini}</span>
                      </div>
                    ))}
                  </div>
                </div>
                <div style={{ margin: '16px 0', height: 1, background: 'var(--obs-hair)' }} />
                <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                  <IField label="Position ID" value="POS-ARCH-SR-004" code />
                  <IField label="Grade" value="Tier 4 / Senior Architect" />
                </div>
              </Surface>
            </div>
          </section>

          {/* Chapter II — Institutional Service */}
          <section id="service">
            <ConstitutionalArticle title="Chapter II" code="POS-ASSIGN-01" emergeDelay={0}>
              Senior Technical Architect, Core Infrastructure, effective 1 March 2024. Third role assignment
              within the institution. Prior positions: Technical Architect (2023–2024) and Senior Engineer
              (2022–2023). Reporting line: VP Infrastructure Operations. Span of control: five engineers across
              distributed systems and cloud platform teams.
            </ConstitutionalArticle>
            <Surface pad="28px" emergeDelay={60} style={{ marginTop: 20 }}>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 24 }}>
                <div>
                  <Label style={{ marginBottom: 10 }}>Current Role</Label>
                  <div style={{ fontSize: 15, fontWeight: 600, color: 'var(--obs-fg)', lineHeight: 1.3 }}>Sr. Technical Architect</div>
                  <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginTop: 5 }}>Since 01 Mar 2024</div>
                </div>
                <div>
                  <Label style={{ marginBottom: 10 }}>Department</Label>
                  <div style={{ fontSize: 15, fontWeight: 600, color: 'var(--obs-fg)', lineHeight: 1.3 }}>Core Infrastructure</div>
                  <div className="voice-mono" style={{ color: 'var(--obs-mute)', marginTop: 5 }}>Engineering Division</div>
                </div>
                <div>
                  <Label style={{ marginBottom: 10 }}>Employment Type</Label>
                  <div style={{ fontSize: 15, fontWeight: 600, color: 'var(--obs-fg)', lineHeight: 1.3 }}>Full-Time · Permanent</div>
                  <Code color="var(--obs-mute)" style={{ marginTop: 5, display: 'block' }}>EMP-TYPE-FT-01</Code>
                </div>
              </div>
            </Surface>
          </section>

          {/* Chapter III — Capability Growth */}
          <section id="capability">
            <ConstitutionalArticle title="Chapter III" code="EMP-CAP-03" emergeDelay={0}>
              Skill record validated across four domains. Distributed Systems Design reached level 5 in August
              2025 — assessed by institutional review board, not self-reported. Capability trajectory consistent
              with Senior Architect grade expectations. Technical Leadership remains at level 3, not yet
              submitted for institutional validation.
            </ConstitutionalArticle>
            <Surface pad="28px" emergeDelay={60} style={{ marginTop: 20 }}>
              <Label style={{ marginBottom: 20 }}>Capability Matrix</Label>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                {SKILLS.map(s => <SkillRow key={s.name} {...s} />)}
              </div>
            </Surface>
            <div className="obs-signals-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 12, marginTop: 14 }}>
              {SIGNALS.map((s, i) => <SignalStrip key={s.id} sig={s} emergeDelay={i * 55} />)}
            </div>
          </section>

          {/* Chapter IV — Recognition */}
          <section id="recognition">
            <ConstitutionalArticle title="Chapter IV" code="RECOG-AWARD-04" emergeDelay={0}>
              One institutional award in 2026. The Q1 Infrastructure Excellence Award is conferred by the CTO
              directly — it requires cross-divisional impact and independent verification, not line-manager
              nomination. Citation references the database migration protocol: an operation affecting 11,240
              on-roll employees across seven payroll regions with zero data loss.
            </ConstitutionalArticle>
            <Surface variant="ink" pad="28px" emergeDelay={80} style={{ marginTop: 20 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 20 }}>
                <div style={{ flex: 1 }}>
                  <Label color="var(--obs-copper-soft)" style={{ marginBottom: 12 }}>Q1 Infrastructure Excellence Award · 2026</Label>
                  <div className="voice-serif" style={{ fontSize: 22, color: '#f2ede3', lineHeight: 1.42, maxWidth: 520 }}>
                    "Flawless execution of the database migration protocol, preserving institutional
                    data integrity across seven payroll regions."
                  </div>
                  <div className="voice-mono" style={{ marginTop: 18, color: 'rgba(242,237,227,0.5)' }}>
                    Conferred by CTO · 19 Jan 2026 · RECOG-AWARD-04
                  </div>
                </div>
                <div className="cham" style={{ width: 44, height: 44, background: 'rgba(177,106,60,0.14)', border: '1px solid rgba(177,106,60,0.32)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="var(--obs-copper)" aria-hidden="true">
                    <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
                  </svg>
                </div>
              </div>
            </Surface>
          </section>

          {/* Chapter V — Compensation */}
          <section id="compensation">
            <ConstitutionalArticle title="Chapter V" code="COMP-REV-11" emergeDelay={0}>
              Compensation standing: Tier 4 / Senior Architect band, compa-ratio 1.12 against P50–P75. Last
              revision April 2026: base increased from CHF 178,000 to CHF 185,000, authorised by VP
              Infrastructure Operations under COMP-REV-11. Equity position: 2,400 RSUs vesting in current year.
            </ConstitutionalArticle>
            <Surface pad="28px" emergeDelay={60} style={{ marginTop: 20 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 22, paddingBottom: 16, borderBottom: '1px solid var(--obs-hair)' }}>
                <div>
                  <Label style={{ marginBottom: 6 }}>Compensation Vector</Label>
                  <div className="voice-serif" style={{ fontSize: 20, color: 'var(--obs-fg)' }}>Tier 4 / Senior Architect</div>
                </div>
                <Code color="var(--obs-mute)">Band P50–P75</Code>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 24 }}>
                <IField label="Base Salary" value="CHF 185,000" />
                <IField label="Target Bonus" value="20%" />
                <IField label="Equity (Current Year)" value="2,400 RSUs" />
              </div>
            </Surface>
          </section>

          {/* Chapter VI — Authority Actions */}
          <section id="authority">
            <ConstitutionalArticle title="Chapter VI" code="RETN-AUTH-07" emergeDelay={0}>
              Retention risk elevated to the 78th percentile within a 6–9 month horizon. A strategic mandate
              under RETN-AUTH-07 creates a binding institutional record and triggers the retention protocol.
              The mandate commits the institution to specific action before the departure window. Commitment
              is irreversible — failure to act before the horizon creates an unacknowledged institutional risk.
            </ConstitutionalArticle>
            <div className="obs-split" style={{ display: 'grid', gridTemplateColumns: '1fr 1.25fr', gap: 20, marginTop: 20 }}>
              <RiskSurface risk={FLIGHT_RISK} emergeDelay={60} />
              <AuthoritySurface
                run="Strategic Mandate"
                authorityCode="RETN-AUTH-07"
                fields={[
                  { k: 'Employee', v: 'EMP-8834-A', sub: 'Elena Rostova' },
                  { k: 'Action',   v: 'Retention',  sub: 'Strategic Mandate' },
                  { k: 'Horizon',  v: 'Q3 2026',    sub: 'Target window' },
                ]}
                buttonLabel="Initiate Mandate"
                publishedLabel="Mandate committed"
                onPublished={() => setMandatePublished(true)}
              />
            </div>
          </section>

          {/* Chapter VII — Institutional Record */}
          <section id="record">
            <ConstitutionalArticle title="Chapter VII" code="AUD-TRAIL-01" emergeDelay={0}>
              Institutional record: five entries since onboarding, each citing its authority law. The record is
              append-only — no entry may be deleted, amended, or reordered. It is the institution's account of
              this person, not the person's account of themselves. Every consequential act leaves a mark here.
            </ConstitutionalArticle>
            <Surface pad="28px" emergeDelay={60} style={{ marginTop: 20 }}>
              <Label style={{ marginBottom: 20 }}>Immutable Record · EMP-8834-A</Label>
              {HISTORY.map((h, i) => <HistEntry key={i} {...h} />)}
              {mandatePublished && (
                <div data-emerge="" style={{ ['--obs-delay' as string]: '80ms', display: 'grid', gridTemplateColumns: '112px 1fr', gap: 20, paddingTop: 18 }}>
                  <div>
                    <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)' }}>2026-06-13</div>
                    <Code color="var(--obs-copper)" style={{ marginTop: 4, display: 'block' }}>RETN_MANDATE</Code>
                  </div>
                  <div>
                    <div style={{ fontSize: 13, lineHeight: 1.55, color: 'var(--obs-fg)' }}>
                      Strategic retention mandate RETN-STRAT-Q3-2026 committed. Authority: MGR acting under RETN-AUTH-07.
                    </div>
                    <Code color="var(--obs-mute)" style={{ marginTop: 5, display: 'block' }}>RETN-AUTH-07</Code>
                  </div>
                </div>
              )}
            </Surface>
            <div style={{ marginTop: 20, display: 'flex', gap: 28, flexWrap: 'wrap', padding: '12px 0', borderTop: '1px solid var(--obs-hair)', borderBottom: '1px solid var(--obs-hair)' }}>
              <LedgerMeta k="Record"        v="EMP-8834-A" />
              <LedgerMeta k="Projection"    v="EmployeeDetailProjection v412" />
              <LedgerMeta k="Freshness"     v="2026-06-12T07:40:11Z" />
              <LedgerMeta k="Last Mutation" v="COMP_REV by M. Vance" />
            </div>
          </section>

        </div>
      </div>
    </div>
  )
}
