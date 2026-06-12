import { useEffect, useState } from 'react'
import { Icon } from '@/components/shell/Icon'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EMP',
  archetype: 'command-center',
  signals: 2,
  risks: 0,
  narratives: 0,
  maps: 0,
  bindu: true,
}

const punches = [
  { icon: 'login', tone: 'text-tamra-copper', label: 'Clock In', loc: 'Zone A - Main Entrance', time: '08:00 AM' },
  { icon: 'restaurant', tone: 'text-secondary', label: 'Break Start', loc: 'Cafeteria', time: '12:00 PM' },
  { icon: 'check_circle', tone: 'text-secondary', label: 'Break End', loc: 'Cafeteria', time: '12:30 PM' },
]

function formatHMS(total: number) {
  const h = Math.floor(total / 3600)
  const m = Math.floor((total % 3600) / 60)
  const s = total % 60
  return [h, m, s].map((n) => String(n).padStart(2, '0')).join(':')
}

export function AttendanceOps() {
  const [seconds, setSeconds] = useState(4 * 3600 + 22 * 60 + 15)
  const [onBreak, setOnBreak] = useState(false)

  useEffect(() => {
    const t = setInterval(() => setSeconds((s) => s + 1), 1000)
    return () => clearInterval(t)
  }, [])

  return (
    <div className="w-full max-w-lg mx-auto flex flex-col gap-6">
      <header className="mb-2">
        <p className="font-mono-label text-mono-label text-on-surface-variant tracking-widest uppercase mb-2">
          Ops · Layer 02 · Karma
        </p>
        <h2 className="font-section-title text-section-title !text-4xl text-primary">Attendance</h2>
      </header>

      {/* Shift status */}
      <div className="flex flex-col items-center justify-center text-center space-y-2 pt-2">
        <h2 className="font-pull-quote text-pull-quote text-on-surface">Active Shift</h2>
        <div className="flex items-center gap-2 font-mono-label text-mono-label text-sthira-forest uppercase tracking-widest">
          <span className="bindu good" /> On Location
        </div>
      </div>

      {/* Timer plate */}
      <div className="relative bg-surface-container-lowest hairline-all p-8 flex flex-col items-center justify-center corner-tick mt-2 plate-container">
        <div className="absolute top-4 left-4 font-mono-label text-mono-label text-on-surface-variant uppercase">
          Elapsed
        </div>
        <div className="font-tabular-data text-[64px] leading-none font-light tracking-tight text-primary mt-6 mb-2 tabular-nums">
          {formatHMS(seconds)}
        </div>
        <div className="font-mono-label text-mono-label text-on-surface-variant">Started at 08:00 AM IST</div>
      </div>

      {/* Primary action — the Bindu */}
      <button
        onClick={() => setOnBreak((b) => !b)}
        className="w-full bg-tamra-copper text-white font-mono-label text-mono-label py-4 px-6 flex items-center justify-center gap-3 hover:bg-tamra-copper-bright transition-colors uppercase tracking-widest"
      >
        <Icon name={onBreak ? 'play_circle' : 'pause_circle'} filled />
        {onBreak ? 'Resume Shift' : 'Log Break'}
      </button>

      {/* Secondary actions */}
      <div className="grid grid-cols-2 gap-4">
        <button className="bg-transparent hairline-all text-primary font-mono-label text-mono-label py-3 px-4 flex flex-col items-center justify-center gap-2 hover:bg-surface-container-low transition-colors uppercase">
          <Icon name="location_on" className="text-on-surface-variant" />
          Verify Geo
        </button>
        <button className="bg-transparent hairline-all text-primary font-mono-label text-mono-label py-3 px-4 flex flex-col items-center justify-center gap-2 hover:bg-surface-container-low transition-colors uppercase">
          <Icon name="logout" className="text-on-surface-variant" />
          End Shift
        </button>
      </div>

      {/* Geo verification readout */}
      <div className="hairline-all bg-surface p-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Icon name="my_location" className="text-sthira-forest !text-[20px]" />
          <div>
            <div className="font-tabular-data text-tabular-data text-on-surface">Geo-verified</div>
            <div className="font-mono-label text-mono-label text-on-surface-variant mt-1">
              17.4401° N, 78.3489° E · ±8m
            </div>
          </div>
        </div>
        <span className="font-mono-label text-mono-label text-sthira-forest uppercase">Within Zone A</span>
      </div>

      {/* Recent punches */}
      <div className="mt-4">
        <div className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-4 hairline-b pb-2">
          Recent Punches
        </div>
        <div className="flex flex-col">
          {punches.map((p) => (
            <div key={p.label} className="flex justify-between items-center py-3 hairline-b">
              <div className="flex items-center gap-3">
                <Icon name={p.icon} className={p.tone} />
                <div className="flex flex-col">
                  <span className="font-tabular-data text-tabular-data text-on-surface">{p.label}</span>
                  <span className="font-mono-label text-mono-label text-on-surface-variant mt-1">{p.loc}</span>
                </div>
              </div>
              <span className="font-tabular-data text-tabular-data text-on-surface">{p.time}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
