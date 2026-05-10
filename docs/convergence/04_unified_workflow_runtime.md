# Phase X: Unified Workflow Runtime

The Unified Workflow Runtime is the single execution engine for all state transitions, approvals, and orchestrations across the Karamchari platform.

## Architecture

```mermaid
graph TD
    A[Domain Module (e.g., Payroll)] -->|1. Submit Request| B(Workflow Router)
    B -->|2. Resolve Definition| C{Tenant Config Store}
    B -->|3. Evaluate Policies| D[Rules Engine]
    D -- Pass --> E[Workflow Instance Created]
    D -- Fail --> F[Request Rejected]
    E --> G[Step 1: Pending]
    G -->|4. Emit Notification Event| H(Notification Pipeline)
    G -->|5. SLA Timer Started| I(Background Job Scheduler)
    G -->|6. Approval Action| J{Assignment Engine}
    J -- Approved --> K[Next Step / State Transition]
    J -- Rejected --> L[Rollback / Compensation Action]
    K --> M[Audit Log Updated]
```

## Core Principles
1.  **Metadata-Driven**: Workflows are not hardcoded in C#. They are defined in JSON/YAML payloads stored in the `Tenant Config Store`.
2.  **No Direct Orchestration in Domains**: The `Payroll` module does not know *who* approves a payroll run or *how long* they have. It only knows that a `PayrollRun` workflow has reached the `Approved` state.
3.  **Event-Sourced Transitions**: Every transition emits an integration event (`WorkflowStepAdvanced`, `WorkflowInstanceCompleted`). Domains listen to these events to execute business logic (e.g., disbursing funds).
4.  **Universal Approval Inbox**: Because all modules use the `WorkflowInstance`, the frontend can query a single API endpoint to render all pending approvals for a manager (Leave, Expenses, Payroll, Promotions).

## SLA and Escalation Engine
The runtime integrates directly with the background job scheduler (e.g., Hangfire) to enforce SLAs. 
*   If a `WorkflowStep` remains pending past its `SLAContract`, the runtime automatically executes the `EscalationRule` (e.g., auto-approve, auto-reject, or notify supervisor) and records the breach in the `AuditEntry`.

## Workflow Replay
To support operational debugging, the Unified Workflow Runtime provides tooling to replay stuck workflows or resend missed notifications without requiring manual database manipulation.
