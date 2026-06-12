# Archetype: Builder

ID: `builder`
Question: *How do I construct or modify a structured artifact?*

Builders produce institutional artifacts: workflows, rosters, surveys, career paths,
scenarios. The artifact under construction owns the canvas; tools stay at the edge.
A builder is a drafting table, not a wizard.

## Zones

1. **Artifact header** — artifact name (editable), version/state chip (draft /
   review / published) in mono.
2. **Canvas** (center, dominant) — the artifact itself: roster grid, workflow graph,
   survey outline, scenario parameter sheet.
3. **Palette rail** (left or right) — addable elements, hairline cards, drag or click
   to place.
4. **Inspector** — properties of the selected element, tabular form fields (Geist,
   never serif).
5. **Validation strip** — live rule check results: coverage violations, unreachable
   workflow branches. Stated as facts with rule references in mono.

## Classification profile

Signals 1–2 (validation counts, capacity totals), Risks 0–1 (hard rule violations),
Narratives 0, Maps 1 (the canvas itself is the Map).

## Bindu

One: **Publish**. Publication is the transfer of authority from draft to institution.
Save-draft stays ink. No celebration on publish (DOCTRINE §5b) — state chip flips,
ledger records it.

## Component manifest

`MapSurface` (canvas frame), `InstitutionalTable`, `RiskCard` (validation), `BinduButton`,
`LedgerStrip`.

## Used by

Workflow Studio, Survey Studio, Roster Builder, Career Path Designer, Org Scenario
Planner, WFP Scenario Planner, Merit Matrix & Pools, Audit Package Builder, KB Studio,
Payroll Simulation Lab.

## Anti-patterns

- Wizards for non-linear work. Builders are spatial, not sequential.
- Publishing without validation surface visible.
- Two copper buttons (Save + Publish). Save is ink; Publish is the Bindu.
