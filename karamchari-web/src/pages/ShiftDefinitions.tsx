import { useEffect, useMemo, useState } from 'react'
import { SignalCard, InstitutionalTable, LedgerStrip, type TableColumn } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'
import { api, ApiError } from '@/lib/api'
import { ensureDevSession } from '@/lib/auth'

// Shifts are institutional primitives, not local exceptions. PHASE 0 (live seam):
// real JWT + GET /api/v1/workforce/rosters/shifts. Columns mirror the BFF
// ShiftDefinition contract; "rosters using" is not exposed by the BFF, so it is
// not shown rather than fabricated.
export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'SYS',
  archetype: 'administration',
  signals: 1,
  risks: 0,
  narratives: 0,
  maps: 0,
  bindu: false,
}

// BFF contract: ShiftDefinition (TimeOnly serializes as "HH:mm:ss").
interface ShiftDto {
  id: string
  code: string
  name: string
  category: number | string
  startTime: string
  endTime: string
  lateArrivalGraceMinutes: number
  autoBreakDeductionMinutes: number
  isActive: boolean
  createdAtUtc: string
}

interface ShiftDef {
  code: string
  name: string
  window: string
  paidBreak: string
  grace: string
  active: boolean
  effective: string
}

const hhmm = (t: string): string => (t ? t.slice(0, 5) : '—')

const columns: TableColumn<ShiftDef>[] = [
  { key: 'code', header: 'Code', render: (d) => <span className="font-mono text-[12px] text-primary">{d.code}</span> },
  { key: 'name', header: 'Definition', render: (d) => <span className="text-primary">{d.name}</span> },
  { key: 'window', header: 'Window', render: (d) => <span className="font-mono text-[12px]">{d.window}</span> },
  { key: 'paidBreak', header: 'Auto Break' },
  { key: 'grace', header: 'Grace' },
  {
    key: 'status',
    header: 'Status',
    render: (d) => (
      <span className="flex items-center gap-2">
        <span className={`bindu ${d.active ? 'good' : 'neutral'}`} />
        <span className="font-mono text-[10px] uppercase tracking-wider">{d.active ? 'Active' : 'Retired'}</span>
      </span>
    ),
  },
  { key: 'effective', header: 'Effective', render: (d) => <span className="font-mono text-[11px]">{d.effective}</span> },
]

export function ShiftDefinitions() {
  const [defs, setDefs] = useState<ShiftDef[]>([])
  const [state, setState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [error, setError] = useState('')

  useEffect(() => {
    let alive = true
    ;(async () => {
      try {
        setState('loading')
        await ensureDevSession()
        const dtos = await api.get<ShiftDto[]>('/api/v1/workforce/rosters/shifts')
        if (!alive) return
        setDefs(
          dtos.map((s) => ({
            code: s.code,
            name: s.name,
            window: `${hhmm(s.startTime)}–${hhmm(s.endTime)}`,
            paidBreak: s.autoBreakDeductionMinutes > 0 ? `${s.autoBreakDeductionMinutes} min` : '—',
            grace: `${s.lateArrivalGraceMinutes} min`,
            active: s.isActive,
            effective: (s.createdAtUtc ?? '').slice(0, 10),
          })),
        )
        setState('ready')
      } catch (e) {
        if (!alive) return
        setError(e instanceof ApiError ? `${e.status} ${e.statusText}` : (e as Error).message)
        setState('error')
      }
    })()
    return () => {
      alive = false
    }
  }, [])

  const activeCount = useMemo(() => defs.filter((d) => d.active).length, [defs])
  const editor = defs[0]

  return (
    <>
      {/* Entity header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Scheduling Configuration · Tenant-wide
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Shift Definitions
          </h2>
        </div>
        <span className="font-mono text-[12px] text-on-surface-variant uppercase tracking-widest">
          {state === 'ready' ? `${defs.length} Primitives` : '—'} · <span className="text-tamra-copper">LIVE</span>
        </span>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-grid-12 mb-10">
        {/* The register */}
        <section className="lg:col-span-8">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
            1. Definition Register
          </h3>
          <div className="bg-surface-container-lowest hairline-all p-6">
            {state === 'loading' && (
              <p className="font-tabular-data text-tabular-data text-on-surface-variant py-8 text-center">
                Authenticating and loading shift definitions from the BFF...
              </p>
            )}
            {state === 'error' && (
              <p className="font-tabular-data text-tabular-data text-rakta-critical py-8 text-center">
                Could not load shift definitions: {error}
              </p>
            )}
            {state === 'ready' && (
              <>
                <InstitutionalTable columns={columns} rows={defs} rowKey={(d) => d.code} />
                <p className="font-tabular-data text-[12px] text-on-surface-variant mt-6">
                  New definitions require scheduling authority review. A request that differs from an
                  existing primitive only in start time or break length is a parameter change, not a
                  new definition; rosters override grace and break per zone without minting shifts.
                </p>
              </>
            )}
          </div>
        </section>

        {/* Editor + count */}
        <div className="lg:col-span-4 flex flex-col gap-grid-12">
          <SignalCard
            label="Active Definitions"
            value={state === 'ready' ? String(activeCount) : '—'}
            note="Definition count is a governance metric; growth needs justification."
            bindu="good"
          />
          <section className="bg-surface-container-lowest hairline-all p-6 flex-1">
            <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-5 uppercase tracking-widest">
              2. Editor
            </h3>
            <div className="space-y-4 font-tabular-data text-tabular-data">
              {[
                { label: 'Code', value: editor?.code ?? '—' },
                { label: 'Window', value: editor?.window ?? '—' },
                { label: 'Auto Break', value: editor?.paidBreak ?? '—' },
                { label: 'Grace Period', value: editor?.grace ?? '—' },
                { label: 'Status', value: editor ? (editor.active ? 'Active' : 'Retired') : '—' },
              ].map((f) => (
                <div key={f.label} className="flex justify-between items-center pb-3 hairline-b">
                  <span className="text-on-surface-variant text-[12px]">{f.label}</span>
                  <span className="font-mono text-[12px] text-primary">{f.value}</span>
                </div>
              ))}
            </div>
            <button className="w-full mt-6 py-2.5 bg-transparent border border-outline-variant text-primary font-mono-label text-mono-label uppercase tracking-widest hover:bg-surface-container-low transition-colors">
              Save Revision
            </button>
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-4">
              Revisions apply from the next attendance period. Definitions are deactivated,
              never deleted; historical periods keep the version they were processed under.
            </p>
          </section>
        </div>
      </div>

      {/* Effect note */}
      <div className="bg-ivory-2 hairline-all p-4 mb-8">
        <p className="font-tabular-data text-tabular-data text-on-surface">
          Definitions on this register govern attendance processing, grace evaluation, and payroll
          hour derivation for every roster in the tenant.
        </p>
      </div>

      <LedgerStrip
        entries={[
          { label: 'Register', value: 'Scheduling.ShiftDefinitions' },
          { label: 'Source', value: 'GET /workforce/rosters/shifts (live)' },
          { label: 'Showing', value: state === 'ready' ? `${defs.length} definitions` : '—' },
        ]}
      />
    </>
  )
}
