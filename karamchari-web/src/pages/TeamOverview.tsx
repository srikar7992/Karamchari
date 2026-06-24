import { SignalCard, NarrativePlate, RiskCard, InstitutionalTable } from '@/components/institutional'
import type { TableColumn } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'registry',
  signals: 4,
  risks: 1,
  narratives: 1,
  maps: 0,
  bindu: false,
}

// --- Data ---

interface TeamMember {
  empId: string
  name: string
  role: string
  grade: string
  location: string
  tenureYrs: number
  status: string
}
const TEAM: TeamMember[] = [
  { empId: 'EMP-88271', name: 'Asha Venkatesan',    role: 'Zone Operator',       grade: 'G4', location: 'Zone A', tenureYrs: 3.1, status: 'Active' },
  { empId: 'EMP-88350', name: 'Rajiv Mohan',         role: 'Zone Operator',       grade: 'G4', location: 'Zone B', tenureYrs: 1.8, status: 'Active' },
  { empId: 'EMP-88412', name: 'Priya Sharma',         role: 'Sr. Zone Operator',   grade: 'G5', location: 'Zone A', tenureYrs: 5.2, status: 'Active' },
  { empId: 'EMP-88490', name: 'Kiran Nair',           role: 'Shift Lead',          grade: 'G6', location: 'Zone C', tenureYrs: 7.4, status: 'Active' },
  { empId: 'EMP-88521', name: 'Deepika Rao',          role: 'Zone Operator',       grade: 'G4', location: 'Zone B', tenureYrs: 0.9, status: 'Probation' },
  { empId: 'EMP-88600', name: 'Suresh Kumar',         role: 'Zone Operator',       grade: 'G4', location: 'Zone A', tenureYrs: 2.3, status: 'Active' },
  { empId: 'EMP-88644', name: 'Meena Pillai',         role: 'Operations Analyst',  grade: 'G5', location: 'HQ',    tenureYrs: 4.0, status: 'Active' },
  { empId: 'EMP-88701', name: 'Arun Krishnamurthy',   role: 'Shift Lead',          grade: 'G6', location: 'Zone C', tenureYrs: 6.1, status: 'Active' },
]

interface DeptRow { dept: string; headcount: number; avgTenure: string; openRoles: number }
const DEPTS: DeptRow[] = [
  { dept: 'Zone Operations', headcount: 6, avgTenure: '2.7 yrs', openRoles: 1 },
  { dept: 'Shift Leadership', headcount: 2, avgTenure: '6.8 yrs', openRoles: 0 },
  { dept: 'Operations Analytics', headcount: 1, avgTenure: '4.0 yrs', openRoles: 1 },
]

// --- Page ---

export function TeamOverview() {
  const memberCols: TableColumn<TeamMember>[] = [
    { key: 'empId',     header: 'ID' },
    { key: 'name',      header: 'Name' },
    { key: 'role',      header: 'Role' },
    { key: 'grade',     header: 'Grade' },
    { key: 'location',  header: 'Location' },
    {
      key: 'tenureYrs',
      header: 'Tenure',
      align: 'right',
      render: (r) => <span className="font-tabular-data text-tabular-data">{r.tenureYrs} yrs</span>,
    },
    {
      key: 'status',
      header: 'Status',
      render: (r) => (
        <span className={`font-mono-label text-mono-label uppercase ${
          r.status === 'Probation' ? 'text-pita-caution' : 'text-sthira-forest'
        }`}>{r.status}</span>
      ),
    },
  ]
  const deptCols: TableColumn<DeptRow>[] = [
    { key: 'dept',       header: 'Department' },
    { key: 'headcount',  header: 'Headcount',  align: 'right' },
    { key: 'avgTenure',  header: 'Avg Tenure' },
    {
      key: 'openRoles',
      header: 'Open Roles',
      align: 'right',
      render: (r) => (
        <span className={`font-tabular-data text-tabular-data ${r.openRoles > 0 ? 'text-pita-caution' : 'text-on-surface-variant'}`}>
          {r.openRoles}
        </span>
      ),
    },
  ]

  const activeCount = TEAM.filter((m) => m.status === 'Active').length
  const newHires = TEAM.filter((m) => m.tenureYrs < 1).length
  const openRoles = DEPTS.reduce((a, d) => a + d.openRoles, 0)

  return (
    <>
      <header className="mb-10 border-b border-outline-variant pb-8">
        <h2 className="font-serif font-light text-[28px] md:text-[44px] leading-[1.1] text-primary">
          Team Overview
        </h2>
        <p className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mt-3">
          MGR-22001 · Ananya Rajesh · Regional Director · June 2026
        </p>
      </header>

      <section className="mb-10">
        <div className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-4 gap-grid-12">
          <SignalCard
            label="Total Headcount"
            value={String(TEAM.length)}
            unit="direct reports"
            cornerTick
          />
          <SignalCard label="Active" value={String(activeCount)} note="Confirmed active" />
          <SignalCard label="New Hires" value={String(newHires)} unit="< 1 yr" note="Q2 2026" />
          <SignalCard
            label="Open Roles"
            value={String(openRoles)}
            note="Against approved headcount"
            bindu={openRoles > 0 ? 'caution' : 'good'}
          />
        </div>
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-grid-12">
        <div className="lg:col-span-2 space-y-10">
          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Team Roster
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={memberCols} rows={TEAM} rowKey={(r) => r.empId} />
            </div>
          </section>

          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Department Breakdown
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={deptCols} rows={DEPTS} rowKey={(r) => r.dept} />
            </div>
          </section>
        </div>

        <div className="space-y-8">
          <RiskCard
            severity="monitored"
            title="Attrition Risk"
            value="2"
            body="Two members show early flight-risk signals based on tenure velocity and engagement survey scores. Recommended: 1:1 retention check-in within 14 days."
          />
          <NarrativePlate kicker="Team Composition" source="TEAM-RPT-001">
            Team of {TEAM.length} spans three zones and one HQ function. Avg tenure is 3.8 yrs.
            Two open roles are under active sourcing. Probationary headcount is within policy.
          </NarrativePlate>
        </div>
      </div>
    </>
  )
}
