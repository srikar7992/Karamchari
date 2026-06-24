import { SignalCard, NarrativePlate, InstitutionalTable } from '@/components/institutional'
import type { TableColumn } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EMP',
  archetype: 'registry',
  signals: 3,
  risks: 0,
  narratives: 1,
  maps: 0,
  bindu: false,
}

// --- Data ---

interface Course { name: string; type: string; due: string; status: string; progress: number }
const ASSIGNED: Course[] = [
  { name: 'Safety & Compliance FY2026',   type: 'Compliance',  due: '2026-07-31', status: 'In Progress', progress: 60  },
  { name: 'Zone Operations Protocol v4',  type: 'Operations',  due: '2026-08-15', status: 'Not Started', progress: 0   },
  { name: 'Data Privacy Awareness',       type: 'Compliance',  due: '2026-07-15', status: 'Completed',   progress: 100 },
  { name: 'Leadership Foundations',       type: 'Development', due: '2026-09-30', status: 'In Progress', progress: 25  },
]

interface Cert { name: string; issued: string; expires: string; authority: string }
const CERTS: Cert[] = [
  { name: 'Zone Safety Certified',    issued: '2025-07-01', expires: '2026-07-01', authority: 'HSE-CERT-01' },
  { name: 'Data Privacy Compliance',  issued: '2026-06-01', expires: '2027-06-01', authority: 'DPC-CERT-22' },
]

// --- Helpers ---

const statusColor = (s: string) =>
  s === 'Completed'   ? 'text-sthira-forest' :
  s === 'In Progress' ? 'text-pita-caution'  :
  'text-on-surface-variant'

// --- Page ---

export function MyLearning() {
  const courseCols: TableColumn<Course>[] = [
    { key: 'name', header: 'Course' },
    { key: 'type', header: 'Type' },
    { key: 'due',  header: 'Due Date' },
    {
      key: 'status',
      header: 'Status',
      render: (r) => <span className={`font-mono-label text-mono-label uppercase ${statusColor(r.status)}`}>{r.status}</span>,
    },
    {
      key: 'progress',
      header: 'Progress',
      align: 'right',
      render: (r) => (
        <div className="flex items-center gap-2 justify-end">
          <div className="w-16 h-1 bg-surface-variant">
            <div className="h-1 bg-primary" style={{ width: `${r.progress}%` }} />
          </div>
          <span className="font-tabular-data text-tabular-data text-on-surface-variant w-8">{r.progress}%</span>
        </div>
      ),
    },
  ]
  const certCols: TableColumn<Cert>[] = [
    { key: 'name',      header: 'Certificate' },
    { key: 'issued',    header: 'Issued' },
    { key: 'expires',   header: 'Expires' },
    { key: 'authority', header: 'Authority' },
  ]

  const compliancePending = ASSIGNED.filter((c) => c.type === 'Compliance' && c.status !== 'Completed')

  return (
    <>
      <header className="mb-10 border-b border-outline-variant pb-8">
        <h2 className="font-serif font-light text-[28px] md:text-[44px] leading-[1.1] text-primary">
          My Learning
        </h2>
        <p className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mt-3">
          EMP-88271 · Q2 2026 · Compliance Cycle
        </p>
      </header>

      <section className="mb-10">
        <div className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-3 gap-grid-12">
          <SignalCard
            label="Completed Courses"
            value={String(ASSIGNED.filter((c) => c.status === 'Completed').length)}
            unit={`of ${ASSIGNED.length}`}
            cornerTick
          />
          <SignalCard label="Hours Logged" value="12.5" unit="hrs" note="YTD 2026" />
          <SignalCard label="Active Certificates" value={String(CERTS.length)} note="All valid" bindu="good" />
        </div>
      </section>

      <div className="space-y-10">
        <section>
          <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
            Assigned Training
          </h3>
          <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
            <InstitutionalTable columns={courseCols} rows={ASSIGNED} rowKey={(r) => r.name} />
          </div>
        </section>

        {compliancePending.length > 0 && (
          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-4">
              Compliance Deadlines
            </h3>
            <div className="space-y-3">
              {compliancePending.map((c) => (
                <div key={c.name} className="flex items-center justify-between hairline-all bg-surface-container-lowest px-6 py-4">
                  <div>
                    <div className="font-tabular-data text-tabular-data text-primary">{c.name}</div>
                    <div className="font-mono-label text-mono-label text-on-surface-variant uppercase mt-1">Due: {c.due}</div>
                  </div>
                  <span className={`font-mono-label text-mono-label uppercase ${statusColor(c.status)}`}>{c.status}</span>
                </div>
              ))}
            </div>
          </section>
        )}

        <section>
          <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
            Certificates
          </h3>
          <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
            <InstitutionalTable columns={certCols} rows={CERTS} rowKey={(r) => r.name} />
          </div>
        </section>

        <NarrativePlate kicker="Learning Summary" source="LMS-SUMMARY-01">
          Two compliance courses remain open for Q2 2026. Safety certification expires
          July 2026 — the renewal is already assigned. Development tracks run through
          Q3 at your own pace.
        </NarrativePlate>
      </div>
    </>
  )
}
