// -----------------------------------------------------------------------
// <copyright file="IRetroPayrollProcessor.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Domain.Adjustments;

namespace Karamchari.Payroll.Services.Calculation;

public sealed record AttendanceCorrectionEvent(
    Guid EmployeeId,
    string TenantId,
    DateOnly CorrectedDate,
    decimal OldHoursWorked,
    decimal NewHoursWorked,
    Guid SourcePayrollRunId,
    string CorrectedBy);

/// <summary>
/// Processes attendance corrections against closed payroll runs.
/// Never modifies closed payroll. Always produces RetroPayrollAdjustment.
/// Applied in the next open payroll run.
/// </summary>
public interface IRetroPayrollProcessor
{
    /// <summary>
    /// Calculates the monetary difference caused by an attendance correction on a closed period.
    /// Returns null if the correction has no financial impact.
    /// </summary>
    Task<RetroPayrollAdjustment?> ProcessCorrectionAsync(
        AttendanceCorrectionEvent correction,
        CancellationToken cancellationToken = default);
}
