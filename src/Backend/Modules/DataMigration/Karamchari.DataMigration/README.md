# Karamchari.DataMigration

Tenant data onboarding: bulk import jobs and per-record outcomes, plus historical salary records and historical payroll summaries imported from legacy systems.

## Domain ownership
Import orchestration and historical data staging. Imported entities are written into their owning modules (HR, TimeAttendance, Payroll), which this project references directly for that purpose.

## Events published
Import lifecycle messages are defined in `Karamchari.DataMigration.Contracts` and published from `API/MigrationEndpoints.cs` (e.g. `ImportJobQueued`).

## Events consumed
| Event | Consumer |
|---|---|
| `ImportJobQueued` (Karamchari.DataMigration.Contracts) | `Consumers/ImportJobQueuedConsumer.cs` |

## Database tables
Source of truth: `Persistence/DataMigrationDbContext.cs` and `Migrations/`.

- `ImportJob`
- `ImportRecord`
- `HistoricalSalaryRecord`
- `HistoricalPayrollSummary`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.DataMigration.Contracts`
- `Karamchari.HR`, `Karamchari.TimeAttendance`, `Karamchari.Payroll` (direct module references — intentional exception to module isolation, required for bulk writes into target domains)

## Wiring
Self-registered via `DependencyInjection/DataMigrationServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.DataMigration.Tests/Karamchari.DataMigration.Tests.csproj
```
