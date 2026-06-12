import { useMemo, useState } from 'react'
import { Icon } from '@/components/shell/Icon'
import { InstitutionalTable, LedgerStrip, BinduButton } from '@/components/institutional'
import type { PageId } from '@/lib/nav'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'registry',
  signals: 1,
  risks: 0,
  narratives: 0,
  maps: 0,
  bindu: true,
}

interface DirectoryRow {
  id: string
  name: string
  position: string
  department: string
  location: string
  status: 'Active' | 'On Leave' | 'Notice Period'
  tone: 'good' | 'caution' | 'rakta-critical'
}

const records: DirectoryRow[] = [
  { id: 'EMP-8834-A', name: 'Elena Rostova', position: 'Sr. Technical Architect', department: 'Core Infrastructure', location: 'Zurich Node', status: 'Active', tone: 'good' },
  { id: 'EMP-8911-C', name: 'Suresh Kumar', position: 'Operations Associate', department: 'Zone A Operations', location: 'Hyderabad East', status: 'Active', tone: 'good' },
  { id: 'EMP-8740-B', name: 'Priya Sharma', position: 'QA Lead', department: 'Quality Systems', location: 'Hyderabad East', status: 'Active', tone: 'good' },
  { id: 'EMP-8602-A', name: 'Marcus Vance', position: 'VP, Infrastructure Operations', department: 'Core Infrastructure', location: 'Zurich Node', status: 'Active', tone: 'good' },
  { id: 'EMP-9012-D', name: 'Anita Desai', position: 'Operations Associate', department: 'Zone A Operations', location: 'Hyderabad East', status: 'On Leave', tone: 'caution' },
  { id: 'EMP-8455-B', name: 'Rajesh Patel', position: 'Platform Engineer', department: 'Core Infrastructure', location: 'Remote', status: 'Active', tone: 'good' },
  { id: 'EMP-8290-C', name: 'Meera Krishnan', position: 'Payroll Specialist', department: 'Financial Operations', location: 'Hyderabad East', status: 'Active', tone: 'good' },
  { id: 'EMP-8821-E', name: 'David Patel', position: 'Sr. Eng Manager', department: 'Platform Engineering', location: 'Zurich Node', status: 'Active', tone: 'good' },
  { id: 'EMP-798-A', name: 'Tomas Chen', position: 'Site Reliability Engineer', department: 'Core Infrastructure', location: 'Remote', status: 'Notice Period', tone: 'rakta-critical' },
  { id: 'EMP-9101-B', name: 'Asha Venkatesan', position: 'Operations Associate', department: 'Zone A Operations', location: 'Hyderabad East', status: 'Active', tone: 'good' },
]

const slices = ['All', 'Core Infrastructure', 'Zone A Operations', 'Financial Operations', 'Platform Engineering', 'Quality Systems']

export function EmployeeDirectory({ onNavigate }: { onNavigate: (id: PageId) => void }) {
  const [query, setQuery] = useState('')
  const [slice, setSlice] = useState('All')

  const rows = useMemo(() => {
    const q = query.trim().toLowerCase()
    return records.filter(
      (r) =>
        (slice === 'All' || r.department === slice) &&
        (!q || r.name.toLowerCase().includes(q) || r.id.toLowerCase().includes(q) || r.position.toLowerCase().includes(q))
    )
  }, [query, slice])

  return (
    <>
      {/* Registry header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Workforce Register · People &amp; Places
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Employee Directory
          </h2>
          <p className="font-mono text-[12px] text-on-surface-variant mt-3">
            <span className="text-primary">1,247</span> ACTIVE RECORDS · 47 JURISDICTIONS
          </p>
        </div>
        <BinduButton>Onboard Operator</BinduButton>
      </div>

      {/* Query bar */}
      <div className="flex flex-col md:flex-row gap-4 mb-8">
        <div className="flex items-center gap-2 hairline-all bg-surface-container-lowest px-4 py-2.5 md:w-96">
          <Icon name="search" className="!text-[18px] text-on-surface-variant" />
          <input
            className="flex-1 bg-transparent font-tabular-data text-tabular-data text-primary placeholder:text-on-surface-variant focus:outline-none"
            placeholder="Name, ID, or position..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </div>
        <div className="flex flex-wrap gap-2">
          {slices.map((s) => (
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
        <InstitutionalTable
          rowKey={(r) => r.id}
          rows={rows}
          columns={[
            {
              key: 'id',
              header: 'Internal ID',
              render: (r) => <span className="font-mono text-[12px] text-on-surface-variant">{r.id}</span>,
            },
            {
              key: 'name',
              header: 'Operator',
              render: (r) => (
                <button
                  onClick={() => onNavigate('employee-360')}
                  className="text-primary font-medium hover:text-tamra-copper transition-colors text-left"
                >
                  {r.name}
                </button>
              ),
            },
            { key: 'position', header: 'Position' },
            {
              key: 'department',
              header: 'Department',
              render: (r) => <span className="text-on-surface-variant">{r.department}</span>,
            },
            {
              key: 'location',
              header: 'Location',
              render: (r) => <span className="text-on-surface-variant">{r.location}</span>,
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
            No records match the current query. The register is complete; the filter is narrow.
          </p>
        )}
      </div>

      <LedgerStrip
        className="mt-8"
        entries={[
          { label: 'Showing', value: `${rows.length} of 1,247` },
          { label: 'Source', value: 'EmployeeListProjection' },
          { label: 'Synced', value: '2026-06-12T08:02:44Z' },
        ]}
      />
    </>
  )
}
