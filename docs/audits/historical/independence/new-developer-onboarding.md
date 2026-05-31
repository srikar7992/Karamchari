# Phase 1: New Developer Time-To-Productivity Audit

This audit evaluates the developer onboarding experience for an engineer unfamiliar with Karamchari, relying solely on `README.md` and `README-LOCAL.md`.

## Time-To-Productivity Metrics

Below are the estimated times to complete key developer tasks starting from a completely clean development machine:

| Activity | Estimated Time | Notes & Constraints |
| :--- | :--- | :--- |
| **Clone** | **< 1 minute** | Repository size is minimal (mostly text/code, few binary assets). |
| **Build** | **2 - 3 minutes** | Depends on internet speeds for first-time NuGet packages restore (CPM enabled via `Directory.Packages.props`). Subsequent builds take <10 seconds. |
| **Run** | **5 - 20 minutes** | **Dependency Pull**: First-time downloading of 9 Docker images (SQL Server 2022, Redis 7, RabbitMQ, OTel Collector, Seq, Prometheus, Grafana, Mailpit, Azurite) is highly bandwidth-dependent (~3GB total download). Once images are local, setup takes ~1-2 minutes. |
| **Login** | **2 - 3 minutes** | Requires utilizing the Bruno collection under `tools/api-tests/` or constructing raw HTTP requests. JWT-based authentication is handled cleanly once the API is running. |
| **Create Employee** | **2 - 3 minutes** | Accessible via the Scalar UI at `https://localhost:60462/scalar` or using the Bruno collection. |
| **Debug Endpoint** | **5 minutes** | Endpoints are cleanly registered in `Karamchari.Api/BFF/` (e.g., [EmployeeEndpoints.cs](src/Backend/Karamchari.Api/BFF/Employee/EmployeeEndpoints.cs)). Easy to set breakpoints and attach. |
| **Fix Bug** | **10 - 15 minutes** | Code is modular and typed, with nullable reference types enabled. Compiler/analyzers and NetArchTest enforce rules immediately, reducing feedback loops. |
| **Create Feature** | **1 - 2 hours** | Monolith boundaries require creating/modifying classes in the Contracts class library first, adding database entities, running migrations, registering handlers, and exposing REST endpoints in BFF. |

---

## Onboarding Friction Points & Gaps

While the startup scripts are extremely robust (`setup-local.sh` covers prerequisites, compilation, migrations, and seeding), several undocumented assumptions and points of confusion exist:

### 1. Port Conflicts (Silent Failures)
The local stack binds to 9 host ports (1433, 6379, 5672, 15672, 8081, 3000, 9090, 8025, 60462). If an engineer already runs local SQL Server, Redis, or PostgreSQL/Grafana instances, `docker compose` or the API runner will fail. `setup-local.sh` checks ports but does not automatically resolve conflicts.

### 2. HTTPS Trust Requirement
For the API to load securely in modern browsers or API clients on `https://localhost:60462/`, the engineer must execute `dotnet dev-certs https --trust`. This command is not explicitly mentioned in the quickstart guides, which leads to SSL connection errors in Bruno or curl.

### 3. Docker Resource Constraints
SQL Server 2022 plus Redis, RabbitMQ, and monitoring containers require a minimum of 4GB of RAM allocated to Docker. Clean systems running on default Docker Desktop settings (especially on older Mac/Windows setups) can encounter OOM crashes.

### 4. Shell Script Execution Permissions
For Unix-like environments, checking out git repositories can reset script executable permissions. A developer may need to manually execute `chmod +x setup-local.sh run-all-tests.sh verify-local.sh` before running.

---

## Verdict: **PASS (with minor gaps)**
The onboarding flow is highly automated using `setup-local.sh`. An engineer can go from zero to a running, seeded system in under 20 minutes if Docker and .NET 10 SDK are installed. Gaps around SSL trust and port conflicts should be documented in a troubleshooting section.
