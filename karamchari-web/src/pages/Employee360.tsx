import { Icon } from '@/components/shell/Icon'
import {
  SignalCard,
  RiskCard,
  NarrativePlate,
  BinduButton,
  MapSurface,
  CaseTimeline,
  LedgerStrip,
  type TimelineEntry,
} from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'record-detail',
  signals: 4,
  risks: 1,
  narratives: 1,
  maps: 1,
  bindu: true,
}

const skills = [
  { name: 'Distributed Systems Design', level: 5 },
  { name: 'Cloud Infrastructure (AWS/GCP)', level: 4 },
  { name: 'Technical Leadership', level: 3 },
  { name: 'Go / Rust Engineering', level: 4 },
]

const history: TimelineEntry[] = [
  {
    ts: '2026-04-02T10:15:00Z',
    tag: 'COMP_REV',
    tone: 'neutral',
    body: 'Annual compensation revision applied. Base CHF 178,000 → CHF 185,000. Approved by M. Vance.',
  },
  {
    ts: '2026-01-19T09:00:00Z',
    tag: 'RECOG',
    tone: 'copper',
    body: 'Q1 Infrastructure Excellence Award conferred by CTO for the database migration protocol.',
  },
  {
    ts: '2025-08-11T14:30:00Z',
    tag: 'SKILL_VAL',
    tone: 'good',
    body: 'Skill validated: Distributed Systems Design, level 5. Evidence: migration design review board.',
  },
  {
    ts: '2024-03-01T08:00:00Z',
    tag: 'POS_ASSIGN',
    tone: 'neutral',
    body: 'Assigned to position Senior Technical Architect, Core Infrastructure. Reports to VP, Infrastructure Operations.',
  },
  {
    ts: '2022-02-14T08:00:00Z',
    tag: 'ONBOARD',
    tone: 'good',
    body: 'Onboarding case completed in 11 days. All documents verified; assets issued.',
  },
]

export function Employee360() {
  return (
    <>
      {/* Dossier header */}
      <div className="mb-12">
        <div className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-4 flex items-center">
          <Icon name="arrow_back" className="!text-[14px] mr-2" />
          <a className="hover:text-primary transition-colors cursor-pointer">Directory</a>
          <span className="mx-2">/</span> Operator Dossier
        </div>
        <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
          <div>
            <h2 className="font-section-title text-section-title !text-4xl md:!text-section-title text-primary tracking-tight mb-2">
              Elena Rostova
            </h2>
            <p className="font-pull-quote text-pull-quote text-on-surface-variant italic">
              Senior Technical Architect
            </p>
          </div>
          <div className="flex gap-4">
            <button className="px-[22px] py-[14px] bg-transparent border border-outline-variant text-primary font-tabular-data text-tabular-data hover:bg-surface-container-low transition-colors flex items-center">
              <Icon name="edit" className="!text-[18px] mr-2" /> Modify Record
            </button>
            <button className="px-[22px] py-[14px] bg-primary text-on-primary font-tabular-data text-tabular-data hover:bg-graphite transition-colors flex items-center">
              <Icon name="mail" className="!text-[18px] mr-2" /> Contact Operator
            </button>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-grid-12">
        {/* Identity rail */}
        <div className="md:col-span-4">
          <div className="bg-surface-container-lowest hairline-all corner-tick relative p-6 h-full min-h-[300px] flex flex-col justify-between">
            <div>
              <div className="flex justify-between items-start mb-6">
                <div className="font-mono-label text-mono-label text-on-surface-variant uppercase">
                  Identity Status
                </div>
                <div className="flex items-center gap-2 font-mono-label text-mono-label text-sthira-forest">
                  <span className="bindu good" /> Active
                </div>
              </div>
              <div className="aspect-square w-32 bg-ivory-2 hairline-all mb-6 flex items-center justify-center">
                <span className="font-serif font-light text-[44px] text-graphite tracking-tight">ER</span>
              </div>
              <div className="space-y-4 font-tabular-data text-tabular-data">
                <div>
                  <div className="text-on-surface-variant text-[12px] mb-1">Internal ID</div>
                  <div className="text-primary font-medium tracking-wide font-mono text-[13px]">EMP-8834-A</div>
                </div>
                <div>
                  <div className="text-on-surface-variant text-[12px] mb-1">Department</div>
                  <div className="text-primary">Core Infrastructure</div>
                </div>
                <div>
                  <div className="text-on-surface-variant text-[12px] mb-1">Base Location</div>
                  <div className="text-primary flex items-center">
                    <Icon name="location_on" className="!text-[16px] mr-1 text-on-surface-variant" />
                    Zurich Node (Hybrid)
                  </div>
                </div>
                <div>
                  <div className="text-on-surface-variant text-[12px] mb-1">Assets Held</div>
                  <div className="text-primary">3 (Laptop, Token, Badge)</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Chain of command + compensation */}
        <div className="md:col-span-8 flex flex-col gap-grid-12">
          <MapSurface kicker="Chain of Command">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-6 font-tabular-data text-tabular-data">
              <div className="flex flex-col">
                <span className="text-[12px] text-on-surface-variant mb-1">Reports To</span>
                <div className="flex items-center gap-3">
                  <div className="w-8 h-8 bg-surface-container-high rounded-full hairline-all flex items-center justify-center text-[10px] font-medium">
                    MV
                  </div>
                  <div>
                    <div className="font-medium text-primary">Marcus Vance</div>
                    <div className="text-[12px] text-on-surface-variant">VP, Infrastructure Operations</div>
                  </div>
                </div>
              </div>
              <div className="h-px w-16 bg-outline-variant hidden sm:block" />
              <div className="flex flex-col">
                <span className="text-[12px] text-on-surface-variant mb-1">Direct Reports</span>
                <div className="flex -space-x-2">
                  {['MR', 'AL', 'TJ'].map((m, i) => (
                    <div
                      key={m}
                      className="w-8 h-8 bg-surface-container-high rounded-full hairline-all flex items-center justify-center text-[10px] font-medium"
                      style={{ zIndex: 30 - i * 10 }}
                    >
                      {m}
                    </div>
                  ))}
                  <div className="w-8 h-8 bg-surface-variant text-on-surface-variant rounded-full hairline-all flex items-center justify-center text-[10px] font-medium">
                    +2
                  </div>
                </div>
              </div>
            </div>
          </MapSurface>

          <div className="bg-surface-container-low hairline-all p-6 flex-1 relative flex flex-col justify-between">
            <div className="flex justify-between items-end mb-8 border-b border-outline-variant pb-4">
              <div>
                <div className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-2">
                  Compensation Vector
                </div>
                <div className="font-pull-quote text-pull-quote text-primary tracking-tight">
                  Tier 4 / Sr. Architect
                </div>
              </div>
              <div className="text-right font-mono text-[10px] uppercase tracking-wider text-on-surface-variant">
                Band P50–P75
              </div>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-6 font-tabular-data text-tabular-data">
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Base Salary</div>
                <div className="text-primary font-medium tracking-wide">CHF 185,000</div>
              </div>
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Target Bonus</div>
                <div className="text-primary font-medium">20%</div>
              </div>
              <div>
                <div className="text-[12px] text-on-surface-variant mb-1">Equity Vesting (Current Year)</div>
                <div className="text-primary font-medium">2,400 RSUs</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Signal row */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-grid-12 mt-grid-12">
        <SignalCard label="Tenure" value="4.2" unit="Years" note="In current role since Mar 2024." />
        <SignalCard
          label="Compa-Ratio"
          value="1.12"
          bindu="good"
          note="Healthy against Tier 4 band median."
        />
        <SignalCard
          label="Attendance Score"
          value="98.4"
          unit="%"
          bar={{ pct: 98, tone: 'bg-sthira-forest' }}
        />
        <SignalCard label="Leave Balance" value="14" unit="Days" note="3 days expire 31 Dec." />
      </div>

      {/* Capability + risk/narrative */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-grid-12 mt-grid-12">
        <div className="bg-surface-container-lowest hairline-all p-6 relative">
          <div className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-6 border-b border-outline-variant pb-2 flex justify-between">
            <span>Capability Matrix</span>
            <Icon name="tune" className="!text-[16px]" />
          </div>
          <ul className="space-y-4 font-tabular-data text-tabular-data">
            {skills.map((s) => (
              <li
                key={s.name}
                className="flex items-center justify-between pb-2 border-b border-surface-variant border-dashed"
              >
                <span className="text-primary">{s.name}</span>
                <span className="flex gap-1">
                  {[1, 2, 3, 4, 5].map((n) => (
                    <span key={n} className={`w-6 h-1 ${n <= s.level ? 'bg-primary' : 'bg-surface-variant'}`} />
                  ))}
                </span>
              </li>
            ))}
          </ul>
          <div className="font-tabular-data text-tabular-data mt-6 pt-4 hairline-t flex items-start gap-3">
            <Icon name="workspace_premium" className="text-tamra-copper !text-[20px] mt-1" />
            <div>
              <div className="text-primary font-medium">Q1 Infrastructure Excellence Award</div>
              <div className="text-on-surface-variant text-[12px]">
                Awarded by CTO for flawless execution of the database migration protocol.
              </div>
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-grid-12">
          <div className="bg-surface-container-lowest hairline-all p-6">
            <div className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-6 border-b border-outline-variant pb-2">
              Retention Analytics
            </div>
            <RiskCard
              severity="elevated"
              title="Flight Risk — Elevated"
              body="4.2 years tenure in current role. Compa-ratio healthy, but peer market velocity for Sr. Architects indicates risk. Top drivers: role tenure (0.41), market demand (0.33), manager span (0.12)."
            />
          </div>
          <NarrativePlate
            source="Buddhi · drivers from EmployeeFlightRiskPrediction, 2026-06-11"
            actions={<BinduButton>Initiate Retention Action</BinduButton>}
          >
            "Elena anchors the institution's infrastructure capability. The record shows mastery still
            compounding, but the role stopped growing eleven months ago. A strategic mandate within Q3 retains
            her at materially lower cost than her replacement."
          </NarrativePlate>
        </div>
      </div>

      {/* Institutional history */}
      <div className="bg-surface-container-lowest hairline-all p-6 mt-grid-12">
        <div className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-6 border-b border-outline-variant pb-2">
          Institutional History
        </div>
        <CaseTimeline entries={history} />
      </div>

      <LedgerStrip
        className="mt-8"
        entries={[
          { label: 'Record', value: 'EMP-8834-A' },
          { label: 'Projection', value: 'EmployeeDetailProjection v412' },
          { label: 'Freshness', value: '2026-06-12T07:40:11Z' },
          { label: 'Last Mutation', value: 'COMP_REV by M. Vance' },
        ]}
      />
    </>
  )
}
