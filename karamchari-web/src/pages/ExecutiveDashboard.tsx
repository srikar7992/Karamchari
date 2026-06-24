import { Icon } from '@/components/shell/Icon'
import { PageHeader } from '@/components/shell/PageHeader'
import { RiskCard, BinduButton } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EXEC',
  archetype: 'dashboard',
  signals: 1,
  risks: 3,
  narratives: 1,
  maps: 1,
  bindu: true,
}

const nineBox = [
  { label: 'High Pot / Low Perf', value: '5%', bg: 'bg-ivory-2' },
  { label: 'High Pot / Med Perf', value: '12%', bg: 'bg-cloud' },
  { label: 'Future Leaders', value: '8%', bg: 'bg-tamra-copper/20', highlight: true },
  { label: 'Med Pot / Low Perf', value: '10%', bg: 'bg-surface-container-high' },
  { label: 'Core Players', value: '35%', bg: 'bg-ivory-2', bold: true },
  { label: 'High Performers', value: '15%', bg: 'bg-cloud' },
  { label: 'Risk Zone', value: '4%', bg: 'bg-surface-variant' },
  { label: 'Inconsistent', value: '7%', bg: 'bg-surface-container-high' },
  { label: 'Solid Professionals', value: '4%', bg: 'bg-ivory-2' },
]

export function ExecutiveDashboard() {
  return (
    <>
      <PageHeader
        kicker="Command Center"
        title="Executive Dashboard"
        sub="Institutional health, talent topography, and strategic risk vectors for the current fiscal quarter."
      />

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-grid-12">
        {/* Institutional Health Score */}
        <section className="col-span-1 lg:col-span-4 structural-hairline bg-surface-container-low p-8 corner-tick flex flex-col justify-between min-h-[320px]">
          <div>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-2">
              Institutional Pulse
            </h3>
            <p className="font-tabular-data text-tabular-data text-on-surface mb-8">
              Composite health score derived from retention, engagement, and operational velocity.
            </p>
          </div>
          <div>
            <div className="flex items-end gap-4 mb-4">
              <span className="font-section-title text-section-title !text-7xl leading-none tracking-tighter text-primary">
                87
              </span>
              <span className="font-body-standard text-body-standard text-sthira-forest mb-2 flex items-center">
                <Icon name="arrow_upward" className="!text-[16px] mr-1" /> +2.4%
              </span>
            </div>
            <div className="w-full bg-surface-variant h-1 mt-6">
              <div className="bg-primary h-1 w-[87%]" />
            </div>
            <div className="flex justify-between mt-2 font-mono-label text-mono-label text-on-surface-variant">
              <span>Critical (&lt; 60)</span>
              <span>Optimal (&gt; 85)</span>
            </div>
          </div>
        </section>

        {/* Workforce Risk Forecast */}
        <section className="col-span-1 lg:col-span-8 structural-hairline bg-surface p-8">
          <div className="flex justify-between items-start mb-8 border-b border-outline-variant pb-4">
            <div>
              <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-2">
                Risk Vectors
              </h3>
              <h4 className="font-pull-quote text-pull-quote text-primary !text-2xl">
                Attrition &amp; Burnout Forecast
              </h4>
            </div>
            <BinduButton>Generate Report</BinduButton>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-8">
            <RiskCard severity="critical" title="High Flight Risk" value="12%" body="Engineering (Senior)" />
            <RiskCard severity="elevated" title="Burnout Indicators" value="28%" body="Sales & Operations" />
            <RiskCard severity="neutral" title="Voluntary Turnover (YTD)" value="4.2%" body="Org Average" />
          </div>
        </section>

        {/* 9-Box Talent Topography */}
        <section className="col-span-1 lg:col-span-12 structural-hairline bg-surface-container-lowest p-8 mt-4">
          <header className="mb-8">
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-2">
              Talent Distribution
            </h3>
            <h4 className="font-pull-quote text-pull-quote text-primary !text-2xl">
              9-Box Organizational Topography
            </h4>
          </header>
          <div className="grid grid-cols-3 grid-rows-3 gap-1 h-[400px]">
            {nineBox.map((cell) => (
              <div
                key={cell.label}
                className={`${cell.bg} flex flex-col justify-between p-4 border border-ink/5 ${
                  cell.highlight ? 'corner-tick' : ''
                }`}
              >
                <span
                  className={`font-mono-label text-mono-label ${
                    cell.highlight ? 'text-tamra-copper font-bold' : 'text-on-surface-variant'
                  }`}
                >
                  {cell.label}
                </span>
                <span
                  className={`font-tabular-data text-tabular-data self-end ${
                    cell.highlight
                      ? 'text-tamra-copper font-bold text-xl'
                      : cell.bold
                        ? 'text-xl font-bold'
                        : 'text-lg'
                  }`}
                >
                  {cell.value}
                </span>
              </div>
            ))}
          </div>
        </section>

        {/* Buddhi Strategic Recommendations */}
        <section className="col-span-1 lg:col-span-12 structural-hairline bg-yantra-indigo text-on-primary p-8 mt-4 relative overflow-hidden">
          <div
            className="absolute inset-0 opacity-10"
            style={{
              backgroundImage:
                'repeating-linear-gradient(45deg, #ffffff 0, #ffffff 1px, transparent 1px, transparent 10px)',
            }}
          />
          <div className="relative z-10 flex flex-col md:flex-row gap-8 items-start">
            <div className="md:w-1/3">
              <h3 className="font-mono-label text-mono-label text-surface-variant uppercase mb-4 flex items-center gap-2">
                <span className="w-2 h-2 rounded-full bg-tamra-copper-bright inline-block" />
                Buddhi Intelligence
              </h3>
              <h4 className="font-pull-quote text-pull-quote text-white !text-3xl mb-4">
                Strategic Structural Interventions
              </h4>
              <p className="font-body-standard text-body-standard text-surface-dim opacity-80">
                Synthesized from cross-functional performance data, engagement surveys, and market
                compensation benchmarks.
              </p>
            </div>
            <div className="md:w-2/3 grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="border border-white/20 p-6 bg-white/[0.03] hover:bg-white/5 transition-colors">
                <Icon name="account_tree" filled className="text-tamra-copper-bright mb-4 block" />
                <h5 className="font-body-standard text-body-standard text-white font-bold mb-2">
                  Flatten Engineering Hierarchy
                </h5>
                <p className="font-tabular-data text-tabular-data text-surface-dim opacity-80">
                  Span of control in Core Eng is currently 1:4. Recommending a shift to 1:8 to increase
                  deployment velocity and reduce middle-management overhead.
                </p>
              </div>
              <div className="border border-white/20 p-6 bg-white/[0.03] hover:bg-white/5 transition-colors">
                <Icon name="payments" filled className="text-pita-caution mb-4 block" />
                <h5 className="font-body-standard text-body-standard text-white font-bold mb-2">
                  Compensation Realignment
                </h5>
                <p className="font-tabular-data text-tabular-data text-surface-dim opacity-80">
                  Data Science cohort is currently 12% below market median. High flight risk detected among
                  top 10% performers. Immediate correction advised.
                </p>
              </div>
            </div>
          </div>
        </section>
      </div>
    </>
  )
}
