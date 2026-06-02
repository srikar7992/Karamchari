# REPOSITORY RUNTIME GOVERNANCE CERTIFICATION

**Date**: 2026-06-02  
**Program**: Sprint 1 + Sprint 2 Runtime Truth Program — Section 1  
**Auditor**: Principal Architect / SRE Lead

---

## Evidence

### Build Reproducibility

| Build | Config | Result | Warnings | Errors |
|-------|--------|--------|----------|--------|
| Solution (local) | Debug | PASS | 0 | 0 |
| Worker (standalone) | Release | PASS | 0 | 0 |
| API Docker | Release | PASS | 0 | 0 |
| Worker Docker | Release | PASS | 0 | 0 |

### Defects Found & Fixed

| # | File | Defect | Fix |
|---|------|--------|-----|
| 1 | `Karamchari.Worker.csproj` | Missing `ProjectReference` for `Karamchari.FinancialOps` and `Karamchari.Recruitment.Api` — Docker isolated build failed, solution build masked it | Added both references |
| 2 | `MessagingServiceCollectionExtensions.cs` | Method signature `bool includeConsumers` did not match caller contract `Action<IBusRegistrationConfigurator>` | Changed to `Action<IBusRegistrationConfigurator>? configureConsumers = null` |
| 3 | `Karamchari.Worker` not in `Karamchari.sln` | Worker host excluded from solution; build CI runs `dotnet build` against sln, missing Worker errors | Added to solution via `dotnet sln add` |
| 4 | `OrganizationHierarchyCleanupWorker` missing | Referenced in `WorkerServiceCollectionExtensions` but not implemented | Created in `Karamchari.HR.Services` |
| 5 | `PayrollCycleCloser` missing | Referenced in `WorkerServiceCollectionExtensions` but not implemented | Created in `Karamchari.Payroll.Services` |
| 6 | `IntelligenceDbContext` value comparer | `IReadOnlyList<Guid>` comparer applied to `IReadOnlyCollection<Guid>` property → model build failure at runtime | Changed to `ReadOnlyCollectionComparer<Guid>()` |

### Solution Structure

| Criterion | Status |
|-----------|--------|
| All projects in approved roots (`src/Backend`, `tests/Backend`) | PASS |
| All projects follow `Karamchari.*` namespace | PASS |
| `Karamchari.Worker` in solution file | PASS (added) |
| Nullable enabled globally (Directory.Build.props) | PASS |
| Treat warnings as errors | PASS |

### Architecture Test Results

```
Karamchari.ArchitectureTests: 60 tests — 0 failures
  NoProjectOutsideApprovedRoots       PASS
  AllProjectsFollowNamespaceConvention PASS
  (dependency layer tests)            PASS
```

---

## Verdict

**CERTIFIED** — Repository governance passes. Six critical build defects discovered and fixed during runtime audit; these would have caused Worker container to fail on any clean build outside the solution context.
