# Archetype: Case Workspace

ID: `case`
Question: *Where does this case stand, and what is its next step?*

A case is work with a lifecycle: opened, progressed through stages, closed with an
outcome. The workspace makes the lifecycle visible and the next step unmistakable.

## Zones

1. **Case header** — case id in JetBrains Mono, subject entity, stage indicator
   (hairline step strip, current stage marked with a bindu), owner, age.
2. **Task ledger** — InstitutionalTable of case tasks: task, owner, due, status bindu.
   Complete/skip actions ink, inline.
3. **Document rail** — required artifacts with verification state (submitted /
   verified / rejected).
4. **Case timeline** — CaseTimeline: every stage transition, document event, and
   comment, actor-stamped.
5. **Outcome strip** — when closing: outcome classification + mandatory note.

## Classification profile

Signals 1–2 (completion %, days in stage), Risks 0–1 (blocked/overdue), Narratives
0–1 (Buddhi case summary on complex cases), Maps 0–1.

## Bindu

One: the case's current decisive act — "Complete Onboarding", "Close Investigation",
"Resolve Ticket". Task-level completion stays ink.

## Component manifest

`CaseTimeline`, `InstitutionalTable`, `SignalCard`, `RiskCard`, `NarrativePlate`,
`BinduButton`, `LedgerStrip`.

## Used by

Onboarding Case, Offboarding View, Leave Investigation & Absence Cases, Compliance
Violations Queue, Legal Hold Console, Helpdesk Tickets (agent + my), Collections
Workbench, Biometric Enrollment, Review Submission, 360 Feedback, My Goals & Reviews,
Bonus Plan lifecycle, Succession Plan Workspace.

## Anti-patterns

- Kanban-as-decoration. Stage strip is structure; cards do not bounce between columns.
- Hidden lifecycle: the current stage and next step are visible without interaction.
- Closing without an outcome record. Cases end with a classified, attributed outcome.
