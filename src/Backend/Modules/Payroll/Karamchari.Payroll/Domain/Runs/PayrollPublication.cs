// -----------------------------------------------------------------------
// <copyright file="PayrollPublication.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.Runs;

/// <summary>
/// Publication artifact created when payroll is published to bank/payment system.
/// Links the payroll run to the bank file for reconciliation.
/// </summary>
public sealed class PayrollPublication : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid Id { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public string PublishedBy { get; private set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; private set; }
    public string? BankFileId { get; private set; }
    public string? BankFileReference { get; private set; }
    public decimal TotalAmountDispatched { get; private set; }
    public int EmployeeCount { get; private set; }

    private PayrollPublication() { }

    public static PayrollPublication Create(
        string tenantId,
        Guid payrollRunId,
        string publishedBy,
        decimal totalAmount,
        int employeeCount,
        string? bankFileId = null,
        string? bankFileReference = null)
    {
        return new PayrollPublication
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayrollRunId = payrollRunId,
            PublishedBy = publishedBy,
            PublishedAt = DateTimeOffset.UtcNow,
            TotalAmountDispatched = totalAmount,
            EmployeeCount = employeeCount,
            BankFileId = bankFileId,
            BankFileReference = bankFileReference
        };
    }
}
