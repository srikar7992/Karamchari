# Infrastructure Dependency Validation

This document outlines the local execution topology, connection details, resilience options, and configuration status of the three primary platform dependencies: **SQL Server**, **RabbitMQ**, and **Redis**.

---

## 1. SQL Server

### Connection Strings
- **Configuration Key**: `ConnectionStrings:KaramchariDb`
- **Development Default**: `Server=(localdb)\MSSQLLocalDB;Database=Karamchari_Local;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True`
- **Docker Compose (Local)**: `Server=sqlserver;Database=Karamchari;User Id=sa;Password=Karamchari@123;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True`

### Resilience & Retry Policies
- **MassTransit Outbox**: Retries are configured at the broker layer.
- **EF Core Database Transactions**: 
  > [!WARNING]
  > **No EF-level connection resilience retry policy is configured.**
  > `options.UseSqlServer(connectionString)` is registered without configuring transient error retries (such as `options => options.EnableRetryOnFailure()`). Under network jitter, EF database actions will fail immediately.
  > **Remediation**: Add `options => options.EnableRetryOnFailure()` in all bounded contexts' `AddDbContext` options builders.

### Migrations & Schema Creation
- **Mechanism**: The API host contains a startup migration routine triggered by the command-line argument `--provision-dev-tenants`.
- **System Schema (`dbo`)**: Serves as the template schema. Standard migrations create database tables under `dbo` (such as `dbo.Employees`).
- **Tenant Isolation (Multi-Schema)**: The `TenantProvisioningService` physically creates schemas (e.g., `tenant_dev`) and clones table structures from `dbo` using:
  ```sql
  SELECT * INTO [tenant_dev].[Employees] FROM [dbo].[Employees] WHERE 1=0
  ```
- **Indexes & Constraints**: Since `SELECT * INTO` does not duplicate indexes or constraints, each bounded context registers a post-provisioning task (`ITenantPostProvisioningTask`) to manually re-apply indexes to the cloned tenant tables.

---

## 2. RabbitMQ

### Connection Details
- **Configuration Key**: `ConnectionStrings:RabbitMQ`
- **Development/Docker Compose Default**: `amqp://guest:guest@rabbitmq:5672`
- **Management Console**: `http://localhost:15672` (Credentials: `guest` / `guest`)

### Message Exchange & Queue Creation
- **Mechanism**: **MassTransit** acts as the async message backbone. When `Karamchari.Worker` starts, MassTransit automatically declares and configures:
  1. **Exchanges**: Set up per message type (contracts).
  2. **Queues**: Set up per consumer class (e.g., `tenant-provisioned`, `billable-entry-consumer`, etc.).
  3. **Bindings**: Set up between type-exchanges and consumer-queues.

### Resilience & Retry Policies
- MassTransit consumers are configured with a standard retry schedule:
  ```csharp
  cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
  ```
  This retries a failed message 3 times with a 5-second interval between attempts before pushing the message to the `*_error` queue.

### Consumer Registration Summary
The background worker (`Karamchari.Worker`) groups consumers by business priority:
1. **High Priority**:
   - `TenantProvisionedConsumer` (HR context - handles initial tenant setup)
   - Payroll, Time & Attendance, Recruitment, and Compensation Consumers
2. **Medium Priority (Workflow)**:
   - Orchestration and Approval Consumers
3. **Low Priority / Heavy Processing**:
   - Billing, Forecasting, PSA, and Intelligence Analytics Consumers
4. **Notifications**:
   - Message delivery digest consumers

---

## 3. Redis

### Connection Details
- **Configuration Key**: `ConnectionStrings:Redis`
- **Development/Docker Compose Default**: `redis:6379` (local port mapped to `6379`)

### Distributed Cache Registration
- > [!CRITICAL]
  > **Redis Distributed Cache registration is completely missing in DI.**
  > Although the NuGet package `Microsoft.Extensions.Caching.StackExchangeRedis` is referenced, the application never calls `builder.Services.AddStackExchangeRedisCache(...)` or `builder.Services.AddRedis(...)` inside `Program.cs` or its DI extensions.
  > The Redis connection string is only referenced to construct the health check in `HealthCheckExtensions.cs`.
  > **Remediation**: Add the following configuration to `InfrastructureExtensions.cs` to ensure distributed caching is backed by Redis:
  > ```csharp
  > services.AddStackExchangeRedisCache(options =>
  > {
  >     options.Configuration = configuration.GetConnectionString("Redis");
  >     options.InstanceName = "Karamchari:";
  > });
  > ```
