import { SignalCard, InstitutionalTable, LedgerStrip, type TableColumn } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'SYS',
  archetype: 'administration',
  signals: 1,
  risks: 0,
  narratives: 0,
  maps: 0,
  bindu: false,
}

interface ShiftDef {
  code: string
  name: string
  window: string
  paidBreak: string
  grace: string
  rosters: number
  active: boolean
  effective: string
}

// Shifts are institutional primitives, not local exceptions. Variation belongs in
// parameters (grace, break, zone overrides), never in a fifth near-duplicate row.
const DEFINITIONS: ShiftDef[] = [
  { code: 'MORN', name: 'Morning', window: '06:00–14:00', paidBreak: '30 min', grace: '15 min', rosters: 142, active: true, effective: '2025-04-01' },
  { code: 'EVE', name: 'Evening', window: '14:00–22:00', paidBreak: '30 min', grace: '15 min', rosters: 118, active: true, effective: '2025-04-01' },
  { code: 'NGT', name: 'Night', window: '22:00–06:00', paidBreak: '45 min', grace: '20 min', rosters: 37, active: true, effective: '2025-04-01' },
  { code: 'GEN', name: 'General', window: '09:00–18:00', paidBreak: '60 min', grace: '10 min', rosters: 15, active: true, effective: '2025-07-01' },
]

const columns: TableColumn<ShiftDef>[] = [
  { key: 'code', header: 'Code', render: (d) => <span className="font-mono text-[12px] text-primary">{d.code}</span> },
  { key: 'name', header: 'Definition', render: (d) => <span className="text-primary">{d.name}</span> },
  { key: 'window', header: 'Window', render: (d) => <span className="font-mono text-[12px]">{d.window}</span> },
  { key: 'paidBreak', header: 'Paid Break' },
  { key: 'grace', header: 'Grace' },
  { key: 'rosters', header: 'Rosters Using', align: 'right', render: (d) => <span className="font-mono text-[12px]">{d.rosters}</span> },
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
          4 Primitives · 312 Rosters Governed
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-grid-12 mb-10">
        {/* The register */}
        <section className="md:col-span-8">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
            1. Definition Register
          </h3>
          <div className="bg-surface-container-lowest hairline-all p-6">
            <InstitutionalTable columns={columns} rows={DEFINITIONS} rowKey={(d) => d.code} />
            <p className="font-tabular-data text-[12px] text-on-surface-variant mt-6">
              New definitions require scheduling authority review. A request that differs from an
              existing primitive only in start time or break length is a parameter change, not a
              new definition; rosters override grace and break per zone without minting shifts.
            </p>
          </div>
        </section>

        {/* Editor + count */}
        <div className="md:col-span-4 flex flex-col gap-grid-12">
          <SignalCard
            label="Active Definitions"
            value="4"
            note="Unchanged for 11 months. Definition count is a governance metric; growth needs justification."
            bindu="good"
          />
          <section className="bg-surface-container-lowest hairline-all p-6 flex-1">
            <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-5 uppercase tracking-widest">
              2. Editor
            </h3>
            <div className="space-y-4 font-tabular-data text-tabular-data">
              {[
                { label: 'Code', value: 'MORN' },
                { label: 'Window', value: '06:00 – 14:00' },
                { label: 'Paid Break', value: '30 min' },
                { label: 'Grace Period', value: '15 min' },
                { label: 'Overtime After', value: '8.0 h' },
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
          hour derivation for all 312 rosters in the tenant.
        </p>
      </div>

      <LedgerStrip
        entries={[
          { label: 'Register', value: 'Scheduling.ShiftDefinitions' },
          { label: 'Last Change', value: 'GEN created 2025-07-01 · S. Menon' },
          { label: 'Version', value: 'v6' },
        ]}
      />
    </>
  )
}
