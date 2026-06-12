# Karamchari Screen Archetypes

Twelve archetypes. Every screen in the platform maps to exactly one — see
[ROUTE-MAP.md](ROUTE-MAP.md). A screen without an archetype does not get built.
The archetype list is frozen the same way the information-class taxonomy is frozen
(DOCTRINE §4): adding a thirteenth requires a doctrine amendment (§11), not a design
discussion.

An archetype is not a template. It is a contract: the question the screen answers,
the zones it is allowed to have, the information-class profile it typically carries,
and where its Bindu (the single copper decision) may live.

| ID | Archetype | Question the screen answers |
|---|---|---|
| `dashboard` | [Dashboard](dashboard.md) | What needs my attention in my domain right now? |
| `registry` | [Registry](registry.md) | What records exist, and which one do I need? |
| `record-detail` | [Record Detail](record-detail.md) | Who/what is this entity, in full institutional context? |
| `approval` | [Approval Workspace](approval.md) | What awaits my decision, and what is the evidence? |
| `case` | [Case Workspace](case.md) | Where does this case stand, and what is its next step? |
| `builder` | [Builder](builder.md) | How do I construct or modify a structured artifact? |
| `analytics` | [Analytics](analytics.md) | What does the data say about how we are performing? |
| `administration` | [Administration](administration.md) | How is the system configured, and how do I change it? |
| `timeline` | [Timeline](timeline.md) | What happened, in what order, by whose hand? |
| `intelligence` | [Intelligence](intelligence.md) | What does the institution recommend, and why? |
| `command-center` | [Command Center](command-center.md) | Is the operation healthy, and what do I run next? |
| `topology` | [Graph / Topology](topology.md) | What is the structural shape of the organization? |

## Enforcement

- `PAGE_CLASSIFICATION.archetype` is mandatory on every page (typed by
  `src/lib/doctrine.ts`, verified by `scripts/check-doctrine.mjs`).
- The TypeScript union makes an invalid archetype a compile error; the checker makes
  a missing declaration a build failure.
- Each spec lists its component manifest from `src/components/institutional/`.
  Screens assemble components; components enforce doctrine.

## Shared skeleton

All archetypes inherit the universal page skeleton (DOCTRINE §5): shell navigation,
Newsreader title with institutional kicker, primary surface, shell-rendered telemetry
band. Archetype specs describe only the primary surface.
