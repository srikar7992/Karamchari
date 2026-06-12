// LAB EXPERIMENT C — Team Attendance as Living Topology Map.
// Not production. Outside doctrine jurisdiction (design/EXPERIMENTS.md).
// Question: what if zones were territories that expand with health and
// contract under risk — the institution literally changing shape — instead of
// rows in a coverage table?

import { useState } from 'react'

interface Zone {
  id: string
  name: string
  present: number
  min: number
  rostered: number
  // Base territory polygon (healthy shape). Risk contracts it toward its centroid.
  poly: [number, number][]
  labelAt: [number, number]
}

const ZONES: Zone[] = [
  {
    id: 'north', name: 'Zone North', present: 14, min: 10, rostered: 15,
    poly: [[80, 40], [330, 28], [400, 120], [300, 190], [120, 170]], labelAt: [200, 105],
  },
  {
    id: 'east', name: 'Zone East', present: 2, min: 3, rostered: 4,
    poly: [[420, 110], [640, 70], [700, 200], [560, 280], [430, 230]], labelAt: [545, 175],
  },
  {
    id: 'south', name: 'Zone South', present: 9, min: 8, rostered: 11,
    poly: [[150, 210], [390, 220], [420, 350], [220, 390], [90, 320]], labelAt: [255, 295],
  },
  {
    id: 'west', name: 'Zone West', present: 6, min: 5, rostered: 6,
    poly: [[440, 260], [620, 300], [600, 420], [400, 430], [380, 330]], labelAt: [500, 355],
  },
]

type Health = 'healthy' | 'tight' | 'breach'

function healthOf(z: Zone): Health {
  if (z.present < z.min) return 'breach'
  if (z.present - z.min <= 1) return 'tight'
  return 'healthy'
}

const FILL: Record<Health, string> = {
  healthy: '#2D5D4B',
  tight: '#9C842A',
  breach: '#7A2B2B',
}

const FILL_OPACITY: Record<Health, number> = { healthy: 0.16, tight: 0.2, breach: 0.26 }
const STROKE_W: Record<Health, number> = { healthy: 1.25, tight: 1.75, breach: 3 }
// Territory contracts under risk: scale toward centroid.
const SCALE: Record<Health, number> = { healthy: 1, tight: 0.93, breach: 0.8 }

function centroid(poly: [number, number][]): [number, number] {
  const n = poly.length
  return [poly.reduce((a, p) => a + p[0], 0) / n, poly.reduce((a, p) => a + p[1], 0) / n]
}

function shapeOf(z: Zone): string {
  const h = healthOf(z)
  const [cx, cy] = centroid(z.poly)
  const s = SCALE[h]
  return z.poly.map(([x, y]) => `${cx + (x - cx) * s},${cy + (y - cy) * s}`).join(' ')
}

// Deterministic operator dots scattered inside the contracted territory.
function dots(z: Zone): [number, number][] {
  const h = healthOf(z)
  const [cx, cy] = centroid(z.poly)
  const out: [number, number][] = []
  for (let i = 0; i < z.present; i++) {
    const v = z.poly[i % z.poly.length]
    const t = 0.25 + ((i * 37) % 50) / 100 // 0.25..0.75 toward vertex
    const x = cx + (v[0] - cx) * t * SCALE[h] * 0.85
    const y = cy + (v[1] - cy) * t * SCALE[h] * 0.85
    out.push([x, y])
  }
  return out
}

export function LivingTopology() {
  const [selected, setSelected] = useState<Zone>(ZONES[1])
  const health = healthOf(selected)

  return (
    <div className="bg-ivory min-h-screen text-ink">
      <div className="max-w-[1200px] mx-auto px-6 md:px-12 py-14">
        <header className="mb-10">
          <p className="font-mono text-[10px] uppercase tracking-[0.3em] text-on-surface-variant mb-5">
            Karamchari · Living Topology · Shift MORN-A · 07:42 IST
          </p>
          <h1 className="font-serif font-light text-[52px] md:text-[64px] leading-none tracking-tight">
            Where the Institution Stands
          </h1>
          <p className="font-sans text-[15px] text-ink/60 mt-4 max-w-[64ch]">
            Each territory is a zone. Healthy territories hold their shape; territories under risk
            contract and their borders harden. Zone East has pulled inward — two operators hold a
            three-operator line.
          </p>
        </header>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-10">
          {/* The map */}
          <div className="lg:col-span-8">
            <svg viewBox="0 0 760 470" className="w-full border border-ink/15 bg-[#F7F2E8]">
              {/* graticule */}
              {[...Array(7)].map((_, i) => (
                <line key={`v${i}`} x1={i * 110 + 50} y1="0" x2={i * 110 + 50} y2="470" stroke="#0E0F12" strokeOpacity="0.05" />
              ))}
              {[...Array(5)].map((_, i) => (
                <line key={`h${i}`} x1="0" y1={i * 100 + 35} x2="760" y2={i * 100 + 35} stroke="#0E0F12" strokeOpacity="0.05" />
              ))}

              {ZONES.map((z) => {
                const h = healthOf(z)
                const sel = z.id === selected.id
                return (
                  <g key={z.id} onClick={() => setSelected(z)} className="cursor-pointer">
                    <polygon
                      points={shapeOf(z)}
                      fill={FILL[h]}
                      fillOpacity={FILL_OPACITY[h] + (sel ? 0.08 : 0)}
                      stroke={FILL[h]}
                      strokeWidth={STROKE_W[h] + (sel ? 0.75 : 0)}
                      strokeLinejoin="round"
                      style={{ transition: 'all 200ms ease' }}
                    />
                    {/* ghost of the healthy shape, visible only when contracted */}
                    {h !== 'healthy' && (
                      <polygon
                        points={z.poly.map((p) => p.join(',')).join(' ')}
                        fill="none"
                        stroke={FILL[h]}
                        strokeOpacity="0.3"
                        strokeWidth="1"
                        strokeDasharray="2 6"
                      />
                    )}
                    {dots(z).map(([x, y], i) => (
                      <circle key={i} cx={x} cy={y} r="3.2" fill="#0E0F12" fillOpacity="0.75" />
                    ))}
                    <text x={z.labelAt[0]} y={z.labelAt[1]} textAnchor="middle" fontFamily="Newsreader, serif" fontStyle="italic" fontSize="20" fill="#0E0F12" fillOpacity="0.75">
                      {z.name}
                    </text>
                    <text x={z.labelAt[0]} y={z.labelAt[1] + 18} textAnchor="middle" fontFamily="JetBrains Mono" fontSize="10" letterSpacing="2" fill={FILL[h]}>
                      {z.present}/{z.min} HELD
                    </text>
                  </g>
                )
              })}

              {/* proposed reassignment: North -> East */}
              <path d="M 330 130 C 400 100, 440 110, 495 150" fill="none" stroke="#B16A3C" strokeWidth="2" strokeDasharray="6 5" markerEnd="url(#arrow)" />
              <defs>
                <marker id="arrow" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
                  <path d="M0,0 L7,3 L0,6 Z" fill="#B16A3C" />
                </marker>
              </defs>
              <text x="408" y="92" fontFamily="JetBrains Mono" fontSize="9.5" letterSpacing="1.5" fill="#B16A3C">PROPOSED MOVE · R. IYER</text>
            </svg>

            <div className="flex flex-wrap gap-x-8 gap-y-2 mt-4 font-mono text-[9.5px] uppercase tracking-[0.2em] text-on-surface-variant">
              <span className="flex items-center gap-2"><span className="w-3 h-3 inline-block" style={{ background: '#2D5D4B', opacity: 0.5 }} /> holds shape — healthy</span>
              <span className="flex items-center gap-2"><span className="w-3 h-3 inline-block" style={{ background: '#9C842A', opacity: 0.5 }} /> contracting — one deep</span>
              <span className="flex items-center gap-2"><span className="w-3 h-3 inline-block" style={{ background: '#7A2B2B', opacity: 0.5 }} /> contracted — line breached</span>
              <span>dashed outline = territory at full strength</span>
            </div>
          </div>

          {/* Territory dossier */}
          <aside className="lg:col-span-4">
            <div className="border-t-2 border-ink pt-5">
              <p className="font-mono text-[10px] uppercase tracking-[0.25em] text-on-surface-variant mb-2">Territory under review</p>
              <h2 className="font-serif font-light text-[36px] leading-none tracking-tight mb-5">{selected.name}</h2>
              <dl className="font-sans text-[14px] space-y-3">
                <div className="flex justify-between border-b border-ink/10 pb-2">
                  <dt className="text-ink/55">Line held</dt>
                  <dd className="font-mono text-[13px]">{selected.present} of {selected.min} minimum</dd>
                </div>
                <div className="flex justify-between border-b border-ink/10 pb-2">
                  <dt className="text-ink/55">Rostered</dt>
                  <dd className="font-mono text-[13px]">{selected.rostered}</dd>
                </div>
                <div className="flex justify-between border-b border-ink/10 pb-2">
                  <dt className="text-ink/55">Condition</dt>
                  <dd className="font-mono text-[13px] uppercase" style={{ color: FILL[health] }}>{health}</dd>
                </div>
              </dl>
              {selected.id === 'east' ? (
                <p className="font-serif font-light italic text-[20px] leading-snug mt-7 text-ink/85">
                  "Zone East cannot absorb one more absence. If the line is not reinforced before the
                  09:00 dispatch, two statutory deliveries fail and the breach becomes contractual,
                  not operational."
                </p>
              ) : (
                <p className="font-serif font-light italic text-[20px] leading-snug mt-7 text-ink/85">
                  "This territory holds. Its surplus is the reserve the institution draws on when a
                  neighbor contracts."
                </p>
              )}
              <p className="font-mono text-[9px] uppercase tracking-[0.2em] text-on-surface-variant mt-5">
                Buddhi · consequence narrative · cites REST-11H on any reassignment
              </p>
            </div>
          </aside>
        </div>
      </div>
    </div>
  )
}
