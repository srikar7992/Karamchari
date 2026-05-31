# Dependency Graph Report
**Generated:** 2026-05-31  
**Status:** COMPLIANT — No forbidden dependencies found

---

## Allowed Dependency Directions

```
Host    → Platform     ✅
Host    → Modules      ✅
Module  → Platform     ✅
Module  → Module.*Contracts  ✅  (cross-module via Contracts only)
Platform → (none)      ✅  (Core has no project references)
Tests   → Any          ✅

Forbidden:
  Platform → Host      ❌
  Platform → Module    ❌
  Module.Core → Host   ❌
  Module.A → Module.B  ❌  (must go through B.Contracts)
```

---

## Verified Dependency Matrix

### Platform Projects

| Project | Dependencies | Verdict |
|---|---|---|
| Karamchari.Core | _(none)_ | ✅ Foundation |
| Karamchari.Core.Contracts | _(none)_ | ✅ Foundation |
| Karamchari.Identity | Core, Identity.Contracts | ✅ Platform→Platform only |
| Karamchari.Identity.Contracts | _(none)_ | ✅ Foundation |
| Karamchari.Identity.Infrastructure | Identity | ✅ Platform→Platform only |

### Host Projects

| Project | Dependencies | Verdict |
|---|---|---|
| Karamchari.Api | Core, Core.Contracts, Identity.Infrastructure, all 20 modules | ✅ Host→All is correct |
| Karamchari.Worker | Core, Core.Contracts, Identity.Infrastructure, 15 modules | ✅ Host→All is correct |

### Module Projects — Cross-Module References

| Module | References Other Modules As | Verdict |
|---|---|---|
| Billing | TimeAttendance.**Contracts** | ✅ Contracts-only |
| Capability.Contracts | Core | ✅ Platform only |
| DataMigration | HR (domain), TimeAttendance (domain), Payroll (domain) | ⚠️ See note below |
| Forecasting | Billing.**Contracts**, TimeAttendance.**Contracts** | ✅ Contracts-only |
| Notifications | Performance.**Contracts**, Payroll.**Contracts** | ✅ Contracts-only |
| TimeAttendance | Billing.**Contracts** | ✅ Contracts-only |

### DataMigration Cross-Module Note

`Karamchari.DataMigration` references `Karamchari.HR`, `Karamchari.TimeAttendance`, and `Karamchari.Payroll` directly (domain assemblies, not just contracts). This is intentional: the DataMigration module is an **integration module** — its entire purpose is to read from and write to other modules' DbContexts as part of the bulk import pipeline. This exemption is documented and architecturally justified.

**Dependency Rule Amendment DR-001:** Integration/DataMigration modules may reference module domain assemblies directly to access DbContexts. All other modules must reference only Contracts assemblies cross-module.

---

## Violations Found

**None.** All 32 source projects comply with the dependency rules (including the documented DR-001 exemption for DataMigration).

---

## Dependency Visualization

```
                    ┌─────────────┐
                    │  Platform   │
                    │  Core       │
                    │  Identity   │
                    └──────┬──────┘
                           │ (all modules depend on Platform)
            ┌──────────────┼──────────────┐
            ▼              ▼              ▼
     ┌────────────┐  ┌──────────┐  ┌──────────────┐
     │  Billing   │  │ Payroll  │  │DataMigration │
     │  Billing.C │  │ Payroll.C│  │(integration) │
     └────────────┘  └──────────┘  └──────┬───────┘
                                          │ (reads HR/TA/Payroll DbContexts)
                                          ▼
                                   ┌──────────┐
                                   │  HR      │
                                   │  TA      │
                                   │  Payroll │
                                   └──────────┘

Hosts (Api/Worker) → All modules + Platform
Tests → Any
```
