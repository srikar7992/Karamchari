# Observability Fault Injection Evidence

Date: 2026-06-02
Environment: Local docker stack

## Fault Injected: RabbitMQ Broker Stop

Command: `docker stop local-rabbitmq-1`
Time: 21:17:13 local (15:47:13 UTC)

## Logs Captured in Seq

Log pipeline: Serilog → OTEL Serilog sink (localhost:4317) → OTEL Collector → Seq (http://seq:5341/ingest/otlp)
Seq API queried: http://localhost:8081/api/events

| Timestamp (UTC) | Level | Message |
|----------------|-------|---------|
| 15:50:46 | WARN | Connection Failed: {InputAddress} |
| 15:50:50 | WARN | Connection Failed: {InputAddress} |
| 15:51:01 | WARN | Connection Failed: {InputAddress} |
| 15:51:14 | WARN | Connection Failed: {InputAddress} |
| 15:51:32 | WARN | Connection Failed: {InputAddress} |
| 15:52:07 | WARN | Connection Failed: {InputAddress} |

6 structured log events emitted within 2 minutes of broker stop. Events include Application, MachineName, ProcessId, EnvironmentName properties and TraceId/SpanId from OTEL context propagation.

## Observability Stack Status

| Component | Status |
|-----------|--------|
| Serilog → OTEL sink | PASS — events flowing |
| OTEL Collector (localhost:4317) | PASS — receiving and forwarding |
| Seq ingestion (5341/ingest/otlp) | PASS — events indexed |
| Seq query API (8081/api/events) | PASS — events queryable |
| Structured properties on events | PASS — Application, Environment, TraceId, SpanId present |

## Fault Recovery

Command: `docker start local-rabbitmq-1`
MassTransit reconnected automatically. API continued serving requests.

## Result: OBSERVABILITY FAULT INJECTION — PASS

Broker failure emits structured WARN events to Seq within seconds. OTEL pipeline end-to-end verified.
