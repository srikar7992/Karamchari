// Map — structural topology frame (DOCTRINE §4). The content it frames counts as
// one Map in PAGE_CLASSIFICATION; it is never a KPI.
import type { ReactNode } from 'react'

interface MapSurfaceProps {
  kicker: string
  title?: string
  children: ReactNode
  axes?: { x: string; y: string }
  tone?: 'light' | 'indigo'
  className?: string
}

export function MapSurface({ kicker, title, children, axes, tone = 'light', className = '' }: MapSurfaceProps) {
  const indigo = tone === 'indigo'
  return (
    <section
      className={`${indigo ? 'bg-yantra-indigo text-on-primary' : 'bg-surface-container-lowest hairline-all'} p-6 flex flex-col ${className}`}
    >
      <header className={`mb-6 pb-4 ${indigo ? 'border-b border-white/20' : 'hairline-b'}`}>
        <h3
          className={`font-mono-label text-mono-label uppercase ${indigo ? 'text-secondary-container opacity-70' : 'text-on-surface-variant'}`}
        >
          {kicker}
        </h3>
        {title && <h4 className="font-pull-quote text-pull-quote text-current !text-2xl mt-2">{title}</h4>}
      </header>
      <div className="flex-1">{children}</div>
      {axes && (
        <div
          className={`flex justify-between text-[10px] mt-4 uppercase tracking-widest font-mono-label ${
            indigo ? 'opacity-50' : 'text-on-surface-variant'
          }`}
        >
          <span>{axes.x}</span>
          <span>{axes.y}</span>
        </div>
      )}
    </section>
  )
}
