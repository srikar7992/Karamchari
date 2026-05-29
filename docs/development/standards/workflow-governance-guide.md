# Workflow Governance Guide

## 1. Saga & Long-Running Process Readiness
- **No Implicit Workflows:** Multi-step processes (e.g., Disbursement, Final Settlement) must be explicitly modeled.
- **State Persistence:** Do not rely on "sleeping" background threads. Workflows must persist state to the database (e.g., `DisbursementBatchState`).
- **Compensation:** Every destructive action in a workflow must have a defined compensating action if the workflow fails downstream.

## 2. Workflow Orchestration Standards
- Use **MassTransit State Machines (Automatonymous)** for distributed orchestration.
- Use **ApprovalStep** Value Objects for sequential human approvals.
- Do NOT build giant procedural "Manager" services that orchestrate across domain boundaries via synchronous REST calls.

## 3. Timeout & Escalation
- All waiting states must have a defined `Timeout`.
- Upon timeout, workflows should either escalate (publish `EscalationRequiredEvent`) or compensate (abort).
