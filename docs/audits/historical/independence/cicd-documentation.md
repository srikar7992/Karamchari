# Phase 6: CI/CD Independence Audit

This audit evaluates whether a new engineer can understand, modify, debug, and execute the CI/CD pipelines defined in the repository.

---

## Workflow Configurations Registry

The continuous integration pipeline is defined across 3 GitHub Actions workflow files inside the [.github/workflows/](file:///Users/srikarbojji/Projects/Karamchari/.github/workflows) directory:

| Workflow File | Trigger Events | Purpose | Execution Environment | Status |
| :--- | :--- | :--- | :--- | :--- |
| [ci.yml](file:///Users/srikarbojji/Projects/Karamchari/.github/workflows/ci.yml) | Push & Pull Requests to `main`, `master` | Compiles codebase, runs unit and architecture tests, audits NuGet vulnerabilities, runs static analysis, and builds Frontend/Mobile packages. | `ubuntu-latest` running .NET SDK `10.0.203` & Node `24`. | **PASS** |
| [tenant-isolation-certification.yml](file:///Users/srikarbojji/Projects/Karamchari/.github/workflows/tenant-isolation-certification.yml) | Push & PRs to `main`, `develop`, Schedule (Nightly) | Runs SQL multi-tenant schema isolation, Row-Level Security, concurrency stress, and chaos engineering verification. | `ubuntu-latest` running Azure SQL Edge services container. | ⚠️ **FAIL (SDK Bug)** |
| [deploy-api.yml](file:///Users/srikarbojji/Projects/Karamchari/.github/workflows/deploy-api.yml) | Push to `main`, Manual dispatch | Builds Docker container image and triggers deployment sequence to Dev, Staging, and Prod. | `ubuntu-latest` with Docker Buildx. | ⚠️ **FAIL (Mocked)** |

---

## Technical Audit Findings

### 1. The .NET SDK Version Mismatch Bug (Blocking Fail)
In the [tenant-isolation-certification.yml](file:///Users/srikarbojji/Projects/Karamchari/.github/workflows/tenant-isolation-certification.yml) file, line 10 configures:
```yaml
env:
  DOTNET_VERSION: '8.0'
```
However, the codebase requires **.NET 10.0** (configured globally in [Directory.Build.props](file:///Users/srikarbojji/Projects/Karamchari/Directory.Build.props#L11)). 
Running this workflow in GitHub Actions causes compilation failures because the .NET 8.0 SDK cannot build `net10.0` assemblies.

### 2. Commented-Out Deployments (Lack of Deployment Capability)
In [deploy-api.yml](file:///Users/srikarbojji/Projects/Karamchari/.github/workflows/deploy-api.yml), the deployment steps to Azure (Azure Login, Docker push to Azure Container Registry, Bicep infrastructure deployment, Azure App Service container rollout) are commented out.
- The pipeline executes only a validation `docker build` check.
- A new developer has no active, automated route to deploy the application or roll back changes.

### 3. NetArchTest Coverlet Conflict Risk
In `ci.yml`, the pipeline runs:
```bash
dotnet test src/Backend/Karamchari.sln --collect:"XPlat Code Coverage"
```
As documented in the test certification reports, NetArchTest (used in `Karamchari.ArchitectureTests`) throws a `TypeLoadException` when reflected over assemblies containing Coverlet instrumentation tracker classes. Running this command on the entire solution will cause the architecture tests to fail on CI unless excluded.

---

## Verdict: **FAIL (Due to SDK mismatch and commented deployments)**

A new engineer cannot deploy the system automatically or run the tenant isolation verification pipeline in CI without encountering build failures. 

### Recommendations:
1. Update `DOTNET_VERSION: '10.0'` inside [tenant-isolation-certification.yml](file:///Users/srikarbojji/Projects/Karamchari/.github/workflows/tenant-isolation-certification.yml).
2. Un-comment or configure the Azure parameters in [deploy-api.yml](file:///Users/srikarbojji/Projects/Karamchari/.github/workflows/deploy-api.yml) with correct environmental secrets.
3. Split the test executions in `ci.yml` so that architecture tests execute separately without coverage collection.
