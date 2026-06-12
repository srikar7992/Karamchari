// Signal — a numeric indicator with consequence (DOCTRINE §4).
// One SignalCard = one declared signal in PAGE_CLASSIFICATION.

interface SignalCardProps {
  label: string
  value: string
  unit?: string
  note?: string
  bindu?: 'good' | 'caution' | 'rakta-critical' | 'neutral'
  bar?: { pct: number; tone?: string }
  cornerTick?: boolean
  className?: string
}

export function SignalCard({ label, value, unit, note, bindu, bar, cornerTick, className = '' }: SignalCardProps) {
  return (
    <div className={`hairline-all bg-surface-container-lowest p-6 ${cornerTick ? 'corner-tick' : ''} ${className}`}>
      <div className="flex justify-between items-start mb-4">
        <div className="font-mono-label text-mono-label text-on-surface-variant uppercase">{label}</div>
        {bindu && <span className={`bindu ${bindu}`} />}
      </div>
      <div className="flex items-baseline gap-2">
        <span className="font-serif font-light text-[40px] leading-none text-primary">{value}</span>
        {unit && <span className="font-tabular-data text-tabular-data text-on-surface-variant">{unit}</span>}
      </div>
      {bar && (
        <div className="w-full bg-surface-variant h-1 mt-4">
          <div className={`h-1 ${bar.tone ?? 'bg-primary'}`} style={{ width: `${bar.pct}%` }} />
        </div>
      )}
      {note && <div className="font-tabular-data text-[12px] text-on-surface-variant mt-4">{note}</div>}
    </div>
  )
}
