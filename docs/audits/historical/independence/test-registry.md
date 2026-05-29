# Phase 8: Test Ownership Audit & Registry

This document catalogs every test project within the Karamchari repository, analyzing their scope, execution speed, CI pipeline inclusion, and coverage gaps to ensure maintainability.

---

## Test Suites Registry

The repository contains 10 separate test projects (split between the root `/tests/` directory and inline source folders):

### 1. Karamchari.Core.UnitTests
- **Location**: [tests/Backend/Karamchari.Core.UnitTests/](file:///Users/srikarbojji/Projects/Karamchari/tests/Backend/Karamchari.Core.UnitTests)
- **Purpose**: Validates tenant resolution primitives, http provider mappings, encryption utilities, and helper extensions.
- **Coverage**: Core libraries.
- **Dependencies**: xUnit, FluentAssertions.
- **Execution Time**: < 1 second.
- **CI Integration**: Runs in [ci.yml](file:///Users/srikarbojji/Projects/Karamchari/.github/workflows/ci.yml).
- **Known Limitations**: Standard unit mock-testing only; does not verify SQL DB interactions.

### 2. Karamchari.Core.IntegrationTests
- **Location**: [tests/Backend/Karamchari.Core.IntegrationTests/](file:///Users/srikarbojji/Projects/Karamchari/tests/Backend/Karamchari.Core.IntegrationTests)
- **Purpose**: Tests dynamic tenant schema rewriting, database connection context bindings, outbox dispatch relays, and Redis caching.
- **Coverage**: Tenancy interceptors, cache providers, and transactional outbox.
- **Dependencies**: xUnit, Microsoft.EntityFrameworkCore.InMemory.
- **Execution Time**: ~5 - 10 seconds.
- **CI Integration**: Runs in `ci.yml`.
- **Known Limitations**: Utilizes InMemory EF databases, which do not mirror full SQL Server features (like schema creation or RLS policies).

### 3. Karamchari.Api.UnitTests
- **Location**: [tests/Backend/Karamchari.Api.UnitTests/](file:///Users/srikarbojji/Projects/Karamchari/tests/Backend/Karamchari.Api.UnitTests)
- **Purpose**: Validates minimal API endpoint models, error handling filters, and request authorization schemas.
- **Coverage**: API Gateway / BFF gateway filters.
- **Dependencies**: xUnit, Moq.
- **Execution Time**: ~1 - 2 seconds.
- **CI Integration**: Runs in `ci.yml`.
- **Known Limitations**: Controller mocks only; no integration testing with actual database instances.

### 4. Karamchari.ArchitectureTests
- **Location**: [tests/Backend/Karamchari.ArchitectureTests/](file:///Users/srikarbojji/Projects/Karamchari/tests/Backend/Karamchari.ArchitectureTests)
- **Purpose**: Enforces bounded-context separation rules (e.g. preventing direct reference from Forecasting to Billing or Payroll to Notifications).
- **Coverage**: Assembly-level architectural separation.
- **Dependencies**: NetArchTest.Rules.
- **Execution Time**: ~2 seconds.
- **CI Integration**: Runs in `ci.yml`.
- **Known Limitations**: **Critical**: Must run without code coverage collection. If Coverlet is active, reflection logic fails with `TypeLoadException` on instrumented assemblies.

### 5. Karamchari.Identity.IntegrationTests
- **Location**: [tests/Backend/Karamchari.Identity.IntegrationTests/](file:///Users/srikarbojji/Projects/Karamchari/tests/Backend/Karamchari.Identity.IntegrationTests)
- **Purpose**: Validates token generation, sign-ups, permission scopes, and session refresh logic.
- **Coverage**: Identity context, Auth handlers, token endpoints.
- **Dependencies**: xUnit, Microsoft.AspNetCore.Mvc.Testing.
- **Execution Time**: ~5 - 8 seconds.
- **CI Integration**: Runs in `ci.yml`.
- **Known Limitations**: Interacts with the database, requiring SQL Server to be running.

### 6. Karamchari.FinancialChaosTests
- **Location**: [tests/Backend/Karamchari.FinancialChaosTests/](file:///Users/srikarbojji/Projects/Karamchari/tests/Backend/Karamchari.FinancialChaosTests)
- **Purpose**: Validates ledger entry consistency and double-entry balancing rules during simulated database errors and transaction retries.
- **Coverage**: FinancialOps domain.
- **Dependencies**: xUnit, NSubstitute.
- **Execution Time**: ~3 seconds.
- **CI Integration**: Runs in `ci.yml`.
- **Known Limitations**: Simulated failure injection; does not run against a real SQL Server failover.

### 7. Karamchari.TenantIsolationCertification
- **Location**: [tests/Backend/Karamchari.TenantIsolationCertification/](file:///Users/srikarbojji/Projects/Karamchari/tests/Backend/Karamchari.TenantIsolationCertification)
- **Purpose**: Evaluates RLS penetration vulnerabilities, schema isolation boundaries, cache separation, background jobs leakage, and SQL concurrency.
- **Coverage**: Security & tenant separation.
- **Dependencies**: xUnit, Live database (SQL Server / Azure SQL Edge).
- **Execution Time**: ~2 - 5 minutes (depending on soak iterations).
- **CI Integration**: Runs in [tenant-isolation-certification.yml](file:///Users/srikarbojji/Projects/Karamchari/.github/workflows/tenant-isolation-certification.yml).
- **Known Limitations**: Heavy dependency on live SQL instance; fails in CI if runner SDK is not .NET 10.

### 8. Karamchari.Payroll.Tests
- **Location**: [src/Backend/Karamchari.Payroll.Tests/](file:///Users/srikarbojji/Projects/Karamchari/src/Backend/Karamchari.Payroll.Tests)
- **Purpose**: Tests statutory calculations (TDS, PF, ESI), variable components, arrear calculations, salary structuring, and Full & Final state machine transitions.
- **Coverage**: Payroll context domain logic.
- **Dependencies**: xUnit, FluentAssertions.
- **Execution Time**: ~2 - 3 seconds.
- **CI Integration**: Runs in `ci.yml`.
- **Known Limitations**: Calculation rules validation only; database logic is mocked.

### 9. Karamchari.PSA.Tests
- **Location**: [src/Backend/Karamchari.PSA.Tests/](file:///Users/srikarbojji/Projects/Karamchari/src/Backend/Karamchari.PSA.Tests)
- **Purpose**: Tests timesheet allocations, billable rate calculations, project structures, and invoice lines aggregation.
- **Coverage**: Professional Services Automation (PSA) context.
- **Dependencies**: xUnit.
- **Execution Time**: ~1 second.
- **CI Integration**: Runs in `ci.yml`.
- **Known Limitations**: Pure domain logic testing; no database state tests.

### 10. Karamchari.TimeAttendance.Tests
- **Location**: [src/Backend/Karamchari.TimeAttendance.Tests/](file:///Users/srikarbojji/Projects/Karamchari/src/Backend/Karamchari.TimeAttendance.Tests)
- **Purpose**: Validates work shift models, leaves calculations, and policy assignments.
- **Coverage**: Time & Attendance context.
- **Dependencies**: xUnit.
- **Execution Time**: < 1 second.
- **CI Integration**: Runs in `ci.yml`.
- **Known Limitations**: Domain validation only.

---

## Untested Bounded Contexts

While key commercial calculation paths (Payroll, PSA, Time & Attendance) are tested, **10 out of 16 modules have absolutely zero test projects**:

1. **Workflow** (`Karamchari.Workflow`)
2. **Forecasting** (`Karamchari.Forecasting`)
3. **Billing** (`Karamchari.Billing`)
4. **Compensation** (`Karamchari.Compensation`)
5. **Recruitment** (`Karamchari.Recruitment`)
6. **Capability** (`Karamchari.Capability`)
7. **Intelligence** (`Karamchari.Intelligence`)
8. **Governance** (`Karamchari.Governance`)
9. **Notifications** (`Karamchari.Notifications`)
10. **Performance** (`Karamchari.Performance`)

Any code changes within these 10 contexts must be verified via global integration tests or manually, presenting a high regression risk for new developers.

---

## Verdict: **FAIL (Due to extensive untested modules)**

The test registry reveals a significant gap: 62% of the modules have no dedicated unit or integration test projects. 

While the Core infrastructure and Payroll calculations are thoroughly tested, the lack of test coverage for critical business features like Workflow, Billing, and Forecasting makes the system fragile for a developer without legacy context.

### Recommendation:
Create test projects (e.g., `Karamchari.Billing.Tests`, `Karamchari.Workflow.Tests`) for the remaining modules to establish baseline regression safety.
