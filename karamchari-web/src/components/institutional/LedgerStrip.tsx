// Record-level provenance strip: ids, freshness, actors. JetBrains Mono only.
// Complements the shell telemetry band (DOCTRINE §5) at surface granularity.

interface LedgerStripProps {
  entries: { label: string; value: string }[]
  className?: string
}

export function LedgerStrip({ entries, className = '' }: LedgerStripProps) {
  return (
    <div
      className={`hairline-t pt-3 flex flex-wrap gap-x-8 gap-y-2 font-mono text-[10px] uppercase tracking-wider text-on-surface-variant ${className}`}
    >
      {entries.map((e) => (
        <span key={e.label}>
          {e.label} <span className="text-primary normal-case">{e.value}</span>
        </span>
      ))}
    </div>
  )
}
