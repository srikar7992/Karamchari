// -----------------------------------------------------------------------
// <copyright file="PayrollProvisioningEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Contracts;

namespace Karamchari.Payroll.Contracts;

/// <summary>
/// Published by Payroll once a draft payroll profile exists for a newly onboarded
/// employee. Consumed by the EmployeeOnboarding saga as the "payroll provisioned"
/// completion step. Idempotent: emitted even when the profile already existed.
/// </summary>
public record PayrollProfileProvisionedIntegrationEventV1(
    Guid EmployeeId,
    string TenantId) : IIntegrationEvent;
