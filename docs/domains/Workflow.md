# Workflow Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Stores workflow definitions and workflow instances and orchestrates approval flows for leave and reimbursements. Evidence: `src/Backend/Karamchari.Workflow/Persistence/WorkflowDbContext.cs:26`, `src/Backend/Karamchari.Workflow/Consumers/WorkflowOrchestratorConsumer.cs:16`. |
| Business Objectives | UNKNOWN beyond approval orchestration exposed by approval endpoints and consumers. |
| Core Concepts | Workflow definition, workflow instance, approval step, quorum rule, workflow status, step status. Evidence: `src/Backend/Karamchari.Core/Domain/Workflows/*.cs`. |
| Aggregates / Entities | `WorkflowDefinition`, `WorkflowInstance`. Evidence: `src/Backend/Karamchari.Workflow/Persistence/WorkflowDbContext.cs:26`. |
| Value Objects | Workflow contracts exist in Core. Evidence: `src/Backend/Karamchari.Core/Domain/Workflows/WorkflowContracts.cs`. |
| State Machines | `WorkflowStatus`, `WorkflowStepStatus`, `ApprovalStatus`, `QuorumRule`. Evidence: `src/Backend/Karamchari.Core/Domain/Workflows/*.cs`. |
| Events | Workflow started, transitioned, approval decision, completed integration events; traceability events via enterprise envelopes. Evidence: `src/Backend/Karamchari.Core.Contracts/IntegrationEvents/IntegrationEvents.cs:151`, `src/Backend/Karamchari.Workflow/Consumers/WorkflowTraceabilityConsumer.cs:14`. |
| Commands | Approve/reject endpoints and orchestration from leave/reimbursement submitted events. Evidence: `src/Backend/Karamchari.Api/BFF/Common/ApprovalEndpoints.cs:23`, `src/Backend/Karamchari.Workflow/Consumers/WorkflowOrchestratorConsumer.cs:16`. |
| Queries | My approvals and workflow timeline. Evidence: `src/Backend/Karamchari.Api/BFF/Common/ApprovalEndpoints.cs:21`. |
| Business Rules / Invariants / Validation | Quorum and status enums exist; full approval routing rules UNKNOWN. |
| Calculation Rules | UNKNOWN. |
| Ownership Rules | Tenant-scoped; approver selection rules UNKNOWN. |
| Dependencies | Leave requests, reimbursement submissions, MassTransit, Core workflow primitives. |
| External Integrations | UNKNOWN beyond MassTransit/SQL. |
| Examples | `GET /api/v1/approvals/my`, `POST /api/v1/approvals/{stepInstanceId}/approve`, `POST /api/v1/approvals/{stepInstanceId}/reject`. |
| Failure Scenarios | Workflow concurrency tests exist in tenant certification. Evidence: `tests/Backend/Karamchari.TenantIsolationCertification/Concurrency/WorkflowConcurrencyTests.cs`. |
| Known Limitations | No dedicated workflow unit/integration test project found. |
