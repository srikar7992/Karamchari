// The institutional ledger surface: mono-label headers, tabular data, hairline rows.
// Newsreader never enters here (DOCTRINE §6).
import type { ReactNode } from 'react'

export interface TableColumn<T> {
  key: string
  header: string
  align?: 'left' | 'right'
  render?: (row: T) => ReactNode
}

interface InstitutionalTableProps<T> {
  columns: TableColumn<T>[]
  rows: T[]
  rowKey: (row: T) => string
  className?: string
}

export function InstitutionalTable<T>({ columns, rows, rowKey, className = '' }: InstitutionalTableProps<T>) {
  return (
    <table className={`w-full text-left font-tabular-data text-tabular-data ${className}`}>
      <thead>
        <tr>
          {columns.map((c) => (
            <th
              key={c.key}
              className={`pb-3 font-mono-label text-mono-label text-on-surface-variant uppercase border-b border-outline-variant font-normal ${
                c.align === 'right' ? 'text-right' : ''
              }`}
            >
              {c.header}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.map((row) => (
          <tr key={rowKey(row)} className="border-b border-outline-variant">
            {columns.map((c) => (
              <td key={c.key} className={`py-[14px] ${c.align === 'right' ? 'text-right' : ''}`}>
                {c.render ? c.render(row) : String((row as Record<string, unknown>)[c.key] ?? '')}
              </td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  )
}
