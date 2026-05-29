# Platform Engineer Onboarding Program

**Goal:** Transform new hires into self-sufficient distributed systems operators within 2 weeks. Platform knowledge must be distributable, not tribal.

## Week 1: Mental Models & Observation
1.  **Read the Mental Models:** Review all 1-pagers in `docs/governance/mental_models/` (Tenant Execution, Replay/Retry, Connection Safety).
2.  **Run the Runtime:** Execute `./setup.ps1` and `./scripts/runtime/run-local.ps1`.
3.  **Trace a Request:** 
    *   Trigger an HR onboarding request via Swagger.
    *   Open Seq (http://localhost:8081).
    *   Find the `karamchari.correlation.id`.
    *   Trace the lifecycle from HTTP -> RabbitMQ -> Background Job -> Database.
4.  **Break the Rules:** Attempt to query `HRDbContext` without establishing a tenant context. Observe the explicit exception.

## Week 2: Chaos & Resilience Labs
1.  **Replay Lab:** Complete `tenant_isolation_lab.md`. Send a duplicate message and trace its rejection in Redis.
2.  **Retry Storm:** Introduce a hardcoded `throw new Exception()` in a consumer. Watch the retry backoff policies fire in Seq, and observe the message move to the Dead-Letter Queue (DLQ).
3.  **Connection Leak Drill:** Intentionally leak a connection by removing an `await` on a database call. Observe the `PooledConnectionContaminationException` catch it.

## Certification
By the end of Week 2, engineers should understand:
*   Where tenant identity originates (JWT / Header).
*   How it propagates across async boundaries (`TenantExecutionContext`).
*   How replay systems protect against duplicate processing.
*   How RLS protects against cross-tenant data spillage.