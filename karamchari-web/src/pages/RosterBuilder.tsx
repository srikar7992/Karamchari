import { PageHeader } from '@/components/shell/PageHeader'
import { Icon } from '@/components/shell/Icon'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'builder',
  signals: 1,
  risks: 1,
  narratives: 0,
  maps: 1,
  bindu: true,
}

type Cell =
  | { kind: 'shift'; time: string; accent?: boolean }
  | { kind: 'off' }
  | { kind: 'gap' }
  | { kind: 'empty'; wash?: 'gap' | 'weekend' }

const days = [
  { d: 'Mon', n: '16' },
  { d: 'Tue', n: '17' },
  { d: 'Wed', n: '18', flagged: true },
  { d: 'Thu', n: '19' },
  { d: 'Fri', n: '20' },
  { d: 'Sat', n: '21', weekend: true },
  { d: 'Sun', n: '22', weekend: true },
]

const rows: { name: string; role: string; cells: Cell[] }[] = [
  {
    name: 'A. Sharma',
    role: 'Sr. Operator',
    cells: [
      { kind: 'shift', time: '06:00 - 14:00' },
      { kind: 'shift', time: '06:00 - 14:00' },
      { kind: 'gap' },
      { kind: 'shift', time: '06:00 - 14:00' },
      { kind: 'shift', time: '06:00 - 14:00' },
      { kind: 'off' },
      { kind: 'off' },
    ],
  },
  {
    name: 'M. Patel',
    role: 'Operator',
    cells: [
      { kind: 'off' },
      { kind: 'off' },
      { kind: 'shift', time: '14:00 - 22:00', accent: true },
      { kind: 'shift', time: '14:00 - 22:00', accent: true },
      { kind: 'shift', time: '14:00 - 22:00', accent: true },
      { kind: 'shift', time: '14:00 - 22:00', accent: true },
      { kind: 'shift', time: '14:00 - 22:00', accent: true },
    ],
  },
  {
    name: 'R. Iyer',
    role: 'Trainee',
    cells: [
      { kind: 'empty' },
      { kind: 'empty' },
      { kind: 'empty', wash: 'gap' },
      { kind: 'empty' },
      { kind: 'empty' },
      { kind: 'empty', wash: 'weekend' },
      { kind: 'empty', wash: 'weekend' },
    ],
  },
]

const coverage = ['100%', '100%', '85%', '100%', '100%', '--', '--']

export function RosterBuilder() {
  return (
    <>
      <PageHeader
        kicker="Shift Orchestration · Week 42"
        title="Roster Builder"
        right={
          <div className="flex gap-3">
            <button className="px-5 py-2.5 border border-outline-variant text-primary font-mono-label text-mono-label uppercase hover:bg-surface-container-low transition-colors flex items-center gap-2">
              <Icon name="content_copy" className="!text-[18px]" />
              Copy Last Week
            </button>
            <button className="px-5 py-2.5 bg-tamra-copper text-white font-mono-label text-mono-label uppercase hover:bg-tamra-copper-bright transition-colors flex items-center gap-2">
              <Icon name="auto_awesome" className="!text-[18px]" />
              Auto-Fill
            </button>
          </div>
        }
      />

      {/* Validation ribbon — constraint, not alarm */}
      <div className="bg-ivory-2 p-4 flex items-center justify-between hairline corner-tick mb-6">
        <div className="flex items-center gap-3">
          <div className="bindu copper shrink-0" />
          <span className="font-tabular-data text-tabular-data text-on-surface">
            2 coverage gaps identified in Morning Shift (Oct 18).
          </span>
        </div>
        <button className="font-mono-label text-mono-label text-primary uppercase underline decoration-1 underline-offset-4 hover:text-graphite">
          Resolve
        </button>
      </div>

      {/* Grid */}
      <div className="bg-surface hairline flex flex-col min-h-0 relative overflow-x-auto">
        <div className="min-w-[892px]">
          {/* Header */}
          <div className="flex hairline-b bg-surface-container-lowest">
            <div className="w-48 p-4 hairline-r flex items-center shrink-0">
              <span className="font-mono-label text-mono-label text-on-surface-variant uppercase">
                Resource
              </span>
            </div>
            <div className="flex-1 grid grid-cols-7">
              {days.map((day, i) => (
                <div
                  key={day.d}
                  className={`p-3 ${i < 6 ? 'hairline-r' : ''} flex flex-col items-center justify-center gap-1 ${
                    day.flagged ? 'bg-ivory-2' : day.weekend ? 'bg-surface-container-low' : ''
                  }`}
                >
                  <span className="font-mono-label text-mono-label text-on-surface-variant uppercase">
                    {day.d}
                  </span>
                  <span
                    className={`font-tabular-data text-tabular-data ${day.weekend ? 'text-on-surface-variant' : 'text-primary'}`}
                  >
                    {day.n}
                  </span>
                  {day.flagged && <div className="w-1.5 h-1.5 rounded-full bg-tamra-copper mt-1" />}
                </div>
              ))}
            </div>
          </div>

          {/* Body */}
          {rows.map((row, ri) => (
            <div
              key={row.name}
              className={`flex group hover:bg-surface-container-low transition-colors ${ri < rows.length - 1 ? 'hairline-b' : ''}`}
            >
              <div className="w-48 p-4 hairline-r flex flex-col justify-center shrink-0 bg-surface group-hover:bg-surface-container-low transition-colors">
                <span className="font-tabular-data text-tabular-data text-primary">{row.name}</span>
                <span className="font-mono-label text-mono-label text-on-surface-variant">{row.role}</span>
              </div>
              <div className="flex-1 grid grid-cols-7">
                {row.cells.map((cell, ci) => {
                  const edge = ci < 6 ? 'hairline-r' : ''
                  if (cell.kind === 'off')
                    return (
                      <div
                        key={ci}
                        className={`p-2 ${edge} bg-surface-container-low flex items-center justify-center`}
                      >
                        <span className="font-mono-label text-mono-label text-on-surface-variant">OFF</span>
                      </div>
                    )
                  if (cell.kind === 'gap')
                    return (
                      <div key={ci} className={`p-2 ${edge} bg-ivory-2/30 relative min-h-[56px]`}>
                        <div className="absolute inset-0 border-2 border-dashed border-tamra-copper/50 m-2 flex items-center justify-center cursor-pointer hover:border-tamra-copper transition-colors">
                          <Icon name="add" className="text-tamra-copper !text-[16px]" />
                        </div>
                      </div>
                    )
                  if (cell.kind === 'empty')
                    return (
                      <div
                        key={ci}
                        className={`p-2 ${edge} hover:bg-surface-dim transition-colors cursor-pointer min-h-[56px] ${
                          cell.wash === 'gap' ? 'bg-ivory-2/30' : cell.wash === 'weekend' ? 'bg-surface-container-low' : ''
                        }`}
                      />
                    )
                  return (
                    <div key={ci} className={`p-2 ${edge}`}>
                      <div
                        className={`h-10 ${cell.accent ? 'bg-surface' : 'bg-ivory-2'} hairline flex items-center justify-center cursor-move hover:border-yantra-indigo transition-colors relative overflow-hidden`}
                      >
                        {cell.accent && <div className="absolute inset-y-0 left-0 w-1 bg-tamra-copper" />}
                        <span className="font-mono-label text-mono-label text-primary">{cell.time}</span>
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>
          ))}

          {/* Coverage footer */}
          <div className="h-12 hairline-t bg-surface-container-lowest flex">
            <div className="w-48 p-3 hairline-r flex items-center shrink-0">
              <span className="font-mono-label text-mono-label text-on-surface-variant uppercase">
                Coverage
              </span>
            </div>
            <div className="flex-1 grid grid-cols-7">
              {coverage.map((c, i) => (
                <div
                  key={i}
                  className={`p-3 ${i < 6 ? 'hairline-r' : ''} flex items-center justify-center ${c === '85%' ? 'bg-ivory-2' : ''}`}
                >
                  <span
                    className={`font-tabular-data text-tabular-data ${
                      c === '85%'
                        ? 'text-tamra-copper font-medium'
                        : c === '--'
                          ? 'text-on-surface-variant'
                          : 'text-primary'
                    }`}
                  >
                    {c}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </>
  )
}
