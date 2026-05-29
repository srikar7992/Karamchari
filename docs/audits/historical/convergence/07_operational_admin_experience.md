# Phase X: Operational Admin Experience

The success of Phase X relies on empowering operational teams (HR Admins, IT Ops) to manage workflows without engineering intervention.

## 1. Workflow Designer
A visual or JSON/YAML based interface where Tenant Admins can:
*   Define `WorkflowStep` sequences.
*   Assign roles to `AssignmentRule` engines.
*   Configure `SLAContract` durations (e.g., "HR Approval must occur within 48 hours").

## 2. Operational Dashboards
Built directly into the platform (and exposed via the BFF):
*   **Audit Explorer**: A unified search over all `WorkflowAuditEntry` records. Allows tracing exactly who approved what, when, and under what condition.
*   **Stuck Workflow Recovery**: Tooling to identify instances that breached SLAs or encountered unhandled exceptions, allowing Admins to manually transition state or re-assign the request.

## 3. Policy Management
A rules engine interface allowing Admins to define `PolicyDefinition` logic:
*   e.g., "Reimbursements over $500 require Leadership approval."
*   e.g., "Leave requests during Blackout Dates require HR override."

## 4. Observability Integration
The Admin Experience integrates tightly with the platform's OpenTelemetry stack:
*   **Trace ID linking**: Every workflow transition in the Admin UI links directly to the Seq trace ID, allowing Tier 2 support to instantly jump from a business complaint ("My leave is stuck") to the exact SQL query or RabbitMQ exception that caused the delay.
