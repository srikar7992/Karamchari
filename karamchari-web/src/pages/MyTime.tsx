import { SignalCard, NarrativePlate, RiskCard, InstitutionalTable, BinduButton } from '@/components/institutional'
import type { TableColumn } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EMP',
  archetype: 'dashboard',
  signals: 3,
  risks: 1,
  narratives: 1,
  maps: 0,
  bindu: true,
}

// --- Data ---

interface LeaveBalance { type: string; entitled: number; taken: number; balance: number; expires: string }
const LEAVE_BALANCES: LeaveBalance[] = [
  { type: 'Casual Leave',     entitled: 12, taken: 4, balance: 8,  expires: '2026-12-31' },
  { type: 'Sick Leave',       entitled: 12, taken: 2, balance: 10, expires: '2026-12-31' },
  { type: 'Earned Leave',     entitled: 21, taken: 5, balance: 16, expires: '2027-03-31' },
  { type: 'Compensatory Off', entitled: 0,  taken: 0, balance: 2,  expires: '2026-09-30' },
]

interface LeaveRequest { id: string; type: string; from: string; to: string; days: number; status: string }
const LEAVE_REQUESTS: LeaveRequest[] = [
  { id: 'LV-2026-018', type: 'Casual Leave', from: '2026-07-10', to: '2026-07-11', days: 2, status: 'Pending' },
  { id: 'LV-2026-012', type: 'Sick Leave',   from: '2026-06-05', to: '2026-06-06', days: 2, status: 'Approved' },
  { id: 'LV-2026-007', type: 'Earned Leave', from: '2026-05-01', to: '2026-05-05', days: 5, status: 'Approved' },
]

interface AttendanceWeek { week: string; present: number; halfDay: number; late: number; status: string }
const ATTENDANCE: AttendanceWeek[] = [
  { week: '16–20 Jun 2026', present: 5, halfDay: 0, late: 0, status: 'Full' },
  { week: '09–13 Jun 2026', present: 5, halfDay: 0, late: 1, status: 'Late Mark' },
  { week: '02–06 Jun 2026', present: 4, halfDay: 1, late: 0, status: 'Half Day' },
]

// --- Helpers ---

const leaveStatusColor = (s: string) =>
  s === 'Approved' ? 'text-sthira-forest' :
  s === 'Pending'  ? 'text-pita-caution'  :
  'text-on-surface-variant'

// --- Page ---

export function MyTime() {
  const balanceCols: TableColumn<LeaveBalance>[] = [
    { key: 'type',     header: 'Leave Type' },
    { key: 'entitled', header: 'Entitled', align: 'right' },
    { key: 'taken',    header: 'Taken',    align: 'right' },
    {
      key: 'balance',
      header: 'Balance',
      align: 'right',
      render: (r) => <span className="font-tabular-data text-tabular-data text-sthira-forest">{r.balance}</span>,
    },
    { key: 'expires', header: 'Expires' },
  ]
  const requestCols: TableColumn<LeaveRequest>[] = [
    { key: 'id',   header: 'Reference' },
    { key: 'type', header: 'Type' },
    { key: 'from', header: 'From' },
    { key: 'to',   header: 'To' },
    { key: 'days', header: 'Days', align: 'right' },
    {
      key: 'status',
      header: 'Status',
      render: (r) => (
        <span className={`font-mono-label text-mono-label uppercase ${leaveStatusColor(r.status)}`}>{r.status}</span>
      ),
    },
  ]
  const attendanceCols: TableColumn<AttendanceWeek>[] = [
    { key: 'week',     header: 'Week' },
    { key: 'present',  header: 'Present',   align: 'right' },
    { key: 'halfDay',  header: 'Half Days', align: 'right' },
    { key: 'late',     header: 'Late Marks', align: 'right' },
    { key: 'status',   header: 'Status' },
  ]

  const totalBalance = LEAVE_BALANCES.reduce((acc, l) => acc + l.balance, 0)
  const pendingCount = LEAVE_REQUESTS.filter((r) => r.status === 'Pending').length

  return (
    <>
      <header className="mb-10 border-b border-outline-variant pb-8 flex items-end justify-between gap-6 flex-wrap">
        <div>
          <h2 className="font-serif font-light text-[28px] md:text-[44px] leading-[1.1] text-primary">
            My Time
          </h2>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mt-3">
            EMP-88271 · June 2026 · 21 Working Days
          </p>
        </div>
        <BinduButton>
          Apply for Leave
        </BinduButton>
      </header>

      <section className="mb-10">
        <div className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-3 gap-grid-12">
          <SignalCard
            label="Attendance Rate"
            value="94.3%"
            bar={{ pct: 94, tone: 'bg-sthira-forest' }}
            note="Jun 2026 · 20 of 21 days"
            cornerTick
          />
          <SignalCard
            label="Leave Balance"
            value={String(totalBalance)}
            unit="days"
            note="Total across all types"
          />
          <SignalCard
            label="Overtime Hours"
            value="8.5"
            unit="hrs"
            note="Jun 2026 · pending sign-off"
          />
        </div>
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-grid-12">
        <div className="lg:col-span-2 space-y-10">
          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Attendance — June 2026
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={attendanceCols} rows={ATTENDANCE} rowKey={(r) => r.week} />
            </div>
          </section>

          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Leave Balances
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={balanceCols} rows={LEAVE_BALANCES} rowKey={(r) => r.type} />
            </div>
          </section>

          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Leave Requests
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={requestCols} rows={LEAVE_REQUESTS} rowKey={(r) => r.id} />
            </div>
          </section>
        </div>

        <div className="space-y-8">
          <RiskCard
            severity="monitored"
            title="Pending Approval"
            value={String(pendingCount)}
            body="LV-2026-018 awaiting manager approval. 2 days Casual Leave from Jul 10. SLA: 48h from submission under LV-SLA-02."
          />
          <NarrativePlate kicker="Time Summary" source="TIME-SUMMARY-01">
            Attendance rate of 94.3% is above the departmental average. Compensatory Off
            balance expires September 2026 — plan consumption before then. Overtime
            from June is pending sign-off under LV-SLA-02.
          </NarrativePlate>
        </div>
      </div>
    </>
  )
}
