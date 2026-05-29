# Forecasting Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Stores forecast metrics and client payment profiles; updates forecasts from timesheet, invoice, and payment events. Evidence: `src/Backend/Karamchari.Forecasting/Persistence/ForecastingDbContext.cs:21`, `src/Backend/Karamchari.Forecasting/Consumers/ForecastUpdateConsumer.cs:15`. |
| Business Objectives | UNKNOWN beyond forecast summary and event-driven updates. |
| Core Concepts | Forecast metrics, client payment profile. |
| Aggregates / Entities | `ForecastMetrics`, `ClientPaymentProfile`. Evidence: `src/Backend/Karamchari.Forecasting/Domain/ForecastModels.cs`. |
| Value Objects | UNKNOWN. |
| State Machines | UNKNOWN. |
| Events | Consumes `TimesheetApprovedIntegrationEvent`, `InvoiceIssuedIntegrationEvent`, `PaymentReceivedIntegrationEvent`. Evidence: `src/Backend/Karamchari.Forecasting/Consumers/ForecastUpdateConsumer.cs:15`. |
| Commands | UNKNOWN. |
| Queries | `GET /api/forecast/summary`. Evidence: `src/Backend/Karamchari.Api/BFF/Billing/BillingEndpoints.cs:36`. |
| Business Rules / Invariants / Validation | Forecasting engine exists; full formulas UNKNOWN. Evidence: `src/Backend/Karamchari.Forecasting/Services/ForecastingEngine.cs`. |
| Calculation Rules | UNKNOWN without full source-level formula extraction. |
| Ownership Rules | Tenant-scoped; finance/ops ownership UNKNOWN. |
| Dependencies | Billing, TimeAttendance, MassTransit, SQL. |
| External Integrations | UNKNOWN. |
| Examples | `GET /api/forecast/summary`. |
| Failure Scenarios | Event-consumer failure; exact recovery UNKNOWN. |
| Known Limitations | No dedicated Forecasting tests found. |
