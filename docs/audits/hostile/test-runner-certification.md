# Test Runner Certification

Scripts audited:

- `run-all-tests.sh`
- `run-all-tests.ps1`

## Execution Evidence

Non-escalated execution hung during initial `dotnet build` in the sandbox. Backend build outside sandbox succeeded:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
Time Elapsed 00:00:03.25
```

Full test runner outside sandbox:

```bash
./run-all-tests.sh
```

Result:

```text
Projects passed: 10
Projects failed: 0
TRX + coverage under: artifacts/test-results
```

Passed projects:

- `Karamchari.Core.UnitTests`
- `Karamchari.Api.UnitTests`
- `Karamchari.ArchitectureTests`
- `Karamchari.Payroll.Tests`
- `Karamchari.PSA.Tests`
- `Karamchari.TimeAttendance.Tests`
- `Karamchari.FinancialChaosTests`
- `Karamchari.Core.IntegrationTests`
- `Karamchari.Identity.IntegrationTests`
- `Karamchari.TenantIsolationCertification`

## Artifact Evidence

TRX files were generated under `artifacts/test-results`, including:

- `Karamchari.Api.UnitTests.trx`
- `Karamchari.ArchitectureTests.trx`
- `Karamchari.Core.IntegrationTests.trx`
- `Karamchari.Core.UnitTests.trx`
- `Karamchari.FinancialChaosTests.trx`
- `Karamchari.Identity.IntegrationTests.trx`
- `Karamchari.PSA.Tests.trx`
- `Karamchari.Payroll.Tests.trx`
- `Karamchari.TenantIsolationCertification.trx`
- `Karamchari.TimeAttendance.Tests.trx`

Coverage XML files were generated, but no HTML coverage report was produced by the script.

## Gaps

- No JUnit output was produced.
- No HTML report was produced.
- Architecture tests intentionally run without coverage due Coverlet/NetArchTest incompatibility.
- The script does not run mutation testing.
- The script does not explicitly classify performance smoke tests beyond existing test project names.

## Verdict

Test runner passes as a TRX/Cobertura backend runner. It fails the full requirement because JUnit, HTML report, and mutation testing are absent.
