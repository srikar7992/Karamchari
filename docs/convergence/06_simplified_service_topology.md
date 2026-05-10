# Phase X: Simplified Service Topology

The Karamchari architecture must avoid "distributed monolith" anti-patterns. We will consolidate chatty microservices into cohesive macro-services bounded by business capabilities rather than technical functions.

## 1. Service Consolidation
The 14+ backend projects will be evaluated for physical deployment convergence. While they remain logically separated in code (`Karamchari.Payroll`, `Karamchari.HR`), they will deploy as a single API Gateway / BFF (`Karamchari.Api`) and a scalable set of Background Workers.

**Current State**: 
*   Too many distinct orchestration sagas.
*   Chatty network calls simulating workflow state transitions.

**Target State**:
*   **The BFF (`Karamchari.Api`)**: Handles all synchronous HTTP requests, authentication, and synchronous validation.
*   **The Worker Pool**: A homogenous pool of background workers processing MassTransit queues.
*   **The Engine**: The Unified Workflow Runtime operates across the Worker Pool, orchestrating events.

## 2. Event Choreography vs Orchestration
*   **Choreography**: Used for pure domain events (e.g., `EmployeeOnboarded` -> Payroll creates an initial profile).
*   **Orchestration**: Used strictly for business workflows requiring state, time, or human input. Handled exclusively by the `Karamchari.Workflow` module. Domains do not orchestrate; they emit and consume.

## 3. Database Topology
We maintain a single physical SQL Server instance, but logical isolation is enforced:
1.  **Schema-per-Tenant**: `[tenant_acme]`, `[tenant_globex]`.
2.  **Row-Level Security (RLS)**: Enforces isolation at the SQL engine level.
3.  **Cross-Schema Queries**: Strictly forbidden. Services must only query within the active `TenantExecutionEnvelope`'s schema.
