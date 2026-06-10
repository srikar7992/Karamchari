# Karamchari.PSA

Professional services automation: clients, client projects, project resources, unbilled revenue, invoices, employee cost snapshots, project profit ledger, and monthly project metrics.

Domain documentation: [docs/domains/PSA.md](../../../../../docs/domains/PSA.md)

## Domain ownership
Project commercials and profitability. Client invoicing documents belong to Billing; time capture belongs to TimeAttendance.

## Events published
None. This module has no Contracts project and defines no integration events.

## Events consumed
| Event | Consumers |
|---|---|
| `TimesheetApprovedIntegrationEvent` | `Consumers/BillableRevenueConsumer.cs`, `Consumers/ProfitCalculationConsumer.cs` |

## Database tables
Source of truth: `Persistence/PSADbContext.cs` and `Migrations/`.

- `Client`
- `ClientProject`
- `ProjectResource`
- `UnbilledRevenue`
- `Invoice`
- `EmployeeCostSnapshot`
- `ProjectProfitLedger`
- `ProjectMonthlyMetrics`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`

## Wiring
Self-registered via `DependencyInjection/PSAServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.PSA.Tests/Karamchari.PSA.Tests.csproj
```
