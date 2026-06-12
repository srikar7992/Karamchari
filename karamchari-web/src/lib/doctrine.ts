import type { Persona } from './nav'

// Screen archetypes — design/archetypes/README.md. Frozen at twelve; amendment
// required to extend (DOCTRINE §4b).
export type Archetype =
  | 'dashboard'
  | 'registry'
  | 'record-detail'
  | 'approval'
  | 'case'
  | 'builder'
  | 'analytics'
  | 'administration'
  | 'timeline'
  | 'intelligence'
  | 'command-center'
  | 'topology'

export const ARCHETYPES: readonly Archetype[] = [
  'dashboard',
  'registry',
  'record-detail',
  'approval',
  'case',
  'builder',
  'analytics',
  'administration',
  'timeline',
  'intelligence',
  'command-center',
  'topology',
]

// Information-class taxonomy — design/DOCTRINE.md §4. Frozen; amendment required to extend.
export interface PageClassification {
  persona: Persona
  archetype: Archetype
  signals: number
  risks: number
  narratives: number
  maps: number
  bindu: boolean
}

// Per-persona density budgets (maxima). Mirrored in scripts/check-doctrine.mjs.
export const PERSONA_BUDGETS: Record<
  Persona,
  Omit<PageClassification, 'persona' | 'archetype' | 'bindu'>
> = {
  EMP: { signals: 3, risks: 1, narratives: 1, maps: 1 },
  MGR: { signals: 8, risks: 3, narratives: 1, maps: 1 },
  EXEC: { signals: 3, risks: 5, narratives: 1, maps: 1 },
  CMP: { signals: 1, risks: 1, narratives: 1, maps: 1 },
  SYS: { signals: 4, risks: 1, narratives: 1, maps: 1 },
}
