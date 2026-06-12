// Payroll run lifecycle — the formal state model behind the Payroll Cockpit and
// Run Console (design/archetypes/command-center.md). Transitions absent from the
// map cannot occur; there is no force path. The irreversibility boundary is
// `locked`: before it, a run can reopen for correction; after it, corrections
// flow through the Corrections Console as new acts, never backward transitions.

export type RunState = 'draft' | 'calculated' | 'validated' | 'locked' | 'approved' | 'published'

export const RUN_SEQUENCE: readonly RunState[] = [
  'draft',
  'calculated',
  'validated',
  'locked',
  'approved',
  'published',
]

export const STATE_LABELS: Record<RunState, string> = {
  draft: 'Draft',
  calculated: 'Calculated',
  validated: 'Validated',
  locked: 'Locked',
  approved: 'Approved',
  published: 'Published',
}

const TRANSITIONS: Record<RunState, readonly RunState[]> = {
  draft: ['calculated'],
  calculated: ['validated', 'draft'],
  validated: ['locked', 'draft'],
  locked: ['approved'],
  approved: ['published'],
  published: [],
}

// The decisive act that moves a run forward from each state. The Bindu wears
// this label; it is the only copper action on the console at any moment.
export const FORWARD_VERBS: Record<RunState, string | null> = {
  draft: 'Run Calculation',
  calculated: 'Run Validation',
  validated: 'Lock Run',
  locked: 'Approve Run',
  approved: 'Publish Run',
  published: null,
}

export interface TransitionRecord {
  from: RunState
  to: RunState
  actor: string
  ts: string
  reason: string
}

export function canTransition(from: RunState, to: RunState): boolean {
  return TRANSITIONS[from].includes(to)
}

export function nextForward(state: RunState): RunState | null {
  const idx = RUN_SEQUENCE.indexOf(state)
  return idx >= 0 && idx < RUN_SEQUENCE.length - 1 ? RUN_SEQUENCE[idx + 1] : null
}

export function transition(from: RunState, to: RunState, actor: string, reason: string): TransitionRecord {
  if (!canTransition(from, to)) {
    throw new Error(`Illegal payroll transition: ${from} -> ${to}`)
  }
  return { from, to, actor, ts: new Date().toISOString(), reason }
}

// First state from which no reopening is possible.
export const IRREVERSIBLE_FROM: RunState = 'locked'
