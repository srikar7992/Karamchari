# Karamchari Design Doctrine

Binding rules for every screen. Philosophy stays structural; interaction stays operational.
Reference critique: "Quiet Power" review, 2026-06-11.

## 1. Positioning

Karamchari is not compared to HRMS. Category: Institutional Operating System.
"Workday manages employees. Karamchari manages institutional behavior."

## 2. Cadence Hierarchy (navigation order)

Navigation is ordered by frequency of human contact, not by layer number.

| Cadence | Persona | Surfaces |
|---|---|---|
| Daily | Operators | Ops, Pulse (90% of all sessions) |
| Weekly | Managers | Pulse, Ops, Topology |
| Monthly | Executives | Buddhi, Temporal, Adaptive |
| Rarely | Compliance | Sutra, Recovery |

Layer numbers (01-09) remain visible as annotations. They are structure, not menu.

## 3. Spatial Principles

- Large margins. 64px page margin desktop. Never SAP-dense.
- Few borders. Whitespace separates first; hairlines (rgba(14,15,18,0.12)) second; filled containers last.
- Persistent navigation. SideNav and TopNav never move, never collapse on desktop, identical on every screen.
- One primary decision per screen — the Bindu. Tamra Copper (#B16A3C) is reserved for it. Zero or one copper action per screen. Never two. Repeated row-level actions (approve queues, audit links) stay ink/outline.

## 4. Information Classes and Density Budgets

Four information classes. Budgets count per class — a map is not a KPI.

| Class | Definition |
|---|---|
| Signal | Numeric indicator (KPI, score, count) |
| Risk | Emergent threat (flight risk, anomaly, violation) |
| Narrative | Institutional reasoning in prose (Buddhi) |
| Map | Structural topology (9-box, roster grid, calibration map, org chain) |

| Screen class | Signals | Risks | Narratives | Maps | Actions |
|---|---|---|---|---|---|
| Operator (EMP) | 3 | — | 1 | — | 1 primary + 1 secondary |
| Manager (MGR) | 8 | 3 | 1 | 1 | 1 team overview |
| Executive (EXEC) | 3 | 5 | 1 | 1 | 1 |
| Compliance (CMP) | 1 | 1 queue | 1 | 1 audit surface | 1 |

Exceeding a budget requires removing something first. Dashboards do not accrete.
A screen proposing "12 charts, 17 KPIs, 6 alerts" is rejected, not discussed.

**Taxonomy freeze.** The four information classes are frozen. "Opportunity," "Insight,"
"Trend," "Health," "Projection" and similar are not classes — they are one of the four,
classified honestly. Adding a fifth class requires a doctrine amendment (§11), not a
design discussion.

**Declaration.** Every page exports `PAGE_CLASSIFICATION` (persona, per-class counts,
bindu flag). The human classifies; the machine verifies the declaration exists, stays
within the persona budget, and matches the detected Bindu count. Undeclared = build
failure. Misdeclared bindu = build failure.

## 4b. Screen Archetypes

Every screen maps to exactly one of twelve archetypes (`design/archetypes/`):
dashboard, registry, record-detail, approval, case, builder, analytics,
administration, timeline, intelligence, command-center, topology.

- The archetype list is frozen like the information-class taxonomy. A thirteenth
  archetype requires an amendment (§11).
- `design/archetypes/ROUTE-MAP.md` maps the full screen inventory. A route without a
  row does not get built.
- Every page declares its archetype in `PAGE_CLASSIFICATION.archetype`. The TypeScript
  union makes an invalid value a compile error; the checker makes a missing one a
  build failure.
- Screens assemble institutional components (`src/components/institutional/`);
  components enforce doctrine. `BinduButton` is the only copper action primitive and
  counts toward the Bindu rule.

## 5. Universal Page Skeleton

Every page, no exceptions:

```
┌─────────────────────────────────────────┐
│ Top Navigation                          │
├───────┬─────────────────────────────────┤
│ Side  │ Page Title (Newsreader)         │
│ Nav   │ Institutional Context (kicker)  │
│       ├─────────────────────────────────┤
│       │ Primary Surface                 │
│       ├─────────────────────────────────┤
│       │ Audit / Metadata / Telemetry    │
└───────┴─────────────────────────────────┘
```

The telemetry band is rendered by the shell (`AppShell`), not by pages: surface id,
data freshness, session id, ledger reference. JetBrains Mono only.

## 5a. Layer Naming — Discoverability Rule

Sanskrit names are institutional architecture. Operational language is user architecture.
The two never diverge: every layer name is always paired with its operational meaning
wherever a user first encounters it (registry cards, onboarding, documentation).

| Layer | Operational pairing |
|---|---|
| Pulse | Workforce Awareness |
| Ops | Daily Execution |
| Pravaha | Flows & Policies |
| Buddhi | Institutional Intelligence |
| Sutra | Memory & Audit |
| Topology | People & Places |
| Temporal | Forecasting & Drift |
| Adaptive | Governance & Rules |
| Recovery | Resilience & Restoration |

Navigation items use operational nouns (Attendance, Roster, Compliance). Layer names
appear as annotations, never as the only label on an interactive element.

## 5b. Motion Doctrine

No decorative motion. Motion may only indicate:

- State transition (hover, focus, selection)
- Authority transfer (approval, publication)
- Data refresh
- Spatial relationship (panel reveal tied to origin)

Rules:
- Transition duration 150–250ms. Nothing slower except ambient state breathing.
- Ambient breathing (`animate-pulse`) is reserved for one pending-state indicator per
  screen, maximum. It marks live awaiting-action state, never decoration.
- Forbidden: bounce, elastic/spring overshoot, spin (except inline data-loading), confetti,
  celebratory animation of any kind. Payroll publication is an act of authority, not a party.
- Reduced-motion preference honored; all motion is optional to comprehension.

## 6. Typography Allocation

- Newsreader (serif): headlines, Buddhi narratives, institutional reports, strategic observations. Never tables. Never forms.
- Geist (sans): 90% of the application. All operational UI, data, labels, actions.
- JetBrains Mono: IDs, coordinates, timestamps, audit logs, rule references, telemetry band.

## 7. Institutional Narrative

Numbers carry reasoning. Wherever a KPI is strategic, Buddhi explains it in prose:

```
Attendance: 93%
```
becomes
```
Attendance adherence improved 4.3% over the previous cycle.
The largest improvement occurred within the Hyderabad East cluster,
primarily driven by transportation stability during morning shifts.
```

Narrative blocks render in Newsreader italic on Yantra Indigo or Sutra Ivory plates.
Not AI chatter. Institutional reasoning — declarative, sourced, calm.

## 8. Emotional Register

Target: Supreme Court library, ISRO mission control, RBI operations center, ATC command room.
Anti-target: modern HR apps, startup dashboards, Material templates.
Test for every screen: does it say "this institution knows what it is doing"?

## 9. Enforcement

Doctrine that cannot fail is decoration. Mechanical checks live in
`karamchari-web/scripts/check-doctrine.mjs`, run via `pnpm check:doctrine`:

1. Bindu rule — max one `bg-tamra-copper` action element per page file.
2. Serif containment — Newsreader classes never inside table markup.
3. Motion rule — no bounce/spin/long-duration animation classes.
4. Ambient breathing — max one `animate-pulse` per page file.
5. Telemetry coverage — TypeScript enforces `PAGE_TELEMETRY: Record<PageId, …>`;
   a new page without a telemetry entry fails compilation.

6. Classification gate — every page exports `PAGE_CLASSIFICATION`; checker verifies
   presence, persona-budget compliance (§4), and bindu-flag consistency with detected
   copper actions.
7. Exceptions — violations may only ship with an active entry in
   `design/EXCEPTIONS.md` (owner + expiry + replacement plan). Expired = build failure.
8. Archetype gate — `PAGE_CLASSIFICATION.archetype` present and one of the twelve
   (§4b). `<BinduButton>` usages count as copper actions in the Bindu check.

Human review gate: the human classifies information honestly; the machine verifies
consistency. Over budget without an exception = rejected, not discussed.

## 10. Crisis Doctrine (restated)

No alarm theater. Crisis communicates through structural constraint: muted rakta (#7A2B2B),
operational compression (UI narrows to mandatory directives), never flashing reds or sirens.
Recovery is measured by return of agency to the human edge.

## 11. Amendments

This doctrine changes by amendment, not by accumulation. An amendment names the section
changed, the reason, and the date, appended below. Anything not amended stands.

| Date | Section | Change |
|---|---|---|
| 2026-06-11 | All | Initial constitution |
| 2026-06-11 | §4, §9 | Information-class taxonomy frozen; classification declarations enforced; exceptions register added |
| 2026-06-12 | §4b, §9 | Twelve screen archetypes adopted and frozen; route map mandatory; archetype declaration enforced; institutional component library constituted |
