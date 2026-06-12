import { Icon } from '@/components/shell/Icon'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'case',
  signals: 1,
  risks: 1,
  narratives: 1,
  maps: 1,
  bindu: true,
}

const timelineNodes = [
  { icon: 'check', label: 'Pre-Arrival', state: 'done' },
  { icon: 'pending', label: 'Day 1 Focus', state: 'active' },
  { icon: 'school', label: 'Week 1 Academics', state: 'todo' },
  { icon: 'flag', label: 'Month 1 Review', state: 'todo' },
]

const ledger = [
  { icon: 'badge', name: 'Government ID Proof', owner: 'EMP', state: 'verified' },
  { icon: 'account_balance', name: 'Payroll Bank Information', owner: 'EMP', state: 'verified' },
  { icon: 'contract', name: 'Non-Disclosure Agreement', owner: 'EMP', state: 'pending' },
  { icon: 'laptop_mac', name: 'Hardware Provisioning', owner: 'IT_OPS', state: 'done-date', date: '12-Jun-26' },
]

export function OnboardingCase() {
  return (
    <>
      {/* Case header */}
      <div className="pb-8 hairline-b mb-12">
        <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
          <div>
            <span className="font-mono-label text-mono-label text-on-surface-variant tracking-widest uppercase mb-4 block">
              Case Ledger // OB-2026-891
            </span>
            <h2 className="font-section-title text-section-title !text-4xl md:!text-section-title text-primary tracking-tight">
              Namaskaram, Aditi.
            </h2>
            <p className="font-body-standard text-body-standard text-on-surface-variant mt-2 max-w-2xl">
              Onboarding procedural flow for Principal Engineer. Scheduled integration date is established.
            </p>
          </div>
          <div className="flex items-center gap-3">
            <button className="px-[22px] py-[14px] bg-transparent border border-outline-variant text-primary font-mono-label text-mono-label uppercase tracking-widest hover:bg-surface-container-low transition-colors">
              Halt Process
            </button>
            <button className="px-[22px] py-[14px] bg-tamra-copper text-white font-mono-label text-mono-label uppercase tracking-widest hover:bg-tamra-copper-bright transition-colors">
              Approve Next Phase
            </button>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-gutter">
        {/* Left: timeline + ledger */}
        <div className="lg:col-span-8 flex flex-col gap-12">
          <section>
            <div className="mb-6 flex justify-between items-baseline">
              <h3 className="font-mono-label text-mono-label text-primary uppercase tracking-widest border-b border-primary inline-block pb-1">
                Integration Timeline
              </h3>
              <span className="font-tabular-data text-tabular-data text-on-surface-variant">45% Complete</span>
            </div>
            <div className="bg-surface-container-lowest hairline-all p-8 relative">
              <div className="absolute top-[48px] left-12 right-12 h-[1px] bg-outline-variant z-0" />
              <div className="absolute top-[48px] left-12 w-[45%] h-[2px] bg-tamra-copper z-0" />
              <div className="relative z-10 flex justify-between">
                {timelineNodes.map((n) => (
                  <div key={n.label} className="flex flex-col items-center gap-3 w-24">
                    <div
                      className={
                        n.state === 'active'
                          ? 'w-8 h-8 rounded-full bg-tamra-copper border-2 border-tamra-copper flex items-center justify-center text-white shadow-[0_0_0_4px_rgba(177,106,60,0.1)]'
                          : n.state === 'done'
                            ? 'w-8 h-8 rounded-full bg-surface border-2 border-tamra-copper flex items-center justify-center text-tamra-copper'
                            : 'w-8 h-8 rounded-full bg-surface-container-lowest border-2 border-outline-variant flex items-center justify-center text-on-surface-variant'
                      }
                    >
                      <Icon name={n.icon} className="!text-[16px]" />
                    </div>
                    <span
                      className={`font-mono-label text-mono-label text-center ${
                        n.state === 'active' ? 'text-primary font-bold' : n.state === 'done' ? 'text-primary' : 'text-on-surface-variant'
                      }`}
                    >
                      {n.label}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          </section>

          <section>
            <div className="mb-6 flex justify-between items-baseline">
              <h3 className="font-mono-label text-mono-label text-primary uppercase tracking-widest border-b border-primary inline-block pb-1">
                Documentation Ledger
              </h3>
              <div className="flex gap-4">
                <span className="font-mono-label text-mono-label text-rakta-critical flex items-center gap-1">
                  <span className="bindu rakta-critical" /> 1 Pending
                </span>
                <span className="font-mono-label text-mono-label text-sthira-forest flex items-center gap-1">
                  <span className="bindu good" /> 3 Verified
                </span>
              </div>
            </div>
            <div className="bg-surface-container-lowest hairline-all flex flex-col">
              <div className="grid grid-cols-12 gap-4 px-6 py-3 border-b border-outline-variant bg-surface-container-low">
                <div className="col-span-1 font-mono-label text-mono-label text-on-surface-variant">STAT</div>
                <div className="col-span-5 font-mono-label text-mono-label text-on-surface-variant">
                  REQUIREMENT
                </div>
                <div className="col-span-3 font-mono-label text-mono-label text-on-surface-variant">OWNER</div>
                <div className="col-span-3 font-mono-label text-mono-label text-on-surface-variant text-right">
                  ACTION
                </div>
              </div>
              {ledger.map((row, i) => (
                <div
                  key={row.name}
                  className={`grid grid-cols-12 gap-4 px-6 py-4 items-center transition-colors ${
                    i < ledger.length - 1 ? 'border-b border-outline-variant' : ''
                  } ${
                    row.state === 'pending'
                      ? 'bg-surface-container-low border-l-2 border-l-rakta-critical'
                      : 'hover:bg-surface'
                  }`}
                >
                  <div className="col-span-1 flex items-center pl-1">
                    <span
                      className={`bindu ${
                        row.state === 'pending' ? 'rakta-critical animate-pulse' : 'good'
                      }`}
                    />
                  </div>
                  <div className="col-span-5 flex items-center gap-3">
                    <Icon
                      name={row.icon}
                      className={row.state === 'pending' ? 'text-rakta-critical' : 'text-on-surface-variant'}
                    />
                    <span
                      className={`font-body-standard text-body-standard text-primary ${row.state === 'pending' ? 'font-bold' : ''}`}
                    >
                      {row.name}
                    </span>
                  </div>
                  <div className="col-span-3 font-tabular-data text-tabular-data text-on-surface-variant">
                    {row.owner}
                  </div>
                  <div className="col-span-3 text-right">
                    {row.state === 'pending' ? (
                      <button className="px-[12px] py-[6px] bg-rakta-critical text-white font-mono-label text-mono-label">
                        Issue Nudge
                      </button>
                    ) : row.state === 'done-date' ? (
                      <span className="font-tabular-data text-tabular-data text-on-surface-variant">
                        {row.date}
                      </span>
                    ) : (
                      <button className="text-yantra-indigo font-mono-label text-mono-label hover:underline">
                        View Artifact
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </section>
        </div>

        {/* Right: culture + training */}
        <div className="lg:col-span-4 flex flex-col gap-12">
          <section>
            <div className="mb-6">
              <h3 className="font-mono-label text-mono-label text-primary uppercase tracking-widest border-b border-primary inline-block pb-1">
                Cultural Induction
              </h3>
            </div>
            <div className="bg-ivory-2 hairline-all p-6 relative corner-tick">
              <div className="flex gap-4 items-start mb-4">
                <div className="w-12 h-12 bg-surface-variant shrink-0 border border-outline-variant flex items-center justify-center">
                  <span className="font-serif font-light text-lg text-graphite">DE</span>
                </div>
                <div>
                  <p className="font-mono-label text-mono-label text-on-surface-variant mb-1">
                    From: Director of Engineering
                  </p>
                  <p className="font-body-standard text-body-standard text-primary italic">
                    "Aditi brings a rigorous structural approach to our backend systems. Her work at her
                    previous ledger firm aligns perfectly with our Sage-Architect principles. Awaiting her
                    contributions."
                  </p>
                </div>
              </div>
              <button className="w-full py-[9px] bg-transparent border border-outline-variant text-primary font-mono-label text-mono-label uppercase tracking-widest hover:bg-surface-container-lowest transition-colors mt-2">
                Add Welcome Note
              </button>
            </div>
          </section>

          <section>
            <div className="mb-6">
              <h3 className="font-mono-label text-mono-label text-primary uppercase tracking-widest border-b border-primary inline-block pb-1">
                Academic Modules
              </h3>
            </div>
            <div className="flex flex-col gap-3">
              <a className="group block bg-surface-container-lowest hairline-all p-4 hover:border-yantra-indigo hover:bg-surface transition-all cursor-pointer">
                <div className="flex justify-between items-start mb-2">
                  <span className="font-mono-label text-mono-label text-tamra-copper bg-surface-container-low px-2 py-1">
                    Mandatory
                  </span>
                  <Icon
                    name="arrow_outward"
                    className="text-outline group-hover:text-yantra-indigo transition-colors"
                  />
                </div>
                <h4 className="font-body-standard text-body-standard text-primary font-bold mb-1">
                  Information Security Level 1
                </h4>
                <p className="font-tabular-data text-tabular-data text-on-surface-variant">
                  Est. time: 45 mins • Due Day 3
                </p>
              </a>
              <a className="group block bg-surface-container-lowest hairline-all p-4 hover:border-yantra-indigo hover:bg-surface transition-all cursor-pointer">
                <div className="flex justify-between items-start mb-2">
                  <span className="font-mono-label text-mono-label text-on-surface-variant bg-surface-container-high px-2 py-1">
                    Recommended
                  </span>
                  <Icon
                    name="arrow_outward"
                    className="text-outline group-hover:text-yantra-indigo transition-colors"
                  />
                </div>
                <h4 className="font-body-standard text-body-standard text-primary font-bold mb-1">
                  Architecture Principles
                </h4>
                <p className="font-tabular-data text-tabular-data text-on-surface-variant">
                  Est. time: 120 mins • Ongoing
                </p>
              </a>
            </div>
          </section>
        </div>
      </div>
    </>
  )
}
