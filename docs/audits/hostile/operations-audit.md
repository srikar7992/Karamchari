# Operational Readiness Audit

## Existing Evidence

- `docs/runbooks/README.md` exists as a source-backed runbook matrix.
- `docs/governance/runbooks/replay_recovery.md` exists.
- Local health checks are implemented and detect SQL/Redis/RabbitMQ failures.
- Local observability stack includes Seq, OTel Collector, Prometheus, and Grafana.

## Missing or Incomplete

| Required Item | Status |
|---|---|
| Production deploy runbook | Missing/insufficient. |
| Rollback guide | Missing. |
| SQL restore guide | Missing. |
| Redis/RabbitMQ restore guide | Missing. |
| On-call procedures | Missing. |
| Escalation chains | Missing. |
| Disaster recovery guide | Missing. |
| Alertmanager routes | Missing. |
| Grafana dashboards as code | Missing. |
| SLO/error budget operational documents | Missing. |

## Verdict

Operational readiness is not certified. Local detection exists, but production recovery is not executable from repository evidence.
