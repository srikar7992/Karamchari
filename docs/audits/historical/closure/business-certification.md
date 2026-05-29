# WS15 — Full Business Workflow Certification + Greenfield Bootstrap

**Status: ✅ CLOSED.** A new developer can bootstrap from absolute zero and run real business flows with no manual SQL or hidden steps.

## Greenfield bootstrap (the hard proof)
Performed a **complete teardown** then the documented bootstrap:
```bash
docker compose -f infrastructure/local/docker-compose.yml down -v   # removed ALL volumes
#  -> local_{sqlserver,redis,rabbitmq,seq,prometheus,grafana}-data removed; network removed
docker compose -f infrastructure/local/docker-compose.yml up -d ...  # fresh infra
dotnet run --project src/Backend/Karamchari.Api -- --provision-dev-tenants
dotnet run --project src/Backend/Karamchari.Api
```
Provisioning from an empty server: **exit 0**, "Provisioning complete." Post-state:
```
identity_tables = 11
tenant_schemas  = 3   (dev, acme, contoso)
rls_policies    = 3
signing_keys    = 1
```
No manual SQL, no data fixes, no undocumented steps.

## 5-step workflow (live, on the greenfield environment)
| # | Step | Result |
|---|---|---|
| 1 | **Register a user** | ✅ `200 {userId}` |
| 2 | **Login** | ✅ `200` access(433) + refresh token |
| 3 | **Create a tenant** (`POST /api/tenants`) | ✅ `201 {tenantId, schemaName}` — runtime-provisioned `tenant_acme_greenfield_*` (RLS policies → 4) |
| 4 | **Create an employee** | ✅ `201 {id}` |
| 5 | **Run a business workflow** (employee lifecycle) | ✅ update `200` → history `200` → terminate `200` |

## Cross-cutting verification during the flow
- **Persistence + RLS**: row visible only with `SESSION_CONTEXT('TenantId')='dev'` (0 rows without) — RLS fail-closed even for `sa`. Lifecycle persisted (`Jane Smith / Terminated`).
- **Messaging**: RabbitMQ exchanges created for both `EmployeeOnboardedIntegrationEvent` and the tenant-stamped `EnterpriseEventEnvelope--EmployeeHired--` (domain event dispatcher).
- **Audit**: employee `History` (transfer/termination) recorded in the tenant schema `EmployeeHistory` table.
- **Tracing/Logging**: traces + logs reached Seq; metrics reached Prometheus (see `observability.md`); responses carry `X-Correlation-Id` + `traceparent`.
- **Runtime tenant provisioning**: `POST /api/tenants` cloned a new schema with full RLS coverage online.

## Workflows exercised
Tenant Provisioning (CLI + runtime API) · User Registration · Login · Employee Creation/Update/Termination · Audit Logging · Messaging (publish) · Cache wiring · Tracing · Correlation. Async consumer-side flows (payroll→notification) require the `Karamchari.Worker` host — see final report "Remaining".

## Verdict
Business Workflows = **PASS**. Greenfield Bootstrap = **PASS**.
