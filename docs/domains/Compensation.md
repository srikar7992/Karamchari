# Compensation Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Manages compensation bands, merit matrices, increment budget pools, and employee compensation records. Evidence: `src/Backend/Karamchari.Compensation/Persistence/CompensationDbContext.cs:27`. |
| Business Objectives | UNKNOWN beyond compensation revision and budget modeling. |
| Core Concepts | Compensation band, band type, merit matrix, merit rating, increment budget pool, budget status, employee compensation record. |
| Aggregates / Entities | `CompensationBand`, `MeritMatrix`, `IncrementBudgetPool`, `EmployeeCompensationRecord`. |
| Value Objects | UNKNOWN. |
| State Machines | `BudgetStatus`, `CompensationBandType`, `MeritRating`, `CompensationRevisionReason`. Evidence: `src/Backend/Karamchari.Compensation/Domain/*.cs`, `src/Backend/Karamchari.Compensation.Contracts/IntegrationEvents/CompensationIntegrationEvents.cs:25`. |
| Events | `EmployeeCompensationRevisedIntegrationEvent`. Evidence: `src/Backend/Karamchari.Compensation.Contracts/IntegrationEvents/CompensationIntegrationEvents.cs:12`. |
| Commands | UNKNOWN; no BFF endpoints found in this pass. |
| Queries | UNKNOWN; no BFF endpoints found in this pass. |
| Business Rules / Invariants / Validation | UNKNOWN. |
| Calculation Rules | Merit matrix/budget pool imply compensation calculations; exact formulas UNKNOWN. |
| Ownership Rules | Tenant-scoped; HR/finance approval ownership UNKNOWN. |
| Dependencies | Core persistence/contracts. |
| External Integrations | UNKNOWN. |
| Examples | UNKNOWN. |
| Failure Scenarios | UNKNOWN. |
| Known Limitations | No dedicated tests or exposed API found. |
