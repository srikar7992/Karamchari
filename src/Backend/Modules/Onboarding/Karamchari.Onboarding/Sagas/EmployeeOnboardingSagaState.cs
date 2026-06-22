// -----------------------------------------------------------------------
// <copyright file="EmployeeOnboardingSagaState.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Messaging.Sagas;

namespace Karamchari.Onboarding.Sagas;

/// <summary>
/// Persistent state for the EmployeeOnboarding saga (H-2 priority 1).
/// CorrelationId == EmployeeId. Tracks the two provisioning steps so the saga
/// only declares an employee fully onboarded when BOTH complete, and escalates
/// (Incomplete) on timeout instead of leaving a silently half-provisioned employee.
/// </summary>
public sealed class EmployeeOnboardingSagaState : SagaStateBase
{
    /// <summary>Whether Payroll has provisioned a profile for the employee.</summary>
    public bool PayrollProvisioned { get; set; }

    /// <summary>Whether Benefits enrollment has been activated for the employee.</summary>
    public bool BenefitsEnrolled { get; set; }

    /// <summary>Scheduled provisioning-deadline token id (for cancellation).</summary>
    public Guid? TimeoutTokenId { get; set; }
}
