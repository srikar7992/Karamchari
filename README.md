# Karamchari

A multi-tenant, domain-agnostic Employee Management System (EMS). Designed to scale to thousands of tenants using a modular monolith backend architecture.

---

## 1. Architecture Overview

| Concern | Pattern / Technology |
| :--- | :--- |
| **Backend Topology** | **Modular Monolith** (Clean assembly boundaries per bounded context) |
| **API Style** | REST Backend-For-Frontend (BFF Minimal APIs) |
| **Multi-Tenancy** | **Shared Database, Isolated Schema** per tenant |
| **Tenant Isolation** | Schema-rewriting command interceptor + SQL Row-Level Security failsafe |
| **Async Backbone** | **MassTransit** + RabbitMQ + Transactional Outbox |
| **Persistence** | EF Core 10 against SQL Server |

---

## 2. Fast Quick Start

To provision and start the local developer stack:

1.  **Clone the Repository**
2.  **Verify Prerequisites**: Docker Desktop and .NET 10 SDK must be running.
3.  **Run the One-Command Setup**:
    *   **Unix / MacOS**:
        ```bash
        ./setup-local.sh
        ```
    *   **Windows (PowerShell)**:
        ```powershell
        ./setup-local.ps1
        ```
4.  **Verify Service Health**:
    *   **Unix / MacOS**:
        ```bash
        ./verify-local.sh
        ```
    *   **Windows (PowerShell)**:
        ```powershell
        ./verify-local.ps1
        ```
    *   **Unified PASS/WARN/FAIL probe** (docker, SQL, Redis, RabbitMQ, observability stack, API):
        ```powershell
        ./health-check.ps1
        ```

Once running, the API Gateway is available at `https://localhost:60462/scalar` (redirects automatically from `/`).

---

## 3. Repository Documentation Map

All project documentation is organized under the [docs/](docs) folder:

-   [onboarding/](docs/onboarding): Platform engineering program and tenant isolation training labs.
-   [architecture/](docs/architecture): Canonical design documentation:
    -   [adrs/](docs/architecture/adrs): Architectural Decision Records (0001 to 0014).
    -   [decisions/](docs/architecture/decisions): Execution flow definitions (Tenant Execution, Replay/Retry).
    -   [database-registry.md](docs/architecture/database-registry.md): Metadata owner registry for all 18 database contexts.
-   [domains/](docs/domains): Explanations of business rules and objectives for each domain module.
-   [release-readiness.md](docs/release-readiness.md): Living release readiness checklist with per-item status and evidence.
-   Per-module READMEs live beside each module project under [src/Backend/Modules/](src/Backend/Modules) (purpose, events published/consumed, tables, dependencies, tests).
-   [operations/](docs/operations): Operational runbooks:
    -   [runbooks/](docs/operations/runbooks): Step-by-step procedures.
    -   [monitoring/](docs/operations/monitoring): Observability guides (OTel, logging, metrics, event topologies).
-   [development/](docs/development): Onboarding guidelines:
    -   [local-setup/](docs/development/local-setup): Unified local installation setup guides.
    -   [standards/](docs/development/standards): Coding principles (commits, branching, warnings-as-errors).
-   [governance/](docs/governance): Code hygiene and repository cleanup checklists.
-   [audits/](docs/audits): Snapshots of system reports:
    -   [hostile/](docs/audits/hostile): Hostile security audits.
    -   [certification/](docs/audits/certification): Day-1 feature certifications.
    -   [historical/](docs/audits/historical): Archived previous validation summaries.

---

## 4. Development & Contribution Workflow

For local development workflows:
-   **Local Setup**: Read the [Setup Guide](docs/development/local-setup/README.md) for details on port layout and troubleshooting.
-   **Coding Standards**: Code must compile with zero warning outputs (`TreatWarningsAsErrors` active).
-   **Tests Execution**: Always execute `./run-all-tests.sh` (or `run-all-tests.ps1`) before submitting a pull request to verify that all suites pass.

---

## 5. Deployment & Pipelines

Continuous integration is handled via GitHub Actions:
*   [ci.yml](.github/workflows/ci.yml): Restores with locked dependencies, verifies formatting and copyright headers, builds with warnings-as-errors, runs tests with a ratcheting line-coverage floor, architecture tests, the tenant isolation gate, dependency audit, and secret scanning.
*   [deploy-api.yml](.github/workflows/deploy-api.yml): Builds Docker containers and pushes builds to Azure App Services.
*   [tenant-isolation-certification.yml](.github/workflows/tenant-isolation-certification.yml): Nightly stress/chaos runner validating multi-tenant isolation.
