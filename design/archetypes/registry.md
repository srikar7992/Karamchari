# Archetype: Registry

ID: `registry`
Question: *What records exist, and which one do I need?*

The institutional ledger of a record type. Calm, complete, searchable. A registry's
authority comes from feeling exhaustive and ordered — like a court docket, not a feed.

## Zones

1. **Header** — record type title, total count in JetBrains Mono, scope statement.
2. **Query bar** — search input + hairline filter chips. Filters are facts (status,
   department, jurisdiction), never vibes.
3. **The table** — one InstitutionalTable carrying the registry. Mono-label headers,
   tabular data, hairline row separators, status bindus inline. Row click navigates to
   Record Detail.
4. **Footer strip** — LedgerStrip: result count, last sync, source projection id.

## Classification profile

Signals: 1 (the count). Risks: 0–1 (a flagged-records strip when relevant).
Narratives: 0. Maps: 0. Registries are deliberately the quietest archetype.

## Bindu

Zero or one: the create action ("Register Asset", "New Requisition") when the persona
owns creation. Browsing personas get zero copper.

## Component manifest

`InstitutionalTable`, `LedgerStrip`, `BinduButton`, `SignalCard` (count card optional).

## Used by

Employee Directory, Asset Registry, Payslip Browser, Position Management,
Requisitions, Candidate Pipeline, Compensation Records, Event/Endpoint Catalogs,
Mobility Marketplace, Regulatory Register, all "My X" lists (Leave, Reimbursements,
Loans, Assets, Payslips), Announcements, Polls.

## Anti-patterns

- Card grids where a table belongs. Records are rows.
- Inline editing. Registries navigate; Record Detail edits.
- More than one copper action. Creation is the only candidate.
