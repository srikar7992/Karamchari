# Karamchari.Compensation

Compensation management: compensation bands, merit matrices, increment budget pools, employee compensation records, and bonus plans.

Domain documentation: [docs/domains/Compensation.md](../../../../../docs/domains/Compensation.md)

## Domain ownership
Compensation structure and revision decisions. Payroll execution (salary calculation, disbursement) belongs to the Payroll module.

## Events published
Defined in `Karamchari.Compensation.Contracts` (`IntegrationEvents/CompensationIntegrationEvents.cs`):

- `EmployeeCompensationRevisedIntegrationEvent` (consumed by HR projections)

## Events consumed
None (no `Consumers/` folder).

## Database tables
Source of truth: `Persistence/CompensationDbContext.cs` and `Migrations/`.

- `CompensationBand`
- `MeritMatrix`
- `IncrementBudgetPool`
- `EmployeeCompensationRecord`
- `BonusPlan`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.Compensation.Contracts`

## Wiring
Self-registered via `DependencyInjection/CompensationServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.Compensation.Tests/Karamchari.Compensation.Tests.csproj
```
