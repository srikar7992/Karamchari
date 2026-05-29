# Runtime Concept Registry

| Concept | Purpose | Owner | Overlap |
| :--- | :--- | :--- | :--- |
| `TenantExecutionContext` | AsyncLocal container for tenant identity | Core (Foundation) | None (Canonical) |
| `TenantContext` | Legacy tenant record (ID, Schema, Source) | Core (Multitenancy) | `TenantExecutionEnvelope` |
| `TenantExecutionEnvelope` | Metadata container (ID, Correlation, Source, Replay) | Core (Foundation) | `TenantContext` |
| `TenantExecutionScope` | general IDisposable scope | Core (Foundation) | `TenantJobExecutionScope`, `TenantMessageConsumerScope` |
| `TenantJobExecutionScope` | Background job specific scope | BackgroundJobs | `TenantExecutionScope` |
| `TenantMessageConsumerScope`| Message consumer specific scope | Messaging | `TenantExecutionScope` |
| `TenantSqlConnectionScope` | Database connection specific scope | Persistence | `RlsConnectionGuard` |
| `RlsConnectionGuard` | SQL Session Context management | Persistence | `TenantSqlConnectionScope` |
| `TenantExecutionEnvelopeSerializer` | Job/Message serialization | Jobs/Messaging | `TenantJobContextSerializer` |
| `ReplayProtectionService` | Idempotency engine | Messaging | `SagaReplayProtectionService` |
| `TenantQueryValidationInterceptor` | SQL protection | Persistence | `RawSqlTenantGuard` |

# Cognitive Complexity Heatmap

| Area | Concept Count | Overlap % | Complexity Score |
| :--- | :--- | :--- | :--- |
| **Multitenancy** | 8 | 60% | 🔥 HIGH |
| **Messaging** | 12 | 40% | 🟡 MODERATE |
| **Persistence** | 6 | 30% | 🟢 LOW |
| **Background Jobs**| 5 | 50% | 🟡 MODERATE |

# Duplicate Concept Matrix

| Concept A | Concept B | Relationship | Consolidation Target |
| :--- | :--- | :--- | :--- |
| `TenantContext` | `TenantExecutionEnvelope` | Identical ID representation | `TenantExecutionEnvelope` |
| `TenantJobExecutionScope`| `TenantExecutionScope` | Implementation detail overlap | `TenantExecutionScope` |
| `TenantMessageConsumerScope`| `TenantExecutionScope` | Implementation detail overlap | `TenantExecutionScope` |
| `TenantJobContextSerializer`| `TenantExecutionEnvelope.ToJson` | Duplicated serialization | `TenantExecutionEnvelope` |
| `SagaReplayProtectionService`| `ReplayProtectionService` | Feature overlap | `ReplayProtectionService` |
