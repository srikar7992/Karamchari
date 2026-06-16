import { useEffect, useState } from 'react'
import {
  Surface, Label, Code,
  ConstitutionalArticle, SpineRail, useScrollSpy,
  SignalStrip,
  type Signal,
} from '@/components/observatory/surfaces'
import type { PageClassification } from '@/lib/doctrine'
import { api, ApiError } from '@/lib/api'
import { ensureDevSession } from '@/lib/auth'

// Institutional biography. PHASE 0 (live seam): real JWT + GET /hr/employees/{id}/profile
// (identity + resolved organizational names) + /history. Compensation/skills/recognition
// require backend projections that do not exist yet and are listed honestly as pending.
export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'record-detail',
  signals: 2,
  risks: 0,
  narratives: 1,
  maps: 0,
  bindu: false,
}

interface EmployeeDto { id: string; legalName: string }

interface ProfileDto {
  id: string
  employeeNumber: string
  legalName: string
  workEmail: string | null
  hiredOn: string
  status: string
  timeZoneId: string
  employmentType: string
  departmentName: string | null
  designationName: string | null
  designationLevel: number | null
  branchName: string | null
  managerName: string | null
}

interface HistoryDto {
  type: string
  previous: string
  new: string
  effectiveFrom: string
  effectiveTo: string | null
  changedBy: string
  loggedAt: string
}

const SPINE_NODES = [
  { id: 'identity', label: 'I · Identity' },
  { id: 'service', label: 'II · Service' },
  { id: 'record', label: 'III · Record' },
  { id: 'pending', label: 'IV · Pending' },
]
const CHAPTER_IDS = SPINE_NODES.map((n) => n.id)

const shortId = (g: string): string => (g ? g.replace(/-/g, '').slice(0, 8).toUpperCase() : '—')

function tenureYears(hiredOn: string): number {
  const days = (Date.now() - new Date(hiredOn).getTime()) / 86_400_000
  return Math.max(0, Math.round((days / 365.25) * 10) / 10)
}

function idFromHash(): string | null {
  const q = window.location.hash.split('?')[1]
  return q ? new URLSearchParams(q).get('id') : null
}

function IField({ label, value, code = false }: { label: string; value: string; code?: boolean }) {
  return (
    <div>
      <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)', marginBottom: 3 }}>{label}</div>
      {code
        ? <Code color="var(--obs-fg)" style={{ fontSize: 12 }}>{value}</Code>
        : <div style={{ fontSize: 13, fontWeight: 500, color: 'var(--obs-fg)' }}>{value}</div>}
    </div>
  )
}

interface RecordEntry { ts: string; tag: string; body: string; meta: string }

const HUMAN_TAG: Record<string, string> = {
  ManagerChange: 'Reporting manager',
  DepartmentTransfer: 'Department',
  DesignationChange: 'Designation',
  BranchTransfer: 'Branch',
}

export function Employee360() {
  const active = useScrollSpy(CHAPTER_IDS)
  const [profile, setProfile] = useState<ProfileDto | null>(null)
  const [record, setRecord] = useState<RecordEntry[]>([])
  const [state, setState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [error, setError] = useState('')
  const [fetchMs, setFetchMs] = useState<number | null>(null)

  useEffect(() => {
    let alive = true
    ;(async () => {
      try {
        setState('loading')
        await ensureDevSession()
        let id = idFromHash()
        const list = await api.get<EmployeeDto[]>('/api/v1/hr/employees')
        if (!id) id = list.length ? list[0].id : null
        if (!id) { if (alive) { setError('No employees registered.'); setState('error') } return }

        const nameById = new Map(list.map((e) => [e.id, e.legalName]))
        const t0 = performance.now()
        const p = await api.get<ProfileDto>(`/api/v1/hr/employees/${id}/profile`)
        const hist = await api.get<HistoryDto[]>(`/api/v1/hr/employees/${id}/history`)
        if (!alive) return
        setFetchMs(Math.round(performance.now() - t0))

        // Resolve organizational guids in the change ledger to human values.
        const resolve = (h: HistoryDto): string => {
          if (h.new === 'None') return '—'
          if (h.type === 'ManagerChange') return nameById.get(h.new) ?? `EMP · ${shortId(h.new)}`
          if (h.type === 'DepartmentTransfer') return p.departmentName ?? shortId(h.new)
          if (h.type === 'DesignationChange') return p.designationName ?? shortId(h.new)
          if (h.type === 'BranchTransfer') return p.branchName ?? shortId(h.new)
          return h.new
        }

        const entries: RecordEntry[] = hist.map((h) => ({
          ts: h.effectiveFrom.slice(0, 10),
          tag: h.type.replace(/([A-Z])/g, ' $1').trim().toUpperCase(),
          body: `${HUMAN_TAG[h.type] ?? h.type} set to ${resolve(h)}.`,
          meta: `Changed by ${shortId(h.changedBy)}`,
        }))
        entries.push({
          ts: p.hiredOn,
          tag: 'ONBOARD',
          body: `${p.legalName} joined the institution as ${p.employeeNumber}.`,
          meta: 'Source: employee record',
        })
        entries.sort((a, b) => b.ts.localeCompare(a.ts))

        setRecord(entries)
        setProfile(p)
        setState('ready')
      } catch (err) {
        if (!alive) return
        setError(err instanceof ApiError ? `${err.status} ${err.statusText}` : (err as Error).message)
        setState('error')
      }
    })()
    return () => { alive = false }
  }, [])

  if (state === 'loading') {
    return (
      <div className="obs obs-canvas" style={{ minHeight: '100vh', padding: '32px 24px' }}>
        <div className="voice-mono" style={{ color: 'var(--obs-mute)' }}>Authenticating and loading the live biography from the BFF…</div>
      </div>
    )
  }
  if (state === 'error' || !profile) {
    return (
      <div className="obs obs-canvas" style={{ minHeight: '100vh', padding: '32px 24px' }}>
        <div className="voice-mono" style={{ color: 'var(--obs-copper)' }}>Could not load the biography: {error}</div>
      </div>
    )
  }

  const tenure = tenureYears(profile.hiredOn)
  const daysEmployed = Math.max(0, Math.round((Date.now() - new Date(profile.hiredOn).getTime()) / 86_400_000))
  const isActive = profile.status === 'Active'
  const role = profile.designationName
    ? `${profile.designationName}${profile.departmentName ? ` · ${profile.departmentName}` : ''}`
    : profile.departmentName ?? 'Role pending'

  const signals: Signal[] = [
    { id: 's-tenure', label: 'Tenure', value: String(tenure), unit: 'yrs', ref: `Hired ${profile.hiredOn}`, delta: '', dir: 'up', spark: [Math.max(0, tenure - 0.5), tenure] },
    { id: 's-days', label: 'Days Employed', value: daysEmployed.toLocaleString(), delta: '', dir: 'up', spark: [Math.max(0, daysEmployed - 30), daysEmployed] },
  ]

  return (
    <div className="obs obs-canvas" style={{ minHeight: '100vh', padding: '32px 24px 80px' }}>
      {/* Screen head */}
      <div style={{ marginBottom: 40 }}>
        <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)', letterSpacing: '0.16em', marginBottom: 14 }}>
          EMPLOYEE DIRECTORY / {profile.employeeNumber} / INSTITUTIONAL BIOGRAPHY · LIVE
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 24, flexWrap: 'wrap' }}>
          <div>
            <div className="voice-serif" style={{ fontSize: 44, lineHeight: 1.05, letterSpacing: '-0.022em', color: 'var(--obs-fg)', fontWeight: 400 }}>
              {profile.legalName}
            </div>
            <div className="voice-mono" style={{ marginTop: 8, color: 'var(--obs-mute)', letterSpacing: '0.1em' }}>
              {role} · {profile.branchName ?? profile.timeZoneId}
            </div>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, paddingBottom: 4 }}>
            <span style={{ width: 7, height: 7, borderRadius: '50%', background: isActive ? 'var(--obs-forest)' : 'var(--obs-mute)' }} />
            <span className="voice-mono" style={{ color: isActive ? 'var(--obs-forest)' : 'var(--obs-mute)' }}>{profile.status} · On-roll</span>
          </div>
        </div>
      </div>

      <div className="obs-shell" style={{ display: 'grid', gridTemplateColumns: '148px minmax(0,1fr)', gap: 48 }}>
        <SpineRail nodes={SPINE_NODES} active={active} />

        <div style={{ display: 'flex', flexDirection: 'column', gap: 64 }}>
          {/* Chapter I — Identity */}
          <section id="identity">
            <ConstitutionalArticle title="Chapter I" code="EMP-ID-01" lead emergeDelay={0}>
              {profile.legalName} is on the institution&apos;s active register as {profile.employeeNumber}, joined{' '}
              {profile.hiredOn}{profile.managerName ? `, reporting to ${profile.managerName}` : ''}. This biography reads
              what the workforce system-of-record can stand behind today — identity, organization, and the immutable
              change record.
            </ConstitutionalArticle>
            <Surface pad="28px" emergeDelay={80} style={{ marginTop: 28 }}>
              <Label style={{ marginBottom: 18 }}>Identity</Label>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2,1fr)', gap: 16 }}>
                <IField label="Internal ID" value={profile.employeeNumber} code />
                <IField label="Work Email" value={profile.workEmail ?? '—'} />
                <IField label="Joined" value={profile.hiredOn} />
                <IField label="Time Zone" value={profile.timeZoneId} />
                <IField label="Reporting Manager" value={profile.managerName ?? '—'} />
                <IField label="Status" value={profile.status} />
              </div>
            </Surface>
          </section>

          {/* Chapter II — Service */}
          <section id="service">
            <ConstitutionalArticle title="Chapter II" code="POS-ASSIGN-01" emergeDelay={0}>
              {tenure} years of tenure since {profile.hiredOn}. Organizational standing is resolved live:{' '}
              {profile.designationName ?? 'designation pending'} in {profile.departmentName ?? 'an unassigned department'},
              based at {profile.branchName ?? 'no branch on record'}.
            </ConstitutionalArticle>
            <div className="obs-signals-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(2,1fr)', gap: 12, marginTop: 20 }}>
              {signals.map((s, i) => <SignalStrip key={s.id} sig={s} emergeDelay={i * 55} />)}
            </div>
            <Surface pad="28px" emergeDelay={120} style={{ marginTop: 14 }}>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 24 }}>
                <IField label="Designation" value={profile.designationName ? `${profile.designationName}${profile.designationLevel != null ? ` · L${profile.designationLevel}` : ''}` : '—'} />
                <IField label="Department" value={profile.departmentName ?? '—'} />
                <IField label="Branch" value={profile.branchName ?? '—'} />
                <IField label="Employment Type" value={profile.employmentType} />
                <IField label="Reporting Manager" value={profile.managerName ?? '—'} />
                <IField label="Time Zone" value={profile.timeZoneId} />
              </div>
            </Surface>
          </section>

          {/* Chapter III — Immutable Record */}
          <section id="record">
            <ConstitutionalArticle title="Chapter III" code="AUD-TRAIL-01" emergeDelay={0}>
              The institutional record is append-only — {record.length} {record.length === 1 ? 'entry' : 'entries'},
              each read live from the change ledger. It is the institution&apos;s account of this person, not the
              person&apos;s account of themselves.
            </ConstitutionalArticle>
            <Surface pad="28px" emergeDelay={60} style={{ marginTop: 20 }}>
              <Label style={{ marginBottom: 20 }}>Immutable Record · {profile.employeeNumber}</Label>
              {record.map((h, i) => (
                <div key={i} style={{ display: 'grid', gridTemplateColumns: '112px 1fr', gap: 20, paddingBottom: 18, marginBottom: 18, borderBottom: '1px solid var(--obs-hair)' }}>
                  <div>
                    <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)' }}>{h.ts}</div>
                    <Code color="var(--obs-copper)" style={{ marginTop: 4, display: 'block' }}>{h.tag}</Code>
                  </div>
                  <div>
                    <div style={{ fontSize: 13, lineHeight: 1.55, color: 'var(--obs-fg)' }}>{h.body}</div>
                    <Code color="var(--obs-mute)" style={{ marginTop: 5, display: 'block' }}>{h.meta}</Code>
                  </div>
                </div>
              ))}
            </Surface>
          </section>

          {/* Chapter IV — Pending projections */}
          <section id="pending">
            <ConstitutionalArticle title="Chapter IV" code="ROADMAP-01" emergeDelay={0}>
              Four chapters of the full biography await backend projections from other modules. They are named here so
              the gap is a documented commitment, not a silent omission.
            </ConstitutionalArticle>
            <Surface pad="28px" emergeDelay={60} style={{ marginTop: 20 }}>
              <Label style={{ marginBottom: 16 }}>Awaiting Backend Projection</Label>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                {[
                  ['Capability', 'Skills matrix + validation — Capability module read slice'],
                  ['Recognition', 'Awards ledger — no module yet'],
                  ['Compensation', 'Band, base, compa-ratio — Compensation module read slice'],
                  ['Authority', 'Flight-risk + retention mandate — Intelligence projection'],
                ].map(([k, v]) => (
                  <div key={k} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingBottom: 12, borderBottom: '1px solid var(--obs-hair)' }}>
                    <div style={{ fontSize: 13, fontWeight: 500, color: 'var(--obs-fg)' }}>{k}</div>
                    <Code color="var(--obs-mute)">{v}</Code>
                  </div>
                ))}
              </div>
            </Surface>
            <div style={{ marginTop: 20, display: 'flex', gap: 28, flexWrap: 'wrap', padding: '12px 0', borderTop: '1px solid var(--obs-hair)', borderBottom: '1px solid var(--obs-hair)' }}>
              <div>
                <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)', marginBottom: 3 }}>Record</div>
                <Code color="var(--obs-mute)">{profile.employeeNumber}</Code>
              </div>
              <div>
                <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)', marginBottom: 3 }}>Source</div>
                <Code color="var(--obs-mute)">GET /hr/employees/{'{id}'}/profile (live)</Code>
              </div>
              <div>
                <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)', marginBottom: 3 }}>Fetch</div>
                <Code color="var(--obs-mute)">{fetchMs != null ? `${fetchMs}ms` : '—'}</Code>
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  )
}
