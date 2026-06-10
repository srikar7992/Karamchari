// -----------------------------------------------------------------------
// <copyright file="EmployeePayrollResult.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Domain.Results.Events;

namespace Karamchari.Payroll.Domain.Results;

/// <summary>
/// Per-employee immutable financial record for one payroll run.
/// Authoritative answer to "what was approved for employee X in period Y?"
///
/// Invariants enforced in domain code:
///   - GrossPay >= 0
///   - TotalDeductions >= 0
///   - NetPay >= 0
///   - Status transitions are one-way: Calculated → Approved → Locked
///   - No financial fields change after Status = Approved
///   - EmployeeId + PayrollRunId is unique (enforced by DB unique index + factory guard)
/// </summary>
public sealed class EmployeePayrollResult : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public string PeriodName { get; private set; } = string.Empty;
    public int Version { get; private set; }

    // Earnings breakdown — set once at Calculate(), never modified
    public decimal BasePay { get; private set; }
    public decimal OvertimePay { get; private set; }
    public decimal HolidayPay { get; private set; }
    public decimal ShiftPremium { get; private set; }
    public decimal AllowancePay { get; private set; }
    public decimal BonusPay { get; private set; }
    public decimal GrossPay { get; private set; }

    // Deductions breakdown — set once at Calculate(), never modified
    public decimal ProvidentFund { get; private set; }
    public decimal EmployeeStateInsurance { get; private set; }
    public decimal ProfessionalTax { get; private set; }
    public decimal TaxDeductedAtSource { get; private set; }
    public decimal OtherDeductions { get; private set; }
    public decimal TotalDeductions { get; private set; }

    // Adjustments — set once at Calculate(), never modified
    public decimal RetroAdjustments { get; private set; }
    public decimal ManualAdjustments { get; private set; }

    public decimal NetPay { get; private set; }

    // Lifecycle
    public EmployeePayrollResultStatus Status { get; private set; }
    public string SnapshotId { get; private set; } = string.Empty;
    public DateTimeOffset CalculatedAtUtc { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public DateTimeOffset? LockedAtUtc { get; private set; }

    // Optimistic concurrency — prevents simultaneous approve + rerun
    public byte[] RowVersion { get; private set; } = [];

    private EmployeePayrollResult() { }

    /// <summary>
    /// Creates and returns a new EmployeePayrollResult in Calculated status.
    /// Financial fields are set here and never change afterwards.
    /// </summary>
    public static EmployeePayrollResult Calculate(
        string tenantId,
        Guid payrollRunId,
        Guid employeeId,
        string employeeName,
        string periodName,
        int version,
        string snapshotId,
        EarningsBreakdown earnings,
        DeductionsBreakdown deductions,
        decimal retroAdjustments,
        decimal manualAdjustments)
    {
        // Invariant: no negative components
        if (earnings.Base < 0) throw new InvalidOperationException($"BasePay cannot be negative. Got {earnings.Base}");
        if (earnings.Overtime < 0) throw new InvalidOperationException($"OvertimePay cannot be negative. Got {earnings.Overtime}");
        if (deductions.ProvidentFund < 0) throw new InvalidOperationException($"PF cannot be negative. Got {deductions.ProvidentFund}");
        if (deductions.TaxDeductedAtSource < 0) throw new InvalidOperationException($"TDS cannot be negative. Got {deductions.TaxDeductedAtSource}");

        var gross = earnings.Base + earnings.Overtime + earnings.Holiday
                  + earnings.ShiftPremium + earnings.Allowance + earnings.Bonus;

        if (gross < 0) throw new InvalidOperationException($"GrossPay cannot be negative. Got {gross}");

        var totalDeductions = deductions.ProvidentFund + deductions.EmployeeStateInsurance
                            + deductions.ProfessionalTax + deductions.TaxDeductedAtSource + deductions.Other;

        if (totalDeductions < 0) throw new InvalidOperationException($"TotalDeductions cannot be negative. Got {totalDeductions}");

        var net = gross - totalDeductions + retroAdjustments + manualAdjustments;

        // Invariant: net pay must not go negative (employer cannot owe negative wages)
        if (net < 0)
            throw new InvalidOperationException(
                $"NetPay cannot be negative for employee {employeeId}. " +
                $"Gross={gross:F2}, Deductions={totalDeductions:F2}, Retro={retroAdjustments:F2}, Net={net:F2}");

        var result = new EmployeePayrollResult
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayrollRunId = payrollRunId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            PeriodName = periodName,
            Version = version,
            SnapshotId = snapshotId,
            BasePay = earnings.Base,
            OvertimePay = earnings.Overtime,
            HolidayPay = earnings.Holiday,
            ShiftPremium = earnings.ShiftPremium,
            AllowancePay = earnings.Allowance,
            BonusPay = earnings.Bonus,
            GrossPay = gross,
            ProvidentFund = deductions.ProvidentFund,
            EmployeeStateInsurance = deductions.EmployeeStateInsurance,
            ProfessionalTax = deductions.ProfessionalTax,
            TaxDeductedAtSource = deductions.TaxDeductedAtSource,
            OtherDeductions = deductions.Other,
            TotalDeductions = totalDeductions,
            RetroAdjustments = retroAdjustments,
            ManualAdjustments = manualAdjustments,
            NetPay = net,
            Status = EmployeePayrollResultStatus.Calculated,
            CalculatedAtUtc = DateTimeOffset.UtcNow,
        };

        result.RaiseDomainEvent(new EmployeePayrollCalculatedEvent(
            result.Id, tenantId, payrollRunId, employeeId, gross, net));

        return result;
    }

    /// <summary>
    /// Transitions to Approved. Financial fields are now permanently frozen.
    /// Any attempt to call Calculate() again for the same employee+run must produce
    /// a new aggregate (new run version), not mutate this one.
    /// </summary>
    public void Approve(string approvedBy)
    {
        if (Status != EmployeePayrollResultStatus.Calculated)
            throw new InvalidOperationException(
                $"Cannot approve result in status {Status}. Expected Calculated. " +
                $"Employee={EmployeeId}, Run={PayrollRunId}");

        if (string.IsNullOrWhiteSpace(approvedBy))
            throw new ArgumentException("ApprovedBy must not be empty.");

        Status = EmployeePayrollResultStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new EmployeePayrollApprovedEvent(Id, TenantId, PayrollRunId, EmployeeId, approvedBy));
    }

    /// <summary>
    /// Transitions to Locked. Locked results are permanently read-only.
    /// No financial field, approval metadata, or status can change after this point.
    /// </summary>
    public void Lock()
    {
        if (Status == EmployeePayrollResultStatus.Locked) return;

        if (Status != EmployeePayrollResultStatus.Approved)
            throw new InvalidOperationException(
                $"Cannot lock result in status {Status}. Expected Approved. " +
                $"Employee={EmployeeId}, Run={PayrollRunId}");

        Status = EmployeePayrollResultStatus.Locked;
        LockedAtUtc = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new EmployeePayrollLockedEvent(Id, TenantId, PayrollRunId, EmployeeId));
    }
}

public sealed record EarningsBreakdown(
    decimal Base,
    decimal Overtime,
    decimal Holiday,
    decimal ShiftPremium,
    decimal Allowance,
    decimal Bonus);

public sealed record DeductionsBreakdown(
    decimal ProvidentFund,
    decimal EmployeeStateInsurance,
    decimal ProfessionalTax,
    decimal TaxDeductedAtSource,
    decimal Other);
