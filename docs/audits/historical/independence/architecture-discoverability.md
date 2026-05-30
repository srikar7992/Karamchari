# Phase 2: Architecture Discoverability Audit

This audit evaluates how easily an engineer unfamiliar with Karamchari can locate key elements (Domain, Application, Infrastructure, Contracts, Database, Endpoints, Events, Consumers, Migrations) without external guidance.

## Code Layout & Conventions

The Karamchari codebase is organized as a modular monolith in a single C# solution ([Karamchari.sln](src/Backend/Karamchari.sln)). It utilizes folder-based conventions to separate concerns:

```
Karamchari/
├── src/
│   ├── Backend/
│   │   ├── Karamchari.Api/                # Endpoints (BFF), API host, gateway routing
│   │   │   ├── BFF/                       # REST endpoint routing folders per domain context
│   │   ├── Karamchari.Core/               # Tenanting engine, Outbox relay, Shared infrastructure
│   │   ├── Karamchari.Core.Contracts/     # Shared integration primitives and event models
│   │   ├── Karamchari.<Context>/          # Bounded context implementation assembly
│   │   │   ├── Domain/                    # Entities, aggregates, value objects
│   │   │   ├── Persistence/ (or Data/)    # DbContext, entity configurations, seeds
│   │   │   ├── Migrations/                # EF Core schema migration records
│   │   │   ├── Services/                  # Application services & business logic execution
│   │   │   ├── Consumers/                 # MassTransit integration event handlers
│   │   ├── Karamchari.<Context>.Contracts/ # (Optional) Public API models & event interfaces
```

---

## Architectural Mapping Matrix

| Bounded Context | Layer | Folder Location | Namespace |
| :--- | :--- | :--- | :--- |
| **HR** | Endpoints | [Karamchari.Api/BFF/Employee/](src/Backend/Karamchari.Api/BFF/Employee) | `Karamchari.Api.BFF.Employee` |
| | Domain | [Karamchari.HR/Domain/](src/Backend/Karamchari.HR/Domain) | `Karamchari.HR.Domain` |
| | Database / Context | [Karamchari.HR/Persistence/](src/Backend/Karamchari.HR/Persistence) | `Karamchari.HR.Persistence` |
| | Migrations | [Karamchari.HR/Migrations/](src/Backend/Karamchari.HR/Migrations) | `Karamchari.HR.Migrations` |
| | Contracts | [Karamchari.HR/Contracts/](src/Backend/Karamchari.HR/Contracts) | `Karamchari.HR.Contracts` |
| **Payroll** | Endpoints | [Karamchari.Api/BFF/Payroll/](src/Backend/Karamchari.Api/BFF/Payroll) | `Karamchari.Api.BFF.Payroll` |
| | Domain | [Karamchari.Payroll/Domain/](src/Backend/Karamchari.Payroll/Domain) | `Karamchari.Payroll.Domain` |
| | Database / Context | [Karamchari.Payroll/Persistence/](src/Backend/Karamchari.Payroll/Persistence) | `Karamchari.Payroll.Persistence` |
| | Contracts | [Karamchari.Payroll.Contracts/](src/Backend/Karamchari.Payroll.Contracts) | `Karamchari.Payroll.Contracts` |
| | Consumers | [Karamchari.Payroll/Consumers/](src/Backend/Karamchari.Payroll/Consumers) | `Karamchari.Payroll.Consumers` |
| **Billing** | Database / Context | [Karamchari.Billing/Persistence/](src/Backend/Karamchari.Billing/Persistence) | `Karamchari.Billing.Persistence` |
| | Contracts | [Karamchari.Billing.Contracts/](src/Backend/Karamchari.Billing.Contracts) | `Karamchari.Billing.Contracts` |
| **Workflow** | Database / Context | [Karamchari.Workflow/Persistence/](src/Backend/Karamchari.Workflow/Persistence) | `Karamchari.Workflow.Persistence` |
| **Identity** | Database / Context | [Karamchari.Identity.Infrastructure/Persistence/](src/Backend/Karamchari.Identity.Infrastructure/Persistence) | `Karamchari.Identity.Infrastructure.Persistence` |
| **Tenanting** | Resolution | [Karamchari.Core/Multitenancy/](src/Backend/Karamchari.Core/Multitenancy) | `Karamchari.Core.Multitenancy` |
| **Messaging** | Outbox Relay | [Karamchari.Core/Messaging/Outbox/](src/Backend/Karamchari.Core/Messaging/Outbox) | `Karamchari.Core.Messaging.Outbox` |

---

## Architectural Discoverability Evaluation

### Structural Discoverability Strengths:
1. **Directory-Namespace Alignment**: Directory structures exactly mirror C# namespaces, allowing developers to locate files using compiler errors or namespace imports.
2. **REST Endpoints Centralization**: Exposing REST routes as minimal APIs inside the [Karamchari.Api/BFF/](src/Backend/Karamchari.Api/BFF) folder organizes all request-handling in one entrypoint.
3. **Database Conventions**: In almost every module, database mapping occurs within `Persistence` folders containing the migrations and configurations.

### Structural Discoverability Weaknesses:
1. **Contract Projects Fragmentation**: Some modules place contracts in a subfolder inside their main project (e.g. `Karamchari.HR/Contracts`), while others have a separate assembly (e.g. `Karamchari.Payroll.Contracts/`). This lack of absolute consistency can confuse engineers looking for where to reference integration types.
2. **Context-to-Context Dependencies**: While `ArchitectureTests` verify coupling rules, the lack of an overarching conceptual map means finding where events cross boundaries requires manual search.

---

## Discoverability Score Card

| Category | Score | Criteria & Rationale |
| :--- | :--- | :--- |
| **Directory-to-Namespace Alignment** | **9.5 / 10** | Perfect alignment between namespace declaration and disk directory path. |
| **API Boundary Discoverability** | **9.0 / 10** | Endpoints are unified in `Karamchari.Api/BFF` under folders mapped by modules. |
| **Domain Logic Separation** | **8.5 / 10** | Entities are grouped under `Domain` folders, separated from API hosts. |
| **Database and Persistence Layout** | **9.0 / 10** | All EF Core configurations and migrations live in standard `Persistence` directories. |
| **Messaging Infrastructure Locatability** | **8.0 / 10** | MassTransit consumers are situated under `Consumers` directories, but tracing events between contexts is not immediately visible from directories. |
| **Overall Knowledge-map Score** | **88.0% (8.8 / 10)** | **Excellent**. The architecture is structured uniformly, with very low layout ambiguity. |
