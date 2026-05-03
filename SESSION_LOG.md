# Karamchari — session log

> Append-only. One block per session. Newest at the top.
> Lower-friction than ADRs for day-to-day decisions; ADRs are for load-bearing
> architectural choices that future sessions must not silently revisit.
>
> Format per session:
> - **Date** + one-line summary
> - **Shipped:** what's on disk and verified
> - **Deferred:** what was scoped out and why
> - **Decisions:** anything not big enough for an ADR but worth remembering
> - **Next up:** the priority list as of session end (so the next session can hit the ground running)

---

## 2026-05-02 — Day 2.5: project continuity setup

### Shipped

- `CLAUDE.md` at the repo root — durable handoff file. Architecture charter, conventions, current state, open questions, workflow rules for any future session.
- `SESSION_LOG.md` (this file) — append-only per-session record.
- `.gitattributes` — LF in repo, CRLF on Windows checkout for `.sln/.csproj/.props/.targets/.cmd/.bat/.ps1`. Binary handling for assets.

### Deferred

- `git init` from the Cowork Linux sandbox doesn't work cleanly against the Windows-mounted workspace — the `.git` directory creation silently fails. Added a one-time runbook in `CLAUDE.md` §8 for the user to run from PowerShell. Once initialized, future sessions can use git normally.

### Decisions

- Keeping `CLAUDE.md` and `SESSION_LOG.md` separate: `CLAUDE.md` is the canonical project context (immutable charter + current state); `SESSION_LOG.md` is the running ledger of what each session shipped/deferred. Future sessions read both.
- Two ADRs were authored independently with number 0002 (security model + provisioning workflow). Renumbered the workflow one to **ADR 0003**; both are kept since they cover distinct material. ADR 0002 = the security model itself; ADR 0003 = the registry + generator workflow that delivers it per tenant.

### Next up

(unchanged from Day 2)

1. SQL Server testcontainer integration test (RLS proof).
2. Targeted unit tests.
3. `IExceptionHandler` for `TenantResolutionException`.
4. JWT bearer config from `IConfiguration`.
5. `Karamchari.Provisioning` service.

---

## 2026-05-02 — Day 2: RLS infrastructure + HR bounded context + MassTransit reconciliation

### Shipped

- `ITenantOwned` marker + `TenantStampingInterceptor` (refuses cross-tenant inserts/updates/deletes; stamps `TenantId` on insert via EF metadata so backing fields are also caught).
- RLS DDL templates embedded in `Karamchari.Core.dll`:
  - `00_security_schema.sql` — creates the `security` schema.
  - `01_predicate_function.sql` — `[security].[fn_tenant_access]`, no admin escape.
  - `02_tenant_policy.template.sql` — per-tenant policy with FILTER + 4 BLOCK predicates per table.
- `TenantTable` + `ITenantTableRegistry` + `RlsScriptGenerator`. Registry is a concrete singleton populated at startup; generator re-validates every identifier at the SQL boundary.
- `IDomainEventDispatcher` + fail-closed `NullDomainEventDispatcher` in `Karamchari.Core.Messaging`.
- `DomainEventDispatchInterceptor` replaces the deleted `DomainEventOutboxInterceptor`. Async-only — sync `SaveChanges` with pending events throws `NotSupportedException`.
- HR bounded context:
  - `Employee` aggregate (`ITenantOwned`, raises `EmployeeHired` from `Hire` factory).
  - `EmploymentStatus` (Active, Terminated).
  - `EmployeeHired` sealed-record domain event with `EventId` + `OccurredOnUtc` captured at construction.
  - `HRDbContext` extending `KaramchariDbContext`; MassTransit's outbox entities pinned to `dbo`.
  - `EmployeeConfiguration` with unique index on `(TenantId, EmployeeNumber)`.
  - `MassTransitDomainEventDispatcher` publishing under each event's runtime type via `IPublishEndpoint`.
  - `AddKaramchariHR` registers Employees as a tenant table, replaces the null dispatcher, registers `HRDbContext`.
- `Karamchari.Api/Program.cs` (user-edited) wires MassTransit (Azure Service Bus in non-Dev, in-memory in Dev), JWT bearer scaffold, HR. `GET /api/hr/employees` exists (auth required, projects to a DTO).
- `appsettings.Development.json` ships `ConnectionStrings:KaramchariDb` (LocalDB) and empty `AzureServiceBus`.
- ADR 0002 — Row-Level Security as the database-layer failsafe.
- `CLAUDE.md` and `SESSION_LOG.md` for cross-session continuity.

### Deferred

- **SQL Server testcontainer integration test.** This is the only thing that proves the schema rewrite + session context + RLS BLOCK predicates compose correctly. Day 3 priority #1.
- **Targeted unit tests** for `HttpTenantProvider`, `TenantSchemaCommandInterceptor`, `TenantStampingInterceptor`, `RlsScriptGenerator`. Day 3 priority #2.
- **`IExceptionHandler` + RFC 7807** to replace the inline middleware mapping `TenantResolutionException` to 401/403.
- **Real JWT bearer config** bound from `IConfiguration` + tenant-registry validation (disabled / not found).
- **`Karamchari.Provisioning` service** that runs bootstrap + per-tenant DDL.
- **Outbox relay implementation.**
- **`IBackgroundTenantScope`** for workers / relays that need to set tenant context outside HttpContext.
- **First Payroll aggregate** — will trigger lifting `MassTransitDomainEventDispatcher` from HR into a shared `Karamchari.Messaging` project.

### Decisions

- **MassTransit's EF Core outbox is THE outbox.** Deleted my custom `OutboxMessage` entity and `DomainEventOutboxInterceptor`. Domain events are dispatched via `IPublishEndpoint` inside `SavingChangesAsync`, captured atomically by MassTransit's bus outbox. (See ADR 0002 for why the MT tables go in `dbo`, not the tenant schema.)
- **Core has zero MassTransit dependency.** The dispatcher abstraction (`IDomainEventDispatcher`) lives in Core; the MassTransit-backed implementation lives in HR for now. When Payroll lands, lift it into a `Karamchari.Messaging` project.
- **No admin escape in the RLS predicate.** Cross-tenant work must explicitly iterate tenants and set session context per tenant — forces the operation to be visible in logs.
- **`TenantStampingInterceptor` order matters:** runs before `DomainEventDispatchInterceptor` so a cross-tenant write is rejected *before* its events would be published.
- Strongly-typed ids deferred — `Guid` is fine for now.

### Next up

1. **SQL Server testcontainer integration test.** Spin up `mcr.microsoft.com/mssql/server:2022-latest`, run bootstrap, provision two tenants, prove FILTER hides cross-tenant rows, prove BLOCK rejects cross-tenant inserts even with raw ADO.NET.
2. Targeted unit tests (Core only — no SQL).
3. `IExceptionHandler` for `TenantResolutionException`.
4. Real JWT bearer config + tenant-registry validation.
5. `Karamchari.Provisioning` service (bootstrap + per-tenant DDL apply).

---

## 2026-05-02 — Day 1: foundation scaffold

### Shipped

- Solution structure (`Karamchari.sln`, 4 csprojs: Api, Core, HR, Payroll).
- Repo-level: `.editorconfig`, `.gitignore`, `Directory.Build.props` (`net10.0`, `Nullable enable`, `TreatWarningsAsErrors`, deterministic builds, source-link), `Directory.Packages.props` (Central Package Management).
- Core domain primitives: `IDomainEvent`, `Entity<TId>`, `AggregateRoot<TId>`, `IHasDomainEvents`, `IAuditable`.
- Multi-tenancy stack:
  - `TenantContext` (record, regex-validated tenant id, derived schema name).
  - `ITenantProvider` with explicit fail-closed contract; `HttpTenantProvider` implementing JWT-primary + header-via-gateway-proof + subdomain-corroborates resolution; `TenantOptions` bound from configuration; `TenantResolutionException` with structured failure reason.
- Persistence base: `KaramchariDbContext` abstract base with `HasDefaultSchema("__tenant__")` placeholder.
- Two interceptors:
  - `TenantSchemaCommandInterceptor` — placeholder rewriter, validates schema name at SQL boundary, refuses on missing tenant.
  - `RlsSessionContextInterceptor` — sets `SESSION_CONTEXT(N'TenantId', @id, @read_only=1)` on every connection open.
- DI composition root: `AddKaramchariCore`, `AddKaramchariInterceptors`.
- `Karamchari.Api/Program.cs` minimal API skeleton with `/health/live` and `/health/ready` endpoints.
- ADR 0001 — Multi-tenancy: shared DB, separated schema, single EF model.

### Decisions

- **Schema rewriting via `IDbCommandInterceptor`, not `IModelCacheKeyFactory` per tenant.** Rationale: keying the model cache by tenant produces N compiled models for N tenants — memory + warmup cost grows linearly, unacceptable for a SaaS targeting thousands of tenants. Single compiled model + runtime rewrite at command execution.
- **One connection string per database / elastic pool**, never per tenant. The schema is the tenant boundary, not the connection.
- **JWT claim is the only authoritative source for user-driven requests.** Subdomain and trusted header are corroboration only. All present sources must agree.
- **Tenant id format is `^[a-z0-9][a-z0-9-]{0,49}$`**, schema becomes `tenant_<id-with-hyphens-as-underscores>`.
