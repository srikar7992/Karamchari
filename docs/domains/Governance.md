# Governance Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Stores service-level objectives, operational incidents, and schema definitions; validates schemas. Evidence: `src/Backend/Karamchari.Governance/Persistence/GovernanceDbContext.cs:26`. |
| Business Objectives | UNKNOWN beyond reliability, incidents, and schema governance. |
| Core Concepts | SLO, operational incident, schema definition, schema compatibility rule, schema status. |
| Aggregates / Entities | `ServiceLevelObjective`, `OperationalIncident`, `SchemaDefinition`. |
| Value Objects | UNKNOWN. |
| State Machines | `AvailabilityClassification`, `IncidentSeverity`, `IncidentStatus`, `SchemaCompatibilityRule`, `SchemaStatus`. Evidence: `src/Backend/Karamchari.Governance/Domain`. |
| Events | UNKNOWN. |
| Commands | UNKNOWN; no public BFF endpoints found. |
| Queries | UNKNOWN; no public BFF endpoints found. |
| Business Rules / Invariants / Validation | `ISchemaValidator` and `SchemaValidator` exist. Evidence: `src/Backend/Karamchari.Governance/Services/ISchemaValidator.cs`, `src/Backend/Karamchari.Governance/Services/SchemaValidator.cs`. |
| Calculation Rules | SLO/error-budget math UNKNOWN. |
| Ownership Rules | UNKNOWN. |
| Dependencies | Core persistence. |
| External Integrations | UNKNOWN. |
| Examples | UNKNOWN. |
| Failure Scenarios | Operational incident entity exists; runbook integration UNKNOWN. |
| Known Limitations | Governance has no exposed operations endpoints or dedicated test project found. |
