import { useEffect, useMemo, useState } from 'react'
import { Icon } from '@/components/shell/Icon'
import {
  SignalCard,
  RiskCard,
  NarrativePlate,
  InstitutionalTable,
  CaseTimeline,
  LedgerStrip,
  type TableColumn,
  type TimelineEntry,
} from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'
import { api, ApiError } from '@/lib/api'
import { ensureDevSession } from '@/lib/auth'

// The asset dossier is evidence of custody: identity, current holder, condition
// over time. PHASE 0 (live seam): real GET /api/v1/assets/{id} + /maintenance +
// /depreciation. Custodial acts (assign, return) live on Asset Custody, the same
// way the candidate dossier reads while the offer act lives on Offer Management.
export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'record-detail',
  signals: 3,
  risks: 1,
  narratives: 1,
  maps: 0,
  bindu: false,
}

// â”€â”€ BFF contract (GetAsset serializes the whole aggregate) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
type Wrapped = string | { value: string }
interface AssignmentDto {
  id: string
  employeeId: string
  assignedOn: string
  returnedOn: string | null
  conditionOnReturn: number | string | null
  notes: string | null
}
interface AssetFullDto {
  id: Wrapped
  categoryId: Wrapped
  name: string
  serialNumber: string | null
  assetTag: string | null
  purchaseValue: number
  purchasedOn: string
  condition: number | string
  status: number | string
  createdAt: string
  assignments: AssignmentDto[]
  currentAssignment: AssignmentDto | null
}
interface CategoryDto {
  id: Wrapped
  name: string
  requiresReturnOnOffboarding: boolean
}
interface MaintenanceDto {
  id: Wrapped
  type: number | string
  status: number | string
  scheduledOn: string
  completedOn: string | null
  estimatedCost: number | null
  actualCost: number | null
  vendor: string | null
  isUnderWarranty: boolean
}
interface DepreciationDto {
  currentBookValue: number
  purchaseValue: number
  usefulLifeYears: number
  method: number | string
  periods: { year: number; amount: number; bookValueAfter: number }[]
}

const STATUS_LABELS = ['Available', 'Assigned', 'Under Maintenance', 'Retired', 'Lost']
const CONDITION_LABELS = ['New', 'Good', 'Fair', 'Poor']
const MAINT_TYPE_LABELS = ['Preventive', 'Corrective', 'Inspection', 'Warranty Service']
const MAINT_STATUS_LABELS = ['Scheduled', 'In Progress', 'Completed', 'Cancelled']

const unwrap = (w: Wrapped): string => (typeof w === 'string' ? w : w?.value ?? '')
const label = (v: number | string | null, table: string[]): string =>
  v == null ? '—' : typeof v === 'number' ? table[v] ?? String(v) : v
const shortId = (g: string): string => (g ? g.replace(/-/g, '').slice(0, 8).toUpperCase() : '—')
const inr = (n: number): string =>
  n >= 1e5 ? `₹${(n / 1e5).toFixed(2)}L` : `₹${n.toLocaleString('en-IN')}`

function daysBetween(fromIso: string, toIso = new Date().toISOString().slice(0, 10)): number {
  const a = new Date(fromIso).getTime()
  const b = new Date(toIso).getTime()
  return Math.max(0, Math.round((b - a) / 86_400_000))
}

function assetIdFromHash(): string | null {
  const q = window.location.hash.split('?')[1]
  return q ? new URLSearchParams(q).get('id') : null
}

interface CustodyView {
  ref: string
  holder: string
  assigned: string
  returned: string
  conditionOut: string
  current: boolean
}

interface MaintenanceView {
  ref: string
  type: string
  date: string
  cost: string
  status: string
  done: boolean
}

const assignmentColumns: TableColumn<CustodyView>[] = [
  { key: 'ref', header: 'Custody Ref', render: (a) => <span className="font-mono text-[11px] text-primary">{a.ref}</span> },
  { key: 'holder', header: 'Holder', render: (a) => <span className="text-primary">{a.holder}</span> },
  { key: 'assigned', header: 'Assigned', render: (a) => <span className="font-mono text-[11px]">{a.assigned}</span> },
  { key: 'returned', header: 'Returned', render: (a) => <span className="font-mono text-[11px]">{a.returned}</span> },
  {
    key: 'conditionOut',
    header: 'Condition',
    render: (a) => (
      <span className="flex items-center gap-2">
        <span className={`bindu ${a.current ? 'good' : 'neutral'}`} />
        <span className="text-on-surface-variant">{a.current ? 'Active custody' : `Returned · ${a.conditionOut}`}</span>
      </span>
    ),
  },
]

const maintenanceColumns: TableColumn<MaintenanceView>[] = [
  { key: 'ref', header: 'Record', render: (m) => <span className="font-mono text-[11px] text-primary">{m.ref}</span> },
  { key: 'type', header: 'Type', render: (m) => <span className="text-primary">{m.type}</span> },
  { key: 'date', header: 'Date', render: (m) => <span className="font-mono text-[11px]">{m.date}</span> },
  { key: 'cost', header: 'Cost', align: 'right', render: (m) => <span className="font-mono text-[12px]">{m.cost}</span> },
  {
    key: 'status',
    header: 'Status',
    render: (m) => (
      <span className="flex items-center gap-2">
        <span className={`bindu ${m.done ? 'good' : 'caution'}`} />
        <span className="font-mono text-[10px] uppercase tracking-wider">{m.status}</span>
      </span>
    ),
  },
]

interface Dossier {
  id: string
  name: string
  serial: string
  tag: string
  category: string
  requiresReturn: boolean
  status: string
  condition: string
  purchaseValue: number
  purchasedOn: string
  bookValue: number
  holder: string | null
  currentRef: string | null
  daysInCustody: number | null
  warranty: string
  custody: CustodyView[]
  maintenance: MaintenanceView[]
  audit: TimelineEntry[]
  depreciationPending: boolean
  depreciationNote: string
}

export function AssetDetail() {
  const [state, setState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [error, setError] = useState('')
  const [d, setD] = useState<Dossier | null>(null)
  const [fetchMs, setFetchMs] = useState<number | null>(null)

  useEffect(() => {
    let alive = true
    ;(async () => {
      try {
        setState('loading')
        await ensureDevSession()

        // Resolve the target asset: prefer the id carried in the hash; otherwise
        // fall back to the first asset so the nav-menu entry still resolves.
        let assetId = assetIdFromHash()
        const cats = await api.get<CategoryDto[]>('/api/v1/assets/categories')
        if (!assetId) {
          const list = await api.get<{ id: Wrapped }[]>('/api/v1/assets')
          assetId = list.length ? unwrap(list[0].id) : null
        }
        if (!assetId) {
          if (!alive) return
          setError('No assets registered yet — register one on the Asset Registry.')
          setState('error')
          return
        }

        const t0 = performance.now()
        const asset = await api.get<AssetFullDto>(`/api/v1/assets/${assetId}`)
        const maint = await api.get<MaintenanceDto[]>(`/api/v1/assets/${assetId}/maintenance`)
        let dep: DepreciationDto | null = null
        try {
          dep = await api.get<DepreciationDto>(`/api/v1/assets/${assetId}/depreciation`)
        } catch (e) {
          if (!(e instanceof ApiError && e.status === 404)) throw e
        }
        if (!alive) return
        setFetchMs(Math.round(performance.now() - t0))

        const catName = new Map(cats.map((c) => [unwrap(c.id), c]))
        const cat = catName.get(unwrap(asset.categoryId))
        const status = label(asset.status, STATUS_LABELS)
        const cur = asset.currentAssignment

        const custody: CustodyView[] = [...(asset.assignments ?? [])]
          .sort((a, b) => b.assignedOn.localeCompare(a.assignedOn))
          .map((a) => ({
            ref: `ASG-${shortId(a.id)}`,
            holder: `EMP · ${shortId(a.employeeId)}`,
            assigned: a.assignedOn,
            returned: a.returnedOn ?? '—',
            conditionOut: label(a.conditionOnReturn, CONDITION_LABELS),
            current: a.returnedOn == null,
          }))

        const maintenance: MaintenanceView[] = maint.map((m) => {
          const st = label(m.status, MAINT_STATUS_LABELS)
          const cost = m.actualCost ?? m.estimatedCost
          return {
            ref: `MNT-${shortId(unwrap(m.id))}`,
            type: label(m.type, MAINT_TYPE_LABELS) + (m.vendor ? ` · ${m.vendor}` : ''),
            date: m.completedOn ?? m.scheduledOn,
            cost: cost != null ? inr(cost) : '—',
            status: st,
            done: st === 'Completed',
          }
        })

        // Audit timeline assembled from real lifecycle events, newest first.
        const audit: TimelineEntry[] = []
        for (const a of asset.assignments ?? []) {
          audit.push({
            ts: `${a.assignedOn}T00:00:00Z`,
            tag: 'ASSET_ASSIGNED',
            tone: 'good',
            body: (
              <>
                Assigned to holder <span className="font-mono text-[11px]">{shortId(a.employeeId)}</span> under custody
                record ASG-{shortId(a.id)}. Single active assignment enforced by the domain.
              </>
            ),
          })
          if (a.returnedOn) {
            audit.push({
              ts: `${a.returnedOn}T00:00:00Z`,
              tag: 'ASSET_RETURNED',
              tone: 'neutral',
              body: `Returned on offboarding. Condition on return: ${label(a.conditionOnReturn, CONDITION_LABELS)}. Asset moved to Available.`,
            })
          }
        }
        for (const m of maint) {
          if (m.completedOn) {
            audit.push({
              ts: `${m.completedOn}T00:00:00Z`,
              tag: 'MAINT_DONE',
              tone: 'good',
              body: `${label(m.type, MAINT_TYPE_LABELS)} service MNT-${shortId(unwrap(m.id))} completed${m.isUnderWarranty ? ' under warranty' : ''}.`,
            })
          }
        }
        audit.push({
          ts: asset.createdAt,
          tag: 'ASSET_REGISTERED',
          tone: 'neutral',
          body: `Registered under category ${cat?.name ?? 'Uncategorized'}, purchase value ${inr(asset.purchaseValue)}.`,
        })
        audit.sort((x, y) => y.ts.localeCompare(x.ts))

        const warrantyRec = maint.find((m) => m.isUnderWarranty)
        const depreciationPending = !dep || (dep.periods?.length ?? 0) === 0

        setD({
          id: unwrap(asset.id),
          name: asset.name,
          serial: asset.serialNumber ?? '—',
          tag: asset.assetTag ?? '—',
          category: cat?.name ?? 'Uncategorized',
          requiresReturn: cat?.requiresReturnOnOffboarding ?? false,
          status,
          condition: label(asset.condition, CONDITION_LABELS),
          purchaseValue: asset.purchaseValue,
          purchasedOn: asset.purchasedOn,
          bookValue: dep?.currentBookValue ?? asset.purchaseValue,
          holder: cur ? shortId(cur.employeeId) : null,
          currentRef: cur ? `ASG-${shortId(cur.id)}` : null,
          daysInCustody: cur ? daysBetween(cur.assignedOn) : null,
          warranty: warrantyRec ? 'Under warranty' : '—',
          custody,
          maintenance,
          audit,
          depreciationPending,
          depreciationNote: dep
            ? `Schedule exists; ${dep.periods?.length ?? 0} period(s) recorded. Book value ${inr(dep.currentBookValue)}.`
            : 'No depreciation schedule recorded for this asset. Book value shown is purchase value.',
        })
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

  const subtitle = useMemo(() => {
    if (!d) return ''
    const custodyState = d.status === 'Assigned' ? 'Currently in custody' : d.status
    return `${d.category} · ${d.tag} · ${custodyState}`
  }, [d])

  if (state === 'loading') {
    return (
      <p className="font-tabular-data text-tabular-data text-on-surface-variant py-16 text-center">
        Authenticating and loading the live asset dossier from the BFF...
      </p>
    )
  }
  if (state === 'error' || !d) {
    return (
      <div className="py-16 text-center">
        <p className="font-tabular-data text-tabular-data text-rakta-critical mb-2">
          Could not load the asset dossier: {error}
        </p>
        <p className="font-mono text-[12px] text-on-surface-variant">
          Source: {api.base}/api/v1/assets/&lt;id&gt; · is the API running and provisioned?
        </p>
      </div>
    )
  }

  return (
    <>
      {/* Dossier header */}
      <div className="mb-12">
        <div className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-4 flex items-center">
          <Icon name="arrow_back" className="!text-[14px] mr-2" />
          <a href="#/asset-registry" className="hover:text-primary transition-colors cursor-pointer">Asset Registry</a>
          <span className="mx-2">/</span> Asset Detail
        </div>
        <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
          <div>
            <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight mb-2">
              {d.name}
            </h2>
            <p className="font-pull-quote text-pull-quote text-on-surface-variant italic">{subtitle}</p>
          </div>
          <a
            href="#/asset-assignment"
            className="px-[22px] py-[14px] bg-transparent border border-outline-variant text-primary font-tabular-data text-tabular-data hover:bg-surface-container-low transition-colors flex items-center self-start md:self-auto"
          >
            <Icon name="assignment_ind" className="!text-[18px] mr-2" /> Open Asset Custody
          </a>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-grid-12 mb-10">
        {/* Identity rail */}
        <div className="lg:col-span-4">
          <div className="bg-surface-container-lowest hairline-all corner-tick relative p-6 h-full flex flex-col">
            <div className="flex justify-between items-start mb-6">
              <div className="font-mono-label text-mono-label text-on-surface-variant uppercase">Identity</div>
              <div className="flex items-center gap-2 font-mono-label text-mono-label text-on-surface-variant">
                <span className={`bindu ${d.status === 'Assigned' ? 'good' : 'neutral'}`} /> {d.status}
              </div>
            </div>
            <div className="aspect-square w-32 bg-ivory-2 hairline-all mb-6 flex items-center justify-center">
              <Icon name="inventory_2" className="!text-[44px] text-graphite" />
            </div>
            <div className="space-y-4 font-tabular-data text-tabular-data">
              <div>
                <div className="text-on-surface-variant text-[12px] mb-1">Asset ID</div>
                <div className="text-primary font-medium font-mono text-[13px]">{shortId(d.id)}</div>
              </div>
              <div>
                <div className="text-on-surface-variant text-[12px] mb-1">Serial</div>
                <div className="text-primary font-mono text-[13px]">{d.serial}</div>
              </div>
              <div>
                <div className="text-on-surface-variant text-[12px] mb-1">Category</div>
                <div className="text-primary">{d.category} · {d.requiresReturn ? 'Requires return' : 'No return required'}</div>
              </div>
              <div>
                <div className="text-on-surface-variant text-[12px] mb-1">Holder</div>
                <div className="text-primary flex items-center">
                  <Icon name="person" className="!text-[16px] mr-1 text-on-surface-variant" />
                  {d.holder ? `EMP · ${d.holder}` : 'Unassigned'}
                </div>
              </div>
            </div>
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-6 pt-4 hairline-t">
              Exactly one custody record is open at a time. The asset cannot be reassigned until the
              current holder returns it — enforced by the domain, not by this screen.
            </p>
          </div>
        </div>

        {/* Signals + custodial state */}
        <div className="lg:col-span-8 flex flex-col gap-grid-12">
          <div className="grid grid-cols-1 xl:grid-cols-3 gap-grid-12">
            <SignalCard label="Book Value" value={inr(d.bookValue)} note={`Purchase ${inr(d.purchaseValue)}`} bindu="neutral" />
            <SignalCard
              label="Days in Custody"
              value={d.daysInCustody != null ? String(d.daysInCustody) : '—'}
              note={d.currentRef ? `Current custody ${d.currentRef}` : 'No active custody'}
              bindu={d.daysInCustody != null ? 'good' : 'neutral'}
            />
            <SignalCard label="Condition" value={d.condition} note={`Status ${d.status}`} bindu="good" />
          </div>

          <div className="bg-surface-container-low hairline-all p-6 flex-1">
            <div className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-6 border-b border-outline-variant pb-2">
              Custodial State
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-6 font-tabular-data text-tabular-data">
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Status</div>
                <div className="text-primary">{d.status}</div>
              </div>
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Custody Record</div>
                <div className="text-primary font-mono text-[13px]">{d.currentRef ?? '—'}</div>
              </div>
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Warranty</div>
                <div className="text-primary font-mono text-[13px]">{d.warranty}</div>
              </div>
            </div>
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-6">
              On offboarding of the current holder, the return is demanded automatically for categories
              marked Requires return — the asset re-enters Available only once the return is recorded.
            </p>
          </div>

          <RiskCard
            severity="monitored"
            title={d.depreciationPending ? 'Depreciation Period Pending' : 'Depreciation On Track'}
            body={d.depreciationNote}
          />
        </div>
      </div>

      {/* Custody history */}
      <section className="bg-surface-container-lowest hairline-all p-6 mb-10">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-6 uppercase tracking-widest border-b border-outline-variant pb-2">
          Custody History
        </h3>
        {d.custody.length ? (
          <InstitutionalTable columns={assignmentColumns} rows={d.custody} rowKey={(a) => a.ref} />
        ) : (
          <p className="font-tabular-data text-tabular-data text-on-surface-variant py-6 text-center">
            No custody records yet. This asset has never been assigned.
          </p>
        )}
      </section>

      {/* Maintenance & narrative */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-grid-12 mb-10">
        <section className="bg-surface-container-lowest hairline-all p-6">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-6 uppercase tracking-widest border-b border-outline-variant pb-2">
            Maintenance Record
          </h3>
          {d.maintenance.length ? (
            <InstitutionalTable columns={maintenanceColumns} rows={d.maintenance} rowKey={(m) => m.ref} />
          ) : (
            <p className="font-tabular-data text-tabular-data text-on-surface-variant py-6 text-center">
              No maintenance events recorded.
            </p>
          )}
        </section>

        <NarrativePlate source="Buddhi · custody synthesis (live)">
          {`${d.custody.length} custody record(s), ${d.maintenance.length} service event(s). `}
          {d.holder
            ? 'The asset is in active custody — utilisation that keeps it off the shelf. '
            : 'The asset is currently on the shelf, available for assignment. '}
          {d.depreciationPending
            ? 'The open thread is bookkeeping: a depreciation period waiting to be struck before the books can close on it.'
            : 'The depreciation schedule is current.'}
        </NarrativePlate>
      </div>

      {/* Audit timeline */}
      <section className="bg-surface-container-lowest hairline-all p-6">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-6 uppercase tracking-widest border-b border-outline-variant pb-2">
          Audit Timeline
        </h3>
        <CaseTimeline entries={d.audit} />
      </section>

      <LedgerStrip
        className="mt-8"
        entries={[
          { label: 'Record', value: shortId(d.id) },
          { label: 'Category', value: `${d.category} · ${d.tag}` },
          { label: 'Source', value: 'GET /api/v1/assets/{id} (live)' },
          { label: 'Fetch', value: fetchMs != null ? `${fetchMs}ms` : '—' },
        ]}
      />
    </>
  )
}
