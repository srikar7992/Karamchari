# Archetype: Record Detail

ID: `record-detail`
Question: *Who/what is this entity, in full institutional context?*

The dossier. The single most important archetype in Karamchari — Employee 360 is its
flagship. A record detail page asserts that the institution knows this entity
completely: identity, position, history, value, risk, and reasoning, on one surface.

## Zones

1. **Dossier header** — breadcrumb in mono, entity name in Newsreader section title,
   role/type as pull-quote, secondary actions (ink/outline) right-aligned.
2. **Identity rail** (left, ~1/3) — monogram block, status bindu, immutable facts in
   tabular data: IDs in JetBrains Mono, placements, locations.
3. **Context plates** (right, ~2/3) — the entity's institutional position: chain of
   command (a small Map), compensation/valuation signals, capability matrix.
4. **Risk + narrative band** — RiskCard for the live threat assessment; NarrativePlate
   for Buddhi's reading of the record. Risk states a fact; narrative explains it.
5. **History** — CaseTimeline of the entity's institutional events, mono timestamps,
   sourced entries.
6. **Ledger strip** — record id, projection freshness, last mutation actor.

## Classification profile

Signals 3–5 (tenure, compa-ratio, scores), Risks 1 (the entity's live risk), Narratives
0–1 (Buddhi on the record), Maps 1 (chain of command / org position).

## Bindu

Zero or one. When the record carries an elevated risk, the Bindu is the intervention
("Initiate Retention Action"). Routine edits stay ink/outline.

## Component manifest

`SignalCard`, `RiskCard`, `NarrativePlate`, `BinduButton`, `CaseTimeline`,
`LedgerStrip`, `MapSurface`, `InstitutionalTable`.

## Used by

Employee 360, Candidate Detail, Asset Detail, Invoice Detail, Position Detail,
Employee Capability Profile, Employee Risk Profile, My Profile, My Skills & Learning,
Employee Onboard Form (creation mode of the dossier).

## Anti-patterns

- Tab mazes. The dossier is one composed surface; tabs only for true sub-ledgers.
- Risk theater: flight risk is a hairline-bordered statement, not a red banner.
- Burying identity: IDs and placement are always visible without scrolling.
