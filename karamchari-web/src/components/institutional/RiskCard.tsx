// Risk — an emergent threat stated as fact (DOCTRINE §4, §10).
// Severity speaks through a hairline border tone, never alarm fills.
import type { ReactNode } from 'react'

const SEVERITY = {
  critical: { border: 'border-rakta-critical', label: 'text-rakta-critical' },
  elevated: { border: 'border-pita-caution', label: 'text-pita-caution' },
  monitored: { border: 'border-sthira-forest', label: 'text-sthira-forest' },
  neutral: { border: 'border-outline-variant', label: 'text-on-surface-variant' },
} as const

interface RiskCardProps {
  severity: keyof typeof SEVERITY
  title: string
  value?: string
  body?: string
  meta?: ReactNode
  className?: string
}

export function RiskCard({ severity, title, value, body, meta, className = '' }: RiskCardProps) {
  const tone = SEVERITY[severity]
  return (
    <div className={`border-l ${tone.border} pl-4 ${className}`}>
      <p className={`font-mono-label text-mono-label uppercase mb-1 ${tone.label}`}>{title}</p>
      {value && (
        <p className="font-section-title text-section-title !text-4xl text-primary">{value}</p>
      )}
      {body && (
        <p className="font-tabular-data text-tabular-data text-on-surface-variant mt-2">{body}</p>
      )}
      {meta && <div className="mt-2">{meta}</div>}
    </div>
  )
}
