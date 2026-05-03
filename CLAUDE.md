# Karamchari — agent handoff

> **For any Claude session opening this project: read this file first.**
> It is the durable context across sessions. The architecture charter is
> immutable; the "current state" and "next up" sections are append-only logs
> of where we are and where we're going.

---

## 1. What this project is

**Karamchari** is a multi-tenant, domain-agnostic **Employee Management System (EMS)** targeting IT firms, schools, and hospitals. Domain modelling is metadata-driven around generic `Actor` entities. Target launch is **April 2027**. Built and maintained by a solo developer (Srikar) — every architectural choice biases toward **strict boundaries over premature optimization**, **fail-closed defaults**, and **operational simplicity**.

## 2. Architecture charter — IMMUTABLE

These rules are non-negotiable. Do not deviate without explicit user approval recorded in an ADR.

### Topology

- **Modular monolith.** One `.sln` containing isolated class-library bounded contexts (`Karamchari.Core`, `Karamchari.HR`, `Karamchari.Payroll`). One deployable: `Karamchari.Api`.
- **No microservices.** **No GraphQL.** REST Backend-for-Frontend (BFF).
- **Bounded-context isolation rules.** A bounded context may reference `Karamchari.Core` and nothing else. Cross-context communication is via integration events on the bus, never via direct DbContext or EF entity references.

### Tech stack

| Layer        | Choice                                                         |
| ------------ | -------------------------------------------------------------- |
| Backend      | .NET 10, ASP.NET Core Minimal APIs, EF Core 10                 |
| Frontend     | Nx + Next.js + TypeScript (not yet initialized)                |
| Database     | Azure SQL Elastic Pools                                        |
| Messaging    | MassTransit + EF Core transactional outbox                     |
| Hosting      | Azure Container Apps (ACA)                                     |
| Secrets      | Azure Key Vault via Managed Identity — NEVER in code           |
| AI           | Microsoft Semantic Kernel + Azure OpenAI + Document Intelligence |

### Multi-tenancy

- **Shared database, separated schema per tenant.** Tables for tenant `acme` live in schema `tenant_acme`. The schema is the tenant boundary, not the connection.
- **One connection string per database / elastic pool**, retrieved from Key Vault via Managed Identity. **Never** per-tenant connection strings.
- **Tenant resolution is hierarchical and fail-closed:**
  - JWT claim (`tenant_id`) is **authoritative** for user-driven requests.
  - `X-Tenant-Id` header is accepted **only** when the trusted gateway proof header (`X-Karamchari-Gateway`) matches the configured fingerprint — service-to-service traffic via APIM only.
  - Subdomain corroborates the JWT, never overrides it.
  - All present sources must agree, or the request is rejected. **No silent fallback.**
- **Schema switching is runtime, not model-time.** Single compiled EF model with `HasDefaultSchema("__tenant__")`; `TenantSchemaCommandInterceptor` rewrites the placeholder to the active tenant schema right before SQL execution. **NEVER** key `IModelCacheKeyFactory` by `TenantId` (model explosion at scale).
- **RLS is the failsafe.** Every tenant table has a SQL Server Row-Level Security policy with FILTER + four BLOCK predicates (after insert, before/after update, before delete) using `SESSION_CONTEXT(N'TenantId')`. The predicate function lives in a dedicated `security` schema with **no admin escape hatch** — cross-tenant work must explicitly iterate tenants and set session context per tenant.

### Security & Zero Trust

- All secrets via Key Vault + Managed Identity. None in code, config, or repo.
- Granular rate limiting at ASP.NET Core middleware (concurrency limiters on heavy endpoints). APIM handles spike arrests.
- LLMs are **untrusted input**. Use Semantic Kernel **strict structured outputs only**. The LLM parses intent and returns JSON parameters; the .NET API executes hardcoded backend logic with `TenantId` from the secure JWT. **The LLM never generates or executes raw SQL.**

### Event-driven backbone

- **MassTransit** for all asynchronous communication.
- **Transactional Outbox** via MassTransit's EF Core outbox. Every publish goes through `IPublishEndpoint` from inside a `SaveChangesAsync` scope so it's captured atomically.
- MassTransit's outbox tables (`InboxState`, `OutboxMessage`, `OutboxState`) are pinned to **`dbo`** (shared infrastructure, NOT tenant-owned).
- Domain events drained from aggregates are dispatched by `DomainEventDispatchInterceptor` → `IDomainEventDispatcher` → `MassTransitDomainEventDispatcher` → `IPublishEndpoint`.
- Consumers must be **idempotent**. External API calls use composite idempotency keys (`TenantId:PeriodId:BusinessId`).
- Long-running sagas (e.g., payroll runs) use **MassTransit State Machines**.

### Performance focus

- I/O over memory micro-optimizations. **No** `Span<T>` or AOT premature optimization unless explicitly justified.
- Prevent N+1 queries at all costs.
- EF Core projections (`.Select(x => new Dto)`) for reads — never return tracked entities.
- `IAsyncEnumerable<T>` for streaming large reports — **never** `.ToList()` on large result sets.
- Single compiled EF model regardless of tenant count.

## 3. Repository layout

```
Karamchari/
├── CLAUDE.md                          # this file — read first
├── SESSION_LOG.md                     # per-session shipped/deferred log
├── README.md                          # human-facing overview
├── .editorconfig                      # file-scoped namespaces, async-suffix, CA1848
├── .gitignore
├── .gitattributes
├── Directory.Build.props              # net10.0, Nullable enable, TreatWarningsAsErrors
├── Directory.Packages.props           # Central Package Management — versions in one place
├── docs/
│   └── adr/
│       ├── 0001-multi-tenancy-model.md
│       └── 0002-row-level-security.md
└── src/
    ├── Backend/
    │   ├── Karamchari.sln
    │   ├── Karamchari.Api/            # ASP.NET Core BFF host (composition root)
    │   ├── Karamchari.Core/           # Tenancy, primitives, persistence interceptors, RLS
    │   ├── Karamchari.HR/             # HR bounded context (Employee, MassTransit dispatcher)
    │   └── Karamchari.Payroll/        # Payroll bounded context (stub)
    └── Frontend/                      # Nx workspace (not yet initialized)
```

## 4. Decisions log (ADRs)

ADRs in `docs/adr/`. Read top-to-bottom before changing anything load-bearing.

| #    | Title                                                       | Status   |
| ---- | ----------------------------------------------------------- | -------- |
| 0001 | Multi-tenancy: shared DB, separated schema, single EF model | Accepted |
| 0002 | Row-Level Security as the database-layer failsafe           | Accepted |
| 0003 | RLS script generation workflow                              | Accepted |

## 5. Conventions

- **C#:** file-scoped namespaces. `Nullable enable`, `TreatWarningsAsErrors=true` repo-wide. Async methods end with `Async`. Use source-generated logging (`LoggerMessage`, CA1848) — always include `TenantId` in scope.
- **EF Core:** projections for reads. `IAsyncEnumerable<T>` for streaming. New `ITenantOwned` entities MUST also be added to `services.RegisterTenantTable("...")` in their bounded context's `AddKaramchari{Context}` extension — this is what makes RLS cover the table. Forgetting it is a security regression.
- **Domain events:** sealed records. Capture `OccurredOnUtc` at construction (so retries don't drift). `EventId` is also the downstream idempotency key.
- **Tests:** xUnit + FluentAssertions + NSubstitute. Integration tests use Microsoft.AspNetCore.Mvc.Testing + a real SQL Server testcontainer.
- **Commits:** Conventional Commits (`feat:`, `fix:`, `refactor:`, `chore:`, `docs:`). Reference ADR numbers when relevant.
- **Naming for new tenants:** lowercase, hyphens allowed, `^[a-z0-9][a-z0-9-]{0,49}$`. Schema becomes `tenant_<id-with-hyphens-as-underscores>`.

## 6. Current state (as of 2026-05-02)

### Built and verified

- Solution scaffold (`.sln`, 4 csprojs), Central Package Management, Directory.Build.props with `net10.0` + `TreatWarningsAsErrors`.
- `Karamchari.Core`:
  - Domain primitives (`AggregateRoot<TId>`, `Entity<TId>`, `IDomainEvent`, `IHasDomainEvents`, `IAuditable`).
  - Multi-tenancy (`ITenantProvider` + `HttpTenantProvider` with JWT-primary / header-via-gateway / subdomain-corroborates resolution; `TenantContext` with format validation; `ITenantOwned` marker).
  - Persistence interceptors:
    - `TenantSchemaCommandInterceptor` — rewrites `__tenant__` → `tenant_<id>` at command execution.
    - `RlsSessionContextInterceptor` — sets `SESSION_CONTEXT(N'TenantId', @id, @read_only=1)` on every connection open.
    - `TenantStampingInterceptor` — stamps `TenantId` on insert; refuses cross-tenant inserts/updates/deletes.
    - `DomainEventDispatchInterceptor` — drains aggregate events, hands to `IDomainEventDispatcher` inside `SavingChangesAsync`. Sync `SaveChanges` with pending events throws.
  - RLS infrastructure:
    - Embedded SQL templates (`00_security_schema`, `01_predicate_function`, `02_tenant_policy.template`).
    - `TenantTableRegistry` (singleton, thread-safe) populated by each context's DI extension.
    - `RlsScriptGenerator` builds bootstrap + per-tenant policy scripts; re-validates every identifier at the SQL boundary.
  - Messaging abstractions: `IDomainEventDispatcher` + fail-closed `NullDomainEventDispatcher`.
  - DI: `AddKaramchariCore`, `RegisterTenantTable`, `AddKaramchariInterceptors`.
- `Karamchari.HR`:
  - `Employee` aggregate (`ITenantOwned`, raises `EmployeeHired` from `Hire` factory, `Rename` / `ChangeWorkEmail` / `Terminate` operations).
  - `EmploymentStatus` enum (Active, Terminated).
  - `EmployeeHired` domain event (sealed record).
  - `HRDbContext` — extends `KaramchariDbContext`, adds MassTransit's outbox entities **pinned to `dbo`**.
  - `EmployeeConfiguration` — table mapping, indexes (unique `(TenantId, EmployeeNumber)`).
  - `MassTransitDomainEventDispatcher` — publishes via `IPublishEndpoint` under each event's runtime type.
  - `AddKaramchariHR` — registers `Employees` as a tenant table, replaces the null dispatcher with the MassTransit one, registers `HRDbContext`.
- `Karamchari.Api`:
  - Wires Core + MassTransit (Azure Service Bus in non-Dev, in-memory bus in Dev) + JWT bearer scaffold + HR.
  - `GET /api/hr/employees` (auth required, projects to `EmployeeListItem`).
  - `/health/live` and `/health/ready` (anonymous).
  - Inline tenant-resolution exception handler (TODO: replace with `IExceptionHandler`).
- `Karamchari.Payroll`: project + csproj only (placeholder).
- ADRs 0001 and 0002 written.

### Not yet built (priority order)

1. **SQL Server testcontainer integration test** (HIGHEST priority — the only thing that proves schema rewrite + session context + RLS BLOCK predicates compose correctly under real SQL Server).
2. Targeted unit tests:
   - `HttpTenantProvider`: every disagreement / missing-source / untrusted-header path.
   - `TenantSchemaCommandInterceptor`: placeholder rewrite, refuse on missing tenant, refuse on bad schema name, no-op on no placeholder.
   - `TenantStampingInterceptor`: stamps on insert, rejects mismatched insert/update/delete.
   - `RlsScriptGenerator`: bootstrap output, per-tenant output with multiple registered tables, identifier rejection.
3. Replace inline tenant-resolution middleware with `IExceptionHandler` + RFC 7807 ProblemDetails.
4. Bind real JWT bearer config from `IConfiguration` (authority, audience, JWKS) + tenant-registry validation (disabled / not found / expired).
5. `Karamchari.Provisioning` service that runs:
   - Bootstrap scripts on deploy.
   - Per-tenant: `CREATE SCHEMA`, run EF migrations against the tenant schema, run per-tenant RLS policy.
6. Outbox relay implementation (drain MassTransit's outbox under `dbo` — single relay, no per-tenant scoping needed).
7. Background job tenant scope: `IBackgroundTenantScope` + `WithTenantAsync(tenantId, ...)` wrapper for the relay and any future workers.
8. First Payroll aggregate + bounded context wiring (will trigger the lift of `MassTransitDomainEventDispatcher` from HR into a shared `Karamchari.Messaging` project).
9. Frontend: initialize Nx + Next.js workspace under `src/Frontend/`.

### Open questions / explicit deferrals

- **Strongly-typed ids** (e.g. `EmployeeId`): deferred. `Employee` uses `Guid` directly for now.
- **Strong-typing tenant id at compile time** (vs string): deferred. `ITenantOwned.TenantId` is a string with regex-validated format.
- **Cross-tenant admin reporting** model: deferred until a real admin endpoint is needed. The pattern will be a dedicated `IAdminTenantProvider` that explicitly iterates tenants and sets session context, with audit logging.

## 7. Workflow rules for any future session

1. **Read this file first.** Then check `SESSION_LOG.md` for what the previous session shipped.
2. **Before any creative work**, ask the user clarifying questions (`AskUserQuestion`). Do not assume scope.
3. **Use TodoList** for any multi-step work — Cowork renders it as a progress widget.
4. **Architectural rules in section 2 are immutable.** Any deviation requires an explicit user decision recorded in a new ADR (next number: 0003).
5. **When the user changes a file under your nose** (system-reminder mentions a modification), reconcile with their direction — don't revert.
6. **Verification step is mandatory** before declaring work complete. Re-grep against the rule audits in `SESSION_LOG.md` (or write new ones for new rules).
7. **Always commit** at the end of a session: `git add -A && git commit -m "<conventional message>"`.

## 8. First-time git init (run this once on the Windows host)

The Cowork Linux sandbox can't reliably initialize a `.git` directory inside the
Windows-mounted workspace, so the very first session leaves git uninitialized.
From a PowerShell or cmd prompt at `C:\Users\srika\Desktop\Karamchari`:

```powershell
git init --initial-branch=main
git config core.autocrlf false
git config core.eol lf
git add .
git commit -m "chore: initial scaffold (Day 1+2)"
```

Subsequent sessions can use git normally — the `.gitattributes` and `.gitignore`
already shipped take care of cross-platform line endings and build-output exclusion.

## 9. Local dev quick-start

```bash
cd src/Backend
dotnet restore
dotnet build
dotnet run --project Karamchari.Api
```

Required local config (in `appsettings.Development.json`, already shipped):
- `ConnectionStrings:KaramchariDb` — defaults to LocalDB.
- `ConnectionStrings:AzureServiceBus` — empty in Dev (uses in-memory bus).
- `Tenancy:RequireSubdomainAgreement` — `false` in Dev.

Once the Provisioning service exists, bootstrapping a fresh local DB will be:
1. Run `RlsScriptGenerator.BuildBootstrapScripts()` SQL.
2. Provision a `dev` tenant: create schema, run migrations, apply per-tenant RLS policy.
3. Issue a dev JWT with `tenant_id=dev` claim against the API.
