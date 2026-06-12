// Offer lifecycle — the formal state model behind Offer Management. Same
// discipline as payroll-lifecycle.ts: the TRANSITIONS map is the only source of
// legality, absent transitions cannot occur, and there is no force path. The
// irreversibility boundary is `extended`: once an offer reaches the candidate
// it can be accepted or declined, never silently rewritten — a changed offer is
// a new version under GF-02. The terminal forward act is the hire itself:
// HIRE-ID-01 says an employee identity is created only from an accepted offer.

export type OfferState = 'draft' | 'approved' | 'extended' | 'accepted' | 'hired' | 'declined'

// Forward path. `declined` is a terminal fork from `extended`, not a sequence step.
export const OFFER_SEQUENCE: readonly OfferState[] = [
  'draft',
  'approved',
  'extended',
  'accepted',
  'hired',
]

export const STATE_LABELS: Record<OfferState, string> = {
  draft: 'Draft',
  approved: 'Approved',
  extended: 'Extended',
  accepted: 'Accepted',
  hired: 'Hired',
  declined: 'Declined',
}

const TRANSITIONS: Record<OfferState, readonly OfferState[]> = {
  draft: ['approved'],
  approved: ['extended', 'draft'],
  extended: ['accepted', 'declined'],
  accepted: ['hired'],
  hired: [],
  declined: [],
}

// The decisive act that moves an offer forward from each state. The Bindu wears
// this label; it is the only copper action on the console at any moment.
export const FORWARD_VERBS: Record<OfferState, string | null> = {
  draft: 'Approve Offer',
  approved: 'Extend Offer',
  extended: 'Record Acceptance',
  accepted: 'Create Employee Identity',
  hired: null,
  declined: null,
}

export interface OfferTransitionRecord {
  from: OfferState
  to: OfferState
  actor: string
  ts: string
  reason: string
}

export function canTransition(from: OfferState, to: OfferState): boolean {
  return TRANSITIONS[from].includes(to)
}

export function nextForward(state: OfferState): OfferState | null {
  const idx = OFFER_SEQUENCE.indexOf(state)
  return idx >= 0 && idx < OFFER_SEQUENCE.length - 1 ? OFFER_SEQUENCE[idx + 1] : null
}

export function transition(from: OfferState, to: OfferState, actor: string, reason: string): OfferTransitionRecord {
  if (!canTransition(from, to)) {
    throw new Error(`Illegal offer transition: ${from} -> ${to}`)
  }
  return { from, to, actor, ts: new Date().toISOString(), reason }
}

// First state from which no reopening is possible.
export const IRREVERSIBLE_FROM: OfferState = 'extended'
