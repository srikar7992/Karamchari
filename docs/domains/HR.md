# HR Domain

## Evidence Boundary

Source: `src/Backend/Karamchari.HR`, HR API endpoints in `src/Backend/Karamchari.Api/BFF/Employee`, and HR persistence in `src/Backend/Karamchari.HR/Persistence/HRDbContext.cs`.

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Manages organization structure and employees. Evidence: `HRDbContext` exposes `Departments`, `Designations`, `Branches`, `LegalEntities`, `CostCenters`, `BusinessUnits`, `Positions`, `Locations`, `Employees`, and `EmployeeRelationships` in `src/Backend/Karamchari.HR/Persistence/HRDbContext.cs:41`. |
| Business Objectives | UNKNOWN beyond the exposed employee and organization-management surface. |
| Core Concepts | Department, designation, branch, legal entity, cost center, business unit, position, location, employee, employee relationship. Evidence: DbSets in `src/Backend/Karamchari.HR/Persistence/HRDbContext.cs:41`. |
| Business Terminology | Employee, department, designation, branch, legal entity, cost center, business unit, position, location, relationship. Evidence: domain files under `src/Backend/Karamchari.HR/Domain`. |
| Aggregates / Entities | `Employee`, `EmployeeHistoryEntry`, `Department`, `Designation`, `Branch`, `LegalEntity`, `CostCenter`, `BusinessUnit`, `Position`, `Location`, `EmployeeRelationship`. Evidence: `src/Backend/Karamchari.HR/Domain/Employees/Employee.cs`, `src/Backend/Karamchari.HR/Domain/Organization/*.cs`. |
| Value Objects | UNKNOWN from this pass. |
| State Machines | Employment status and type exist. Evidence: `src/Backend/Karamchari.HR/Domain/Employees/EmploymentStatus.cs`, `src/Backend/Karamchari.HR/Domain/Employees/EmploymentType.cs`. Full transition rules are UNKNOWN. |
| Events | `EmployeeHired`, `DepartmentCreated`; `EmployeeOnboardedIntegrationEvent` is a cross-module contract. Evidence: `src/Backend/Karamchari.HR/Domain/Employees/Events/EmployeeHired.cs`, `src/Backend/Karamchari.HR/Domain/Departments/Events/DepartmentCreated.cs`, `src/Backend/Karamchari.Core.Contracts/IntegrationEvents/IntegrationEvents.cs:201`. |
| Commands | Employee onboarding/update API commands exist. Evidence: `src/Backend/Karamchari.HR/Contracts/Employees/UpdateEmployeeCommand.cs`, `src/Backend/Karamchari.Api/Validation/OnboardEmployeeCommandValidator.cs`. |
| Queries | Employee list, get, history endpoints. Evidence: `src/Backend/Karamchari.Api/BFF/Employee/EmployeeEndpoints.cs:25`, `src/Backend/Karamchari.Api/BFF/Employee/EmployeeEndpoints.cs:28`, `src/Backend/Karamchari.Api/BFF/Employee/EmployeeEndpoints.cs:38`. |
| Business Rules / Invariants / Validation | API validators exist for onboarding and update; exact rule catalog must be read from validators and `EmployeeService`. Evidence: `src/Backend/Karamchari.Api/Validation/OnboardEmployeeCommandValidator.cs`, `src/Backend/Karamchari.Api/Validation/UpdateEmployeeCommandValidator.cs`, `src/Backend/Karamchari.HR/Services/EmployeeService.cs`. |
| Calculation Rules | UNKNOWN. |
| Ownership Rules | Tenant ownership is enforced by inherited `KaramchariDbContext` tenant filters for tenant-owned entities. HR-specific ownership is UNKNOWN. Evidence: `src/Backend/Karamchari.Core/Persistence/KaramchariDbContext.cs:123`. |
| Dependencies | Core primitives, MassTransit dispatcher, tenant provisioning consumer. Evidence: `src/Backend/Karamchari.HR/Messaging/MassTransitDomainEventDispatcher.cs`, `src/Backend/Karamchari.HR/Consumers/TenantProvisionedConsumer.cs:12`. |
| External Integrations | Document intelligence service options exist; provider details are UNKNOWN. Evidence: `src/Backend/Karamchari.HR/Services/DocumentIntelligenceService.cs`, `src/Backend/Karamchari.HR/Services/DocumentIntelligenceOptions.cs`. |
| Examples | CRUD and transfer surface: `POST /api/v1/hr/employees`, `GET /api/v1/hr/employees`, `PUT /api/v1/hr/employees/{id}`, `DELETE /api/v1/hr/employees/{id}`, `POST /api/v1/hr/employees/{id}/transfer`. Evidence: `src/Backend/Karamchari.Api/BFF/Employee/EmployeeEndpoints.cs:19`. |
| Failure Scenarios | Tenant provisioning consumer failures, validation failures, tenant resolution failures. Exact recovery procedures UNKNOWN. |
| Known Limitations | HR-specific integration tests were not found; behavioral coverage is not certified. |
