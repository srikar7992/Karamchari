# Phase 9: Production Operations Audit & Observability

This audit reviews the observability configurations, dashboard definitions, alerts, and operational metrics needed to maintain the platform during production incidents.

---

## Telemetry Infrastructure Overview

The local stack includes a modern, structured telemetry collection setup:
1. **OpenTelemetry Protocol (OTLP)**: Custom trace and metric collectors are configured in the API and Worker configurations via builder extensions.
2. **Seq Logs**: Injects structured logging and application traces on port `8081` (via `otel-collector-config.yaml` OTLPHTTP sink).
3. **Prometheus**: Pulls system and custom metrics from the OTel Collector at port `8889` (via `prometheus.yaml`).
4. **Grafana**: Starts as a container on port `3000` to visualize Prometheus metrics.

---

## Observability Dashboards Status

We evaluated the existence of preconfigured dashboards and alerts in the repository for critical subsystems:

| Subsystem Component | Dashboard Configured | Alerts Defined | Metrics Source |
| :--- | :--- | :--- | :--- |
| **API BFF** | ❌ **Missing** | ❌ **Missing** | OpenTelemetry ASP.NET Core metrics |
| **Worker Host** | ❌ **Missing** | ❌ **Missing** | OpenTelemetry Host process metrics |
| **RabbitMQ Broker** | ❌ **Missing** | ❌ **Missing** | RabbitMQ management API metrics |
| **Redis Cache** | ❌ **Missing** | ❌ **Missing** | Redis exporter metrics |
| **SQL Server** | ❌ **Missing** | ❌ **Missing** | SQL Server DMV metrics |
| **Tenants Isolation** | ❌ **Missing** | ❌ **Missing** | Custom telemetry metrics |
| **Identity Service** | ❌ **Missing** | ❌ **Missing** | ASP.NET Identity telemetry metrics |
| **Outbox Relay** | ❌ **Missing** | ❌ **Missing** | Custom relay logs |

### Findings:
- **No dashboard configurations**: While Grafana and Prometheus run in Docker Compose, no Grafana provisioning files (JSON dashboards or datasource mappings) are checked into the repository.
- **No alert definitions**: The repository does not contain Alertmanager configurations, Prometheus rules files, or Seq query alerts to identify anomalies automatically.
- **No SLOs/SLIs**: There are no documented Service Level Objectives (SLOs) or Service Level Indicators (SLIs) to measure platform reliability.

---

## Simulated Production Metrics & Escalation

Because alert definitions and procedures are missing, a new engineer must manually look up metrics or write queries during an incident. Below are the key queries/patterns required:

### 1. API Availability & Latency SLI
To track the HTTP error rates and response latency, query Prometheus:
```promql
# API Error Rate (HTTP 5xx responses)
sum(rate(http_server_duration_seconds_count{http_status_code=~"5.."}[5m]))

# 95th Percentile Latency
histogram_quantile(0.95, sum(rate(http_server_duration_seconds_bucket[5m])) by (le))
```

### 2. Outbox Relay Delay SLI
If the Outbox relay stops processing events, background queues will drift.
Seq Query:
```seq
MessageId is not null and ProcessedAt is null and Category = 'OutboxRelay'
```

### 3. Escalation Workflow (Reconstructed Proposal)
Since no escalation rules exist, we recommend the following baseline policy for incident management:
- **Tier 1 (Automated Page)**: Triggered if API 5xx Error Rate > 1% over a 5-minute window or database connection latency > 200ms.
- **Tier 2 (Platform Lead)**: Escalated if Tier 1 page remains unacknowledged for 15 minutes.
- **Tier 3 (Executive Strategy Audit)**: Escalated if outage exceeds 1 hour.

---

## Verdict: **FAIL (No production dashboards or alerts exist)**

The production operations audit fails. While the telemetry plumbing is implemented, a new developer would be blind during an outage since no Grafana dashboards, Prometheus rules, or escalation policies are preconfigured.

### Recommendation:
Configure Grafana dashboard provisioning (JSON files in `infrastructure/local/grafana/dashboards/`) and define baseline Prometheus alert rules.
