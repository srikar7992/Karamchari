# Karamchari.AssetManagement

Tracks company assets issued to employees: asset master data, categories, depreciation schedules, and maintenance records. Reacts to employee terminations to drive asset recovery.

## Domain ownership
Asset lifecycle (procurement metadata, assignment, depreciation, maintenance). Employee master data belongs to the HR module; this module only references employee identifiers.

## Events published
None. This module has no Contracts project and defines no integration events.

## Events consumed
| Event | Consumer |
|---|---|
| `EmployeeTerminatedIntegrationEvent` (Karamchari.Core.Contracts) | `Consumers/EmployeeTerminatedAssetConsumer.cs` |

## Database tables
Source of truth: `Persistence/AssetManagementDbContext.cs` and `Migrations/`.

- `Asset`
- `AssetCategory`
- `AssetDepreciationSchedule`
- `AssetMaintenanceRecord`

## Project dependencies
- `Karamchari.Core` (platform building blocks)
- `Karamchari.Core.Contracts` (shared integration events)

## Wiring
Self-registered via `DependencyInjection/AssetManagementServiceCollectionExtensions.cs`, called from the API host (`src/Backend/Hosts/Karamchari.Api`).

## Testing
No dedicated test project yet. Cross-cutting coverage comes from `tests/Backend/Karamchari.Api.UnitTests` and the architecture/isolation suites. Full sweep:

```powershell
.\run-all-tests.ps1
```
