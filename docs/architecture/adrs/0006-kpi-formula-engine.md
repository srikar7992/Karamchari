# ADR 0006 — KPI Formula Engine: Temporary Roslyn → Planned AST DSL

- **Status:** Accepted (with planned migration)
- **Date:** 2026-05-08
- **Deciders:** Solo founder

## Context

KPI definitions require a formula engine that lets HR administrators express metric
calculations such as `(AttendedDays / WorkingDays) * 100` without deploying new code.
Two approaches were evaluated:

1. **Roslyn scripting** (`Microsoft.CodeAnalysis.CSharp.Scripting`) with an allowlist of
   permitted identifiers and no `using` directives outside the allowlist.
2. **Declarative AST-based DSL** with a custom parser, limited operator set, and no
   runtime compilation.

## Decision

**Phase 1 (now):** Roslyn scripting with allowlist and expression sandboxing.
**Phase 2 (before Year 2 / before tenant-authored formulas go live in production):**
Migrate to a declarative AST DSL.

The Roslyn path is temporary, used only for internal HR-configured formulas during the
development period. Tenant-authored formulas must NOT go live on the Roslyn engine.

## Rationale for Phase 1

- Roslyn scripting is available today, requires no custom parser, and can be incrementally
  hardened via allowlists.
- Internal HR formulas are trusted inputs — the blast radius is limited.
- Implementing a production-grade DSL parser at this stage would block feature delivery.

## Rationale for Phase 2 Migration

- Even with allowlists, Roslyn scripting carries risks at tenant scale:
  - Script caching is non-trivial; memory pressure from many unique formula strings.
  - Runaway expression evaluation (e.g., deeply recursive property chains).
  - Sandbox escape via reflection or type coercion edge cases in future Roslyn versions.
  - Audit complexity — "what code ran" is harder to explain than "what AST was evaluated."
- A declarative DSL with `+`, `-`, `*`, `/`, `%`, `()`, named variable references, and
  `IF(condition, then, else)` covers >95% of real KPI formulas with zero runtime compilation.

## Allowed DSL Operators (Phase 2 Target)

```
expr     := term (('+' | '-') term)*
term     := factor (('*' | '/' | '%') factor)*
factor   := '(' expr ')' | number | variable | if_expr
if_expr  := 'IF' '(' expr ',' expr ',' expr ')'
variable := [A-Za-z_][A-Za-z0-9_]*
number   := [0-9]+ ('.' [0-9]+)?
```

Variables are resolved from a `IKPIFormulaContext` that exposes only the data
dimensions explicitly registered for the KPI definition. No reflection, no assembly
loading, no type system access.

## Migration Path

1. Build the AST parser alongside Roslyn (feature-flagged per tenant).
2. Shadow-run both engines on staging, compare results.
3. Hard-cutover for new tenants. Existing tenants migrate formulas at next review cycle.
4. Remove Roslyn dependency.

## Consequences

- The Roslyn engine must be behind a feature flag from day one.
  Toggling `KpiFormula:Engine=Roslyn|Dsl` in configuration controls which engine runs.
- Formula strings stored in `KPIDefinition.FormulaExpression` are engine-agnostic text.
  Both engines parse the same surface syntax, so stored formulas are portable.
- No tenant should have access to author formulas until the DSL engine is in place.
