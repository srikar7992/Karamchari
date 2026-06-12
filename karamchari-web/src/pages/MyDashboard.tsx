import { Icon } from '@/components/shell/Icon'
import { SignalCard, NarrativePlate, BinduButton } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EMP',
  archetype: 'dashboard',
  signals: 3,
  risks: 0,
  narratives: 1,
  maps: 1,
  bindu: true,
}

const timeline = [
  { time: '08:00', icon: 'flag', task: 'Shift Commencement', status: 'Completed', state: 'done' },
  { time: '09:30', icon: 'inventory_2', task: 'Inventory Audit', status: 'Completed', state: 'done' },
  { time: '11:00', icon: 'verified', task: 'Quality Assurance Loop', status: 'In Progress', state: 'active' },
  { time: '13:00', icon: 'restaurant', task: 'Sustenance Pause', status: 'Scheduled', state: 'todo' },
  { time: '15:30', icon: 'sync', task: 'Cross-Zone Handover Sync', status: 'Scheduled', state: 'todo' },
]

export function MyDashboard() {
  return (
    <>
      {/* Greeting header */}
      <header className="mb-12 border-b border-outline-variant pb-8 flex flex-col md:flex-row md:items-end justify-between gap-6">
        <div>
          <h2 className="font-serif font-light text-[44px] md:text-[56px] leading-[1.02] tracking-tight text-primary">
            Namaskaram, Asha.
          </h2>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mt-3">
            Operator Dashboard • Session 04
          </p>
        </div>
        <div className="flex items-center gap-3 hairline-all px-4 py-2 bg-surface-container-lowest">
          <span className="bindu copper" />
          <span className="font-mono-label text-mono-label text-tamra-copper uppercase">Active Shift</span>
          <span className="font-mono text-[13px] text-primary tabular-nums">04:12:33</span>
        </div>
      </header>

      {/* Operational pulse */}
      <section className="mb-12">
        <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-4">
          Operational Pulse
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-grid-12">
          <SignalCard
            label="Attendance Score"
            value="98.4%"
            cornerTick
            bar={{ pct: 98, tone: 'bg-sthira-forest' }}
          />
          <SignalCard
            label="Leave Balance"
            value="14"
            unit="Days"
            note="3 days expire 31 Dec. Plan ahead."
          />
          <SignalCard
            label="Today's Adherence"
            value="100%"
            bindu="good"
            note="All checkpoints geo-verified."
          />
        </div>
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-grid-12">
        {/* Shift timeline */}
        <section className="lg:col-span-8">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-4">
            Shift Timeline
          </h3>
          <div className="hairline-all bg-surface-container-lowest">
            <div className="grid grid-cols-12 gap-4 px-6 py-3 border-b border-outline-variant bg-surface-container-low">
              <div className="col-span-2 font-mono-label text-mono-label text-on-surface-variant uppercase">
                Time
              </div>
              <div className="col-span-7 font-mono-label text-mono-label text-on-surface-variant uppercase">
                Sutra / Task
              </div>
              <div className="col-span-3 font-mono-label text-mono-label text-on-surface-variant uppercase text-right">
                Status
              </div>
            </div>
            {timeline.map((row, i) => (
              <div
                key={row.task}
                className={`grid grid-cols-12 gap-4 px-6 py-4 items-center ${
                  i < timeline.length - 1 ? 'border-b border-outline-variant' : ''
                } ${row.state === 'active' ? 'bg-surface-container-low border-l-2 border-l-tamra-copper' : 'hover:bg-surface transition-colors'}`}
              >
                <div className="col-span-2 font-mono text-[13px] text-on-surface-variant tabular-nums">
                  {row.time}
                </div>
                <div className="col-span-7 flex items-center gap-3">
                  <Icon
                    name={row.icon}
                    className={`!text-[20px] ${row.state === 'active' ? 'text-tamra-copper' : 'text-on-surface-variant'}`}
                  />
                  <span
                    className={`font-body-standard text-body-standard text-primary ${row.state === 'active' ? 'font-bold' : ''}`}
                  >
                    {row.task}
                  </span>
                </div>
                <div className="col-span-3 text-right font-tabular-data text-tabular-data">
                  {row.state === 'done' && (
                    <span className="text-sthira-forest flex items-center justify-end gap-1">
                      <Icon name="check" className="!text-[14px]" /> Completed
                    </span>
                  )}
                  {row.state === 'active' && (
                    <span className="text-tamra-copper flex items-center justify-end gap-1">
                      <span className="bindu copper" /> In Progress
                    </span>
                  )}
                  {row.state === 'todo' && <span className="text-on-surface-variant">Scheduled</span>}
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* Right rail: Buddhi insight + recognition */}
        <div className="lg:col-span-4 flex flex-col gap-grid-12">
          <NarrativePlate actions={<BinduButton>Acknowledge</BinduButton>}>
            "Asha, your focus has been consistent this week. You have reached 92% of your learning goals."
          </NarrativePlate>

          <section className="hairline-all bg-surface-container-lowest p-6">
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-4">
              Recognition
            </h3>
            <div className="flex items-start gap-3">
              <Icon name="workspace_premium" className="text-tamra-copper !text-[22px] mt-0.5" />
              <div>
                <div className="font-body-standard text-body-standard text-primary font-medium">
                  Precision Badge awarded by Operations Lead.
                </div>
                <div className="font-mono-label text-mono-label text-on-surface-variant mt-2">YESTERDAY</div>
              </div>
            </div>
          </section>

          <section className="hairline-all bg-surface-container-lowest p-6">
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-4">
              Self-Governance
            </h3>
            <ul className="space-y-3 font-tabular-data text-tabular-data">
              <li className="flex justify-between items-center pb-2 border-b border-surface-variant border-dashed">
                <span className="text-primary">Request Leave</span>
                <Icon name="arrow_outward" className="!text-[16px] text-outline" />
              </li>
              <li className="flex justify-between items-center pb-2 border-b border-surface-variant border-dashed">
                <span className="text-primary">View Payslips</span>
                <Icon name="arrow_outward" className="!text-[16px] text-outline" />
              </li>
              <li className="flex justify-between items-center">
                <span className="text-primary">Update Skills Register</span>
                <Icon name="arrow_outward" className="!text-[16px] text-outline" />
              </li>
            </ul>
          </section>
        </div>
      </div>
    </>
  )
}
