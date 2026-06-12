// Lab chrome. Marks every experiment as non-production and provides movement
// between experiments and back to the governed application.

import type { ReactNode } from 'react'

export type LabId = 'ledger' | 'observatory' | 'topology'

export const LAB_ROUTES: { id: LabId; label: string }[] = [
  { id: 'ledger', label: 'A · Constitutional Ledger' },
  { id: 'observatory', label: 'B · Observatory' },
  { id: 'topology', label: 'C · Living Topology' },
]

export function LabShell({ active, children }: { active: LabId; children: ReactNode }) {
  const dark = active === 'observatory'
  return (
    <div>
      <div
        className={`flex flex-wrap items-center gap-x-6 gap-y-2 px-6 py-2 font-mono text-[9.5px] uppercase tracking-[0.2em] border-b ${
          dark ? 'bg-ink-2 text-ivory/60 border-ivory/15' : 'bg-ivory-2 text-ink/60 border-ink/15'
        }`}
      >
        <span className={dark ? 'text-tamra-copper-bright' : 'text-tamra-copper'}>
          Visual experiment — not production, outside doctrine jurisdiction
        </span>
        {LAB_ROUTES.map((r) => (
          <a
            key={r.id}
            href={`#/lab/${r.id}`}
            className={r.id === active ? (dark ? 'text-ivory underline' : 'text-ink underline') : 'hover:underline'}
          >
            {r.label}
          </a>
        ))}
        <a href="#/pulse" className="ml-auto hover:underline">
          ← Governed application
        </a>
      </div>
      {children}
    </div>
  )
}
