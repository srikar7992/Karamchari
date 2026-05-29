# Karamchari Knowledge Gap Report

Scope: repository evidence available on 2026-05-30. This report does not use Slack, meetings, personal notes, or inferred intent.

## Critical Gaps

| Area | Gap | Evidence | Survivability Impact |
|---|---|---|---|
| Deployment | `deploy-api.yml` validates Docker build and prints success messages, but Azure login, image push, Bicep deployment, and App Service deployment are commented out. There is no rollback, canary, blue-green, migration, smoke-test, or recovery implementation. | `.github/workflows/deploy-api.yml:16`, `.github/workflows/deploy-api.yml:26`, `.github/workflows/deploy-api.yml:37`, `.github/workflows/deploy-api.yml:50`, `.github/workflows/deploy-api.yml:58`, `.github/workflows/deploy-api.yml:66`, `.github/workflows/deploy-api.yml:76` | Platform remains Founder Dependent for production deployment and rollback. |
| Owners | CODEOWNERS maps all files and a small subset of modules to one GitHub handle. No primary, secondary, tertiary ownership exists per module. | `.github/CODEOWNERS:1`, `.github/CODEOWNERS:3` | Bus factor is 1. |
| Observability operations | Local Prometheus and Grafana services exist, but no committed Grafana dashboard JSON, alert rules, Alertmanager routes, SLO documents, or escalation policies were found in the searched paths. | `infrastructure/local/docker-compose.yml:75`, `infrastructure/local/docker-compose.yml:83`, `infrastructure/local/prometheus.yaml:5` | Telemetry can be collected locally, but production operations are not certifiable. |
| Mutation testing | No Stryker configuration was found. | `find . -maxdepth 4 -iname '*stryker*'` returned no files. | Required mutation certification is absent. |
| Independent validation | No repository evidence shows an independent unfamiliar team executed build, run, debug, deploy, rollback, feature, migration, or recovery tasks. | No matching committed report in `docs/independence` satisfies the requested measured task set. | Independence cannot be certified. |
| Business rationale | ADRs exist for several architecture topics, but many requested ADR topics do not show source-backed alternatives and rejection rationale in the requested `docs/architecture/adrs/` path. | `docs/adr/0001-multi-tenancy-model.md` through `docs/adr/0014-search-infrastructure.md`; no prior `docs/architecture/adrs/` directory before this pass. | Architecture is discoverable, but rationale remains incomplete. |
| Runbooks | Some governance/runbook documents exist, but the requested operational runbooks for SQL, Redis, RabbitMQ, outbox, JWT rotation, tenant provisioning, RLS, resource exhaustion, incident response, and DR are not present as executable, environment-specific recovery procedures. | `docs/governance/runbooks/replay_recovery.md`; no matched complete set under `docs/runbooks/` before this pass. | Recovery depends on operator judgment. |

## Domain Gaps

The code exposes many entities, events, DbSets, consumers, and endpoints, but several requested business-level fields are not fully knowable from source alone.

| Required Field | Status |
|---|---|
| Purpose | Partially derivable from module names, DbSets, endpoints, and consumers. |
| Business Objectives | Mostly UNKNOWN unless explicit names make objectives testable. |
| State Machines | Partially derivable from enums and transition methods. |
| Ownership Rules | Mostly UNKNOWN except tenant ownership and authorization middleware. |
| External Integrations | Partially derivable: SQL Server, Redis, RabbitMQ/MassTransit, OpenTelemetry, Azure Bicep resources, Mailpit/Azurite local services. Production third-party provider contracts are mostly UNKNOWN. |
| Failure Scenarios | Partially derivable from tests, health checks, outbox dead-letter logic, and chaos tests. |
| Known Limitations | Partially derivable from missing tests, fake deployment, missing alerts, and UNKNOWN business rationale. |

## Verdict Impact

These gaps force the final verdict to `Founder Dependent` unless and until deployment, operations, ownership, mutation testing, observability alerts, and independent validation are proven by committed evidence.
