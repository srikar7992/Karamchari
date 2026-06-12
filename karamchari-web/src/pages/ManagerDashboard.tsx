import { PageHeader } from '@/components/shell/PageHeader'
import { Icon } from '@/components/shell/Icon'
import { InstitutionalTable } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'dashboard',
  signals: 5,
  risks: 3,
  narratives: 0,
  maps: 1,
  bindu: false,
}

const roster = [
  { name: 'Suresh Kumar', loc: 'Zone A - Floor 2', shift: '09:00 - 18:00', status: 'In', tone: 'good' },
  { name: 'Priya Sharma', loc: 'Zone B - Floor 1', shift: '09:00 - 18:00', status: 'In', tone: 'good' },
  { name: 'Rajesh Patel', loc: 'Remote', shift: '10:00 - 19:00', status: 'Late (14m)', tone: 'caution' },
  { name: 'Anita Desai', loc: 'Zone A - Floor 3', shift: '08:00 - 17:00', status: 'Absent', tone: 'rakta-critical' },
  { name: 'Vikram Singh', loc: 'Zone C - Floor 1', shift: '09:00 - 18:00', status: 'In', tone: 'good' },
  { name: 'Meera Krishnan', loc: 'Zone B - Floor 2', shift: '09:00 - 18:00', status: 'In', tone: 'good' },
]

const approvals = [
  { kind: 'LEAVE REQUEST', age: '2h ago', who: 'Ramesh D. • 2 Days (Sick)', detail: 'Oct 12 - Oct 13' },
  { kind: 'EXPENSE CLAIM', age: '5h ago', who: 'Meera K. • ₹4,500', detail: 'Client Dinner - Q3 Offsite' },
  { kind: 'SHIFT SWAP', age: '1d ago', who: 'Vikram S. ↔ Priya S.', detail: 'Oct 15, Evening Shift' },
]

const goals = [
  { name: 'Platform Migration v2.0', pct: 68, tone: 'bg-primary', note: 'On track. Database sharding complete.', pctTone: '' },
  { name: 'Reduce Defect Leakage', pct: 42, tone: 'bg-pita-caution', note: 'At risk. QA automation pipeline delayed by 2 weeks.', pctTone: 'text-pita-caution' },
  { name: 'API Documentation Coverage', pct: 95, tone: 'bg-sthira-forest', note: 'Completed ahead of schedule.', pctTone: 'text-sthira-forest' },
]

export function ManagerDashboard() {
  return (
    <>
      <PageHeader kicker="Overview" title="Team Pulse" />

      <div className="grid grid-cols-1 md:grid-cols-12 gap-grid-12">
        {/* Attendance Board */}
        <div className="col-span-1 md:col-span-8 bg-ivory-2 hairline-all p-6 plate-container flex flex-col h-[420px]">
          <div className="flex justify-between items-end mb-6 hairline-b pb-4">
            <div>
              <h3 className="font-pull-quote text-pull-quote text-primary">Attendance Board</h3>
              <p className="font-mono-label text-mono-label text-on-surface-variant mt-2">
                TODAY'S ROSTER • <span className="text-primary font-bold">42/45</span> PRESENT
              </p>
            </div>
            <button className="bg-transparent border border-outline-variant text-primary px-[22px] py-[9px] hover:bg-surface-container-high transition-colors font-mono-label text-mono-label">
              VIEW ALL
            </button>
          </div>
          <div className="flex-1 overflow-y-auto pr-2">
            <InstitutionalTable
              rowKey={(r) => r.name}
              rows={roster}
              columns={[
                { key: 'name', header: 'Associate' },
                {
                  key: 'loc',
                  header: 'Location',
                  render: (r) => <span className="text-on-surface-variant">{r.loc}</span>,
                },
                {
                  key: 'shift',
                  header: 'Shift',
                  render: (r) => <span className="text-on-surface-variant">{r.shift}</span>,
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
          </div>
        </div>

        {/* Action Queue */}
        <div className="col-span-1 md:col-span-4 bg-surface-container-lowest hairline-all p-6 plate-container flex flex-col h-[420px]">
          <div className="mb-6 hairline-b pb-4 flex justify-between items-center">
            <h3 className="font-pull-quote text-pull-quote text-primary !text-[24px]">Action Queue</h3>
            <span className="bg-primary text-on-primary font-mono-label text-mono-label px-2 py-1">
              7 Pending
            </span>
          </div>
          <div className="flex-1 overflow-y-auto space-y-4 pr-2">
            {approvals.map((a) => (
              <div
                key={a.kind + a.who}
                className="hairline-all p-4 bg-surface hover:bg-surface-container-low transition-colors cursor-pointer"
              >
                <div className="flex justify-between items-start mb-2">
                  <span className="font-mono-label text-mono-label text-on-surface-variant">{a.kind}</span>
                  <span className="text-xs text-on-surface-variant">{a.age}</span>
                </div>
                <p className="font-medium text-primary mb-1">{a.who}</p>
                <p className="text-sm text-on-surface-variant mb-4">{a.detail}</p>
                <div className="flex gap-2">
                  <button className="flex-1 bg-primary text-on-primary font-mono-label text-mono-label py-2 hover:opacity-90">
                    APPROVE
                  </button>
                  <button className="flex-1 bg-transparent border border-outline-variant text-primary font-mono-label text-mono-label py-2 hover:bg-surface-container-high">
                    REVIEW
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="h-8" />

      <div className="grid grid-cols-1 md:grid-cols-12 gap-grid-12">
        {/* Q3 Objectives */}
        <div className="col-span-1 md:col-span-7 bg-surface-container-lowest hairline-all p-6">
          <div className="mb-6">
            <h3 className="font-pull-quote text-pull-quote text-primary !text-[28px] mb-2">Q3 Objectives</h3>
            <p className="font-mono-label text-mono-label text-on-surface-variant uppercase">
              Engineering Delivery Pod Alpha
            </p>
          </div>
          <div className="space-y-6">
            {goals.map((g) => (
              <div key={g.name}>
                <div className="flex justify-between items-center mb-2">
                  <span className="font-tabular-data text-tabular-data font-medium">{g.name}</span>
                  <span className={`font-mono-label text-mono-label ${g.pctTone}`}>{g.pct}%</span>
                </div>
                <div className="h-2 w-full bg-surface-container-high rounded-full overflow-hidden">
                  <div className={`h-full ${g.tone}`} style={{ width: `${g.pct}%` }} />
                </div>
                <p className="text-sm text-on-surface-variant mt-2">{g.note}</p>
              </div>
            ))}
          </div>
        </div>

        {/* Risk Signals */}
        <div className="col-span-1 md:col-span-5 bg-yantra-indigo text-on-primary p-6 plate-container">
          <div className="mb-6">
            <h3 className="font-pull-quote text-pull-quote text-on-primary !text-[28px] mb-2">Risk Signals</h3>
            <p className="font-mono-label text-mono-label text-outline-variant uppercase">
              Capacity &amp; Burnout Analytics
            </p>
          </div>
          <div className="space-y-4">
            <div className="flex items-start gap-4 pb-4 border-b border-white/10">
              <Icon name="warning" className="text-tamra-copper-bright mt-1" />
              <div>
                <p className="font-medium">High Utilization Alert</p>
                <p className="text-sm text-outline-variant mt-1">
                  3 team members logging &gt;55hrs/week for the last 3 weeks.
                </p>
                <div className="mt-2 flex gap-2">
                  {['T. Chen', 'M. Singh', 'R. Das'].map((n) => (
                    <span key={n} className="text-xs bg-white/10 px-2 py-1">
                      {n}
                    </span>
                  ))}
                </div>
              </div>
            </div>
            <div className="flex items-start gap-4 pb-4 border-b border-white/10">
              <Icon name="schedule" className="text-pita-caution mt-1" />
              <div>
                <p className="font-medium">Leave Balances Expiring</p>
                <p className="text-sm text-outline-variant mt-1">
                  4 members have &gt;15 days accrued leave expiring in Dec.
                </p>
                <button className="mt-3 text-sm font-mono-label underline decoration-1 underline-offset-4 hover:text-outline-variant transition-colors">
                  INITIATE LEAVE PLAN
                </button>
              </div>
            </div>
            <div className="flex items-start gap-4">
              <Icon name="trending_down" className="text-sthira-forest mt-1" />
              <div>
                <p className="font-medium">Capacity Headroom</p>
                <p className="text-sm text-outline-variant mt-1">
                  Pod Beta operating at 71% utilization. 2 FTE of absorbable surge capacity available.
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  )
}
