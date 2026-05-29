# Phase 12 — Logging Certification

**Result: ✅ PASS (enrichment configured & code-verified); ⚠️ not observable in running sinks this session.**

## Enrichment (code-verified — `LoggingExtensions.cs` + `TenantLogEnricher.cs`)
| Field | Source | Status |
|---|---|---|
| TenantId | `TenantLogEnricher` (`envelope.TenantId`, both `TenantTelemetryTags.TenantId` and `"TenantId"`) | ✅ configured |
| CorrelationId | `TenantLogEnricher` (`envelope.CorrelationId`) | ✅ configured |
| UserId | `TenantLogEnricher` (`envelope.UserIdentity`, when present) | ✅ configured |
| Environment | `.Enrich.WithProperty("Environment", env)` + `WithEnvironmentName()` | ✅ |
| MachineName | `.Enrich.WithProperty("MachineName", Environment.MachineName)` | ✅ |
| ApplicationVersion | `.Enrich.WithProperty("ApplicationVersion", asm version)` | ✅ |
| ProcessId / Application | `.Enrich.WithProcessId()` / `WithProperty("Application","Karamchari.Api")` | ✅ |

Sinks: **Console**, **File** (`logs/karamchari-YYYYMMDD.log`, daily rolling), **OpenTelemetry (OTLP)**.

## Logs generated for required categories (live, file sink)
- **CRUD/EF:** EF `DbCommand` statements logged with durations (e.g. `InboxState`, `OutboxState` queries). ✅
- **Messaging:** MassTransit bus connect/start/endpoint-ready logged at Debug. ✅
- **Errors:** Register/Login 500s logged with full exception + stack. ✅
- **Authentication:** login attempt path reached (`IdentityEndpoints.Login`) and error logged. ✅ (negative path)

## Findings
- **MEDIUM (observability):** Console **and** File output templates use `{Message:lj}` and **do not include `{Properties}`** — so TenantId/CorrelationId/UserId/MachineName/ApplicationVersion are **attached to events but invisible in text logs**. They are only emitted to structured sinks. Add `{Properties:j}` (or use a structured/JSON formatter) for the file/console sinks.
- **MEDIUM (observability):** The OTLP log sink targets `localhost:4317` (otel-collector). The README advertises **Seq** (8081, ingest 5341) for structured logs, but the app does **not** write to Seq's ingestion endpoint. With only Seq running, no logs reach it. Either point a sink at Seq (`:5341`) or run the collector with a Seq exporter.

## Verdict
Enrichment is comprehensive and correctly wired; logs are produced across all required categories. Live property visibility is blocked by sink/template config — a real but low-risk observability gap.
