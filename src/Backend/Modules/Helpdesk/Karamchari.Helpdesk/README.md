# Karamchari.Helpdesk

Internal employee helpdesk: support tickets, ticket categories, knowledge base articles, and escalation rules.

## Domain ownership
Employee support workflow. Notification delivery belongs to the Notifications module.

## Events published
None. This module has no Contracts project and defines no integration events.

## Events consumed
| Event | Consumer |
|---|---|
| `EmployeeTerminatedIntegrationEvent` (Karamchari.Core.Contracts) | `Consumers/EmployeeTerminatedHelpdeskConsumer.cs` |

## Database tables
Source of truth: `Persistence/HelpdeskDbContext.cs` and `Migrations/`.

- `SupportTicket`
- `TicketCategory`
- `KbArticle`
- `EscalationRule`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`

## Wiring
Self-registered via `DependencyInjection/HelpdeskServiceCollectionExtensions.cs`, called from the API host.

## Testing
No dedicated test project yet. Full sweep:

```powershell
.\run-all-tests.ps1
```
