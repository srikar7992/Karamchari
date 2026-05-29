# Platform Evolution Principles

These principles serve as our architectural cultural safeguards. They are permanent, binding rules for how the Karamchari platform evolves.

### 1. Prefer Deletion Over Addition
Complexity is an existential threat. Before building a new interceptor or scope, attempt to remove or generalize an existing one. Deleting platform code is celebrated.

### 2. Prefer Unification Over Specialization
If messaging, background jobs, and HTTP requests handle tenant propagation slightly differently, unify them. One canonical execution model (`TenantExecutionContext.Establish()`) is always better than three specialized ones.

### 3. Prefer Explicitness Over Magic
Do not hide critical isolation boundaries behind impenetrable generic middleware. The start and end of a tenant scope must be explicitly visible in the codebase and in the distributed traces.

### 4. Prefer Explainability Over Cleverness
If a junior engineer cannot understand how a message gets from RabbitMQ into an RLS-protected database query within 30 minutes, the abstraction is too clever. Write code for humans to debug at 3 AM.

### 5. Prefer Operational Clarity Over Abstraction Elegance
"Elegant" code that produces cryptic logs is a failure. An interceptor that is slightly more verbose but logs exact failure modes, execution sources, and correlation IDs is vastly superior.

### 6. Prefer Boring Business Domains
Feature engineers should build HR and Payroll features. They should NEVER interact with `TenantExecutionEnvelope` or connection pooling logic. The platform must absorb this complexity entirely.

### 7. Governance Must Be Lightweight
Architecture reviews should take minutes, not days. We rely on executable fitness functions (e.g., `NetArchTest.Rules`), not manual code review, to catch violations.