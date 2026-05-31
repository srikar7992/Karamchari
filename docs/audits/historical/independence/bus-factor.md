# Phase 10: Bus Factor Audit

This audit evaluates the human redundancy and knowledge concentration risks within the Karamchari engineering team, identifying single-point-of-failure risks.

---

## Bus Factor Scores

The "Bus Factor" represents the minimum number of engineers who must disappear from a project before development, maintenance, and operations completely stall.

| Component / Subsystem | Bus Factor | Primary Owner | Risks & Knowledge Concentration Details |
| :--- | :---: | :--- | :--- |
| **Core Tenancy Infrastructure** | **1** | `@srika` | High complexity. Interceptors, schema rewriting, and Row-Level Security (RLS) script generations are custom configurations. Loss of owner leaves tenancy isolation at risk of leakages. |
| **Identity & Security Context**| **1** | `@srika` | Exposes key signing algorithms, permissions policy translations, and user token stores. |
| **Payroll Bounded Context** | **1** | `@srika` | Contains complex logic for taxes (TDS/PF), arrears calculators, variable pay, and Full & Final settlements. No written domain specs exist. |
| **Other Domain Modules** | **1** | `@srika` | Billing, forecasting, and workflows have no tests and no documentation. Owner disappearance halts progress. |
| **CI/CD Pipelines & Infra** | **1** | `@srika` | commented-out deployments and .NET version configuration bugs in workflows require deep legacy knowledge to rectify. |
| **Overall Platform** | **1** | `@srika` | **Severe Risk**. The entire system is built and maintained by a single architect. |

---

## Supporting Evidence

1. **CODEOWNERS Registry**: The [.github/CODEOWNERS](.github/CODEOWNERS) file maps every path in the codebase to a single username:
   ```
   * @srika
   /src/Backend/Karamchari.Payroll/ @srika
   /src/Backend/Karamchari.Identity/ @srika
   /src/Backend/Karamchari.Identity.Infrastructure/ @srika
   /src/Backend/Karamchari.Core/ @srika
   /Directory.Build.props @srika
   /Directory.Packages.props @srika
   /.github/workflows/ @srika
   ```
2. **Zero Secondary Maintainers**: No other engineers are assigned or mentioned as reviewers in the repository configurations.
3. **No Legacy Documentation**: As audited in Phase 3 and Phase 4, the absence of bounded context domain readmes and runbooks means that all legacy reasoning, design tradeoffs, and operational tricks are stored entirely in the mind of the original architect.

---

## Verdict: **HIGH RISK (Bus Factor = 1)**

The platform is completely dependent on a single individual. If `@srika` disappears, a new team will struggle to keep the system operational or extend it without significant reverse-engineering, introducing high business continuity risks.

### Recommendations:
1. Formally onboard secondary maintainers and update `CODEOWNERS` rules.
2. Draft domain documentation (as recommended in Phase 3) to transfer tribal knowledge into repository markdown files.
