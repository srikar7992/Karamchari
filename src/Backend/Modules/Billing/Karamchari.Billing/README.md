# Karamchari.Billing

Client billing: billing contracts, rate cards, billable entries derived from approved timesheets, invoices, payments, and collection cases.

Domain documentation: [docs/domains/Billing.md](../../../../../docs/domains/Billing.md)

## Domain ownership
Commercial billing lifecycle from billable work capture to payment collection. Time capture itself belongs to TimeAttendance; project profitability belongs to PSA.

## Events published
Defined in `Karamchari.Billing.Contracts` (`BillingEvents.cs`):

- `InvoiceIssuedIntegrationEvent`
- `PaymentReceivedIntegrationEvent`

## Events consumed
| Event | Consumer |
|---|---|
| `TimesheetApprovedIntegrationEvent` | `Consumers/BillableEntryConsumer.cs` |
| `InvoiceIssuedIntegrationEvent` | `Consumers/CollectionCaseConsumer.cs` |
| `PaymentReceivedIntegrationEvent` | `Consumers/CollectionCaseConsumer.cs` |

## Database tables
Source of truth: `Persistence/BillingDbContext.cs` and `Migrations/`.

- `BillingContract`
- `RateCard`
- `EmployeeRole`
- `BillableEntry`
- `Invoice`
- `Payment`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Billing.Contracts`
- `Karamchari.TimeAttendance.Contracts`

## Wiring
Self-registered via `DependencyInjection/BillingServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.Billing.Tests/Karamchari.Billing.Tests.csproj
```
