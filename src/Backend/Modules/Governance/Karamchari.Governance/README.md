# Karamchari.Governance

Platform operational governance: service level objectives, operational incidents, and schema definitions.

Domain documentation: [docs/domains/Governance.md](../../../../../docs/domains/Governance.md)

## Domain ownership
Operational reliability artifacts for the platform itself. Regulatory/data compliance belongs to the Compliance module.

## Events published
None. This module has no Contracts project and defines no integration events.

## Events consumed
None (no `Consumers/` folder).

## Database tables
Source of truth: `Persistence/GovernanceDbContext.cs` and `Migrations/`.

- `ServiceLevelObjective`
- `OperationalIncident`
- `SchemaDefinition`

## Project dependencies
- `Karamchari.Core`

## Wiring
Self-registered via `DependencyInjection/GovernanceServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.Governance.Tests/Karamchari.Governance.Tests.csproj
```
