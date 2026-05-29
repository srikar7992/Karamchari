# Billing Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Manages billing contracts, rate cards, employee role mapping, billable entries, invoices, payments, collections, and AR analytics. Evidence: `src/Backend/Karamchari.Billing/Persistence/BillingDbContext.cs:23`. |
| Business Objectives | UNKNOWN beyond invoice generation/finalization, payment capture, collections, and AR summary exposed by endpoints. |
| Core Concepts | Billing contract, rate card, employee role, billable entry, invoice, payment, collection case, collection policy, processed event log. |
| Aggregates / Entities | DbSets in `BillingDbContext`. Evidence: `src/Backend/Karamchari.Billing/Persistence/BillingDbContext.cs:23`. |
| Value Objects | UNKNOWN. |
| State Machines | Collection and invoice/payment statuses may exist in domain files; full transitions UNKNOWN from this pass. Evidence: `src/Backend/Karamchari.Billing/Domain`. |
| Events | `InvoiceIssuedIntegrationEvent`, `PaymentReceivedIntegrationEvent`. Evidence: `src/Backend/Karamchari.Billing.Contracts/BillingEvents.cs:6`. |
| Commands | Contract/rate/role/invoice/payment/collection command endpoints. Evidence: `src/Backend/Karamchari.Api/BFF/Billing/BillingEndpoints.cs:23`. |
| Queries | AR summary, collection cases, forecast summary. Evidence: `src/Backend/Karamchari.Api/BFF/Billing/BillingEndpoints.cs:28`, `src/Backend/Karamchari.Api/BFF/Billing/BillingEndpoints.cs:32`, `src/Backend/Karamchari.Api/BFF/Billing/BillingEndpoints.cs:37`. |
| Business Rules / Invariants / Validation | Invoice generation and collections workers/services exist; complete rule catalog UNKNOWN. Evidence: `src/Backend/Karamchari.Billing/Services/InvoiceGeneratorService.cs`, `src/Backend/Karamchari.Billing/Services/CollectionsBackgroundWorker.cs`. |
| Calculation Rules | AR analytics and invoice generation services exist; formulas UNKNOWN without source-level extraction. Evidence: `src/Backend/Karamchari.Billing/Services/ARAnalyticsService.cs`, `src/Backend/Karamchari.Billing/Services/InvoiceGeneratorService.cs`. |
| Ownership Rules | Tenant-scoped; business authority UNKNOWN. |
| Dependencies | Consumes approved timesheets and invoice/payment events. Evidence: `src/Backend/Karamchari.Billing/Consumers/BillableEntryConsumer.cs:13`, `src/Backend/Karamchari.Billing/Consumers/CollectionCaseConsumer.cs:15`. |
| External Integrations | UNKNOWN beyond SQL/MassTransit. |
| Examples | `POST /api/billing/invoices/generate`, `POST /api/billing/invoices/{id}/finalize`, `POST /api/billing/invoices/{id}/payment`, `GET /api/billing/ar/summary`. |
| Failure Scenarios | Collections and processed event logs indicate duplicate/event-processing concerns; full recovery UNKNOWN. |
| Known Limitations | No dedicated Billing test project was found under `tests/Backend`; risk coverage is not certified. |
