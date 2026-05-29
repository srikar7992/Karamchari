# Domain Knowledge Report

Verdict: INCOMPLETE.

## Evidence Produced

- Per-domain evidence docs were created under `docs/domains/<Domain>/README.md`.
- Enterprise terminology was consolidated in `docs/domain-dictionary/README.md`.
- Unknowns and missing business rules were recorded in `docs/institutionalization/KNOWLEDGE_GAP_REPORT.md`.

## Domain Coverage

| Domain | Repository Evidence Found | Independence Status |
|---|---|---|
| HR | Entities, DbContext, endpoints, events, validators, services. | Partially documented; business objectives and ownership rules remain UNKNOWN. |
| Payroll | Rich domain model, DbContext, endpoints, consumers, events, tests. | Partially documented; calculations, authority, and recovery remain incomplete. |
| Recruitment | Entities and DbContext. | Founder dependent; no API/tests/rules found. |
| Compensation | Entities, DbContext, revision event. | Founder dependent; no API/tests/rule catalog found. |
| Performance | Rich domain model, DbContext, events, workspace endpoints. | Partially documented; scoring and authority remain UNKNOWN. |
| Workflow | DbContext, Core workflow primitives, approval endpoints, consumers. | Partially documented; routing/quorum business rules incomplete. |
| Forecasting | DbContext, engine, event consumers, forecast summary endpoint. | Founder dependent; formulas and tests missing. |
| Billing | DbContext, endpoints, consumers, services. | Partially documented; rules and tests incomplete. |
| FinancialOps | DbContext, contracts, consistency/replay services, chaos tests. | Partially documented; no public API/support path found. |
| TimeAttendance | Rich DbContext, endpoints, events, consumers, tests. | Partially documented; policy math and recovery incomplete. |
| Identity | Endpoints, token stores, JWT services, integration tests. | Partially documented; JWT rotation/recovery incomplete. |
| Governance | DbContext, entities, schema validator. | Founder dependent; no operational UI/API found. |
| Capability | DbContext, enums, endpoints. | Partially documented; rules/tests missing. |
| PSA | DbContext, endpoints, services, tests. | Partially documented; broader risk coverage incomplete. |
| Notifications | DbContext, consumers, channels, orchestrator, endpoints. | Partially documented; delivery policy and provider recovery incomplete. |
| Intelligence | DbContext, endpoints, services, drift worker, events. | Partially documented; model thresholds/semantics incomplete. |

## Hard Finding

The codebase has strong domain surface area, but business knowledge still lives inside source files. A new Senior Engineer could discover many nouns and flows, but cannot safely change compensation, payroll, approval, forecasting, utilization, or intelligence behavior without reading implementation internals and making judgment calls.
