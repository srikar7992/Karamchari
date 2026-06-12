// Forensic event trail — Sutra made visible (design/archetypes/timeline.md).
// ISO timestamps in mono, severity by hairline tone, entries immutable.
import type { ReactNode } from 'react'

const TONES = {
  neutral: { line: 'border-outline-variant', dot: 'bg-outline-variant', tag: '' },
  good: { line: 'border-sthira-forest', dot: 'bg-sthira-forest', tag: 'text-sthira-forest' },
  caution: { line: 'border-pita-caution', dot: 'bg-pita-caution', tag: 'text-pita-caution' },
  critical: { line: 'border-rakta-critical', dot: 'bg-rakta-critical', tag: 'text-rakta-critical' },
  copper: { line: 'border-tamra-copper', dot: 'bg-tamra-copper', tag: 'text-tamra-copper' },
} as const

export interface TimelineEntry {
  ts: string
  tag: string
  tone?: keyof typeof TONES
  body: ReactNode
}

interface CaseTimelineProps {
  entries: TimelineEntry[]
  className?: string
}

export function CaseTimeline({ entries, className = '' }: CaseTimelineProps) {
  return (
    <div className={`space-y-4 ${className}`}>
      {entries.map((e) => {
        const tone = TONES[e.tone ?? 'neutral']
        return (
          <div key={e.ts + e.tag} className={`pl-4 border-l-2 ${tone.line} relative`}>
            <div className={`absolute w-2 h-2 ${tone.dot} rounded-full -left-[5px] top-1`} />
            <div className="flex justify-between items-baseline mb-1">
              <span className="font-mono text-[10px] text-on-surface-variant">{e.ts}</span>
              <span
                className={`font-mono text-[9px] bg-surface-container-high px-1.5 py-0.5 border border-outline-variant ${tone.tag}`}
              >
                {e.tag}
              </span>
            </div>
            <div className="font-tabular-data text-[13px] text-primary">{e.body}</div>
          </div>
        )
      })}
    </div>
  )
}
