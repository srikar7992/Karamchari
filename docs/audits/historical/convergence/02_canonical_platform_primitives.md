# Phase X: Canonical Platform Primitives

To eliminate conceptual duplication, all future modules MUST execute through these shared platform primitives. These primitives form the foundation of the Unified Workflow Runtime.

## 1. Core Workflow Primitives
*   **`WorkflowDefinition`**: The template describing the allowed transitions, steps, rules, and SLA contracts for a specific entity type (e.g., "LeaveRequest", "PayrollRun").
*   **`WorkflowInstance`**: The active execution of a definition attached to a specific `EntityId`.
*   **`WorkflowStep`**: A node within the workflow (e.g., "Manager Approval").
*   **`WorkflowTransition`**: The movement from one step to another, triggered by an event, rule, or human action.
*   **`WorkflowState`**: The canonical representation of where an entity resides in its lifecycle.

## 2. Approval & Assignment Primitives
*   **`ApprovalRequest`**: A centralized inbox item routed to a user or role.
*   **`ApprovalDecision`**: The outcome (Approve, Reject, RequestInfo) coupled with an `AuditEntry`.
*   **`AssignmentRule`**: Dynamic logic determining who receives an `ApprovalRequest` (e.g., "Employee's Direct Manager").

## 3. Operations & Observability Primitives
*   **`SLAContract`**: The defined duration a `WorkflowStep` can remain pending before action is required.
*   **`EscalationRule`**: The automated response (notification, reassignment) when an `SLAContract` is breached.
*   **`AuditEntry`**: A unified, immutable ledger of all state changes, decisions, and system actions.
*   **`NotificationEvent`**: A standardized trigger emitted by the runtime to notify stakeholders of transitions or required actions.

## 4. Extensibility Primitives
*   **`PolicyDefinition`**: Database-configurable business rules that act as pre-conditions or transition constraints.
*   **`BusinessAction`**: An atomic, idempotent operation executed by a domain service upon entering or exiting a `WorkflowStep`.
*   **`CompensationAction`**: The defined rollback mechanism if a subsequent step in the workflow fails.

## Enforcement Rule
No module is permitted to define its own `Status` enum or `ApprovalChain` object. If a domain entity requires state management or multi-party agreement, it MUST integrate with these primitives.
