import { Icon } from '@/components/shell/Icon'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EXEC',
  archetype: 'topology',
  signals: 3,
  risks: 1,
  narratives: 1,
  maps: 1,
  bindu: false,
}

const bench = [
  { initials: 'MK', name: 'Marcus Kim', role: 'Dir. Cloud Infrastructure', readiness: '92%', tone: 'text-sthira-forest', tags: ['Ready Now', 'Top Talent'] },
  { initials: 'EL', name: 'Elena Rodriguez', role: 'Dir. Platform Eng', readiness: '78%', tone: 'text-pita-caution', tags: ['1-2 Years', 'High Potential'] },
  { initials: 'DP', name: 'David Patel', role: 'Sr. Eng Manager', readiness: '65%', tone: 'text-tamra-copper', tags: ['3-5 Years'] },
]

const calibration: { label: string; marker?: string; forest?: boolean }[] = [
  { label: 'Enigma' },
  { label: 'Growth', marker: 'EL' },
  { label: 'Future Leader', marker: 'MK', forest: true },
  { label: 'Dilemma' },
  { label: 'Core', marker: 'DP' },
  { label: 'High Impact' },
  { label: 'Risk' },
  { label: 'Effective' },
  { label: 'Trusted Prof.' },
]

const gaps = [
  { who: 'Marcus Kim (92%)', tone: 'caution', gap: 'Cross-functional Org Design', note: 'Requires exposure to non-technical departmental structuring.', suggestion: 'Interim rotation on the Q3 Enterprise Restructure Taskforce.' },
  { who: 'Elena Rodriguez (78%)', tone: 'rakta-critical', gap: 'Executive Board Communication', note: 'Current communication style overly technical for C-suite alignment.', suggestion: 'Assign Executive Coach (Comm. Focus) + Present Q4 Tech Roadmap to Board.' },
  { who: 'David Patel (65%)', tone: 'rakta-critical', gap: 'Strategic Budget Allocation', note: 'Lacks experience managing portfolios over $5M.', suggestion: 'Co-own FY27 Engineering CapEx budget planning with incumbent.' },
]

export function SuccessionWorkspace() {
  return (
    <div className="space-y-16">
      {/* Critical role profile */}
      <section className="grid grid-cols-1 md:grid-cols-12 gap-gutter items-start">
        <div className="md:col-span-8 space-y-6">
          <div className="font-mono-label text-mono-label text-yantra-indigo uppercase tracking-widest mb-4 flex items-center gap-2">
            <span className="bindu rakta-critical" /> CRITICAL ROLE PROFILE
          </div>
          <h1 className="font-section-title text-section-title !text-4xl md:!text-section-title text-primary">
            Vice President of Engineering
          </h1>
          <p className="font-pull-quote text-pull-quote text-on-surface-variant max-w-2xl">
            Incumbent risk assessed at high due to impending retirement window within 12-18 months. Immediate
            succession pipeline activation required.
          </p>
        </div>
        <div className="md:col-span-4 bg-surface-container-lowest hairline-all p-6 corner-tick plate-container">
          <div className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-6 hairline-b pb-2">
            Incumbent Status
          </div>
          <div className="flex items-center gap-4 mb-6">
            <div className="w-16 h-16 bg-ivory-2 hairline-all flex items-center justify-center">
              <span className="font-serif font-light text-2xl text-graphite">SJ</span>
            </div>
            <div>
              <div className="font-bold text-primary">Sarah Jenkins</div>
              <div className="text-on-surface-variant text-sm mt-1">Tenure: 8 Years</div>
            </div>
          </div>
          <div className="space-y-4">
            <div className="flex justify-between items-center text-sm">
              <span className="text-on-surface-variant">Flight Risk</span>
              <span className="font-tabular-data text-tabular-data text-rakta-critical font-medium">
                HIGH (85%)
              </span>
            </div>
            <div className="flex justify-between items-center text-sm">
              <span className="text-on-surface-variant">Impact of Loss</span>
              <span className="font-tabular-data text-tabular-data font-medium">CRITICAL</span>
            </div>
          </div>
        </div>
      </section>

      {/* Bench + calibration */}
      <section className="grid grid-cols-1 md:grid-cols-12 gap-grid-12">
        <div className="md:col-span-7 bg-surface-container-lowest hairline-all p-8 flex flex-col h-[500px] plate-container">
          <div className="flex justify-between items-end hairline-b pb-4 mb-6">
            <h2 className="font-mono-label text-mono-label text-primary uppercase">Succession Bench</h2>
            <span className="text-sm text-on-surface-variant">3 Candidates</span>
          </div>
          <div className="flex-1 overflow-y-auto pr-4 space-y-4">
            {bench.map((c) => (
              <div
                key={c.name}
                className="group hairline-all bg-surface p-4 hover:bg-surface-container-low transition-colors duration-200 cursor-pointer"
              >
                <div className="flex justify-between items-start mb-3">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-ink text-white flex items-center justify-center font-mono-label text-mono-label">
                      {c.initials}
                    </div>
                    <div>
                      <div className="font-medium text-primary">{c.name}</div>
                      <div className="text-xs text-on-surface-variant mt-0.5">{c.role}</div>
                    </div>
                  </div>
                  <div className="text-right">
                    <div className={`font-tabular-data text-tabular-data font-medium ${c.tone}`}>
                      {c.readiness}
                    </div>
                    <div className="text-[10px] uppercase text-on-surface-variant">Readiness</div>
                  </div>
                </div>
                <div className="flex gap-2">
                  {c.tags.map((t, i) => (
                    <span
                      key={t}
                      className={`px-2 py-1 text-xs ${i === 0 ? 'bg-surface-variant text-on-surface' : 'hairline-all text-on-surface-variant'}`}
                    >
                      {t}
                    </span>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="md:col-span-5 bg-yantra-indigo text-on-primary p-8 flex flex-col h-[500px]">
          <h2 className="font-mono-label text-mono-label text-secondary-container uppercase opacity-70 mb-6 border-b border-white/20 pb-4">
            Calibration Map
          </h2>
          <div className="flex-1 grid grid-cols-3 grid-rows-3 gap-1 p-1 bg-inverse-surface">
            {calibration.map((cell) => (
              <div
                key={cell.label}
                className={`${cell.forest ? 'bg-sthira-forest/30' : 'bg-yantra-indigo/90'} p-2 flex items-start justify-end relative group cursor-pointer`}
              >
                <div className="absolute inset-0 bg-white/5 opacity-0 group-hover:opacity-100 transition-opacity" />
                {cell.marker && (
                  <div className="w-4 h-4 bg-secondary-container rounded-sm flex items-center justify-center text-[8px] text-ink font-bold">
                    {cell.marker}
                  </div>
                )}
                <span className="text-[10px] opacity-40 absolute bottom-2 left-2">{cell.label}</span>
              </div>
            ))}
          </div>
          <div className="flex justify-between text-[10px] mt-4 opacity-50 uppercase tracking-widest font-mono-label">
            <span>← Performance →</span>
            <span>↑ Potential ↑</span>
          </div>
        </div>
      </section>

      {/* Buddhi readiness gaps */}
      <section className="pt-8 hairline-t">
        <div className="flex items-center gap-3 mb-8">
          <Icon name="psychiatry" className="text-tamra-copper" />
          <h2 className="font-serif font-light text-[32px] text-primary">Readiness Gaps &amp; Mitigation</h2>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {gaps.map((g) => (
            <div key={g.who} className="hairline-l pl-6">
              <div className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-4">
                {g.who}
              </div>
              <div>
                <div className="flex items-center gap-2 text-sm font-medium mb-1">
                  <span className={`bindu ${g.tone}`} /> {g.gap}
                </div>
                <p className="text-sm text-on-surface-variant">{g.note}</p>
                <div className="mt-2 p-3 bg-surface-container-low hairline-all text-xs">
                  <span className="font-bold text-tamra-copper">Buddhi Suggestion:</span> {g.suggestion}
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  )
}
