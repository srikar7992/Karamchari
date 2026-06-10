// -----------------------------------------------------------------------
// <copyright file="ExpenseCategory.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.FinancialOps.Contracts.Reimbursements;

namespace Karamchari.FinancialOps.Domain.ExpensePolicy;

public enum MileageUnit { Kilometers, Miles }

/// <summary>
/// Tenant-configured expense category with spending policy caps.
/// ExpenseClaim.CategoryId references this entity's Id.
/// Policy is evaluated at claim submission time; policyVersion is snapshotted on the claim.
/// </summary>
public sealed class ExpenseCategory : AggregateRoot<ExpenseCategoryId>, ITenantOwned
{
    private ExpenseCategory(
        ExpenseCategoryId id,
        string tenantId,
        string name,
        string? description,
        decimal? maxSingleClaimAmount,
        decimal? dailyCapAmount,
        decimal? monthlyCapAmount,
        bool requiresReceipt,
        decimal? receiptRequiredAbove,
        bool requiresApproval,
        decimal? approvalRequiredAbove,
        bool isMileage,
        decimal? mileageRatePerUnit,
        MileageUnit mileageUnit)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        MaxSingleClaimAmount = maxSingleClaimAmount;
        DailyCapAmount = dailyCapAmount;
        MonthlyCapAmount = monthlyCapAmount;
        RequiresReceipt = requiresReceipt;
        ReceiptRequiredAbove = receiptRequiredAbove;
        RequiresApproval = requiresApproval;
        ApprovalRequiredAbove = approvalRequiredAbove;
        IsMileage = isMileage;
        MileageRatePerUnit = mileageRatePerUnit;
        MileageUnit = mileageUnit;
        IsActive = true;
        Version = 1;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private ExpenseCategory() { TenantId = string.Empty; Name = string.Empty; }

    public string TenantId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    // Per-claim cap
    public decimal? MaxSingleClaimAmount { get; private set; }

    // Aggregate caps — enforced at claim submission by comparing against employee's period totals
    public decimal? DailyCapAmount { get; private set; }
    public decimal? MonthlyCapAmount { get; private set; }

    // Receipt policy
    public bool RequiresReceipt { get; private set; }
    public decimal? ReceiptRequiredAbove { get; private set; }

    // Approval policy
    public bool RequiresApproval { get; private set; }
    public decimal? ApprovalRequiredAbove { get; private set; }

    // Mileage-specific
    public bool IsMileage { get; private set; }
    public decimal? MileageRatePerUnit { get; private set; }
    public MileageUnit MileageUnit { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Monotonically incremented on each policy change. Snapshotted on ExpenseClaim for audit.
    /// </summary>
    public int Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static ExpenseCategory Create(
        string tenantId,
        string name,
        string? description = null,
        decimal? maxSingleClaimAmount = null,
        decimal? dailyCapAmount = null,
        decimal? monthlyCapAmount = null,
        bool requiresReceipt = false,
        decimal? receiptRequiredAbove = null,
        bool requiresApproval = true,
        decimal? approvalRequiredAbove = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ExpenseCategory(
            new ExpenseCategoryId(Guid.NewGuid()),
            tenantId, name, description,
            maxSingleClaimAmount, dailyCapAmount, monthlyCapAmount,
            requiresReceipt, receiptRequiredAbove,
            requiresApproval, approvalRequiredAbove,
            false, null, MileageUnit.Kilometers);
    }

    public static ExpenseCategory CreateMileage(
        string tenantId,
        string name,
        decimal ratePerUnit,
        MileageUnit unit = MileageUnit.Kilometers,
        decimal? monthlyCapAmount = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ExpenseCategory(
            new ExpenseCategoryId(Guid.NewGuid()),
            tenantId, name, $"Mileage reimbursement at {ratePerUnit} per {unit}",
            null, null, monthlyCapAmount,
            false, null,
            true, null,
            true, ratePerUnit, unit);
    }

    /// <summary>
    /// Updates spending caps. Bumps Version so existing claims retain their snapshot.
    /// </summary>
    public void UpdateCaps(decimal? maxSingle, decimal? daily, decimal? monthly)
    {
        MaxSingleClaimAmount = maxSingle;
        DailyCapAmount = daily;
        MonthlyCapAmount = monthly;
        BumpVersion();
    }

    public void UpdateReceiptPolicy(bool requiresReceipt, decimal? requiredAbove)
    {
        RequiresReceipt = requiresReceipt;
        ReceiptRequiredAbove = requiredAbove;
        BumpVersion();
    }

    public void UpdateApprovalPolicy(bool requiresApproval, decimal? requiredAbove)
    {
        RequiresApproval = requiresApproval;
        ApprovalRequiredAbove = requiredAbove;
        BumpVersion();
    }

    public void UpdateMileageRate(decimal ratePerUnit)
    {
        if (!IsMileage) throw new InvalidOperationException("Category is not a mileage category.");
        MileageRatePerUnit = ratePerUnit;
        BumpVersion();
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;

    /// <summary>
    /// Returns whether a claim amount would violate the single-claim cap.
    /// </summary>
    public bool ExceedsSingleClaimCap(decimal amount) =>
        MaxSingleClaimAmount.HasValue && amount > MaxSingleClaimAmount.Value;

    /// <summary>
    /// Returns whether receipt is required for the given amount.
    /// </summary>
    public bool IsReceiptRequired(decimal amount) =>
        RequiresReceipt || (ReceiptRequiredAbove.HasValue && amount > ReceiptRequiredAbove.Value);

    /// <summary>
    /// Returns whether manager approval is required for the given amount.
    /// </summary>
    public bool IsApprovalRequired(decimal amount) =>
        RequiresApproval || (ApprovalRequiredAbove.HasValue && amount > ApprovalRequiredAbove.Value);

    private void BumpVersion()
    {
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
