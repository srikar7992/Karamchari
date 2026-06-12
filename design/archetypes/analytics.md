# Archetype: Analytics

ID: `analytics`
Question: *What does the data say about how we are performing?*

Analytics screens are read for judgment, not triage. They are the institutional
equivalent of a published statistical report: stable layout, comparable periods,
sourced figures. Reading one should feel like reading an RBI bulletin.

## Zones

1. **Report header** — analysis title in Newsreader, period selector in mono,
   data-source citation.
2. **Headline figures** — 2–3 SignalCards with period-over-period deltas.
3. **The analysis** — tables first, charts second. InstitutionalTable for breakdowns;
   charts only where shape matters (trend, distribution), drawn in ink/forest/indigo,
   never rainbow palettes.
4. **Narrative plate** — one Buddhi NarrativePlate interpreting the result. An analytics
   screen without interpretation is a data dump.
5. **Export strip** — export to Report Center, ink button, job id returned in mono.

## Classification profile

Signals up to persona budget, Risks 0–1 (an adverse finding), Narratives 1 (the
interpretation), Maps 0–1 (a distribution grid).

## Bindu

Usually zero. Analytics informs decisions made elsewhere. Exception: a screen whose
entire purpose terminates in one act (e.g. "Commit Forecast").

## Component manifest

`SignalCard`, `InstitutionalTable`, `NarrativePlate`, `RiskCard`, `LedgerStrip`.

## Used by

Recruitment Analytics, Pay Equity Report, Attendance Reports (7), Attendance Health
Dashboard, Project Profitability, Revenue Forecast, Gap Analysis, Career Readiness
Board, Forecast Board, Accuracy Monitor, Leave Liability & Statutory Reports.

## Anti-patterns

- Chart-first layouts. The table is the evidence; the chart is the summary.
- Unsourced figures. Every headline figure names its projection and period.
- Dashboards in disguise: analytics has a period and a thesis, not live triage.
