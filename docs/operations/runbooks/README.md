# Operational Runbooks

These runbooks are source-backed where possible. Environment-specific commands, cloud resource names, approval chains, and escalation contacts are `UNKNOWN` because the repository does not contain enough production deployment evidence.

## Common Detection Sources

- API health checks are registered in `src/Backend/Karamchari.Api/DependencyInjection/HealthCheckExtensions.cs`.
- Local infrastructure includes SQL Server, Redis, RabbitMQ, Seq, OpenTelemetry Collector, Prometheus, Grafana, Azurite, and Mailpit in `infrastructure/local/docker-compose.yml`.
- Outbox metrics exist in `src/Backend/Karamchari.Core/Messaging/Outbox/OutboxRelayMetrics.cs`.
- Deployment workflow is not production-capable: `.github/workflows/deploy-api.yml`.

## Runbook Matrix

| Scenario | Symptoms | Detection | Diagnosis | Recovery Procedure | Validation | Escalation | Postmortem Required |
|---|---|---|---|---|---|---|---|
| Deploy API | UNKNOWN in production; workflow only builds Docker locally. | GitHub Actions `deploy-api.yml`. | Check build step and commented deployment steps. | UNKNOWN until real deployment pipeline exists. | UNKNOWN; no smoke test exists. | UNKNOWN. | Yes. |
| Rollback API | UNKNOWN. | UNKNOWN. | Determine deployed artifact version. | UNKNOWN; no rollback automation exists. | UNKNOWN. | UNKNOWN. | Yes. |
| Deploy Worker | UNKNOWN; worker project exists. | `src/Backend/Karamchari.Worker`. | UNKNOWN. | UNKNOWN; no worker deployment workflow found. | UNKNOWN. | UNKNOWN. | Yes. |
| Rollback Worker | UNKNOWN. | UNKNOWN. | UNKNOWN. | UNKNOWN. | UNKNOWN. | UNKNOWN. | Yes. |
| SQL Failure | API/worker DB operations fail; health check may fail. | Health checks, logs, SQL container/service status. | Check connection string, SQL availability, migrations, RLS/session context. | Local: restart `sqlserver` service in docker compose and validate DB. Production: UNKNOWN. | Health endpoint green, EF queries succeed, migrations not pending. | DBA/on-call UNKNOWN. | Yes. |
| SQL Restore | Data missing/corrupt. | Backup/restore monitoring UNKNOWN. | Identify backup point and tenant impact. | UNKNOWN; no backup/restore automation found. | Validate row counts, tenant isolation, app health. | DBA/on-call UNKNOWN. | Yes. |
| Redis Failure | Cache operations fail; potential degraded performance. | Health checks/logs; local Redis service. | Check `ConnectionStrings__Redis` and Redis service. | Local: restart Redis container. Production: UNKNOWN. | Cache-dependent tests/flows pass. | UNKNOWN. | Yes. |
| Redis Restore | Cache persistence issue. | Redis append-only file local setting exists. | Check Redis data persistence. | Local: restart/recreate Redis volume if cache can be rebuilt. Production: UNKNOWN. | App functions without cross-tenant cache leakage. | UNKNOWN. | Yes. |
| RabbitMQ Failure | Consumers stop receiving events; outbox backlog grows. | RabbitMQ health, outbox queue depth metrics, consumer logs. | Check RabbitMQ availability and MassTransit connection. | Local: restart RabbitMQ container. Production: UNKNOWN. | Outbox backlog drains and consumers process test event. | UNKNOWN. | Yes. |
| RabbitMQ Restore | Broker state lost or corrupted. | RabbitMQ management UI/logs. | Check queues/exchanges and MassTransit topology. | UNKNOWN; topology recovery not documented. | Publish/consume known event. | UNKNOWN. | Yes. |
| Outbox Failure | `OutboxState` backlog, dead-letter rows, retry backlog, stale locks. | `OutboxRelayMetrics`; SQL tables `dbo.OutboxState`, `dbo.OutboxDeadLetter`, `dbo.OutboxProcessingState`. | Inspect `OutboxRelayService` logs and dead-letter reasons. | Restart relay/worker to release stale locks; poison messages require manual analysis. Exact production command UNKNOWN. | Queue depth decreases, oldest age falls, no new dead letters. | UNKNOWN. | Yes. |
| Consumer Failure | Event side effects absent; retry/dead-letter growth. | MassTransit logs, outbox/dead-letter, module processed event logs. | Identify failing consumer and event contract. | Fix data/contract issue, redeploy, replay if supported. Replay procedure UNKNOWN. | Consumer side-effect table updates. | UNKNOWN. | Yes. |
| Identity Failure | Login/refresh/logout fail. | Identity endpoints, logs, token DB tables. | Check JWT options, signing keys, refresh/revoked token stores. | UNKNOWN; no identity recovery automation. | Register/login/refresh/logout tests pass. | Security/on-call UNKNOWN. | Yes. |
| JWT Rotation | Tokens fail validation or signing key expires. | Signing key metadata table and auth errors. | Check active signing key records. | UNKNOWN; no rotation command/runbook found. | New token issued and old token behavior matches policy. | Security owner UNKNOWN. | Yes. |
| Tenant Provisioning Failure | Tenant creation or module seed fails. | `POST /api/tenants`, tenant provisioning service logs, tenant provisioning consumers. | Check tenant schema/RLS/migrations and consumer failures. | UNKNOWN; provisioning idempotency docs exist but production procedure not certified. | New tenant can authenticate and access isolated data. | UNKNOWN. | Yes. |
| RLS Failure | Cross-tenant leakage risk or blocked data access. | Tenant isolation certification tests, RLS SQL scripts, logs. | Verify session context and predicate/policy deployment. | Stop affected traffic if leakage suspected; repair RLS policy from scripts. Production command UNKNOWN. | Tenant isolation tests pass against affected DB. | Security/DBA UNKNOWN. | Yes. |
| Certificate Expiry | TLS/API calls fail. | Certificate monitoring UNKNOWN. | Inspect hosting/cert provider. | UNKNOWN. | HTTPS and dependent integrations succeed. | UNKNOWN. | Yes. |
| Disk Full | Writes/logs/DB fail. | Host/container metrics UNKNOWN. | Identify volume: SQL, Redis, RabbitMQ, logs, artifacts. | Free/expand disk. Production procedure UNKNOWN. | Write paths recover and no data corruption. | Infra on-call UNKNOWN. | Yes. |
| Memory Pressure | OOM/restarts/high latency. | Host/container metrics UNKNOWN. | Identify API/worker/SQL/Redis/RabbitMQ process. | Scale/restart/tune. Production procedure UNKNOWN. | Error rate/latency recover. | UNKNOWN. | Yes. |
| High CPU | Latency, timeouts. | Metrics UNKNOWN; local Prometheus can scrape collector. | Identify hot service/query/consumer. | Scale or disable offending workload. Exact procedure UNKNOWN. | CPU and latency normalize. | UNKNOWN. | Yes. |
| High Error Rate | 5xx/API failures/consumer failures. | Logs, health checks, metrics if configured. | Segment by endpoint/consumer/tenant. | Roll forward/back if deployment-related; rollback automation UNKNOWN. | Error rate below SLO; SLO UNKNOWN. | UNKNOWN. | Yes. |
| High Latency | Slow endpoints/queues. | Metrics UNKNOWN. | Check SQL, Redis, RabbitMQ, outbox backlog. | Scale/tune/restart based on diagnosis. Production procedure UNKNOWN. | Latency below SLO; SLO UNKNOWN. | UNKNOWN. | Yes. |
| Incident Response | Any production-impacting event. | Alerts UNKNOWN. | Establish severity, tenant impact, data impact. | Mitigate, communicate, preserve evidence. | Customer impact resolved. | Escalation chain UNKNOWN. | Yes. |
| Disaster Recovery | Region/resource/data loss. | UNKNOWN. | Determine RPO/RTO; both UNKNOWN. | UNKNOWN; no DR automation or restore certification found. | Full platform restored and tenant isolation verified. | Executive/on-call UNKNOWN. | Yes. |

## Postmortem Template

- Incident ID:
- Date/time UTC:
- Detected by:
- Affected tenants:
- Affected modules:
- Customer impact:
- Timeline:
- Root cause:
- Contributing factors:
- Recovery actions:
- Validation evidence:
- Data integrity evidence:
- Tenant isolation evidence:
- Preventive actions:
- Owners:
- Due dates:

## Runbook Verdict

These are not complete production runbooks. They are a source-backed gap-preserving baseline. The missing production commands, contacts, alert links, dashboards, backup locations, artifact identifiers, and approval authorities are survivability blockers.
