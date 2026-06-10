// -----------------------------------------------------------------------
// <copyright file="LeaveProjections.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Contracts.Projections;

/// <summary>
/// Summary of leave and loss-of-pay for a specific period, consumed by Payroll.
/// </summary>
public sealed record LeavePayrollSummaryProjection(
    Guid EmployeeId,
    string PeriodName,
    decimal PaidLeaveDays,
    decimal UnpaidLeaveDays,
    decimal LossOfPayDays,
    decimal EncashedDays);

/// <summary>
/// View of an employee's current leave balances.
/// </summary>
public sealed record LeaveBalanceProjection(
    Guid EmployeeId,
    Guid PolicyId,
    string PolicyName,
    decimal AvailableBalance,
    decimal ConsumedBalance,
    DateTimeOffset LastUpdatedAt);
