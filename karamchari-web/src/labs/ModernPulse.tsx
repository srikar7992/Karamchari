// LAB EXPERIMENT D — ultra-modern, youthful direction probe.
// Not production. Outside doctrine jurisdiction (design/EXPERIMENTS.md).
// Deliberately breaks current doctrine: vivid gradients, glassmorphism, spring
// motion, count-up numbers, staggered entrances, glow. Question: should
// Karamchari's visual language move here — wholly, partly (per persona), or
// not at all?

import { useEffect, useRef, useState } from 'react'

function useCountUp(target: number, ms = 1200, decimals = 0) {
  const [v, setV] = useState(0)
  const raf = useRef(0)
  useEffect(() => {
    const t0 = performance.now()
    const tick = (t: number) => {
      const p = Math.min(1, (t - t0) / ms)
      const eased = 1 - Math.pow(1 - p, 3)
      setV(Number((target * eased).toFixed(decimals)))
      if (p < 1) raf.current = requestAnimationFrame(tick)
    }
    raf.current = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(raf.current)
  }, [target, ms, decimals])
  return v
}

function ShiftRing({ pct }: { pct: number }) {
  const r = 84
  const c = 2 * Math.PI * r
  const shown = useCountUp(pct, 1400)
  return (
    <div className="relative w-56 h-56">
      <svg viewBox="0 0 200 200" className="w-full h-full -rotate-90">
        <defs>
          <linearGradient id="ringGrad" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="#22d3ee" />
            <stop offset="55%" stopColor="#818cf8" />
            <stop offset="100%" stopColor="#e879f9" />
          </linearGradient>
        </defs>
        <circle cx="100" cy="100" r={r} fill="none" stroke="rgba(255,255,255,0.08)" strokeWidth="14" />
        <circle
          cx="100" cy="100" r={r} fill="none" stroke="url(#ringGrad)" strokeWidth="14" strokeLinecap="round"
          strokeDasharray={c} strokeDashoffset={c * (1 - shown / 100)}
          style={{ filter: 'drop-shadow(0 0 12px rgba(129,140,248,0.65))', transition: 'stroke-dashoffset 80ms linear' }}
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <span className="text-5xl font-bold tracking-tight bg-gradient-to-r from-cyan-300 via-indigo-300 to-fuchsia-300 bg-clip-text text-transparent">
          {Math.round(shown)}%
        </span>
        <span className="text-xs font-medium text-white/50 mt-1">shift complete</span>
        <span className="text-[11px] text-white/35">3h 12m to go</span>
      </div>
    </div>
  )
}

const STATS = [
  { label: 'Attendance streak', value: 23, unit: 'days', emoji: '🔥', grad: 'from-orange-400 to-rose-500', note: 'Personal best: 31' },
  { label: 'Leave balance', value: 14, unit: 'days', emoji: '🌴', grad: 'from-emerald-400 to-teal-500', note: '3 expire in Dec' },
  { label: 'This month pay', value: 42.8, unit: 'k', emoji: '💸', grad: 'from-cyan-400 to-blue-500', note: 'Payday in 6 days', decimals: 1 },
  { label: 'Kudos received', value: 9, unit: '', emoji: '⚡', grad: 'from-violet-400 to-fuchsia-500', note: '2 new this week' },
]

const FEED = [
  { who: 'Priya', grad: 'from-fuchsia-500 to-pink-500', text: 'approved your leave request for Jun 20–21', time: '2m', chip: 'Approved ✓', chipCls: 'bg-emerald-400/15 text-emerald-300' },
  { who: 'Rahul', grad: 'from-cyan-500 to-blue-500', text: 'sent you kudos for covering Zone East', time: '1h', chip: '+1 ⚡', chipCls: 'bg-violet-400/15 text-violet-300' },
  { who: 'Team', grad: 'from-amber-400 to-orange-500', text: 'MORN-A crew hit 100% coverage, 5 days straight', time: '3h', chip: 'Streak 🔥', chipCls: 'bg-orange-400/15 text-orange-300' },
]

const ACTIONS = [
  { label: 'Clock out', grad: 'from-cyan-500 to-indigo-500' },
  { label: 'Request leave', grad: 'from-emerald-500 to-teal-500' },
  { label: 'Swap shift', grad: 'from-violet-500 to-fuchsia-500' },
  { label: 'View payslip', grad: 'from-rose-500 to-orange-500' },
]

function Stat({ s, i }: { s: (typeof STATS)[number]; i: number }) {
  const v = useCountUp(s.value, 1100 + i * 150, s.decimals ?? 0)
  return (
    <div
      className="group rounded-3xl border border-white/10 bg-white/[0.06] backdrop-blur-xl p-5 transition-all duration-300 hover:-translate-y-1.5 hover:bg-white/[0.1] hover:shadow-[0_18px_50px_-12px_rgba(129,140,248,0.35)] opacity-0"
      style={{ animation: `fadeUp 600ms cubic-bezier(0.22,1,0.36,1) ${200 + i * 110}ms forwards` }}
    >
      <div className={`w-10 h-10 rounded-2xl bg-gradient-to-br ${s.grad} flex items-center justify-center text-lg shadow-lg mb-4 transition-transform duration-300 group-hover:scale-110 group-hover:rotate-6`}>
        {s.emoji}
      </div>
      <div className="text-3xl font-bold tracking-tight">
        {v}
        <span className="text-base font-semibold text-white/40 ml-1">{s.unit}</span>
      </div>
      <div className="text-[13px] font-medium text-white/60 mt-1">{s.label}</div>
      <div className="text-[11px] text-white/35 mt-2">{s.note}</div>
    </div>
  )
}

export function ModernPulse() {
  return (
    <div className="min-h-screen bg-[#070A14] text-white overflow-hidden relative font-sans">
      <style>{`
        @keyframes fadeUp { from { opacity: 0; transform: translateY(22px) } to { opacity: 1; transform: none } }
        @keyframes blob1 { 0%,100% { transform: translate(0,0) scale(1) } 50% { transform: translate(60px,-40px) scale(1.15) } }
        @keyframes blob2 { 0%,100% { transform: translate(0,0) scale(1) } 50% { transform: translate(-50px,50px) scale(1.1) } }
        @keyframes shimmer { 0% { background-position: -200% 0 } 100% { background-position: 200% 0 } }
      `}</style>

      {/* ambient gradient blobs */}
      <div className="pointer-events-none absolute -top-40 -left-32 w-[480px] h-[480px] rounded-full bg-indigo-600/30 blur-3xl" style={{ animation: 'blob1 14s ease-in-out infinite' }} />
      <div className="pointer-events-none absolute top-1/3 -right-40 w-[520px] h-[520px] rounded-full bg-fuchsia-600/20 blur-3xl" style={{ animation: 'blob2 18s ease-in-out infinite' }} />
      <div className="pointer-events-none absolute bottom-0 left-1/4 w-[420px] h-[420px] rounded-full bg-cyan-500/15 blur-3xl" style={{ animation: 'blob1 22s ease-in-out infinite' }} />

      <div className="relative max-w-[1100px] mx-auto px-6 py-10">
        {/* header */}
        <header className="flex flex-wrap items-center justify-between gap-4 mb-10 opacity-0" style={{ animation: 'fadeUp 600ms cubic-bezier(0.22,1,0.36,1) 60ms forwards' }}>
          <div>
            <p className="text-sm text-white/45 font-medium">Thursday, June 12 · Zone A</p>
            <h1 className="text-4xl md:text-5xl font-extrabold tracking-tight mt-1">
              Morning,{' '}
              <span className="bg-gradient-to-r from-cyan-300 via-indigo-300 to-fuchsia-300 bg-clip-text text-transparent">Asha</span>{' '}
              👋
            </h1>
          </div>
          <div
            className="px-4 py-2 rounded-full border border-white/10 text-sm font-semibold text-white/80"
            style={{
              background: 'linear-gradient(110deg, rgba(255,255,255,0.06) 40%, rgba(255,255,255,0.18) 50%, rgba(255,255,255,0.06) 60%)',
              backgroundSize: '200% 100%',
              animation: 'shimmer 2.6s linear infinite',
            }}
          >
            🔥 23-day streak
          </div>
        </header>

        <div className="grid grid-cols-1 lg:grid-cols-5 gap-6">
          {/* shift ring card */}
          <section
            className="lg:col-span-2 rounded-[2rem] border border-white/10 bg-white/[0.05] backdrop-blur-xl p-8 flex flex-col items-center justify-center opacity-0"
            style={{ animation: 'fadeUp 600ms cubic-bezier(0.22,1,0.36,1) 140ms forwards' }}
          >
            <ShiftRing pct={64} />
            <div className="flex gap-3 mt-6">
              <span className="px-3 py-1.5 rounded-full bg-emerald-400/15 text-emerald-300 text-xs font-semibold">On time today</span>
              <span className="px-3 py-1.5 rounded-full bg-cyan-400/15 text-cyan-300 text-xs font-semibold">MORN-A · 06:00–15:00</span>
            </div>
          </section>

          {/* stats */}
          <section className="lg:col-span-3 grid grid-cols-2 gap-4">
            {STATS.map((s, i) => (
              <Stat key={s.label} s={s} i={i} />
            ))}
          </section>
        </div>

        {/* quick actions */}
        <section className="mt-8 flex flex-wrap gap-3 opacity-0" style={{ animation: 'fadeUp 600ms cubic-bezier(0.22,1,0.36,1) 480ms forwards' }}>
          {ACTIONS.map((a) => (
            <button
              key={a.label}
              className={`px-6 py-3.5 rounded-2xl bg-gradient-to-r ${a.grad} font-bold text-[15px] shadow-lg transition-all duration-200 hover:scale-[1.05] hover:shadow-2xl active:scale-95`}
            >
              {a.label}
            </button>
          ))}
        </section>

        {/* activity feed */}
        <section className="mt-8 rounded-[2rem] border border-white/10 bg-white/[0.04] backdrop-blur-xl p-6 opacity-0" style={{ animation: 'fadeUp 600ms cubic-bezier(0.22,1,0.36,1) 620ms forwards' }}>
          <h2 className="text-sm font-bold text-white/50 uppercase tracking-wider mb-5">Happening now</h2>
          <ul className="space-y-4">
            {FEED.map((f) => (
              <li key={f.text} className="flex items-center gap-4 group">
                <div className={`w-11 h-11 rounded-2xl bg-gradient-to-br ${f.grad} flex items-center justify-center font-bold text-sm shadow-md transition-transform duration-200 group-hover:scale-110`}>
                  {f.who[0]}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-[14px] text-white/85">
                    <span className="font-bold">{f.who}</span> {f.text}
                  </p>
                  <p className="text-[11px] text-white/35">{f.time} ago</p>
                </div>
                <span className={`px-3 py-1 rounded-full text-xs font-semibold ${f.chipCls}`}>{f.chip}</span>
              </li>
            ))}
          </ul>
        </section>

        <footer className="mt-8 text-[11px] text-white/25 font-medium">
          Experiment D · ultra-modern probe · every doctrine rule intentionally broken: gradients, glass, glow, spring hover, count-up, stagger, emoji
        </footer>
      </div>
    </div>
  )
}
