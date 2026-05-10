# Phase X: Platform Forensic Analysis

**Date**: May 11, 2026
**Status**: ACTIVE

## Executive Summary
Karamchari has evolved into a highly capable but conceptually fragmented ecosystem. Modules like Payroll, Performance, and HR have implemented localized solutions for orchestration, state transitions, and approvals. This forensic analysis identifies areas of duplication that must be converged into a unified platform runtime.

## 1. Duplicated Workflow & Approval Logic
**Observation**: We currently have multiple isolated implementations of approvals and state transitions.
*   `Karamchari.Workflow` module implements a metadata-driven approval workflow (`WorkflowDefinition`, `WorkflowInstance`, `WorkflowStep`).
*   `Karamchari.Core.Domain.Workflows.ApprovalStep` implements an isolated value-object approval chain.
*   `Karamchari.Performance.Domain.Promotions.PromotionApprovalStage` hardcodes approval chains into enums.
*   `Karamchari.Payroll.StateMachines` uses MassTransit Sagas (`PayrollRunStateMachine`, `FnFSettlementStateMachine`) to orchestrate complex domain-specific processes that should ideally be represented as generic workflow transitions.

**Impact**: 
*   Inconsistent audit trails.
*   Inability to build a single "My Approvals" inbox for users.
*   High engineering cost to add workflow capabilities to new features (e.g., Leave requests).

## 2. Shared Capability Matrix
| Capability | Current State | Target State |
| :--- | :--- | :--- |
| **Orchestration** | Fragmented (MassTransit Sagas, Code-behind) | Canonical Workflow Runtime |
| **Approvals** | Module-specific (Enums, Value Objects) | Unified `WorkflowInstance` |
| **State Mgt** | Hardcoded Status enums in Entities | Dynamic `WorkflowState` |
| **Notifications** | Direct MassTransit publishing | Event-driven Notification Rules |
| **SLAs/Escalations**| Non-existent / Hardcoded | Configurable `SLAContract` |

## 3. Service Boundary Violations
Certain modules are highly coupled to workflow orchestration logic rather than pure domain logic.
*   **Payroll**: Hardcodes state machines for Corrections, Disbursements, and FnF. These should be decoupled; Payroll should calculate pay, while the platform orchestrates the transitions.
*   **Performance**: Hardcodes promotion stages (`Manager`, `HR`, `Leadership`). This prevents tenant-specific customizations (e.g., adding a `Finance` approval step).

## 4. Candidate Extraction Strategy
To achieve consolidation without breaking existing functionality, we will:
1.  Elevate `Karamchari.Workflow` to be the **sole** canonical engine for approvals and state transitions.
2.  Deprecate localized approval models (`ApprovalStep.cs`, `PromotionApprovalStage.cs`).
3.  Migrate MassTransit domain sagas to trigger generic `WorkflowTransition` events, allowing the unified runtime to handle orchestration, SLA tracking, and audit logging.
