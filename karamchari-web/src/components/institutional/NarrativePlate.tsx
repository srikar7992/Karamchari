// Narrative — institutional reasoning in prose (DOCTRINE §4, §7).
// Newsreader italic on Sutra Ivory or Yantra Indigo. Declarative, sourced, calm.
import type { ReactNode } from 'react'

interface NarrativePlateProps {
  kicker?: string
  children: ReactNode
  tone?: 'ivory' | 'indigo'
  source?: string
  actions?: ReactNode
  className?: string
}

export function NarrativePlate({
  kicker = 'Buddhi Insight',
  children,
  tone = 'ivory',
  source,
  actions,
  className = '',
}: NarrativePlateProps) {
  const indigo = tone === 'indigo'
  return (
    <section
      className={`${indigo ? 'bg-yantra-indigo text-on-primary' : 'bg-ivory-2 hairline-all corner-tick'} p-6 ${className}`}
    >
      <h3
        className={`font-mono-label text-mono-label uppercase tracking-widest mb-4 flex items-center gap-2 ${
          indigo ? 'text-surface-variant' : 'text-on-surface-variant'
        }`}
      >
        <span className={`w-2 h-2 rounded-full inline-block ${indigo ? 'bg-tamra-copper-bright' : 'bg-tamra-copper'}`} />
        {kicker}
      </h3>
      <div
        className={`font-serif italic font-light text-[22px] leading-[1.35] ${indigo ? 'text-white' : 'text-ink'}`}
      >
        {children}
      </div>
      {source && (
        <div
          className={`font-mono text-[10px] uppercase tracking-wider mt-4 ${
            indigo ? 'text-surface-variant opacity-70' : 'text-on-surface-variant'
          }`}
        >
          {source}
        </div>
      )}
      {actions && <div className="mt-6">{actions}</div>}
    </section>
  )
}
