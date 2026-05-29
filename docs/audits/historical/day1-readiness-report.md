# Day-1 Readiness Report

This report evaluates the modular monolith platform across architectural guidelines, infrastructure setup, security standards, and developer onboarding capabilities.

---

## 1. Technical Health Scorecard

```mermaid
radar-chart
    title Platform Maturity Model
    "Infrastructure" : 75
    "Architecture" : 65
    "Security" : 90
    "Multi-tenancy" : 95
    "Developer Experience" : 85
    "Testability" : 95
    "Production Readiness" : 75
```

- **Infrastructure: 75/100**
  - *Strengths*: Main services run in Docker Compose; OTEL Collector and Seq are pre-configured.
  - *Weaknesses*: No database connection retry policy. Redis is listed in health checks but not registered as a distributed cache in DI.
- **Architecture: 65/100**
  - *Strengths*: Highly structured module composition. Shared kernel rules clearly separated.
  - *Weaknesses*: Direct coupling between Forecasting and Billing. Direct dependency between Payroll and Notifications. Domain purity leaks in FinancialOps. Gaps in Outbox registrations (missing for Billing, Forecasting, and Workflow).
- **Security: 90/100**
  - *Strengths*: Extremely strong token-based authentication and permission authorization scheme. Integrated Row-Level Security (RLS).
  - *Weaknesses*: Minor gaps in database health checking could lead to silent failures.
- **Multi-tenancy: 95/100**
  - *Strengths*: Highly mature multi-schema clone pattern, schema rewrites, and stamping interceptor.
  - *Weaknesses*: Cloning tables using `SELECT * INTO` strips indexes, requiring manual post-provisioning tasks.
- **Developer Experience: 85/100**
  - *Strengths*: Native Scalar API explorer integration. Added automated stack validator scripts and Bruno API tests.
  - *Weaknesses*: REST API lacks standard Create, Update, and Delete endpoints for Employee administration (logic is only in the service layers).
- **Testability: 95/100**
  - *Strengths*: Comprehensive test coverage including 600+ isolation certification tests.
- **Production Readiness: 75/100**
  - *Strengths*: Standard outbox pattern, rate limiters, SignalR support, and centralized exceptions.
  - *Weaknesses*: Outbox is missing for Billing, Forecasting, and Workflow. Databases for Workflow and FinancialOps are not monitored by health checks.

---

## 2. Issues & Remediation Backlog

### Critical Priority Issues

#### 1. Forecasting Bypasses Bounded Context Rules
- **Impact**: `Karamchari.Forecasting` references `Karamchari.Billing` directly. `ForecastingEngine` queries `BillingDbContext` directly. This breaks tenant database boundaries and limits microservice splits.
- **Remediation**:
  1. Remove the project reference to `Karamchari.Billing` in `Karamchari.Forecasting.csproj`.
  2. Implement an asynchronous request/response consumer in Billing that answers a `GetBillingForecasts` command, or populate a localized cache in Forecasting by subscribing to Billing integration events.

#### 2. Missing Redis Cache DI Registration
- **Impact**: The distributed cache is never registered in ASP.NET Core DI. The Redis container is only verified in health checks.
- **Remediation**:
  Add Redis distributed cache registration inside `InfrastructureExtensions.cs`:
  ```csharp
  services.AddStackExchangeRedisCache(options =>
  {
      options.Configuration = configuration.GetConnectionString("Redis");
      options.InstanceName = "Karamchari:";
  });
  ```

---

### High Priority Issues

#### 1. Missing Transactional Outbox for Bounded Contexts
- **Impact**: `BillingDbContext`, `ForecastingDbContext`, and `WorkflowDbContext` do not have an Entity Framework outbox registered in MassTransit. Events are published immediately during `SaveChanges`, risking inconsistencies if database updates fail.
- **Remediation**:
  Add outbox registrations for these three contexts in `MassTransitExtensions.cs` and `WorkerServiceCollectionExtensions.cs`:
  ```csharp
  x.AddEntityFrameworkOutbox<BillingDbContext>(o => { o.UseSqlServer(); o.UseBusOutbox(); });
  x.AddEntityFrameworkOutbox<ForecastingDbContext>(o => { o.UseSqlServer(); o.UseBusOutbox(); });
  x.AddEntityFrameworkOutbox<WorkflowDbContext>(o => { o.UseSqlServer(); o.UseBusOutbox(); });
  ```

#### 2. Missing Database Health Monitoring
- **Impact**: `WorkflowDbContext` and `FinancialOpsDbContext` databases are not monitored by the `/health` endpoint.
- **Remediation**:
  Add checks in `HealthCheckExtensions.cs`:
  ```csharp
  healthBuilder.AddDbContextCheck<WorkflowDbContext>("Database:Workflow", tags: DbTags);
  healthBuilder.AddDbContextCheck<FinancialOpsDbContext>("Database:FinancialOps", tags: DbTags);
  ```

#### 3. Domain Purity Violation in FinancialOps
- **Impact**: `FinancialConsistencyGuard.cs` and `FinancialChaosEngine.cs` in `Karamchari.FinancialOps/Domain` import `Microsoft.EntityFrameworkCore` and use `FinancialOpsDbContext` directly, leaking persistence details into the domain model.
- **Remediation**:
  1. Define a clean repository interface (`IFinancialLedgerRepository`) in the domain layer.
  2. Implement the interface inside the persistence layer.
  3. Alternatively, move the consistency guard classes to the application/service layer.

---

### Medium Priority Issues

#### 1. Lack of SQL Server Connection Retry Policies
- **Impact**: Temporary network glitches between the API/Worker and SQL Server will fail database transactions immediately instead of retrying.
- **Remediation**:
  Configure transient failure retries in EF Core setups:
  ```csharp
  options.UseSqlServer(connectionString, sqlOpts => sqlOpts.EnableRetryOnFailure());
  ```

#### 2. Payroll Directly Coupled to Notifications
- **Impact**: `Karamchari.Payroll` references `Karamchari.Notifications` directly instead of publishing async message commands.
- **Remediation**:
  Refactor Payroll to publish a notification command message or event via MassTransit.

---

### Low Priority Issues

#### 1. SELECT * INTO strips Indexes during Provisioning
- **Impact**: `TenantProvisioningService` clones table structures using SQL Server `SELECT * INTO`, which drops constraints, foreign keys, and indexes. Bounded contexts must manually define and re-apply indexes in post-provisioning tasks.
- **Remediation**:
  In a future platform iteration, replace the table cloning logic with a migration runner that executes DDL schema generation scripts directly against the target schema.
