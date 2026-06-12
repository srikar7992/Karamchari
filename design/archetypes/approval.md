# Archetype: Approval Workspace

ID: `approval`
Question: *What awaits my decision, and what is the evidence?*

Approval is an act of authority. The surface presents evidence calmly and records the
decision permanently. No urgency theater: the queue is ordered, the evidence is
complete, the decision is one act.

## Zones

1. **Queue rail** (left) — pending items as plain cards: kind in mono-label, requester,
   age. Selection state, not notification noise.
2. **Evidence panel** (right) — the selected item in full: request facts in tabular
   data, balance/policy context, conflicts (e.g. team calendar overlap), prior
   decisions on similar requests.
3. **Decision strip** — Approve / Reject / Delegate. Approve and Reject are ink
   (repeated, row-level acts of the role, not the screen's single decision).
4. **Decision timeline** — CaseTimeline of the item's routing: submitted, escalated,
   delegated, decided. Every decision lands here with actor and timestamp.

## Classification profile

Signals 1–2 (queue depth, SLA position), Risks 1 (policy conflict on the selected
item), Narratives 0–1, Maps 0–1 (calendar/coverage conflict view).

## Bindu

Usually zero — approve/reject repeat per item and stay ink. One copper only when a
batch-level act exists ("Finalize Delegation Window").

## Component manifest

`InstitutionalTable`, `CaseTimeline`, `RiskCard`, `LedgerStrip`, `BinduButton` (rare).

## Used by

Approvals Inbox (unified), Leave Approval, Regularization Queue, Offer Management,
IT Declaration Review, Arrears/Corrections Consoles, FnF Settlement, Reimbursement
Admin, Loan Admin, Variable Pay, Salary Revision, Timesheets, Swap Center, Promotion
Pipeline, Expense Claims Admin, Review Inbox & Team Goals.

## Anti-patterns

- Approving without evidence on screen. The panel always shows policy context.
- Copper Approve buttons. Authority is exercised in ink; copper is for the rare
  screen-level decision.
- Decisions that vanish. Every decision appends to the visible timeline.
