# Phase 1C.1 Distributed Systems & Governance Audit Report

## 1. Distributed Systems Risk Report
- **Event Dispatch Flow:** `MassTransitDomainEventDispatcher` blindly dispatches raw `IDomainEvent` objects to the bus. MassTransit Outbox is configured (via `modelBuilder.AddOutboxMessageEntity()`), but the events lack an enterprise-grade envelope. This means `CorrelationId`, `CausationId`, `TenantId`, and `EventVersion` are missing or inconsistently tracked.
- **Side-Effect Consistency:** While domain events are safely persisted within the EF transaction, the downstream consumers lack idempotency standards for consuming these events, risking duplicate side effects.
- **Workflow Coupling:** Modules currently use simple REST integrations (BFF orchestration) or raw event subscriptions without formal Saga/State Machine definitions. This creates implicit workflows that are hard to trace and recover upon failure.

## 2. Governance Gap Report
- **Event Contracts:** Missing a central registry. `IntegrationEvent` records are scattered inside module `Contracts` folders (e.g., `Karamchari.Payroll.Contracts`) and are strictly typed but lack explicit versioning rules.
- **Contract Drift:** No backward compatibility checks exist. A schema change in a contract will break consumers without warning.
- **Observability Propagation:** OpenTelemetry is configured for HTTP/EF, but `CorrelationId` and `CausationId` are not flowing cleanly through background event handlers via MassTransit.

## 3. Workflow Consistency Report
- **Implicit Workflows:** The loan and disbursement approval chains rely on sequential database state updates rather than orchestrated sagas. A failure in step 2 leaves the system in an ambiguous state without automated compensation.
- **Recommendations:** Standardize on MassTransit State Machines (Sagas) for long-running workflows like Disbursements and Final Settlements.

## 4. Contract Drift Report
- **Current State:** Direct reference to DLL contracts (`Karamchari.Payroll.Contracts`). If modules are extracted to separate microservices, this shared DLL pattern becomes a distributed monolith bottleneck.
- **Fix:** Establish explicit JSON-based schema governance or versioned DTO/Event standards.
