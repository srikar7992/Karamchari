# Architecture ADR Preservation Audit

This directory records what can be preserved from repository evidence without inventing rationale.

## Existing ADR Evidence

The repository already contains ADRs under `docs/adr/`:

- Multi-tenancy model: `docs/adr/0001-multi-tenancy-model.md`
- Row-level security: `docs/adr/0002-row-level-security.md`
- RLS script generation workflow: `docs/adr/0003-rls-script-generation-workflow.md`
- Performance BC architecture: `docs/adr/0004-performance-bc-architecture.md`
- Anonymous feedback HMAC privacy: `docs/adr/0005-anonymous-feedback-hmac-privacy.md`
- KPI formula engine: `docs/adr/0006-kpi-formula-engine.md`
- Performance DbContext split roadmap: `docs/adr/0007-performance-dbcontext-split-roadmap.md`
- Notification orchestration platform: `docs/adr/0008-notification-orchestration-platform.md`
- CQRS read model strategy: `docs/adr/0009-cqrs-read-model-strategy.md`
- Search/talent discovery architecture: `docs/adr/0010-search-talent-discovery-architecture.md`
- BFF architecture: `docs/adr/0011-bff-architecture.md`
- Notification orchestration pipeline: `docs/adr/0012-notification-orchestration-pipeline.md`
- Reporting export pipeline: `docs/adr/0013-reporting-export-pipeline.md`
- Search infrastructure: `docs/adr/0014-search-infrastructure.md`

## Source-Backed Architecture Decisions

| Topic | Decision visible in code | Source Evidence | Why chosen | Alternatives rejected | Tradeoffs / consequences |
|---|---|---|---|---|---|
| Modular monolith | Backend is split into module projects under one solution. | `src/Backend/Karamchari.sln`, `src/Backend/Karamchari.*/*.csproj` | UNKNOWN from code alone. | UNKNOWN. | Module boundaries are visible, but ownership and extension rules are incomplete. |
| Multi-tenancy | Tenant context, tenant-owned entities, tenant schema/RLS interceptors, and tenant propagation filters exist. | `src/Backend/Karamchari.Core/Multitenancy`, `src/Backend/Karamchari.Core/Persistence/Interceptors`, `src/Backend/Karamchari.Core/Messaging/Tenant` | Existing ADR may explain; code alone shows implementation, not rationale. | UNKNOWN. | Strong technical isolation primitives, but operations/runbooks remain incomplete. |
| Schema strategy | Tenant schema command interceptor and `__tenant__` placeholder appear in migrations. | `src/Backend/Karamchari.Core/Persistence/Interceptors/TenantSchemaCommandInterceptor.cs`, `src/Backend/Karamchari.TimeAttendance/Migrations/TimeAttendanceDbContextModelSnapshot.cs:43` | UNKNOWN from code alone. | UNKNOWN. | Schema substitution requires careful migration/recovery procedures. |
| RLS | SQL scripts and session-context interceptor exist. | `src/Backend/Karamchari.Core/Persistence/Sql/Rls/01_predicate_function.sql`, `src/Backend/Karamchari.Core/Persistence/Sql/Rls/02_tenant_policy.template.sql`, `src/Backend/Karamchari.Core/Persistence/Interceptors/RlsSessionContextInterceptor.cs` | Existing ADR may explain; code alone does not prove why. | UNKNOWN. | RLS failure requires explicit runbook and tests. |
| Outbox | MassTransit outbox and custom relay/dead-letter processing exist. | `src/Backend/Karamchari.Core/Messaging/Outbox`, `src/Backend/Karamchari.Core/Migrations/20260508031638_AddOutboxInfrastructure.cs` | UNKNOWN from code alone. | UNKNOWN. | Operational maturity depends on dead-letter handling, queue-depth alerting, and stale-lock recovery. |
| MassTransit | Consumers and filters are used across modules. | `src/Backend/*/Consumers/*.cs`, `src/Backend/Karamchari.Core/Messaging/Tenant/*Filter.cs` | UNKNOWN. | UNKNOWN. | Event contracts must be governed; duplicate contracts exist between Core and module contracts in some areas. |
| Caching | Redis cache registration exists when a Redis connection string is present. | `src/Backend/Karamchari.Core/DependencyInjection/CoreServiceCollectionExtensions.cs:124`, `infrastructure/local/docker-compose.yml:34` | UNKNOWN. | UNKNOWN. | Cache isolation requires tests and operational runbook. |
| Identity/AuthN/AuthZ | Identity endpoints, JWT services, token stores, permission handler, and authorization middleware exist. | `src/Backend/Karamchari.Identity`, `src/Backend/Karamchari.Identity.Infrastructure`, `src/Backend/Karamchari.Core/Security`, `src/Backend/Karamchari.Core/Middleware/TenantAuthorizationMiddleware.cs` | UNKNOWN. | UNKNOWN. | JWT rotation and identity outage recovery are not certified. |
| Observability | OpenTelemetry collector, Prometheus, Grafana local services and tenant tracing/metrics code exist. | `infrastructure/local/docker-compose.yml:63`, `infrastructure/local/prometheus.yaml:5`, `src/Backend/Karamchari.Core/Observability/Tenant`, `src/Backend/Karamchari.Core/Messaging/Outbox/OutboxRelayMetrics.cs` | UNKNOWN. | UNKNOWN. | Dashboards and alerts are absent, so telemetry is not operationalized. |
| API design | Minimal API BFF endpoint groups are used. | `src/Backend/Karamchari.Api/BFF/**/*.cs` | Existing ADR `docs/adr/0011-bff-architecture.md`; code alone shows pattern. | UNKNOWN. | Many modules have no public endpoints, limiting supportability. |
| Database design | EF Core DbContexts per module plus migrations. | `src/Backend/Karamchari.*/Persistence/*DbContext.cs`, `src/Backend/Karamchari.*/Migrations` | UNKNOWN. | UNKNOWN. | Migration order and tenant provisioning recovery need certification. |

## ADR Gap Verdict

Architecture is partially preserved. Implementation choices are visible, but most `why`, rejected alternatives, and accepted tradeoffs remain UNKNOWN unless already explained in `docs/adr`. This is not enough for Enterprise Survivable status.
