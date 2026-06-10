# Karamchari.Integration

Outbound integration surface: webhook subscriptions for external systems.

## Domain ownership
External system connectivity. Internal event transport (MassTransit/RabbitMQ) is platform infrastructure in `Karamchari.Core`.

## Events published
None. This module has no Contracts project and defines no integration events.

## Events consumed
None (no `Consumers/` folder).

## Database tables
Source of truth: `Persistence/IntegrationDbContext.cs`.

- `WebhookSubscription`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`

## Wiring
Self-registered via `DependencyInjection/IntegrationServiceCollectionExtensions.cs`, called from the API host.

## Testing
No dedicated test project yet. Full sweep:

```powershell
.\run-all-tests.ps1
```
