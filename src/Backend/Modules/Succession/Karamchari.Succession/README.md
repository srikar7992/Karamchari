# Karamchari.Succession

Succession planning aggregates: critical roles, succession plans, talent pools, and career paths.

## Domain ownership
Succession planning decisions. Capability-derived succession projections (bench strength, successor candidates, readiness) live in the Capability module; HR org data is read via a direct project reference.

## Events published
None. This module has no Contracts project and defines no integration events.

## Events consumed
None (no `Consumers/` folder).

## Database tables
Source of truth: `Persistence/SuccessionDbContext.cs` and `Migrations/`.

- `CriticalRole`
- `SuccessionPlan`
- `TalentPool`
- `CareerPath`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.HR` (direct module reference)

## Wiring
Self-registered via `DependencyInjection/SuccessionServiceCollectionExtensions.cs`, called from the API host.

## Testing
No dedicated test project yet. Full sweep:

```powershell
.\run-all-tests.ps1
```
