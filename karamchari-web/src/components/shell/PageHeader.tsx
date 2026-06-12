interface PageHeaderProps {
  kicker: string
  title: string
  sub?: string
  right?: React.ReactNode
}

export function PageHeader({ kicker, title, sub, right }: PageHeaderProps) {
  return (
    <header className="mb-12 border-b border-outline-variant pb-8 flex flex-col md:flex-row md:items-end md:justify-between gap-6">
      <div>
        <p className="font-mono-label text-mono-label text-on-surface-variant mb-4 tracking-widest uppercase">
          {kicker}
        </p>
        <h2 className="font-section-title text-section-title text-primary !text-4xl md:!text-section-title">
          {title}
        </h2>
        {sub && (
          <p className="font-pull-quote text-pull-quote text-on-surface-variant mt-4 max-w-3xl !text-xl md:!text-2xl">
            {sub}
          </p>
        )}
      </div>
      {right && <div className="flex-shrink-0">{right}</div>}
    </header>
  )
}
