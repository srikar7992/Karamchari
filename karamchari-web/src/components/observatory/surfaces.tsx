// Observatory surface primitives (ui3 grammar, production TS).
// Ported from design/ui3/os/primitives.jsx + authority.jsx, adapted to the
// production doctrine palette and typed. Scoped under the `.obs` wrapper.
// Information classes only: Signal, Risk, Narrative, Authority, Ledger, Map.

import { useEffect, useRef, useState, type ReactNode, type CSSProperties } from 'react'

/* ---- Sutra glyph: triangle bound by thread, bindu at centroid ---- */
export function Glyph({ size = 26, stroke = 'currentColor', bindu = '#b16a3c' }: { size?: number; stroke?: string; bindu?: string }) {
  return (
    <svg width={size} height={size} viewBox="0 0 100 100" aria-hidden="true" style={{ display: 'block' }}>
      <g stroke={stroke} strokeWidth="7" strokeLinecap="round" strokeLinejoin="round" fill="none">
        <path d="M22 76 L50 24 L78 76" />
        <line x1="34" y1="58" x2="66" y2="58" />
      </g>
      <circle cx="50" cy="58" r="5" fill={bindu} />
    </svg>
  )
}

/* ---- Chamfered, hairline-framed container — replaces the rounded card ---- */
export function Surface({
  variant = 'surf',
  risk = false,
  pad = '24px',
  emergeDelay,
  className = '',
  style = {},
  children,
}: {
  variant?: 'surf' | 'sunken' | 'ink'
  risk?: boolean
  pad?: string
  emergeDelay?: number
  className?: string
  style?: CSSProperties
  children: ReactNode
}) {
  const bg = variant === 'ink' ? 'var(--obs-ink)' : variant === 'sunken' ? 'var(--obs-sunken)' : 'var(--obs-surf)'
  const isInk = variant === 'ink'
  const cham = risk ? 'cham-risk' : 'cham'
  const eProps = emergeDelay !== undefined ? { 'data-emerge': '' } : {}
  return (
    <div className={cham} style={{ background: 'var(--obs-hair)', padding: 1, ...(emergeDelay !== undefined ? { ['--obs-delay' as string]: `${emergeDelay}ms` } : {}) }} {...eProps}>
      <div
        className={`${cham} ${className}`}
        style={{ background: bg, color: isInk ? '#f2ede3' : 'var(--obs-fg)', padding: pad, height: '100%', ...style }}
      >
        {children}
      </div>
    </div>
  )
}

/* ---- Mono label + code identifier (visual anchors) ---- */
export function Label({ children, color = 'var(--obs-mute)', style = {} }: { children: ReactNode; color?: string; style?: CSSProperties }) {
  return <div className="voice-mono" style={{ color, ...style }}>{children}</div>
}
export function Code({ children, color = 'var(--obs-copper)', style = {} }: { children: ReactNode; color?: string; style?: CSSProperties }) {
  return <span className="voice-code" style={{ fontSize: 11, letterSpacing: '0.03em', color, ...style }}>{children}</span>
}

/* ---- Sparkline ---- */
export function Sparkline({ data, w = 96, h = 28, color = 'var(--obs-copper)', fill = false }: { data: number[]; w?: number; h?: number; color?: string; fill?: boolean }) {
  const min = Math.min(...data), max = Math.max(...data), rng = max - min || 1
  const pts = data.map((d, i) => [(i / (data.length - 1)) * w, h - 3 - ((d - min) / rng) * (h - 6)] as const)
  const line = pts.map((p) => p.join(',')).join(' ')
  return (
    <svg width={w} height={h} style={{ display: 'block', overflow: 'visible' }} aria-hidden="true">
      {fill && <polygon points={`0,${h} ${line} ${w},${h}`} fill={color} opacity="0.08" />}
      <polyline points={line} fill="none" stroke={color} strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
      <circle cx={pts[pts.length - 1][0]} cy={pts[pts.length - 1][1]} r="2.4" fill={color} />
    </svg>
  )
}

/* ---- Institutional Spine: the signature continuity device ---- */
export interface SpineNode { id: string; label: string }
export function SpineRail({ nodes, active = 0 }: { nodes: SpineNode[]; active?: number }) {
  const jump = (id: string) => {
    const el = document.getElementById(id)
    if (el) window.scrollTo({ top: el.getBoundingClientRect().top + window.scrollY - 96, behavior: 'smooth' })
  }
  return (
    <div className="obs-spine" style={{ position: 'sticky', paddingTop: 4 }}>
      <div style={{ position: 'relative' }}>
        <div className="obs-spine-line" />
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {nodes.map((n, i) => (
            <button key={n.id} className="obs-spine-node" data-on={i === active ? '1' : '0'} onClick={() => jump(n.id)}>
              <span className="obs-spine-tick" />
              <span className="obs-spine-label">{n.label}</span>
            </button>
          ))}
        </div>
      </div>
    </div>
  )
}

export function useScrollSpy(ids: string[]): number {
  const [active, setActive] = useState(0)
  useEffect(() => {
    const els = ids.map((id) => document.getElementById(id)).filter(Boolean) as HTMLElement[]
    if (!els.length) return
    const obs = new IntersectionObserver(
      (entries) => {
        entries.forEach((e) => {
          if (e.isIntersecting) {
            const idx = ids.indexOf(e.target.id)
            if (idx >= 0) setActive(idx)
          }
        })
      },
      { rootMargin: '-45% 0px -45% 0px', threshold: 0 }
    )
    els.forEach((el) => obs.observe(el))
    return () => obs.disconnect()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ids.join('|')])
  return active
}

/* ---- NARRATIVE: a constitutional article, not a card ----
   Broken rectangle — copper article-spine, title hung in the margin,
   reasoning set as an indented Newsreader column on the bare canvas. */
export function ConstitutionalArticle({
  title, code, children, attribution, lead = false, emergeDelay,
}: {
  title: string; code?: string; children: ReactNode; attribution?: string; lead?: boolean; emergeDelay?: number
}) {
  const eProps = emergeDelay !== undefined ? { 'data-emerge': '' } : {}
  return (
    <div {...eProps} style={{ position: 'relative', padding: '32px 24px 32px 0', ...(emergeDelay !== undefined ? { ['--obs-delay' as string]: `${emergeDelay}ms` } : {}) }}>
      <span style={{ position: 'absolute', left: 0, top: 8, bottom: 8, width: lead ? 4 : 3, background: 'var(--obs-copper)' }} />
      <div style={{ display: 'grid', gridTemplateColumns: '164px minmax(0,1fr)', gap: 32 }} className="obs-article-grid">
        <div style={{ textAlign: 'right', paddingTop: 7, paddingLeft: 24 }}>
          <div className="voice-mono" style={{ color: 'var(--obs-copper)', letterSpacing: '0.14em' }}>{title}</div>
          {code && <div className="voice-code" style={{ fontSize: 10.5, color: 'var(--obs-mute)', marginTop: 9 }}>{code}</div>}
        </div>
        <div style={{ maxWidth: lead ? 780 : 660 }}>
          <div className="voice-serif" style={{ fontSize: lead ? 30 : 23, lineHeight: lead ? 1.42 : 1.5, letterSpacing: '-0.014em', color: 'var(--obs-fg)', fontWeight: 400, textWrap: 'pretty' }}>
            {children}
          </div>
          {attribution && <div className="voice-mono" style={{ marginTop: 24, color: 'var(--obs-mute)' }}>{attribution}</div>}
        </div>
      </div>
    </div>
  )
}

/* ---- SIGNAL: measurable truth. `slim` demotes it to a supporting strip ---- */
export interface Signal { id: string; label: string; value: string; unit?: string; ref?: string; delta: string; dir: 'up' | 'down'; spark: number[] }
export function SignalStrip({ sig, emergeDelay }: { sig: Signal; emergeDelay?: number }) {
  const up = sig.dir === 'up'
  const tone = up ? 'var(--obs-forest)' : 'var(--obs-copper)'
  const eProps = emergeDelay !== undefined ? { 'data-emerge': '' } : {}
  return (
    <Surface pad="16px 20px" emergeDelay={emergeDelay}>
      <div {...eProps} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}>
        <div>
          <Label style={{ marginBottom: 7 }}>{sig.label}</Label>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: 5 }}>
            <span className="tnum" style={{ fontSize: 22, lineHeight: 1, letterSpacing: '-0.02em', fontWeight: 600 }}>{sig.value}</span>
            {sig.unit && <span style={{ fontSize: 11, color: 'var(--obs-mute)' }}>{sig.unit}</span>}
            <span style={{ fontSize: 10.5, color: tone, fontWeight: 600, marginLeft: 4 }}>{sig.delta}</span>
          </div>
        </div>
        <Sparkline data={sig.spark} color={tone} w={64} h={26} fill />
      </div>
    </Surface>
  )
}

/* ---- RISK: constrained exposure, sharper geometry, never bright red ---- */
export interface Risk { id: string; level: 'elevated' | 'watch'; title: string; detail: string; exposure: string; horizon: string; owner: string }
export function RiskSurface({ risk, emergeDelay }: { risk: Risk; emergeDelay?: number }) {
  const tone = risk.level === 'elevated' ? 'var(--obs-risk-hi)' : 'var(--obs-risk)'
  return (
    <Surface risk pad="24px" emergeDelay={emergeDelay} style={{ borderTop: `2px solid ${tone}` }}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 12, height: '100%' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ width: 6, height: 6, background: tone, transform: 'rotate(45deg)', display: 'inline-block' }} />
            <Label color={tone} style={{ letterSpacing: '0.14em' }}>{risk.level}</Label>
          </div>
          <Code color="var(--obs-mute)">{risk.id}</Code>
        </div>
        <div style={{ fontSize: 15, fontWeight: 600, letterSpacing: '-0.01em', lineHeight: 1.25 }}>{risk.title}</div>
        <div style={{ fontSize: 12.5, color: 'var(--obs-text-2)', lineHeight: 1.5, textWrap: 'pretty' }}>{risk.detail}</div>
        <div style={{ display: 'flex', gap: 24, marginTop: 'auto', paddingTop: 12, borderTop: '1px solid var(--obs-hair)' }}>
          <Meta k="Exposure" v={risk.exposure} />
          <Meta k="Horizon" v={risk.horizon} />
          <Meta k="Owner" v={risk.owner} />
        </div>
      </div>
    </Surface>
  )
}
function Meta({ k, v }: { k: string; v: string }) {
  return (
    <div>
      <div className="voice-mono" style={{ fontSize: 9.5, color: 'var(--obs-mute)', marginBottom: 3 }}>{k}</div>
      <div style={{ fontSize: 12.5, fontWeight: 500 }}>{v}</div>
    </div>
  )
}

/* ---- AUTHORITY: the irreversible decision. Hold-to-authorise ceremony.
   Folded-seal silhouette, copper edge, wax seal. Never a normal button. ---- */
const AUTH_STATES = ['SUBMITTED', 'IN AUTHORITY', 'PUBLISHED'] as const
export function AuthoritySurface({ run, authorityCode = 'PAY-AUTH-04', fields, buttonLabel = 'Publish Payroll', publishedLabel = 'Payroll authority', onPublished }: { run: string; authorityCode?: string; fields?: Array<{k: string; v: string; sub: string}>; buttonLabel?: string; publishedLabel?: string; onPublished?: () => void }) {
  const [phase, setPhase] = useState<'idle' | 'arming' | 'published'>('idle')
  const [progress, setProgress] = useState(0)
  const raf = useRef(0)
  const holding = useRef(false)
  const stage = phase === 'published' ? 2 : phase === 'arming' ? 1 : 0
  const published = phase === 'published'
  const renderFields = fields ?? [
    { k: 'Impact', v: '11,240', sub: 'on-roll employees' },
    { k: 'Disbursement', v: '₹4.21 Cr', sub: 'provisional' },
    { k: 'Result', v: 'Generate', sub: 'payslips · bank file' },
  ]

  useEffect(() => () => cancelAnimationFrame(raf.current), [])

  const start = () => {
    if (published) return
    holding.current = true
    setPhase('arming')
    const t0 = performance.now()
    const tick = (now: number) => {
      if (!holding.current) return
      const p = Math.min(1, (now - t0) / 1150)
      setProgress(p)
      if (p >= 1) { commit(); return }
      raf.current = requestAnimationFrame(tick)
    }
    raf.current = requestAnimationFrame(tick)
  }
  const end = () => {
    if (published) return
    holding.current = false
    cancelAnimationFrame(raf.current)
    if (progress < 1) { setProgress(0); setPhase('idle') }
  }
  const commit = () => {
    holding.current = false
    cancelAnimationFrame(raf.current)
    setProgress(1)
    setPhase('published')
    if (navigator.vibrate) navigator.vibrate(18)
    onPublished?.()
  }

  return (
    <div data-emerge="" style={{ position: 'relative', ['--obs-delay' as string]: '120ms', filter: 'drop-shadow(0 28px 50px var(--obs-auth-shadow))', margin: '8px 4px 24px' }}>
      <div style={{ background: 'var(--obs-hair-strong)', padding: 1 }} className="cham-seal">
        <div className="cham-seal" style={{ position: 'relative', background: 'var(--obs-ink)', color: '#f2ede3', padding: '48px 32px 32px', overflow: 'hidden' }}>
          {/* faint yantra guide */}
          <svg width="280" height="280" viewBox="0 0 100 100" style={{ position: 'absolute', right: -60, top: -50, opacity: 0.06 }} aria-hidden="true">
            <g stroke="#f2ede3" strokeWidth="0.6" fill="none">
              <rect x="20" y="20" width="60" height="60" /><path d="M50 22 L76 70 L24 70 Z" /><path d="M50 78 L24 30 L76 30 Z" /><circle cx="50" cy="50" r="30" />
            </g>
          </svg>

          <div style={{ position: 'relative', display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
                {!published && <span className="obs-pulse" style={{ width: 6, height: 6 }} />}
                <Label color="var(--obs-copper-soft)" style={{ letterSpacing: '0.2em' }}>{published ? publishedLabel : 'Awaiting authority'}</Label>
              </div>
              <div className="voice-serif" style={{ fontSize: 27, lineHeight: 1.1, marginTop: 8, color: '#f2ede3', letterSpacing: '-0.01em' }}>Authorise {run}</div>
            </div>
            <span className="voice-code cham-sm" style={{ fontSize: 10.5, letterSpacing: '0.06em', padding: '4px 9px', background: 'rgba(177,106,60,0.18)', color: 'var(--obs-copper-soft)', border: '1px solid rgba(177,106,60,0.4)' }}>{authorityCode}</span>
          </div>

          <div style={{ position: 'relative', display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 24, marginTop: 24, paddingTop: 16, borderTop: '1px solid rgba(242,237,227,0.13)' }}>
            {renderFields.map(f => <AField key={f.k} k={f.k} v={f.v} sub={f.sub} />)}
          </div>

          {/* authority rail — marker moves through states (spatial, not colour) */}
          <div style={{ position: 'relative', display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', marginTop: 16 }}>
            <div style={{ position: 'absolute', top: 5, left: '16.6%', right: '16.6%', height: 1, background: 'rgba(242,237,227,0.13)' }} />
            <div style={{ position: 'absolute', top: 1, left: `calc(16.6% + ${stage} * 33.3% - 5px)`, width: 10, height: 10, borderRadius: '50%', background: 'var(--obs-copper)', boxShadow: '0 0 0 4px rgba(177,106,60,0.28)', transition: 'left 250ms cubic-bezier(0.22,0.61,0.36,1)' }} />
            {AUTH_STATES.map((s, i) => (
              <div key={s} style={{ textAlign: i === 0 ? 'left' : i === 2 ? 'right' : 'center', paddingTop: 18 }}>
                <span className="voice-mono" style={{ fontSize: 9.5, color: i <= stage ? '#f2ede3' : 'rgba(242,237,227,0.46)' }}>{s}</span>
              </div>
            ))}
          </div>

          <div style={{ position: 'relative', marginTop: 32 }}>
            {!published ? (
              <button
                onPointerDown={start} onPointerUp={end} onPointerLeave={end} onPointerCancel={end}
                className="cham-sm"
                style={{ position: 'relative', width: '100%', border: 'none', cursor: 'pointer', padding: 0, height: 56, background: 'transparent', overflow: 'hidden', touchAction: 'none', userSelect: 'none' }}
              >
                <span className="cham-sm bg-tamra-copper" style={{ position: 'absolute', inset: 0 }} />
                <span className="cham-sm" style={{ position: 'absolute', left: 0, top: 0, bottom: 0, width: `${progress * 100}%`, background: 'color-mix(in oklab, #b16a3c 55%, #000)', transition: holding.current ? 'none' : 'width 200ms ease' }} />
                <span style={{ position: 'relative', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 10, height: '100%', color: '#fff', fontWeight: 600, fontSize: 13.5, letterSpacing: '0.14em', textTransform: 'uppercase' }}>
                  {phase === 'arming' ? 'Hold to authorise…' : buttonLabel}
                  <span className="voice-mono" style={{ fontSize: 9.5, opacity: 0.75, letterSpacing: '0.1em', textTransform: 'none' }}>{phase === 'arming' ? `${Math.round(progress * 100)}%` : 'press & hold'}</span>
                </span>
              </button>
            ) : (
              <div className="cham-sm" data-emerge="" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '16px 20px', background: 'var(--obs-forest)', color: '#f2ede3' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round"><path d="M20 6 9 17l-5-5" /></svg>
                  <span style={{ fontWeight: 600, fontSize: 13.5, letterSpacing: '0.06em' }}>{run} PUBLISHED</span>
                </div>
                <span className="voice-code" style={{ fontSize: 11, opacity: 0.85 }}>{nowIST()} · ANANYA R.</span>
              </div>
            )}
            <div className="voice-mono" style={{ marginTop: 12, color: 'rgba(242,237,227,0.46)', fontSize: 9.5, lineHeight: 1.6 }}>
              {published ? `Committed to ledger · immutable · ${run}` : 'Publication is irreversible. Amendments require a new run.'}
            </div>
          </div>
        </div>
      </div>
      {/* copper authority edge + wax seal in the folded corner */}
      <span style={{ position: 'absolute', left: 0, top: 16, bottom: 16, width: 3, background: 'var(--obs-copper)' }} />
      <span style={{ position: 'absolute', top: 7, right: 7, width: 15, height: 15, transform: 'rotate(45deg)', background: 'var(--obs-copper)', boxShadow: '0 0 0 3px rgba(177,106,60,0.22)' }} />
    </div>
  )
}
function AField({ k, v, sub }: { k: string; v: string; sub: string }) {
  return (
    <div>
      <div className="voice-mono" style={{ fontSize: 9.5, color: 'rgba(242,237,227,0.46)', marginBottom: 6 }}>{k}</div>
      <div className="tnum" style={{ fontSize: 22, fontWeight: 500, color: '#f2ede3', letterSpacing: '-0.01em' }}>{v}</div>
      <div style={{ fontSize: 11, color: 'rgba(242,237,227,0.7)', marginTop: 2 }}>{sub}</div>
    </div>
  )
}

export function nowIST(): string {
  return new Date().toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: false }) + ' IST'
}
