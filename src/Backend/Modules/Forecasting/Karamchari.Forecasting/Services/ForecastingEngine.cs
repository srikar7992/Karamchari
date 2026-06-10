// -----------------------------------------------------------------------
// <copyright file="ForecastingEngine.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Billing.Contracts;
using Karamchari.Forecasting.Domain;
using Karamchari.Forecasting.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Forecasting.Services;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ForecastSummary(
    decimal TotalUnbilledRevenue,
    decimal ExpectedCashNext30Days,
    decimal HighRiskAR,
    IReadOnlyList<MonthlyForecast> Trends);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record MonthlyForecast(
    string Month,
    decimal Revenue,
    decimal Cash);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class ForecastingEngine
{
    private readonly IBillingReadService _billingReadService;
    private readonly ForecastingDbContext _forecastDb;

    public ForecastingEngine(IBillingReadService billingReadService, ForecastingDbContext forecastDb)
    {
        _billingReadService = billingReadService;
        _forecastDb = forecastDb;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task<ForecastSummary> GetSummaryAsync(string tenantId, CancellationToken ct = default)
    {
        // 1. Revenue Forecast (Unbilled Billable Entries)
        var unbilledRevenue = await _billingReadService.GetUnbilledRevenueAsync(tenantId, ct);

        // 2. Cash Forecast (Probability-weighted Outstanding Invoices)
        var outstandingInvoices = await _billingReadService.GetOutstandingInvoicesAsync(tenantId, ct);

        var profiles = await _forecastDb.ClientPaymentProfiles
            .Where(p => p.TenantId == tenantId)
            .ToDictionaryAsync(p => p.ClientId, ct);

        decimal expectedCash = 0;
        decimal highRiskAr = 0;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var inv in outstandingInvoices)
        {
            var outstanding = inv.GrandTotal - inv.PaymentAmounts.Sum();
            var days = today.DayNumber - inv.PeriodEnd.DayNumber;

            profiles.TryGetValue(inv.ClientId, out var profile);
            var probability = profile?.GetCollectionProbability(days) ?? GetDefaultProbability(days);

            expectedCash += outstanding * probability;

            if (days > 90 || (profile != null && profile.AverageDaysToPay > 60))
            {
                highRiskAr += outstanding;
            }
        }

        // 3. Trends (Combine historic data + future projections)
        var trends = await GenerateTrendsAsync(tenantId, ct);

        return new ForecastSummary(unbilledRevenue, expectedCash, highRiskAr, trends);
    }

    private async Task<List<MonthlyForecast>> GenerateTrendsAsync(string tenantId, CancellationToken ct)
    {
        // Placeholder for real time-series logic
        return new List<MonthlyForecast>
        {
            new("Current", 1200000, 950000),
            new("Next Month", 1500000, 1100000)
        };
    }

    private decimal GetDefaultProbability(int days) => days switch
    {
        <= 30 => 0.95m,
        <= 60 => 0.75m,
        <= 90 => 0.50m,
        _ => 0.20m
    };
}
