import { useEffect, useMemo, useState } from 'react'
import { Icon } from '@/components/shell/Icon'
import { InstitutionalTable, LedgerStrip, BinduButton } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'
import { api, ApiError } from '@/lib/api'
import { ensureDevSession, getLastLoginMs } from '@/lib/auth'

// Custody, not presence or obligation. The registry proves the institution owns
// things. PHASE 0: this is the live seam — real JWT login + GET /api/v1/assets,
// no fixtures. The first page in the app that reasons over reality.
export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'registry',
  signals: 1,
  risks: 0,
  narratives: 0,
  maps: 0,
  bindu: true,
}

// ── BFF contract (verified live, not assumed) ────────────────────────────────
// Ids may serialize as a strongly-typed wrapper ({ value }) or a bare string;
// enums as their integer ordinal or name. The mappers below tolerate both so a
// serializer-config change on the backend doesn't silently blank the table.
type Wrapped = string | { value: string }
interface AssetDto {
  id: Wrapped
  name: string
  serialNumber: string | null
  assetTag: string | null
  status: number | string
  condition: number | string
  categoryId: Wrapped
  purchaseValue: number
}
interface CategoryDto {
  id: Wrapped
  name: string
  requiresReturnOnOffboarding: boolean
}

const STATUS_LABELS = ['Available', 'Assigned', 'Under Maintenance', 'Retired', 'Lost']
const CONDITION_LABELS = ['New', 'Good', 'Fair', 'Poor']

const unwrap = (w: Wrapped): string => (typeof w === 'string' ? w : w?.value ?? '')
const label = (v: number | string, table: string[]): string =>
  typeof v === 'number' ? table[v] ?? String(v) : v

function statusTone(status: string): 'good' | 'caution' | 'neutral' | 'rakta-critical' {
  switch (status) {
    case 'Assigned': return 'good'
    case 'Under Maintenance': return 'caution'
    case 'Lost': return 'rakta-critical'
    default: return 'neutral'
  }
}

interface AssetRow {
  id: string
  tag: string
  name: string
  category: string
  status: string
  condition: string
  tone: 'good' | 'caution' | 'neutral' | 'rakta-critical'
}

export function AssetRegistry() {
  const [query, setQuery] = useState('')
  const [slice, setSlice] = useState('All')
  const [records, setRecords] = useState<AssetRow[]>([])
  const [categories, setCategories] = useState<string[]>(['All'])
  const [state, setState] = useState<'loading' | 'ready' | 'error'>('loading')
  const [error, setError] = useState<string>('')
  const [loginMs, setLoginMs] = useState<number | null>(null)
  const [fetchMs, setFetchMs] = useState<number | null>(null)

  useEffect(() => {
    let alive = true
    ;(async () => {
      try {
        setState('loading')
        await ensureDevSession()
        const t0 = performance.now()
        const [cats, assets] = await Promise.all([
          api.get<CategoryDto[]>('/api/v1/assets/categories'),
          api.get<AssetDto[]>('/api/v1/assets'),
        ])
        if (!alive) return
        setFetchMs(Math.round(performance.now() - t0))
        setLoginMs(getLastLoginMs())

        const catName = new Map(cats.map((c) => [unwrap(c.id), c.name]))
        const rows: AssetRow[] = assets.map((a) => {
          const status = label(a.status, STATUS_LABELS)
          return {
            id: unwrap(a.id),
            tag: a.assetTag ?? '—',
            name: a.name,
            category: catName.get(unwrap(a.categoryId)) ?? 'Uncategorized',
            status,
            condition: label(a.condition, CONDITION_LABELS),
            tone: statusTone(status),
          }
        })
        setRecords(rows)
        setCategories(['All', ...[...new Set(rows.map((r) => r.category))].sort()])
        setState('ready')
      } catch (e) {
        if (!alive) return
        const msg = e instanceof ApiError ? `${e.status} ${e.statusText}` : (e as Error).message
        setError(msg)
        setState('error')
      }
    })()
    return () => {
      alive = false
    }
  }, [])

  const rows = useMemo(() => {
    const q = query.trim().toLowerCase()
    return records.filter(
      (r) =>
        (slice === 'All' || r.category === slice) &&
        (!q || r.name.toLowerCase().includes(q) || r.tag.toLowerCase().includes(q) || r.id.toLowerCase().includes(q))
    )
  }, [query, slice, records])

  return (
    <>
      {/* Registry header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Custody Register · Things the Institution Owns
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Asset Registry
          </h2>
          <p className="font-mono text-[12px] text-on-surface-variant mt-3">
            <span className="text-primary">{state === 'ready' ? records.length : '—'}</span> TRACKED ASSETS ·{' '}
            {categories.length - 1} CATEGORIES · <span className="text-tamra-copper">LIVE</span>
          </p>
        </div>
        <BinduButton>Register Asset</BinduButton>
      </div>

      {/* Query bar */}
      <div className="flex flex-col md:flex-row gap-4 mb-8">
        <div className="flex items-center gap-2 hairline-all bg-surface-container-lowest px-4 py-2.5 md:w-96">
          <Icon name="search" className="!text-[18px] text-on-surface-variant" />
          <input
            className="flex-1 bg-transparent font-tabular-data text-tabular-data text-primary placeholder:text-on-surface-variant focus:outline-none"
            placeholder="Name, tag, or asset ID..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </div>
        <div className="flex flex-wrap gap-2">
          {categories.map((s) => (
            <button
              key={s}
              onClick={() => setSlice(s)}
              className={`px-3 py-2 font-mono-label text-mono-label uppercase tracking-wider transition-colors ${
                slice === s
                  ? 'bg-primary text-on-primary'
                  : 'hairline-all text-on-surface-variant hover:bg-surface-container-low'
              }`}
            >
              {s}
            </button>
          ))}
        </div>
      </div>

      {/* The register */}
      <div className="bg-surface-container-lowest hairline-all p-6">
        {state === 'loading' && (
          <p className="font-tabular-data text-tabular-data text-on-surface-variant py-8 text-center">
            Authenticating and loading the live register from the BFF...
          </p>
        )}
        {state === 'error' && (
          <div className="py-8 text-center">
            <p className="font-tabular-data text-tabular-data text-rakta-critical mb-2">
              Could not reach the live register: {error}
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
                  key: 'tag',
                  header: 'Asset Tag',
                  render: (r) => <span className="font-mono text-[12px] text-on-surface-variant">{r.tag}</span>,
                },
                {
                  key: 'name',
                  header: 'Asset',
                  render: (r) => (
                    <button
                      onClick={() => {
                        window.location.hash = `/asset-detail?id=${encodeURIComponent(r.id)}`
                      }}
                      className="text-primary font-medium hover:text-tamra-copper transition-colors text-left"
                    >
                      {r.name}
                    </button>
                  ),
                },
                {
                  key: 'category',
                  header: 'Category',
                  render: (r) => <span className="text-on-surface-variant">{r.category}</span>,
                },
                {
                  key: 'condition',
                  header: 'Condition',
                  render: (r) => <span className="text-on-surface-variant">{r.condition}</span>,
                },
                {
                  key: 'status',
                  header: 'Status',
                  align: 'right',
                  render: (r) => (
                    <>
                      <span className={`bindu ${r.tone} mr-2`} />
                      {r.status}
                    </>
                  ),
                },
              ]}
            />
            {rows.length === 0 && (
              <p className="font-tabular-data text-tabular-data text-on-surface-variant py-8 text-center">
                The live register holds no assets matching this query. Register one, or widen the filter.
              </p>
            )}
          </>
        )}
      </div>

      <LedgerStrip
        className="mt-8"
        entries={[
          { label: 'Showing', value: state === 'ready' ? `${rows.length} of ${records.length}` : '—' },
          { label: 'Source', value: 'GET /api/v1/assets (live)' },
          { label: 'Login', value: loginMs != null ? `${loginMs}ms` : 'cached' },
          { label: 'Fetch', value: fetchMs != null ? `${fetchMs}ms` : '—' },
        ]}
      />
    </>
  )
}
