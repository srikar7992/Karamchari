# Phase 11 — Developer Experience Audit

Target: an unfamiliar developer reaches first build → first login → first API call → first workflow in **< 30 minutes**, unaided.

## Measured (this audit host; macOS arm64, images already pulled / packages restored)
| Milestone | Mechanism | Time |
|---|---|---|
| Infra up (`compose up -d`) | setup-local.sh step 3–4 | ~30–60 s to SQL-ready |
| Restore + build | step 5 (incremental) | seconds (warm) / minutes (cold) |
| Provision DB/RLS/tenants | step 6 | ~60–90 s |
| API to `/health=200` | step 9 | ~10–20 s |
| **Total via `./setup-local.sh` (warm)** | one command | **well under 5 min** |
| First **register** + **login** (token) | curl | seconds |
| First authenticated **API call** (`GET /api/v1/hr/employees` → 200) | curl | seconds |
| First **workflow** (register→login→tenant context→employee read) | — | within the same session |

A first-ever cold run (Docker image pulls for SQL/Redis/RabbitMQ/Seq/etc. + cold NuGet restore) adds several minutes of downloads but remains comfortably **< 30 min** on a normal connection.

## Friction points found (the honest list)
| # | Severity | Friction |
|---|---|---|
| D1 | **HIGH (as-found)** | Following README/README-LOCAL **verbatim fails** on macOS/Linux (LocalDB crash). Without this audit's fix, time-to-first-call is effectively **∞** for a non-Windows dev. Fixed. |
| D2 | MEDIUM | **Three competing setup narratives** (README `setup.ps1`, README-LOCAL manual, `docs/engineering/local_setup.md`) + two compose files. A newcomer must guess the canonical path. The new `setup-local.sh` is now the single front door. |
| D3 | MEDIUM | **PowerShell-only official scripts** (`setup.ps1`, `scripts/**`) don't run on macOS/Linux without `pwsh`. Unix devs had no one-command path until `setup-local.sh`. |
| D4 | LOW | Port confusion (60462 vs 8080 vs 5000) depending on launch profile/env. |
| D5 | LOW | HTTPS dev-cert trust + emulated SQL on Apple Silicon are undocumented gotchas. |

## Verdict
Developer Experience = **PASS (as-remediated), FAIL (as-found for non-Windows)**. With `setup-local.sh` and the connection-string fix, an unfamiliar developer reaches a working authenticated API call in **far under 30 minutes via a single command**. As originally documented, a non-Windows developer could not get past provisioning at all.
