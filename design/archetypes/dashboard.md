# Archetype: Dashboard

ID: `dashboard`
Question: *What needs my attention in my domain right now?*

A dashboard is a triage surface, not a report. It exists to route the reader to the
right working surface within one glance and one click. Anything that does not change
what the reader does next is removed.

## Zones (top to bottom)

1. **Greeting / context header** — persona-scoped. Operator dashboards may greet by
   name (Newsreader); manager/exec dashboards state scope ("Engineering Delivery Pod
   Alpha").
2. **Signal row** — SignalCards within persona budget. Numbers with consequences only.
3. **Working surface** — the persona's main object: shift timeline (EMP), attendance
   board + action queue (MGR), risk vectors + topography (EXEC).
4. **Narrative plate** — one Buddhi NarrativePlate when the numbers need reasoning.
5. **Map** — at most one topology surface (9-box, roster grid).

## Classification profile

Persona budgets apply exactly (DOCTRINE §4). Dashboards are the screens most prone to
accretion; the budget is the diet. A new card requires removing an old one.

## Bindu

Zero or one. If present, it is the single decision the persona most plausibly takes
from triage (Acknowledge insight, Generate report). Row-level approve buttons in
queues stay ink/outline — they are repeated actions, not the decision locus.

## Component manifest

`SignalCard`, `RiskCard`, `NarrativePlate`, `BinduButton`, `InstitutionalTable`,
`MapSurface`.

## Used by

My Dashboard, Manager Dashboard, Executive Dashboard, Compliance Dashboard,
Workforce Intelligence Dashboard, Attendance Health Dashboard.

## Anti-patterns

- KPI walls. Twelve charts is a rejected screen, not a dense one.
- Charts without consequence ("page views over time" energy).
- Duplicate surfaces: the dashboard links to the queue, it does not re-implement it.
