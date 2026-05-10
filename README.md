# Karamchari

A multi-tenant, domain-agnostic Employee Management System (EMS).
Targets IT companies, schools, hospitals via a metadata-driven `Actor` model.

> **Status:** Day 1 scaffold. Foundations only — no business domain yet.
> **Target launch:** April 2027.

## Architecture at a glance

| Concern              | Choice                                                                       |
| -------------------- | ---------------------------------------------------------------------------- |
| Backend topology     | **Modular monolith** (one `.sln`, isolated bounded contexts)                 |
| API style            | REST Backend-for-Frontend (BFF). **No GraphQL.**                             |
| Multi-tenancy model  | **Shared database, separated schema** per tenant                             |
| Tenant isolation     | Schema-aware `DbCommandInterceptor` + SQL Server **Row-Level Security** failsafe |
| Tenant resolution    | JWT claim primary; trusted header (APIM) for service-to-service; subdomain validates only |
| Async backbone       | **MassTransit** + Transactional Outbox + idempotent consumers                |
| Persistence          | EF Core 10 against Azure SQL Elastic Pools                                   |
| Secrets              | Azure Key Vault via Managed Identity. **Never in code.**                     |
| AI                   | Semantic Kernel with **structured outputs only**. LLMs never emit raw SQL.   |

## Repository layout

```
Karamchari/
├── src/
│   ├── Backend/                           # .NET 10 modular monolith
│   │   ├── Karamchari.sln
│   │   ├── Karamchari.Api/                # ASP.NET Core Minimal APIs (BFF)
│   │   ├── Karamchari.Core/               # Cross-cutting: tenancy, outbox, primitives
│   │   ├── Karamchari.HR/                 # HR bounded context
│   │   └── Karamchari.Payroll/            # Payroll bounded context
│   └── Frontend/                          # Nx workspace (Next.js + TypeScript)
├── tests/
├── docs/
│   └── adr/                               # Architecture Decision Records
├── .editorconfig
├── .gitignore
├── Directory.Build.props                  # repo-wide MSBuild props
├── Directory.Packages.props               # Central Package Management (CPM)
└── README.md
```

## Bounded context rules

A bounded context (e.g., `Karamchari.HR`) **may**:

- depend on `Karamchari.Core`
- expose contracts (DTOs, integration events) to other contexts via `Karamchari.Core`'s contract abstractions
- own its own `DbContext` and EF entities

A bounded context **may not**:

- reference another bounded context's `DbContext`, EF entities, or domain types
- query another bounded context's tables directly
- bypass MassTransit for cross-context communication

These rules are enforced by project references and (eventually) ArchUnitNET tests.

## Multi-tenancy contract

1. **Connection strings.** One per database / elastic pool, stored once in Key Vault. The schema is the tenant boundary, not the connection.
2. **Tenant resolution.** `ITenantProvider.GetTenant()` reads the tenant from the validated JWT, cross-checks any present subdomain or trusted header, and throws `TenantResolutionException` if any source is missing or disagrees.
3. **Schema injection.** A single EF model is shared across tenants. The `TenantSchemaCommandInterceptor` rewrites the placeholder schema `__tenant__` to the active tenant schema right before the SQL command is executed. We do **not** include `TenantId` in `IModelCacheKeyFactory` — that causes model explosion at scale.
4. **RLS failsafe.** `RlsSessionContextInterceptor` sets `SESSION_CONTEXT('TenantId', @id)` on every connection open. SQL Server Row-Level Security policies use this value as a defense-in-depth guarantee even if a query somehow escapes schema rewriting.
5. **Outbox.** `DomainEventOutboxInterceptor` harvests aggregate domain events on `SavingChanges` and writes them to the tenant's outbox table inside the same transaction.

See `docs/adr/0001-multi-tenancy-model.md` for the full rationale.

## Getting started

The Karamchari platform is a fully reproducible local execution environment. To onboard:

1. **Install Prerequisites:**
   - [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
   - [Docker Desktop](https://www.docker.com/products/docker-desktop/)
   - [Node.js](https://nodejs.org/) (for frontend/mobile)

2. **Run One-Command Setup:**
   ```powershell
   ./setup.ps1
   ```
   This script validates your environment, restores packages, pulls infrastructure, provisions the database, and seeds dev tenants (`dev`, `acme`, `contoso`).

3. **Start the Local Runtime:**
   ```powershell
   ./scripts/runtime/run-local.ps1
   ```
   Starts the core infrastructure and the API with hot-reload enabled.

4. **Validate Your Changes:**
   ```powershell
   ./scripts/validation/validate-runtime.ps1
   ```
   Runs unit tests, integration tests, and the **Tenant Isolation Certification** suite.

## Local Runtime & Observability

The local environment mirrors production topology to ensure deterministic behavior.

### Infrastructure Stack (Docker)
- **SQL Server 2022:** Primary persistence with RLS and multi-schema isolation.
- **Redis 7:** Distributed caching and lock management.
- **RabbitMQ:** Asynchronous messaging backbone (MassTransit).
- **Seq:** Structured log ingestion and visualization.
- **OpenTelemetry Collector:** Centralized trace/metric collection.
- **Prometheus & Grafana:** Infrastructure and application metrics.
- **Azurite:** Local Azure Storage (Blob/Queue/Table) simulation.
- **Mailpit:** Local SMTP capture for notification testing.

### Developer Dashboards
Once the runtime is started, the following dashboards are available:
- **API Gateway:** [http://localhost:8080](http://localhost:8080)
- **Seq (Logs & Traces):** [http://localhost:8081](http://localhost:8081)
- **RabbitMQ Management:** [http://localhost:15672](http://localhost:15672) (guest/guest)
- **Grafana (Metrics):** [http://localhost:3000](http://localhost:3000)
- **Prometheus:** [http://localhost:9090](http://localhost:9090)
- **Mailpit (Inbox):** [http://localhost:8025](http://localhost:8025)

### Chaos Engineering
Run local chaos injection to verify platform resilience:
```powershell
./scripts/chaos/run-chaos.ps1
```
This suite injects database outages, network latency, and retry storms while verifying that tenant isolation and replay safety remain intact.


## Conventions

- **C#:** file-scoped namespaces, `Nullable enable`, warnings as errors, `async`-suffixed async methods.
- **EF Core:** projections (`.Select(x => new Dto)`) over tracked entities for reads. `IAsyncEnumerable<T>` for streaming reports. **Never** `.ToList()` on large result sets.
- **Logging:** source-generated `LoggerMessage` (CA1848). Always include `TenantId` in scope.
- **Tests:** xUnit + FluentAssertions + NSubstitute.
