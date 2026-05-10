# Karamchari Platform Contracts

Platform contracts form the single source of truth for runtime behavior across the distributed system.

## 1. Tenant Execution Contract
*   **Metadata**: Every execution boundary MUST carry `TenantId`, `CorrelationId`, `RequestId`, and `ExecutionSource`.
*   **Propagation**: The execution envelope is serialized automatically across HTTP (`X-Tenant-Id`), Messaging (Headers), and Background Jobs (Payloads).
*   **Immutability**: Once an execution scope is `Established()`, the Tenant ID cannot be mutated on that thread.

## 2. Persistence Contract
*   **Isolation**: Every tenant receives a dedicated logical schema (`tenant_{id}`).
*   **RLS Enforcement**: SQL Server Row-Level Security blocks cross-schema reads. All connections MUST execute `sp_set_session_context` prior to querying.
*   **Connection Pooling**: Connections returned to the pool MUST be explicitly cleared of session state to prevent contamination.

## 3. Messaging Contract
*   **Replay Safety**: Messages must carry deterministic `MessageId`s. The `ReplayProtectionService` guarantees idempotency.
*   **Retry Semantics**: Retries carry the exact same `TenantId` but increment the `RetryAttempt` counter. State is reset fully on every retry.
*   **Dead-Lettering**: Poison messages preserve the tenant envelope for safe replay.

## 4. Observability Contract
*   **Tracing**: Every trace MUST be tagged with `karamchari.tenant.id` and `karamchari.correlation.id`.
*   **Logging**: Structured logs automatically enrich with the active `TenantExecutionContext`.
*   **Passive Behavior**: Observability systems MUST NOT crash the application or alter execution flow if they fail.

## 5. Cache Contract
*   **Prefixing**: All cache keys are strictly prefixed: `{TenantId}:{Namespace}:{Key}`.
*   **Poisoning Protection**: The `TenantCacheValidator` ensures keys cannot traverse directories (`../`).
