# Phase X: Convergence Success Criteria

This initiative succeeds only when Karamchari transforms into a cohesive workforce operations platform. The following criteria dictate the completion of Phase X.

## 1. Architectural Reduction
*   **ZERO** module-specific approval tables or value objects (e.g., `ApprovalStep`, `PromotionApprovalStage` must be deleted).
*   **ZERO** hardcoded MassTransit Sagas mimicking workflow orchestration (Payroll Run state is managed by the Unified Workflow Runtime).

## 2. Shared Execution
*   All domain transitions are triggered by `WorkflowInstanceCompleted` or `WorkflowStepAdvanced` integration events.
*   All modules leverage the canonical `WorkflowDefinition` for rules, SLAs, and routing.

## 3. Cognitive Load Metrics
*   **Onboarding**: A new engineer can build a complex multi-step approval feature (e.g., a new "Equipment Request" module) solely by defining a JSON configuration and writing the pure domain logic.
*   **Code Volume**: A net-negative lines of code (LOC) outcome across the solution, as duplicated state machines and orchestration logic are deleted.

## 4. Tenant Manageability
*   Tenant Administrators can modify approval chains and SLA rules without code deployments or database migrations.
*   All tenant customizations are sandboxed to the `Tenant Config Store` and cannot crash the core platform.

## 5. System Predictability
*   Every state transition produces a uniform `WorkflowAuditEntry`.
*   Platform behavior is identical whether processing a $10 reimbursement or a $10M payroll run.
