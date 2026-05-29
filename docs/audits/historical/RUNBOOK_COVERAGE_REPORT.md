# Runbook Coverage Report

Verdict: FAILED.

## Evidence

- Runbook baseline created: `docs/runbooks/README.md`.
- Local dependencies are declared in `infrastructure/local/docker-compose.yml`.
- Health check registration exists in `src/Backend/Karamchari.Api/DependencyInjection/HealthCheckExtensions.cs`.
- Outbox operational metrics exist in `src/Backend/Karamchari.Core/Messaging/Outbox/OutboxRelayMetrics.cs`.

## Coverage Result

| Required Runbook Area | Status |
|---|---|
| API deploy/rollback | FAILED: deployment workflow has no real deploy or rollback. |
| Worker deploy/rollback | FAILED: no worker deployment workflow found. |
| SQL failure/restore | PARTIAL: local service exists; production restore is UNKNOWN. |
| Redis failure/restore | PARTIAL: local service exists; production restore is UNKNOWN. |
| RabbitMQ failure/restore | PARTIAL: local service exists; production restore/topology recovery UNKNOWN. |
| Outbox failure | PARTIAL: metrics and dead-letter tables exist; operational replay procedure UNKNOWN. |
| Consumer failure | PARTIAL: consumers exist; replay/escalation UNKNOWN. |
| Identity failure/JWT rotation | FAILED: no recovery or rotation procedure found. |
| Tenant provisioning/RLS failure | PARTIAL: code/tests/scripts exist; production recovery UNKNOWN. |
| Certificate/disk/memory/CPU/high error/high latency | FAILED: no production monitoring or command procedures found. |
| Incident response/DR | FAILED: no contacts, RTO/RPO, backup, restore, or escalation evidence found. |

## Required Next Evidence

Real runbooks must include production command paths, dashboard links, alert names, owners, escalation contacts, backup locations, restore commands, validation scripts, and postmortem storage location.
