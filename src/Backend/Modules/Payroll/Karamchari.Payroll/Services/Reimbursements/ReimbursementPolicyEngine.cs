// -----------------------------------------------------------------------
// <copyright file="ReimbursementPolicyEngine.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Domain.Reimbursements;

namespace Karamchari.Payroll.Services.Reimbursements;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record PolicyCheckResult(
    bool IsAllowed,
    decimal PolicyLimit,
    ReimbursementTaxability Taxability,
    string? ViolationReason,
    FraudIndicatorLevel FraudIndicator);

/// <summary>
/// Evaluates reimbursement claims against tenant policy rules.
/// Detects duplicate receipts, policy violations, and fraud indicators.
/// </summary>
public sealed class ReimbursementPolicyEngine
{
    // Policy limits per category per month â€” in production: load from tenant config
    private static readonly Dictionary<ReimbursementCategory, decimal> DefaultLimits = new()
    {
        [ReimbursementCategory.Travel] = 50_000m,
        [ReimbursementCategory.Meal] = 3_000m,
        [ReimbursementCategory.Internet] = 1_000m,
        [ReimbursementCategory.Fuel] = 5_000m,
        [ReimbursementCategory.Mobile] = 1_500m,
        [ReimbursementCategory.Relocation] = 1_00_000m,
        [ReimbursementCategory.FlexibleBenefit] = 10_000m,
        [ReimbursementCategory.Medical] = 15_000m,
        [ReimbursementCategory.Training] = 20_000m,
        [ReimbursementCategory.Other] = 5_000m
    };

    private static readonly Dictionary<ReimbursementCategory, ReimbursementTaxability> DefaultTaxability = new()
    {
        [ReimbursementCategory.Travel] = ReimbursementTaxability.Exempt,
        [ReimbursementCategory.Meal] = ReimbursementTaxability.Exempt,
        [ReimbursementCategory.Internet] = ReimbursementTaxability.Exempt,
        [ReimbursementCategory.Fuel] = ReimbursementTaxability.Exempt,
        [ReimbursementCategory.Mobile] = ReimbursementTaxability.Exempt,
        [ReimbursementCategory.Relocation] = ReimbursementTaxability.Taxable,
        [ReimbursementCategory.FlexibleBenefit] = ReimbursementTaxability.PartiallyTaxable,
        [ReimbursementCategory.Medical] = ReimbursementTaxability.Exempt,
        [ReimbursementCategory.Training] = ReimbursementTaxability.Exempt,
        [ReimbursementCategory.Other] = ReimbursementTaxability.Taxable
    };

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static PolicyCheckResult Evaluate(
        ReimbursementCategory category,
        decimal claimedAmount,
        string? attachmentHash,
        IEnumerable<string?> existingHashesForEmployee)
    {
        DefaultLimits.TryGetValue(category, out var limit);
        DefaultTaxability.TryGetValue(category, out var taxability);

        // Duplicate receipt detection
        if (!string.IsNullOrWhiteSpace(attachmentHash)
            && existingHashesForEmployee.Any(h => h == attachmentHash))
        {
            return new PolicyCheckResult(
                false, limit, taxability,
                "Duplicate receipt detected â€” same attachment hash already submitted.",
                FraudIndicatorLevel.High);
        }

        // Policy limit check
        if (claimedAmount > limit)
        {
            return new PolicyCheckResult(
                false, limit, taxability,
                $"Claimed amount {claimedAmount:C} exceeds policy limit {limit:C} for {category}.",
                FraudIndicatorLevel.None);
        }

        // Fraud indicator: suspiciously round amounts
        var fraudLevel = FraudIndicatorLevel.None;
        if (claimedAmount > 0 && claimedAmount % 1000 == 0 && claimedAmount > 10_000)
            fraudLevel = FraudIndicatorLevel.Low;

        return new PolicyCheckResult(true, limit, taxability, null, fraudLevel);
    }
}
