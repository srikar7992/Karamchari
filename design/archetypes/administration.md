# Archetype: Administration

ID: `administration`
Question: *How is the system configured, and how do I change it?*

Configuration surfaces. The most repetitive archetype (Org Structure Admin alone is
eight screens of it), so its value is uniformity: learn one admin screen, know them
all. Quietness is mandatory — configuration is plumbing, not product.

## Zones

1. **Entity header** — entity type, count in mono, scope (tenant/jurisdiction).
2. **The register** — InstitutionalTable of configured records: name, key fields,
   status bindu (active/inactive), effective dates in mono.
3. **Editor panel** — create/edit form, Geist labels, mono for keys and codes,
   hairline field separators. Validation messages are factual sentences.
4. **Effect note** — one line stating the blast radius: "Policies created here govern
   attendance processing for all Zone A rosters."
5. **Ledger strip** — last change, actor, version.

## Classification profile

Signals 0–1, Risks 0, Narratives 0, Maps 0. Administration is the floor of the
density scale; if an admin screen wants a chart, it is misclassified.

## Bindu

Zero or one: activation of a new configuration version where activation is
consequential ("Activate Policy Version"). CRUD save stays ink.

## Component manifest

`InstitutionalTable`, `LedgerStrip`, `BinduButton` (rare).

## Used by

Org Structure Admin (8), Users & Roles, Tenant Administration, Attendance/Leave
Policy Admin, Shift Definitions, Geo Zones, Biometric Devices, Tax & Statutory
Config, Salary Structure Admin, Skill Catalog, Role Requirements, Coverage Rules,
SLA & Escalation Admin, Helpdesk Categories, Asset Categories, Notification
Templates, Webhook Admin, Onboarding Template Admin, Goal/Review Cycle Admin,
Governance records, Compliance Retention Admin, Gate Tester.

## Anti-patterns

- Creative layouts. Admin screens are deliberately identical.
- Hidden consequences: every config screen states what it governs.
- Deleting where deactivating is meant. Institutions deactivate; they rarely erase.
