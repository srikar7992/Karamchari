# Tenant Isolation & Idempotency Lab

**Purpose**: A guided simulation to prove to new engineers that the platform protects itself from distributed failures without feature-team intervention.

## Exercise 1: The Duplicate Delivery
1.  **Setup**: Start the stack using `./scripts/runtime/run-local.ps1`.
2.  **Action**: Publish the same `EmployeeOnboardedIntegrationEvent` to RabbitMQ twice with the exact same `MessageId`.
3.  **Observation**: 
    *   Open Seq. Search for the `MessageId`.
    *   You will see the first message processed successfully.
    *   You will see a log: `Duplicate message detected via ReplayProtectionService. Discarding.`
4.  **Learning**: Idempotency is handled purely via platform middleware. The business consumer is completely ignorant.

## Exercise 2: The Malicious Tenant Switch
1.  **Setup**: Write a temporary HTTP endpoint that queries `HRDbContext`.
2.  **Action**: 
    *   Establish context for `tenant_acme`.
    *   Attempt to manually run `EXEC sp_set_session_context @key=N'TenantId', @value='tenant_globex'`.
    *   Execute an EF Core query.
3.  **Observation**:
    *   The `RlsConnectionGuard` will intercept the pool return.
    *   It will detect that the session context was illegally mutated.
    *   It throws `PooledConnectionContaminationException`.
4.  **Learning**: You cannot trick the connection pool. The platform enforces state integrity aggressively.

## Exercise 3: The Cross-Domain Leak
1.  **Setup**: In `Karamchari.HR`, attempt to reference `Karamchari.Core.Multitenancy.TenantExecutionEnvelope`.
2.  **Action**: Run `./scripts/validation/validate-runtime.ps1`.
3.  **Observation**: The architectural fitness function `BusinessBoundaryTests` fails the build immediately.
4.  **Learning**: Business domains are strictly forbidden from orchestrating distributed runtime state.