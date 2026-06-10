# Release Readiness Checklist

Living document. Status as of 2026-06-10. Update on every readiness-affecting change.

## Code

| Item | Status | Evidence |
|---|---|---|
| Folder structure (src/Backend/{Hosts,Platform,Modules}, tests/, docs/, infrastructure/, tools/) | DONE | Repository layout |
| Namespaces match folder hierarchy | DONE | `Karamchari.<Module>.<Layer>` throughout; verified by sampling. Recruitment is the only layered (6-project) module — accepted inconsistency |
| Copyright headers on all C# sources | DONE | 1,577 files headed via `scripts/add-copyright-headers.ps1`; enforced in CI by `scripts/verify-copyright.ps1` (generated code excluded) |
| XML documentation enforced | WAIVED | CS1591 suppressed repo-wide by policy (see Directory.Build.props comment); doc files still generated |
| Warnings as errors, analyzers, format | DONE | Directory.Build.props + `dotnet format --verify-no-changes` in CI |

## Documentation

| Item | Status | Evidence |
|---|---|---|
| Root README | DONE | README.md |
| Per-module READMEs (purpose, events, tables, deps, tests) | DONE | `src/Backend/Modules/*/Karamchari.*/README.md` (24 modules, code-derived 2026-06-10) |
| Architecture docs / ADRs | DONE | docs/architecture/ (22 files) |
| Domain documentation | PARTIAL | docs/domains/ covers 15 of 24 modules |

## Testing

| Item | Status | Evidence |
|---|---|---|
| Unit + integration suites green | DONE | Full solution test run 2026-06-10, exit 0 (30 xunit projects) |
| Architecture tests (NetArchTest) | DONE | tests/Backend/Karamchari.ArchitectureTests, in CI (runs without Coverlet — instrumentation breaks NetArchTest) |
| Coverage gate | DONE (ratcheting) | CI floor 14% vs measured baseline 15.2% line coverage. Raise the floor as coverage grows; 95% is aspiration, not reality |
| Endpoint authorization audit | DONE | EndpointCatalogTests in Karamchari.SecurityTests: every endpoint must declare RequireAuthorization or AllowAnonymous; anonymous set locked |
| Performance/chaos/DR suites | EXISTS, NOT GATED | Karamchari.PerformanceTests (NBomber), ChaosTests, DisasterRecoveryTests compile and run on demand; not in the PR pipeline |

## Operations

| Item | Status | Evidence |
|---|---|---|
| Health endpoints | DONE | /health, /health/live, /health/ready, /health/startup |
| Unified health-check script | DONE | `./health-check.ps1` — PASS/WARN/FAIL across docker, SQL, Redis, RabbitMQ, observability stack, API |
| Endpoint registry | DONE | GET /api/v1/ops/endpoint-catalog (authorized) — live route/module/auth/request/response registry |
| Dashboards | DONE (local) | Grafana provisioning + Karamchari Platform Overview dashboard (infrastructure/local/grafana/) |
| Alerts | DONE (local) | Prometheus alert rules (infrastructure/local/alert-rules.yaml): 5xx rate, p95 latency, DLQ, projection lag, isolation violations, workflow SLA |
| Backup strategy | PARTIAL | backups/ exists; restore drill evidenced by DisasterRecoveryTests; no documented schedule |
| Seed data | PARTIAL | sample-data/ (employees, leave balances, payroll history, salary components/revisions + IMPORT_GUIDE.md) imported via DataMigration module; no per-module reference seeds for skills/courses/career paths |

## Security

| Item | Status | Evidence |
|---|---|---|
| Tenant isolation certification | DONE | Karamchari.TenantIsolationCertification, P0 gate in CI + nightly workflow |
| Secret scanning | DONE | trufflehog in CI; fail-fast JWT secret validation at startup |
| Dependency scanning | DONE | NuGetAudit (all/transitive, moderate) + `dotnet list package --vulnerable` in CI; suppressions reviewed and dated |
| Endpoint auth posture | DONE | Audit test (above); /api/analytics/projects/daily hole closed 2026-06-10 |
| SignalR hubs authenticated | DONE | RequireAuthorization on all hubs; bearer via access_token query handled in JWT OnMessageReceived (2026-06-10) |

## Production

| Item | Status | Evidence |
|---|---|---|
| IaC | EXISTS, NOT LOCALLY VALIDATED | infrastructure/bicep (main + 6 modules); compiled only via deploy pipeline — no bicep CLI on dev machine |
| Deployment automation | DONE | .github/workflows/deploy-api.yml (Docker build → Azure App Services) |
| Disaster recovery tested | PARTIAL | DisasterRecoveryTests exist; no documented production DR runbook drill |
| Load testing | EXISTS, NOT BASELINED | NBomber suite present; no recorded baseline numbers |

## Performance engineering (items 14-15)

| Item | Status | Evidence |
|---|---|---|
| Benchmark harness | DONE | tests/Backend/Karamchari.Benchmarks (BenchmarkDotNet, in solution); BurnoutScoreCalculator covered (single + 5k tenant sweep, MemoryDiagnoser); smoke-validated |
| Memory optimization (Span/ArrayPool/etc.) | DEFERRED BY POLICY | No profile shows a hotspot yet; policy and triggers in docs/development/performance-and-parallelization.md |
| Parallelization review | DONE | Full inventory of Parallel.ForEachAsync/Task.WhenAll usage with verdicts in docs/development/performance-and-parallelization.md; nightly tenant recompute already parallel (scope-per-tenant, DOP 4); evaluated-and-rejected candidates documented with re-evaluation triggers |

## Deliberately not done

- CS1591 un-suppression (would surface ~10k missing-doc errors; tracked policy exception).
- Speculative Span/CollectionsMarshal rewrites: blocked by policy until a benchmark or profile shows the call site is hot (see performance doc above).
- 95% coverage target: replaced by a ratcheting floor starting at the measured baseline.
