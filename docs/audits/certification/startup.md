# Phase 2 — Application Startup Certification

**Result: ✅ PASS** — boots clean from a built artifact; several model-configuration warnings logged.

## Method
```bash
export ConnectionStrings__KaramchariDb='Server=localhost,1433;Database=Karamchari;User Id=sa;Password=Karamchari@123;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True'
export ConnectionStrings__Redis='localhost:6379'
export ConnectionStrings__RabbitMQ='amqp://guest:guest@localhost:5672'
dotnet run --project src/Backend/Karamchari.Api --no-build
```
Full log: `docs/certification/evidence/api-run.log`. Solution build log: `docs/certification/evidence/build.log` (Build succeeded, **0 warnings, 0 errors**).

## Evidence — Startup sequence (from log)
```
[INF] OutboxRelayService started. BatchSize=100 Interval=00:00:05
[DBG] Starting bus: rabbitmq://localhost/
[DBG] Connected: guest@localhost:5672/
[INF] Bus started: rabbitmq://localhost/
[INF] Now listening on: https://localhost:60462
[INF] Now listening on: http://localhost:60463
[INF] Application started.
[INF] Hosting environment: Development
```

| Check | Result | Evidence |
|---|---|---|
| Configuration loading | ✅ | Connection strings bound from env; Tenancy/OutboxRelay sections applied |
| DI container build | ✅ | App reached "Application started" with all modules registered |
| Middleware registration | ✅ | ExceptionHandler, RateLimiter, Authentication, TenantAuthorization, Authorization, TenantObservability all in pipeline (Program.cs) |
| Serilog startup | ✅ | Structured console output active from first line; file sink `logs/` + OTLP sink configured (`LoggingExtensions.cs`) |
| OpenTelemetry startup | ⚠️ | Configured (Serilog OTLP sink → `localhost:4317`); collector not running this session, exporter fails silently. See `opentelemetry.md` |
| DbContexts registration | ✅ | 16 contexts registered & migratable; 14 surfaced in health checks |
| MassTransit registration | ✅ | Bus connected to RabbitMQ; EF outbox registered for 15 DbContexts |
| Consumer registration | ⚠️ | API host intentionally declares **no receive endpoints** (consumers run in Worker). 0 queues declared. See `rabbitmq.md` |

## Findings
- **MEDIUM (data integrity):** ~30+ EF Core warnings `No store type was specified for the decimal property …` (e.g. `PayrollProfile.AnnualCTC`, `Invoice.CgstRate`, `Goal.TargetValue`). Without explicit precision/scale, SQL Server defaults to `decimal(18,2)` and **silently truncates** — unacceptable for payroll/billing money math. Set `HasPrecision`/`HasColumnType`.
- **LOW:** Several collection properties have value converters but **no value comparer** (`PayrollLedgerEntry.Earnings`, `ExpenseClaim.Receipts`, etc.) → EF change tracking may miss in-place edits.

## Verdict
Clean startup, all critical subsystems initialize. Warnings are real correctness risks (decimal precision) but do not block boot.
