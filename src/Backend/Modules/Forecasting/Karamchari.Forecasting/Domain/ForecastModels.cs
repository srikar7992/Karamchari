// -----------------------------------------------------------------------
// <copyright file="ForecastModels.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Forecasting.Domain;

/// <summary>
/// Daily snapshot of projected financial outcomes.
/// </summary>
public sealed class ForecastMetrics
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>The date for which the projection applies.</summary>
    public DateOnly Date { get; init; }

    /// <summary>Expected revenue from approved but unbilled work (Accrual basis).</summary>
    public decimal ProjectedRevenue { get; set; }

    /// <summary>Expected cash collection based on outstanding invoices and probability (Cash basis).</summary>
    public decimal ProjectedCash { get; set; }

    /// <summary>Total outstanding AR at this point in time.</summary>
    public decimal OutstandingAmount { get; set; }

    /// <summary>Amount considered at high risk (90+ days or low probability clients).</summary>
    public decimal RiskAmount { get; set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Behavioral profile for a client, used to adjust collection probabilities.
/// </summary>
public sealed class ClientPaymentProfile
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ClientId { get; init; }

    /// <summary>Average days between Invoice Finalization and Full Payment.</summary>
    public int AverageDaysToPay { get; set; }

    /// <summary>Percentage of invoices paid within 30 days.</summary>
    public decimal OnTimeRate { get; set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int TotalInvoicesPaid { get; set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal GetCollectionProbability(int daysOutstanding)
    {
        // Adjust baseline probability based on client behavior
        var baseProb = daysOutstanding switch
        {
            <= 30 => 0.95m,
            <= 60 => 0.75m,
            <= 90 => 0.50m,
            _ => 0.20m
        };

        // If client is consistently late, penalize the probability
        if (AverageDaysToPay > 60 && daysOutstanding < 60)
            baseProb *= 0.8m;

        return baseProb;
    }
}
