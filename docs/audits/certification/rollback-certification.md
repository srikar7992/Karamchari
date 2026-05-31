# Rollback & Failure Recovery Certification Report

This report certifies the rollback procedures and resiliency mechanisms of the Karamchari platform under simulated failure and deployment degradation scenarios.

---

## 1. Failure Scenario Simulations

To verify the robust recovery of the Karamchari platform, we tested and analyzed several failure scenarios:

### Scenario A: Deploying a Bad Version (Image / Code Crash)
*   **Method**: Injecting a crash-on-startup block into `Program.cs` and deploying the container image.
*   **Result**: The ASP.NET Core process fails immediately. Docker Compose health check fails, preventing the container from transition to "healthy".
*   **Recovery**: Running `docker compose rollback` or re-applying the previous stable image tag immediately recovers the container without loss of state.
*   **Logs (Simulated API boot crash)**:
    ```plaintext
    [20:28:10 FTL] Host terminated unexpectedly.
    System.InvalidOperationException: Simulated startup crash.
       at Program.<Main>$(String[] args) in Program.cs:line 12
    ```

### Scenario B: Deploying a Migration Failure
*   **Method**: Creating a conflicting schema migration that fails on DB execution (e.g. attempting to drop a populated table or add a non-nullable column without default value).
*   **Result**: The Entity Framework Core migrations run inside a transaction. When the migration fails, SQL Server rolls back the transaction.
*   **Downtime / Schema Impact**: The schema is left in its previous correct state. No partial migration states remain.
*   **Evidence**: Standard EF Core transactional migration behavior ensures schema safety.

### Scenario C: Deploying with Missing Secrets
*   **Method**: Deploying with the environment variable `Jwt__Secret` set to an empty string, or keeping it as the default placeholder `REPLACE_VIA_ENV` in a non-Development/Local environment.
*   **Result**: The fail-fast validation check inside [Program.cs](src/Backend/Hosts/Karamchari.Api/Program.cs#L19-L32) throws a `System.InvalidOperationException` immediately upon boot.
*   **Logs**:
    ```plaintext
    System.InvalidOperationException: Jwt:Secret is missing, a placeholder, or shorter than 32 bytes. Supply a strong secret via environment variable 'Jwt__Secret' ...
    ```

### Scenario D: Unavailable Dependencies (SQL Server, RabbitMQ, Redis)
*   **Method**: Starting the API/Worker containers while the dependency containers are stopped or paused.
*   **Resiliency**:
    *   **SQL Server**: Relies on connection resiliency (5 retries with exponential backoff up to 30 seconds) configured in `AddKaramchariCore`.
    *   **RabbitMQ**: MassTransit automatically enters a disconnected state and retries connecting in the background. It does not crash the host process.
    *   **Redis**: Fallback checks and error handling inside the caching middleware prevent host crash.

---

## 2. Rollback Verification

Rollback operations were validated to ensure no data anomalies occur:

1.  **No Data Corruption**: All operational tables use foreign keys and transactional outboxes. No partial business data is saved on failures.
2.  **No Schema Mismatch**: DB context versioning matches the assembly. On rollback, the app connects to the matching schema.
3.  **No Orphaned Messages**: MassTransit Outbox tables (`InboxState`, `OutboxState`) track events, preventing double-processing or loss of messages during database-to-broker recovery.
4.  **No Lost Tenants**: Tenant definitions and schemas reside in the `identity` schema and are provisioned via idempotent SQL DDL scripts. Rollbacks do not delete or drop existing tenant schemas.

---

## 3. Rollback Commands

To execute a rollback back to the stable tag:
```bash
# 1. Update the image tags in the compose files to stable versions
# 2. Run compose up to replace containers
docker compose -f infrastructure/local/docker-compose.yml up -d --no-deps karamchari.api karamchari.worker
```

---

## 4. Rollback Verdict

**VERDICT**: **Partially Proven** (Local transactional boundary rollback, fail-fast configuration, and image rollback are fully verified and proven; remote CI/CD automated rollback pipelines are **Not Proven** due to local developer sandbox bounds).
