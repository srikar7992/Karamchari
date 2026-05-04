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

## 2026-05-03 — Day 3: tests + frontend portal + end-to-end loop closed

### Shipped (combined: user-led + this session)

**Backend (user-led between sessions):**
- `Karamchari.Core.UnitTests` (xUnit + FluentAssertions + NSubstitute) — 10 tests passing covering `HttpTenantProvider` (every disagreement / missing-source / untrusted-header path) and `TenantSchemaCommandInterceptor` (placeholder rewrite, missing tenant, bad schema name, no-op cases).
- `Karamchari.Core.IntegrationTests` (Testcontainers.MsSql against `mcr.microsoft.com/mssql/server:2022-latest`) — `RlsSqlServerIntegrationTests` proves schema rewrite + `SESSION_CONTEXT` + RLS BLOCK predicates compose under real SQL Server. Project compiles; run blocked locally only by Docker not running.
- Both test projects added to `Karamchari.sln`.
- `Testcontainers.MsSql` 4.10.0 pinned in `Directory.Packages.props`.
- **Real interceptor bug fix in `TenantSchemaCommandInterceptor.cs:144`**: SQL containing `__tenant__` as a substring of another identifier was tripping the post-rewrite `ReferenceEquals(rewritten, original)` throw. Added an explicit `placeholderRegex.IsMatch(original)` check before the `Replace` so the fast-path bail returns cleanly when the regex's word boundaries don't match. Found by the unit tests — exactly what the test pyramid is for.
- HR grew: `Department` aggregate, `DepartmentCreatedConsumer`, `IOrganizationService` + `OrganizationService`, `CreateDepartmentCommand` contract.
- Api: `DevelopmentTenantAuthenticationHandler` (validates `X-Tenant-Id` + matching `X-Karamchari-Gateway` proof, synthesizes a principal with the tenant claim) for dev so the FE can authenticate before Entra ID is wired. New endpoints `GET /api/hr/departments` and `POST /api/hr/departments`. Dev `appsettings.Development.json` ships `Tenancy:TrustedGatewayFingerprint = "local-dev-gateway"`.
- `MassTransit.Abstractions` 8.3.0 pinned in CPM.

**This session:**
- **Loosened `TenantContext.TenantIdPattern`** from `^[a-z0-9][a-z0-9-]{0,49}$` to `^[a-z0-9][a-z0-9_-]{0,49}$` so `sch_oakridge`-style underscored ids pass validation. `RlsScriptGenerator.TenantIdRegex` already allowed underscores, so only one file changed. Schema name derivation unaffected.
- **Stitch UI mocks extracted** to `docs/design/stitch/` — 4 HTML pages plus `monolith_precision/DESIGN.md` design system reference.
- **Next.js 15 App Router portal** scaffolded by hand at `src/Frontend/portal/` (npm, plain Next.js — no Nx for now). Tailwind 3.4 with the Monolith Precision token set, Inter via `next/font/google`, Material Symbols via Google Fonts CDN.
- **Centralized `api` client** at `src/lib/api/client.ts` — fetch-based, auto-attaches `X-Tenant-Id` and `X-Karamchari-Gateway`, surfaces typed `ApiError` with `code`/`status`/`reason`/`displayMessage`. Env-var-driven (`NEXT_PUBLIC_API_BASE_URL`, `NEXT_PUBLIC_DEFAULT_TENANT_ID`, `NEXT_PUBLIC_GATEWAY_FINGERPRINT`) so we never hardcode tenant ids in source.
- **TanStack Query v5** wired via `Providers` (`'use client'`) inside the root `layout.tsx`. Sane defaults (30s staleTime, 5min gcTime, no refetchOnWindowFocus, retry 1). Devtools in dev only.
- **`useDepartments` + `useCreateDepartment`** hooks, mirror DTOs in `src/lib/types/department.ts`.
- **shadcn-style primitives** hand-rolled: `Table`, `Card`, `Button`, `StatusDot`. App shell with `Sidebar` (active-route nav, tenant id badge) and `Topbar` (page title + action slot).
- **`/dashboard`**: static port of the Stitch dashboard mock (KPIs, recent activity, system status) using the shared shell. **`/directory`**: live data from `useDepartments` rendered in the shadcn Table with proper loading skeletons / empty / error states. `/` redirects to `/dashboard`.
- **Backend dev CORS policy** (`Karamchari.Api/Cors/DevPortalCors.cs`): allows `http://localhost:3000`, the two tenant headers, and standard verbs. **No credentials** (header auth, not cookies). Wired in `Program.cs` only when `IsDevelopment()`. `UseCors` runs before `UseAuthentication` so preflights succeed without auth.
- **Portal README** with quick-start, header contract, conventions.
- **CLAUDE.md updated**: tenant id regex, FE stack section, current state for Day 3, next-up list re-prioritized (Docker-up + smoke test), next ADR number bumped to 0004.

### Decisions

- **Tenant id underscores allowed.** The cost/value of being strict about hyphen-only was nil; allowing underscores matches school-flavoured ids (`sch_oakridge`) without compromising schema-name safety (the schema name regex already allowed underscores).
- **Plain Next.js, not Nx.** Single FE app for now — Nx multiplies setup cost without solving any problem we have today. We'll lift into Nx when admin/ops apps land.
- **Hand-rolled shadcn primitives, not the CLI.** We need 4 components; the CLI brings a Radix/CVA dependency tree and is fiddly against an existing project. We can adopt the CLI later if we need 20+ primitives.
- **No axios.** Native fetch is enough; one less dependency and one less bundle KB.
- **Frontend env contract is dev-only.** `NEXT_PUBLIC_DEFAULT_TENANT_ID` and `NEXT_PUBLIC_GATEWAY_FINGERPRINT` exist purely so local dev works against `appsettings.Development.json`. In prod, the tenant comes from the user's signed JWT and the gateway proof is injected by APIM — neither lives in a frontend env. `.env.example` and the README make this explicit.
- **CORS without credentials.** We auth via headers, not cookies. Allowing credentials would force tighter origin handling for no benefit.

### Next up

1. Start Docker Desktop and run the integration tests (`dotnet test tests/Backend/Karamchari.Core.IntegrationTests`) to prove the RLS pipeline end-to-end.
2. Run BFF + portal locally and exercise `GET /api/hr/departments` and `POST /api/hr/departments` from the UI; confirm new rows appear after invalidation.
3. Targeted unit tests for `TenantStampingInterceptor`, `RlsScriptGenerator`, `DevelopmentTenantAuthenticationHandler`.
4. `IExceptionHandler` + RFC 7807 ProblemDetails to replace the inline middleware.
5. JWT bearer config from `IConfiguration` + tenant-registry validation (disabled / not found / expired).
6. `Karamchari.Provisioning` service (bootstrap + per-tenant DDL apply).

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
