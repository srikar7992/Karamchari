# Local Development Setup & Workflows

Welcome to the Karamchari development environment. This document is the single source of truth for setting up, validating, and testing the platform locally.

---

## 1. Prerequisites
Ensure the following tools are installed on your host system:
*   **.NET SDK**: Version `10.0` or newer (defined by `global.json`)
*   **Docker Desktop**: Version `20.10` or newer
*   **Node.js**: Version `24` or newer (for Portal and Mobile builds)
*   **Bruno**: For executing API collection requests (optional)

---

## 2. Port Allocations
Before running the local runtime, ensure the following local ports are free:
*   `60462` / `60463` - Karamchari Web API Gateway (HTTPS/HTTP)
*   `1433` - SQL Server primary database
*   `6379` - Redis Cache server
*   `5672` / `15672` - RabbitMQ Broker & management dashboard
*   `8081` - Seq structured logs and trace viewer
*   `3000` - Grafana telemetry dashboard
*   `9090` - Prometheus metrics collector
*   `8025` - Mailpit SMTP capture dashboard
*   `10000` / `10001` / `10002` - Azurite storage emulator

---

## 3. Official Developer Workflows

Karamchari enforces exactly one official command workflow for local development:

### Setup Workflow
Restores backend/frontend packages, starts containerized middleware, runs database migrations, provisions row-level security policies, and seeds test data.
*   **Unix / MacOS**:
    ```bash
    ./setup-local.sh
    ```
*   **Windows (PowerShell)**:
    ```powershell
    ./setup-local.ps1
    ```
*Parameters*:
- `--fresh` / `-Fresh`: Cleans existing docker volumes first (destroys data).
- `--no-run` / `-NoRun`: Configures infrastructure and compiles code but does not run the API gateway.
- `--skip-seed` / `-SkipSeed`: Skips running SQL data seeds.

### Smoke Validation Workflow
Performs quick local port connectivity and API health check endpoint smoke tests.
*   **Unix / MacOS**:
    ```bash
    ./verify-local.sh
    ```
*   **Windows (PowerShell)**:
    ```powershell
    ./verify-local.ps1
    ```

### Test Execution Workflow
Compiles code, runs all unit, integration, architecture, and tenant isolation tests, and exports TRX summaries and Cobertura coverage.
*   **Unix / MacOS**:
    ```bash
    ./run-all-tests.sh
    ```
*   **Windows (PowerShell)**:
    ```powershell
    ./run-all-tests.ps1
    ```
*Parameters*:
- `--no-integration` / `-SkipIntegration`: Skips suites that require a running Docker environment.

---

## 4. Git Hooks & Code Quality Standards

Karamchari uses `.githooks` to enforce validation before check-ins:
- Activate the repository hooks path manually on Unix systems:
  ```bash
  git config core.hooksPath .githooks
  ```
- Backend builds are strictly configured with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. Any warning raised by code analyzers or formatting rules will fail the build.

---

## 5. Secret Handling Rules

*   **Never commit connection strings, keys, or credentials** to git.
*   Do not check in environment-specific `appsettings.*.json`, `.env` files, or SSL certificates.
*   In local development, use environment variables (e.g. `ConnectionStrings__KaramchariDb`), `dotnet user-secrets`, or let the setup scripts populate environment values dynamically.

---

## 6. Local API Testing
Local requests and authentication scenarios are stored as a **Bruno** collection under [tools/api-tests/](file:///Users/srikarbojji/Projects/Karamchari/tools/api-tests). 
Open the Bruno desktop client, import the directory, and select the `Local` profile to start executing requests.
Scalar is also available at `https://localhost:60462/scalar` when the API is running in the `Development` environment.
