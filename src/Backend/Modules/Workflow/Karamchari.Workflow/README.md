# Karamchari.Workflow

Generic workflow orchestration: workflow definitions and running workflow instances. Listens for domain events that require approval flows and drives them through configured workflows.

Domain documentation: [docs/domains/Workflow.md](../../../../../docs/domains/Workflow.md)

## Domain ownership
Workflow definition and execution state. Domain-specific approval semantics (e.g. leave approval effects) stay in the owning modules, which react to workflow lifecycle events.

## Events published
Workflow lifecycle events are defined in `Karamchari.Core.Contracts`:

- `WorkflowStartedIntegrationEvent`
- `WorkflowTransitionedIntegrationEvent`
- `WorkflowApprovalDecisionIntegrationEvent`
- `WorkflowCompletedIntegrationEvent`

## Events consumed
| Event | Consumer |
|---|---|
| `LeaveRequestCreatedIntegrationEvent` | `Consumers/WorkflowOrchestratorConsumer.cs` |
| `ReimbursementSubmittedIntegrationEvent` | `Consumers/WorkflowOrchestratorConsumer.cs` |

## Database tables
Source of truth: `Persistence/WorkflowDbContext.cs` and `Migrations/`.

- `WorkflowDefinition`
- `WorkflowInstance`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`

## Wiring
Self-registered via `DependencyInjection/WorkflowServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.Workflow.Tests/Karamchari.Workflow.Tests.csproj
```
