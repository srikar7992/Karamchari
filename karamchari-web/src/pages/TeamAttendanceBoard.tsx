import {
  SignalCard,
  RiskCard,
  NarrativePlate,
  MapSurface,
  BinduButton,
  LedgerStrip,
} from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'command-center',
  signals: 4,
  risks: 3,
  narratives: 1,
  maps: 1,
  bindu: true,
}

// Zone → coverage → people. The map is operational topology, not GIS.
interface Zone {
  name: string
  present: number
  min: number
  rostered: number
  people: { initials: string; state: 'present' | 'late' | 'absent' | 'leave' }[]
}

const ZONES: Zone[] = [
  {
    name: 'Zone A · Floor',
    present: 14,
    min: 12,
    rostered: 15,
    people: [
      { initials: 'AV', state: 'present' },
      { initials: 'RD', state: 'present' },
      { initials: 'MK', state: 'present' },
      { initials: 'SP', state: 'late' },
      { initials: 'JT', state: 'present' },
    ],
  },
  {
    name: 'Zone B · Dispatch',
    present: 11,
    min: 10,
    rostered: 12,
    people: [
      { initials: 'VS', state: 'present' },
      { initials: 'PS', state: 'present' },
      { initials: 'NK', state: 'leave' },
      { initials: 'AB', state: 'present' },
    ],
  },
  {
    name: 'Zone East · Intake',
    present: 2,
    min: 3,
    rostered: 4,
    people: [
      { initials: 'VR', state: 'absent' },
      { initials: 'LD', state: 'present' },
      { initials: 'TG', state: 'present' },
      { initials: 'HM', state: 'leave' },
    ],
  },
  {
    name: 'Control Room',
    present: 4,
    min: 4,
    rostered: 4,
    people: [
      { initials: 'KR', state: 'present' },
      { initials: 'DS', state: 'present' },
      { initials: 'FA', state: 'present' },
      { initials: 'QW', state: 'present' },
    ],
  },
]

const STATE_DOT = {
  present: 'bg-sthira-forest',
  late: 'bg-pita-caution',
  absent: 'bg-rakta-critical',
  leave: 'bg-outline-variant',
} as const

export function TeamAttendanceBoard() {
  return (
    <>
      {/* State header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Shift Day <span className="font-mono">2026-06-12</span> · Morning 06:00–14:00
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Team Attendance Board
          </h2>
        </div>
        <div className="flex items-center gap-3 font-mono text-[12px] text-on-surface-variant uppercase tracking-widest">
          <span className="bindu caution" />
          Shift Active · 52 Rostered
        </div>
      </div>

      {/* Presence signals */}
      <section className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-grid-12 mb-10">
        <SignalCard label="Present" value="41" note="Checked in across 4 zones" bindu="good" />
        <SignalCard label="Absent" value="3" note="1 without leave record" bindu="rakta-critical" />
        <SignalCard label="Late" value="2" note="Within 30-minute grace window" bindu="caution" />
        <SignalCard label="On Leave" value="6" note="All approved before cutoff" bindu="neutral" />
      </section>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-grid-12 mb-10">
        {/* Coverage topology */}
        <div className="md:col-span-7">
          <MapSurface
            kicker="Coverage Topology · Morning Shift"
            axes={{ x: 'Zone', y: 'Present / Minimum' }}
          >
            <div className="space-y-5">
              {ZONES.map((z) => {
                const under = z.present < z.min
                return (
                  <div key={z.name} className="pb-4 hairline-b last:border-b-0 last:pb-0">
                    <div className="flex justify-between items-baseline mb-2">
                      <span className="font-tabular-data text-tabular-data text-primary">{z.name}</span>
                      <span className={`font-mono text-[12px] ${under ? 'text-rakta-critical' : 'text-on-surface-variant'}`}>
                        {z.present}/{z.min} MIN · {z.rostered} ROSTERED
                      </span>
                    </div>
                    <div className="w-full bg-surface-variant h-1 mb-3">
                      <div
                        className={`h-1 ${under ? 'bg-rakta-critical' : 'bg-sthira-forest'}`}
                        style={{ width: `${Math.min(100, (z.present / z.min) * 100)}%` }}
                      />
                    </div>
                    <div className="flex gap-2 flex-wrap">
                      {z.people.map((p) => (
                        <span
                          key={p.initials}
                          className="flex items-center gap-1.5 px-2 py-1 hairline-all font-mono text-[10px] text-primary"
                        >
                          <span className={`w-1.5 h-1.5 rounded-full ${STATE_DOT[p.state]}`} />
                          {p.initials}
                        </span>
                      ))}
                    </div>
                  </div>
                )
              })}
            </div>
          </MapSurface>
        </div>

        {/* Risk column */}
        <div className="md:col-span-5 bg-surface-container-lowest hairline-all p-6 space-y-6">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest">
            Emergent Risks
          </h3>
          <RiskCard
            severity="critical"
            title="Coverage — Zone East"
            value="2/3"
            body="Below minimum since 06:40. Both available floaters lack intake certification; skill gap blocks direct substitution."
          />
          <RiskCard
            severity="elevated"
            title="No-Show — V. Rao"
            body="No check-in 45 minutes after shift start. No leave record on file. Contact attempt logged 06:52."
          />
          <RiskCard
            severity="monitored"
            title="Geo Variance — 1 Check-in"
            body="S. Pillai checked in 40 m outside Zone A fence. Device GPS drift suspected; regularization REG-2026-0712 routed."
          />
        </div>
      </div>

      {/* Narrative + decisive act */}
      <NarrativePlate
        source="AttendanceProjection v218 · computed 2026-06-12T07:58:04Z"
        actions={<BinduButton>Assign Replacement</BinduButton>}
        className="mb-10"
      >
        Morning coverage holds in three of four zones. Zone East runs below minimum with two
        certified operators unavailable, and neither floater carries intake certification.
        If a certified replacement is not assigned before 14:00, the evening handover
        inherits the gap and overtime exposure becomes probable.
      </NarrativePlate>

      <LedgerStrip
        entries={[
          { label: 'Feed', value: 'AttendanceProjection v218' },
          { label: 'Hub', value: '/hubs/attendance · connected' },
          { label: 'Synced', value: '2026-06-12T08:02:31Z' },
        ]}
      />
    </>
  )
}
