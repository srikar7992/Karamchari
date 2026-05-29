# Operational and Utility Scripts Directory

This directory contains internal operational, setup, and diagnostics scripts used by the main entry workflows and pipelines.

---

## 1. Main Entrypoint Workflows (Root Level)
For standard operations, developers should invoke the root-level scripts:
*   [setup-local.sh](file:///Users/srikarbojji/Projects/Karamchari/setup-local.sh) / [setup-local.ps1](file:///Users/srikarbojji/Projects/Karamchari/setup-local.ps1): Complete system bootstrap.
*   [verify-local.sh](file:///Users/srikarbojji/Projects/Karamchari/verify-local.sh) / [verify-local.ps1](file:///Users/srikarbojji/Projects/Karamchari/verify-local.ps1): Ports and API smoke validation.
*   [run-all-tests.sh](file:///Users/srikarbojji/Projects/Karamchari/run-all-tests.sh) / [run-all-tests.ps1](file:///Users/srikarbojji/Projects/Karamchari/run-all-tests.ps1): Test suite executor.

---

## 2. Reclaimed Sub-Scripts Directory Index

### Setup Sub-Scripts
-   [scripts/setup/setup.ps1](file:///Users/srikarbojji/Projects/Karamchari/scripts/setup/setup.ps1): The secondary bootstrap runner executing compile steps, tenant migrations, and DB provisioning. Called directly by `setup-local.ps1`.
-   [scripts/setup/EnvironmentValidator.ps1](file:///Users/srikarbojji/Projects/Karamchari/scripts/setup/EnvironmentValidator.ps1): Utility validating port binds, Docker daemon reactivity, and the active .NET SDK framework level.

### Teardown & Reset
-   [scripts/teardown/reset-local.ps1](file:///Users/srikarbojji/Projects/Karamchari/scripts/teardown/reset-local.ps1): Powers down compose containers and destroys persistent data volumes (permits restarting from a fresh, zero-state database).

### Diagnostics & Chaos
-   [scripts/diagnostics/diagnose-runtime.ps1](file:///Users/srikarbojji/Projects/Karamchari/scripts/diagnostics/diagnose-runtime.ps1): Evaluates connection metrics for SQL Server, Redis cache pool, and MassTransit.
-   [scripts/chaos/run-chaos.ps1](file:///Users/srikarbojji/Projects/Karamchari/scripts/chaos/run-chaos.ps1): Simulates DB deadlocks, Redis service drops, and network latency storms while validating system idempotency.

### Developer Utilities
-   [scripts/add_xml_docs.ps1](file:///Users/srikarbojji/Projects/Karamchari/scripts/add_xml_docs.ps1): Utility recursively appending triple-slash XML summary stubs (`/// <summary>`) above undocumented public classes, interfaces, and methods.
