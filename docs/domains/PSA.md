# PSA Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Manages clients, projects, project resources, unbilled revenue, invoices, cost snapshots, profit ledger, and monthly project metrics. Evidence: `src/Backend/Karamchari.PSA/Persistence/PSADbContext.cs:25`. |
| Business Objectives | UNKNOWN beyond project profitability, invoicing, pricing, anomaly, and cash-flow endpoints. |
| Core Concepts | Client, project, resource assignment, unbilled revenue, invoice, employee cost snapshot, project profit ledger, monthly metrics. |
| Aggregates / Entities | DbSets in `PSADbContext`. Evidence: `src/Backend/Karamchari.PSA/Persistence/PSADbContext.cs:25`. |
| Value Objects | Pricing, simulation, anomaly, AR aging, and cash-flow records exist in services. Evidence: `src/Backend/Karamchari.PSA/Services/*.cs`. |
| State Machines | Billing type enum exists. Evidence: `src/Backend/Karamchari.PSA/Domain/ClientProject.cs:8`. Other transitions UNKNOWN. |
| Events | Consumes `TimesheetApprovedIntegrationEvent`. Evidence: `src/Backend/Karamchari.PSA/Consumers/BillableRevenueConsumer.cs:25`, `src/Backend/Karamchari.PSA/Consumers/ProfitCalculationConsumer.cs:27`. |
| Commands | Client/project/resource/invoice/simulation endpoints. Evidence: `src/Backend/Karamchari.Api/BFF/PSA/PSAEndpoints.cs:22`. |
| Queries | Client/project lists, employee projects, invoice download, profitability, trend, pricing, anomalies, cashflow aging/forecast, client profitability. Evidence: `src/Backend/Karamchari.Api/BFF/PSA/PSAEndpoints.cs:23`. |
| Business Rules / Invariants / Validation | Pricing engine tests exist; full PSA rule catalog UNKNOWN. Evidence: `src/Backend/Karamchari.PSA.Tests/PricingEngineTests.cs`, `src/Backend/Karamchari.PSA/Services/PricingEngine.cs`. |
| Calculation Rules | Pricing, profit, cash-flow, anomaly, and simulation services exist. Evidence: `src/Backend/Karamchari.PSA/Services`. |
| Ownership Rules | Tenant-scoped; project/account ownership UNKNOWN. |
| Dependencies | TimeAttendance approved timesheets and employee cost data. Evidence: PSA consumers. |
| External Integrations | UNKNOWN beyond shared infrastructure. |
| Examples | `POST /api/psa/projects`, `POST /api/psa/projects/{projectId}/resources`, `GET /api/analytics/projects`, `GET /api/analytics/cashflow/forecast`. |
| Failure Scenarios | Anomaly service and anomaly endpoint exist. Evidence: `src/Backend/Karamchari.PSA/Services/AnomalyDetectionService.cs`, `src/Backend/Karamchari.Api/BFF/PSA/PSAEndpoints.cs:36`. |
| Known Limitations | PSA has tests colocated under source, not under `tests/Backend`; coverage breadth is not certified. |
