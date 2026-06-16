import { useEffect, useMemo, useState } from 'react'
import { Icon } from '@/components/shell/Icon'
import { InstitutionalTable, LedgerStrip, BinduButton } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'
import { api, ApiError } from '@/lib/api'
import { ensureDevSession, getLastLoginMs } from '@/lib/auth'

// Custody in motion: who the institution has entrusted things to, and what it has
// recovered. PHASE 0 (live seam): the BFF has no "list all custody records"
// endpoint, so the ledger is assembled from GET /api/v1/assets + a per-asset
// detail fetch, flattening each asset's assignment history. Assignment proves
// entrustment; return proves recovery. One ledger, filtered between the verbs.
export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'registry',
  signals: 1,
  risks: 0,
  narratives: 0,
  maps: 0,
  bindu: true,
}

type Wrapped = string | { value: string }
interface AssignmentDto {
  id: string
  employeeId: string
  assignedOn: string
  returnedOn: string | null
}
interface AssetFullDto {
  id: Wrapped
  name: string
  assetTag: string | null
  status: number | string
  assignments: AssignmentDto[]
}

const unwrap = (w: Wrapped): string => (typeof w === 'string' ? w : w?.value ?? '')
const shortId = (g: string): string => (g ? g.replace(/-/g, '').slice(0, 8).toUpperCase() : '—')

interface CustodyRow {
  id: string
  assetId: string
  asset: string
  tag: string
  holder: string
  assigned: string
  returned: string
  state: 'Entrusted' | 'Recovered'
  tone: 'good' | 'neutral'
}

const filters = ['All', 'Entrusted', 'Recovered'] as const

export function AssetAssignment() {
  const [query, setQuery] = useState('')
  const [filter, setFilter] = useState<(typeof filters)[number]>('All')
  const [records, setRecords] = useState<CustodyRow[]>([])
  const [state, setState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [error, setError] = useState('')
  const [loginMs, setLoginMs] = useState<number | null>(null)
  const [fetchMs, setFetchMs] = useState<number | null>(null)

  useEffect(() => {
    let alive = true
    ;(async () => {
      try {
        setState('loading')
        await ensureDevSession()
        const t0 = performance.now()
        const list = await api.get<{ id: Wrapped }[]>('/api/v1/assets')
        // No bulk custody endpoint — fan out per asset to read assignment history.
        const details = await Promise.all(
          list.map((a) => api.get<AssetFullDto>(`/api/v1/assets/${unwrap(a.id)}`)),
        )
        if (!alive) return
        setFetchMs(Math.round(performance.now() - t0))
        setLoginMs(getLastLoginMs())

        const rows: CustodyRow[] = []
        for (const asset of details) {
          for (const a of asset.assignments ?? []) {
            const recovered = a.returnedOn != null
            rows.push({
              id: `ASG-${shortId(a.id)}`,
              assetId: unwrap(asset.id),
              asset: asset.name,
              tag: asset.assetTag ?? '—',
              holder: `EMP · ${shortId(a.employeeId)}`,
              assigned: a.assignedOn,
              returned: a.returnedOn ?? '—',
              state: recovered ? 'Recovered' : 'Entrusted',
              tone: recovered ? 'neutral' : 'good',
            })
          }
        }
        rows.sort((x, y) => y.assigned.localeCompare(x.assigned))
        setRecords(rows)
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

  const entrusted = useMemo(() => records.filter((r) => r.state === 'Entrusted').length, [records])

  const rows = useMemo(() => {
    const q = query.trim().toLowerCase()
    return records.filter(
      (r) =>
        (filter === 'All' || r.state === filter) &&
        (!q || r.asset.toLowerCase().includes(q) || r.holder.toLowerCase().includes(q) || r.tag.toLowerCase().includes(q)),
    )
  }, [query, filter, records])

  const openDetail = (assetId: string) => {
    window.location.hash = `/asset-detail?id=${encodeURIComponent(assetId)}`
  }

  return (
    <>
      {/* Registry header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Custody Ledger · Entrusted &amp; Recovered
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Asset Custody
          </h2>
          <p className="font-mono text-[12px] text-on-surface-variant mt-3">
            <span className="text-primary">{state === 'ready' ? entrusted : '—'}</span> ACTIVE CUSTODY RECORDS ·{' '}
            {state === 'ready' ? records.length : '—'} TOTAL · <span className="text-tamra-copper">LIVE</span>
          </p>
        </div>
        <BinduButton>Assign Asset</BinduButton>
      </div>

      {/* Query bar */}
      <div className="flex flex-col md:flex-row gap-4 mb-8">
        <div className="flex items-center gap-2 hairline-all bg-surface-container-lowest px-4 py-2.5 md:w-96">
          <Icon name="search" className="!text-[18px] text-on-surface-variant" />
          <input
            className="flex-1 bg-transparent font-tabular-data text-tabular-data text-primary placeholder:text-on-surface-variant focus:outline-none"
            placeholder="Asset, holder, or tag..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </div>
        <div className="flex flex-wrap gap-2">
          {filters.map((f) => (
            <button
              key={f}
              onClick={() => setFilter(f)}
              className={`px-3 py-2 font-mono-label text-mono-label uppercase tracking-wider transition-colors ${
                filter === f
                  ? 'bg-primary text-on-primary'
                  : 'hairline-all text-on-surface-variant hover:bg-surface-container-low'
              }`}
            >
              {f}
            </button>
          ))}
        </div>
      </div>

      {/* The ledger */}
      <div className="bg-surface-container-lowest hairline-all p-6">
        {state === 'loading' && (
          <p className="font-tabular-data text-tabular-data text-on-surface-variant py-8 text-center">
            Authenticating and assembling the live custody ledger from the BFF...
          </p>
        )}
        {state === 'error' && (
          <div className="py-8 text-center">
            <p className="font-tabular-data text-tabular-data text-rakta-critical mb-2">
              Could not reach the live ledger: {error}
            </p>
            <p className="font-mono text-[12px] text-on-surface-variant">
              Source: {api.base}/api/v1/assets · is the API running and provisioned?
            </p>
          </div>
        )}
        {state === 'ready' && (
          <>
            <InstitutionalTable
              rowKey={(r) => r.id}
              rows={rows}
              columns={[
                {
                  key: 'id',
                  header: 'Custody Ref',
                  render: (r) => <span className="font-mono text-[12px] text-on-surface-variant">{r.id}</span>,
                },
                {
                  key: 'asset',
                  header: 'Asset',
                  render: (r) => (
                    <button
                      onClick={() => openDetail(r.assetId)}
                      className="text-primary font-medium hover:text-tamra-copper transition-colors text-left"
                    >
                      {r.asset}
                    </button>
                  ),
                },
                {
                  key: 'holder',
                  header: 'Entrusted To',
                  render: (r) => <span className="text-on-surface-variant">{r.holder}</span>,
                },
                {
                  key: 'assigned',
                  header: 'Assigned',
                  render: (r) => <span className="font-mono text-[11px] text-on-surface-variant">{r.assigned}</span>,
                },
                {
                  key: 'returned',
                  header: 'Returned',
                  render: (r) => <span className="font-mono text-[11px] text-on-surface-variant">{r.returned}</span>,
                },
                {
                  key: 'state',
                  header: 'State',
                  align: 'right',
                  render: (r) => (
                    <>
                      <span className={`bindu ${r.tone} mr-2`} />
                      {r.state}
                    </>
                  ),
                },
              ]}
            />
            {rows.length === 0 && (
              <p className="font-tabular-data text-tabular-data text-on-surface-variant py-8 text-center">
                No custody records match the current query. Assign an asset on its dossier, or widen the filter.
              </p>
            )}
          </>
        )}
      </div>

      <LedgerStrip
        className="mt-8"
        entries={[
          { label: 'Showing', value: state === 'ready' ? `${rows.length} of ${records.length}` : '—' },
          { label: 'Source', value: 'GET /api/v1/assets + per-asset (live)' },
          { label: 'Login', value: loginMs != null ? `${loginMs}ms` : 'cached' },
          { label: 'Fetch', value: fetchMs != null ? `${fetchMs}ms` : '—' },
        ]}
      />
    </>
  )
}
