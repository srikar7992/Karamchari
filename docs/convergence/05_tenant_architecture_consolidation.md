# Phase X: Tenant Architecture Consolidation

Karamchari's multi-tenancy model must be formalized beyond simple data isolation (RLS). Tenants require distinct configurations, workflows, and rules.

## 1. Tenant Resolution Strategy
The platform enforces a strict, hierarchical resolution strategy:
1.  **JWT Claim (`tenant_id`)**: Authoritative for all authenticated user requests.
2.  **Trusted Header (`X-Tenant-Id`)**: Used for service-to-service communication behind the API Gateway.
3.  **Background Payload**: Serialized into the `TenantExecutionEnvelope` for all async jobs and message consumers.
*Subdomain resolution is deprecated to prevent spoofing and simplify local development.*

## 2. Tenant Configuration Engine
Every tenant possesses a hierarchical configuration JSON document.
*   **Global Default**: Baseline platform behavior.
*   **Tenant Override**: Tenant-specific customizations.
*   **Module Override**: Specific settings for a module (e.g., `Payroll.Currency = "INR"`).

## 3. Tenant-Aware Workflow Execution
The Unified Workflow Runtime resolves the `WorkflowDefinition` based on the active `TenantExecutionEnvelope`.
*   Tenant A might require a 1-step approval for Leave Requests.
*   Tenant B might require a 3-step approval (Manager -> HR -> Leadership) with a 24-hour SLA.
The runtime handles this transparently; the `Leave` domain module is unaware of the difference.

## 4. Extension Model (Controlled Customization)
Tenants cannot upload custom C# code. All customization must occur through:
1.  **Workflow DSL**: Modifying the JSON workflow definitions.
2.  **Expression Parser**: Writing simple boolean expressions for conditional routing (e.g., `Request.Amount > 5000`).
3.  **Webhooks**: Subscribing to platform events (`WorkflowInstanceCompleted`) to trigger external external integrations via a secure, egress-only payload.
