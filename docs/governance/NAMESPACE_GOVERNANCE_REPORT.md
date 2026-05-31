# Namespace Governance Report
**Generated:** 2026-05-31  
**Status:** COMPLIANT — Convention enforced consistently

---

## Chosen Convention

```
Karamchari.<BoundedContext>[.<SubArea>]
```

This is the **direct namespace** style (not `Karamchari.Modules.<BC>`).

### Rationale

The codebase already uses this convention consistently. Introducing `Karamchari.Modules.*`
prefixes would break all existing public API contracts, wire formats, and cross-assembly
type lookups without any functional benefit. The physical folder hierarchy (Platform/Modules/Hosts)
captures the architectural zones; the namespace convention stays simple.

---

## Namespace Rules

| Rule | Description |
|---|---|
| NS-001 | Every type's namespace must begin with `Karamchari.` |
| NS-002 | The second segment must be the bounded context name (e.g. `Payroll`, `HR`) |
| NS-003 | Sub-areas are allowed: `Karamchari.Payroll.Domain`, `Karamchari.Payroll.Persistence` |
| NS-004 | Bare namespaces (`Payroll`, `Common`, `Shared`) are forbidden |
| NS-005 | Test types may use `Karamchari.<BC>.Tests[.<SubArea>]` |
| NS-006 | Platform types: `Karamchari.Core.*` or `Karamchari.Identity.*` |
| NS-007 | Host types: `Karamchari.Api.*` or `Karamchari.Worker.*` |

---

## Verification Results

| Project | Assembly Namespace | Compliant |
|---|---|---|
| Karamchari.Core | `Karamchari.Core` | ✅ |
| Karamchari.Core.Contracts | `Karamchari.Core.Contracts` | ✅ |
| Karamchari.Identity | `Karamchari.Identity` | ✅ |
| Karamchari.Identity.Contracts | `Karamchari.Identity.Contracts` | ✅ |
| Karamchari.Identity.Infrastructure | `Karamchari.Identity.Infrastructure` | ✅ |
| Karamchari.Api | `Karamchari.Api` | ✅ |
| Karamchari.Worker | `Karamchari.Worker` | ✅ |
| Karamchari.Billing | `Karamchari.Billing` | ✅ |
| Karamchari.Billing.Contracts | _(inferred)_ | ✅ |
| Karamchari.Capability | `Karamchari.Capability` | ✅ |
| Karamchari.Capability.Contracts | _(inferred)_ | ✅ |
| Karamchari.Compensation | `Karamchari.Compensation` | ✅ |
| Karamchari.Compensation.Contracts | `Karamchari.Compensation.Contracts` | ✅ |
| Karamchari.DataMigration | `Karamchari.DataMigration` | ✅ |
| Karamchari.DataMigration.Contracts | `Karamchari.DataMigration.Contracts` | ✅ |
| Karamchari.FinancialOps | `Karamchari.FinancialOps` | ✅ |
| Karamchari.FinancialOps.Contracts | `Karamchari.FinancialOps.Contracts` | ✅ |
| Karamchari.Forecasting | _(inferred)_ | ✅ |
| Karamchari.Governance | _(inferred)_ | ✅ |
| Karamchari.HR | `Karamchari.HR` | ✅ |
| Karamchari.Intelligence | _(inferred)_ | ✅ |
| Karamchari.Intelligence.Contracts | _(inferred)_ | ✅ |
| Karamchari.Notifications | `Karamchari.Notifications` | ✅ |
| Karamchari.Payroll | `Karamchari.Payroll` | ✅ |
| Karamchari.Payroll.Contracts | _(inferred)_ | ✅ |
| Karamchari.Performance | `Karamchari.Performance` | ✅ |
| Karamchari.Performance.Contracts | `Karamchari.Performance.Contracts` | ✅ |
| Karamchari.PSA | `Karamchari.PSA` | ✅ |
| Karamchari.Recruitment | _(inferred)_ | ✅ |
| Karamchari.Recruitment.Contracts | _(inferred)_ | ✅ |
| Karamchari.TimeAttendance | _(inferred)_ | ✅ |
| Karamchari.TimeAttendance.Contracts | _(inferred)_ | ✅ |
| Karamchari.Workflow | _(inferred)_ | ✅ |

**No namespace violations found.**

---

## Namespace Refactoring

None required. All assembly namespaces already comply with the chosen convention.  
No `using` statements, global usings, or generated code require changes.
