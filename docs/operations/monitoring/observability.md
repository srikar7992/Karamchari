# Karamchari Observability Framework Specification

This document defines the distributed tracing, structured logging, application metrics, and request correlation tracking architecture for the Karamchari platform.

---

## 1. Trace Flow & Correlation Tracking

Correlation tracking links requests across Web API and background workers, database commands, and asynchronous event dispatches.

```mermaid
sequenceDiagram
    autonumber
    actor User as Client Application
    running API as API Gateway / BFF
    participant DB as Tenant DB Schema
    participant Bus as MassTransit / RabbitMQ
    participant Worker as Background Worker

    User->>running API: HTTP Request (X-Correlation-Id if present)
    Note over running API: Correlation ID Middleware:<br/>Extracts or generates CorrelationId<br/>Binds to TenantExecutionContext
    running API->>DB: SQL Command
    Note over DB: EF Core Interceptor:<br/>Stamps CorrelationId and TenantId
    running API->>Bus: Publish/Send Integration Event
    Note over Bus: MassTransit Publish Filter:<br/>Injects CorrelationId into envelope
    Bus->>Worker: Dispatch Event
    Note over Worker: MassTransit Consume Filter:<br/>Extracts CorrelationId from envelope<br/>Binds to TenantExecutionContext
    Worker->>DB: Save Changes / Update State
    running API-->>User: HTTP Response (X-Correlation-Id header returned)
```

---

## 2. Distributed Tracing (OpenTelemetry)

OpenTelemetry is registered globally in [InfrastructureExtensions.cs](src/Backend/Karamchari.Api/DependencyInjection/InfrastructureExtensions.cs) to capture request traces across boundaries:

-   **Incoming HTTP Requests**: Captures ASP.NET Core request spans, excluding health check endpoints, and enriches traces with the active `tenant.id` claim.
-   **Outbound HTTP Clients**: Traces external HTTP requests.
-   **Database Queries**: Captures EF Core SQL query text, parameter names, and command execution durations.
-   **Asynchronous Messaging**: Tracks MassTransit events publishing, sending, and consumption spans.
-   **Custom Trace Sources**: Registers custom activity sources (`"Karamchari.*"`) to record internal domain processes.

### Telemetry Collector Configuration
Traces are exported via OTLP (OpenTelemetry Protocol) to the local collector container on port `4317` (`OTEL_EXPORTER_OTLP_ENDPOINT`).
- **Exporter Note**: The collector sends traces and logs to **Seq** using OTLP over HTTP (`otlphttp/seq` pointing to `http://seq:5341/ingest/otlp`). OTLP gRPC to Seq is not supported.

---

## 3. Structured Logging (Serilog)

Logging is handled by Serilog, structured using JSON property formatting, and enriched with:
*   `TenantId`: Extracted from active `TenantExecutionContext`.
*   `CorrelationId`: Contextual correlation identifier linking requests.
*   `UserId`: Active authenticated user claim.
*   `Environment`: Deployment environment (e.g. `Development`, `Production`).
*   `MachineName` & `ApplicationVersion`: Host process variables.

### Logging Sinks
1.  **Console**: Local terminal output optimized for readability.
2.  **Rolling Files**: Daily log outputs written to files under `logs/karamchari-.log`.
3.  **OTLP Sink**: Log events are sent structured directly to the OTel Collector.

---

## 4. Metrics & Dashboards

Metrics are collected using OTel metrics and scraped by Prometheus:
-   **Process Metrics**: CPU, thread pool usage, memory, and Garbage Collection (GC) metrics.
-   **ASP.NET Core Metrics**: Active connection rates, lease counts, and request HTTP status codes.
-   **MassTransit Metrics**: Messaging processing times, throughput, and fail counts.

### Developer Dashboards Port Map
Once the runtime is running, developer dashboards are available at:
-   **Seq (Logs & Traces)**: [http://localhost:8081](http://localhost:8081)
-   **Prometheus (Scraped metrics)**: [http://localhost:9090](http://localhost:9090)
-   **Grafana (Metrics visualization)**: [http://localhost:3000](http://localhost:3000)
-   **RabbitMQ Dashboard**: [http://localhost:15672](http://localhost:15672) (guest / guest)
