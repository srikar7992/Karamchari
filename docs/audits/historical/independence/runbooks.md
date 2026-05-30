# Phase 4: Operational Runbook Audit

This audit evaluates the existence, adequacy, and repeatability of operational guides and runbooks required to manage production issues, deploy changes, recover services, or perform maintenance.

---

## Operational Runbooks Registry

To ensure operational survival, an engineer must have access to clear guides for core tasks. We evaluated the existence of the 10 requested runbooks:

| Runbook Concern | Status | Location in Codebase | Repeatability for New Engineer |
| :--- | :--- | :--- | :--- |
| **Deploy Guide** | ❌ **Missing** | Skeleton in [.github/workflows/deploy-api.yml](.github/workflows/deploy-api.yml) (Azure deployment steps are commented out). | **Low**: A developer must manually configure ACR, Bicep parameter values, and App Service bindings. |
| **Rollback Guide** | ❌ **Missing** | None. | **Low**: No strategy for database migrations revert or blue-green container routing. |
| **Recovery Guide** | ❌ **Missing** | None. | **Medium**: Relies on docker-compose local files, but lacks production backup/restore steps. |
| **RabbitMQ Guide** | ❌ **Missing** | Mentioned in [README-LOCAL.md](docs/development/local-setup/README.md) for local access. | **Medium**: Local troubleshooting is simple, but lacks production scale/clustering guidelines. |
| **Redis Guide** | ❌ **Missing** | None. | **Low**: No guidelines for cache eviction, redis cluster recovery, or connection tuning. |
| **SQL Migrations** | ❌ **Missing** | EF migrations folders in bounded contexts. | **High**: Standard EF Core CLI migrations pattern is followed. |
| **Tenant Provisioning**| ❌ **Missing** | Code logic in `TenantProvisioningService.cs` and API flag `--provision-dev-tenants`. | **High**: Execution of CLI switches is scripted. |
| **Auth Setup / Keys** | ❌ **Missing** | Developer tokens referenced in [docs/startup-sequence.md](docs/architecture/startup-sequence.md). | **Medium**: Key validation relies on standard ASP.NET Core Identity. |
| **Outbox Monitoring** | ❌ **Missing** | Mentions in `OutboxRelayDbContext` registry. | **Low**: Tracing stuck outbox messages requires database-level querying without a dedicated dashboard. |
| **Worker Scaling** | ❌ **Missing** | `Karamchari.Worker` project code. | **Medium**: Can be run as a Docker container, but lacks auto-scaling guidelines. |

---

## Reconstructed Operational Runbooks

Because these documents do not exist formally in the repository, we have reverse-engineered the codebase and reconstructed the operational steps to provide immediate capability to a new engineer.

### Runbook A: SQL Schema Migrations & Schema Deployment

Since Karamchari uses a multi-tenant shared database with isolated tenant schemas, applying database changes requires a specific sequence.

#### 1. Generating a new Migration
Migrations must be generated against a specific DbContext. To generate a migration for the `HR` context:
```bash
dotnet ef migrations add <MigrationName> \
  --project src/Backend/Karamchari.HR \
  --startup-project src/Backend/Karamchari.Api \
  --context HRDbContext
```

#### 2. Applying Migrations locally
Local database migrations are automatically executed by passing the `--provision-dev-tenants` flag on start:
```bash
dotnet run --project src/Backend/Karamchari.Api -- --provision-dev-tenants
```

#### 3. Production Deployment
Production database schema migrations must be applied using EF Core bundles or command execution prior to rolling out new API container versions. Migrations must execute against the main database and then populate individual tenant schemas via schema cloning queries.

---

### Runbook B: Tenant Schema Provisioning

To provision a new tenant (e.g., `tenant_neworg`) in production:

#### 1. Trigger API endpoint
Call the POST `/api/v1/tenants/provision` endpoint (defined in [IdentityEndpoints.cs](src/Backend/Karamchari.Api/BFF/Identity/IdentityEndpoints.cs) or via the API gateway):
```json
{
  "companyName": "NewOrg Inc",
  "adminEmail": "admin@neworg.com"
}
```

#### 2. Verify Schema Creation
Login to SQL Server and verify the schema exists:
```sql
SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'tenant_neworg';
```

#### 3. Seed Metadata & Base Tables
The tenant provisioning service clones tables from the master template to the new tenant schema and configures Row-Level Security (RLS) predicates for the new schema boundary.

---

### Runbook C: Service Outage Recovery (Seq + Redis + RabbitMQ)

In the event of a local environment failure or connection storm:

#### 1. Perform Infrastructure Restart
Run the following to restart all containerized middleware:
```bash
docker compose -f infrastructure/local/docker-compose.yml down
docker compose -f infrastructure/local/docker-compose.yml up -d
```

#### 2. Inspect Logs and Health
Access the local **Seq** dashboard at `http://localhost:8081` and filter for:
- `Level == 'Error'`
- `@Properties.TenantId` to isolate tenant-specific database connection issues.
- `Category == 'MassTransit'` to detect queue blockages.

Verify health metrics by executing:
```bash
curl -k https://localhost:60462/health
```

---

### Runbook D: Outbox Relay Monitoring & Troubleshooting

The Transactional Outbox relies on:
1. `Karamchari.Core.Messaging.Outbox.OutboxRelayService`: A background worker polling the databases.
2. `dbo.OutboxRelayState`: Tracking execution pointers.

If messages are not propagating across modules (e.g., Payroll runs completed but notifications are not sent):

#### 1. Inspect Outbox Table
Run the following SQL query to verify the count of pending outbox events:
```sql
-- Replace 'tenant_dev' with active tenant schema name
SELECT COUNT(*) FROM [tenant_dev].[OutboxMessages] WHERE ProcessedAt IS NULL;
```

#### 2. Check Relay Service Status
Query Seq logs for the keyword `OutboxRelayService`. Verify if the polling loop is encountering database timeouts or SQL Server connection failures.

---

## Verdict: **FAIL (No formal runbooks exist)**

The operational documentation fails the audit due to the complete lack of dedicated runbooks for production deployments, rollbacks, and infrastructure disaster recovery. 

While we have reconstructed these runbooks from the codebase, a new engineer would struggle to handle a production incident under time pressure without these guidelines.

### Recommendation:
Publish these reconstructed guides to a new directory `/docs/operations/` and hook up deployment automation in Github Actions.
