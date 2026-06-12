// The Bindu — the single copper decision locus (DOCTRINE §3).
// At most one per screen; check-doctrine.mjs counts every <BinduButton> usage.
// Everything else stays ink or outline.
import type { ButtonHTMLAttributes } from 'react'

export function BinduButton({ className = '', children, ...rest }: ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      className={`px-5 py-2.5 bg-tamra-copper text-white font-mono-label text-mono-label uppercase tracking-widest hover:bg-tamra-copper-bright transition-colors ${className}`}
      {...rest}
    >
      {children}
    </button>
  )
}
