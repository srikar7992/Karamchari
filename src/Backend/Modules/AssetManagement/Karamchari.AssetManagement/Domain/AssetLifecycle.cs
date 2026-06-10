// -----------------------------------------------------------------------
// <copyright file="AssetLifecycle.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.AssetManagement.Domain;

public record MaintenanceRecordId(Guid Value);
public record DepreciationScheduleId(Guid Value);

public enum DepreciationMethod { StraightLine, DecliningBalance }
public enum MaintenanceType { Preventive, Corrective, Inspection, WarrantyService }
public enum MaintenanceStatus { Scheduled, InProgress, Completed, Cancelled }

// ── AssetMaintenanceRecord ────────────────────────────────────────────────────
/// <summary>
/// Tracks a single maintenance/service event on an asset.
/// Warranty expiry checked at scheduling time.
/// </summary>
public sealed class AssetMaintenanceRecord : Entity<MaintenanceRecordId>
{
    private AssetMaintenanceRecord() { Description = string.Empty; }

    public AssetMaintenanceRecord(MaintenanceRecordId id, AssetId assetId, MaintenanceType type,
        string description, DateOnly scheduledOn, DateOnly? warrantyExpiresOn,
        string? vendor, decimal? estimatedCost) : base(id)
    {
        AssetId = assetId;
        Type = type;
        Description = description;
        ScheduledOn = scheduledOn;
        WarrantyExpiresOn = warrantyExpiresOn;
        Vendor = vendor;
        EstimatedCost = estimatedCost;
        Status = MaintenanceStatus.Scheduled;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public AssetId AssetId { get; private set; } = null!;
    public MaintenanceType Type { get; private set; }
    public string Description { get; private set; }
    public DateOnly ScheduledOn { get; private set; }
    public DateOnly? CompletedOn { get; private set; }
    public DateOnly? WarrantyExpiresOn { get; private set; }
    public string? Vendor { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public decimal? ActualCost { get; private set; }
    public string? TechnicianNotes { get; private set; }
    public MaintenanceStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsUnderWarranty => WarrantyExpiresOn.HasValue && DateOnly.FromDateTime(DateTime.UtcNow) <= WarrantyExpiresOn.Value;

    public void StartWork()
    {
        if (Status != MaintenanceStatus.Scheduled) throw new InvalidOperationException("Only scheduled maintenance can start.");
        Status = MaintenanceStatus.InProgress;
    }

    public void Complete(DateOnly completedOn, decimal actualCost, string? notes)
    {
        if (Status != MaintenanceStatus.InProgress) throw new InvalidOperationException("Only in-progress maintenance can complete.");
        CompletedOn = completedOn;
        ActualCost = actualCost;
        TechnicianNotes = notes;
        Status = MaintenanceStatus.Completed;
    }

    public void Cancel() => Status = MaintenanceStatus.Cancelled;
}

// ── AssetDepreciationSchedule ─────────────────────────────────────────────────
/// <summary>
/// Financial depreciation schedule for an asset.
/// Tracks book value over time using configured method.
/// </summary>
public sealed class AssetDepreciationSchedule : AggregateRoot<DepreciationScheduleId>, ITenantOwned
{
    private readonly List<DepreciationPeriodEntry> _periods = [];

    private AssetDepreciationSchedule() { TenantId = string.Empty; }

    private AssetDepreciationSchedule(DepreciationScheduleId id, string tenantId, AssetId assetId,
        decimal purchaseValue, decimal residualValue, int usefulLifeYears,
        DepreciationMethod method, DateOnly startDate) : base(id)
    {
        TenantId = tenantId;
        AssetId = assetId;
        PurchaseValue = purchaseValue;
        ResidualValue = residualValue;
        UsefulLifeYears = usefulLifeYears;
        Method = method;
        StartDate = startDate;
        CurrentBookValue = purchaseValue;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public AssetId AssetId { get; private set; } = null!;
    public decimal PurchaseValue { get; private set; }
    public decimal ResidualValue { get; private set; }
    public int UsefulLifeYears { get; private set; }
    public DepreciationMethod Method { get; private set; }
    public DateOnly StartDate { get; private set; }
    public decimal CurrentBookValue { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<DepreciationPeriodEntry> Periods => _periods.AsReadOnly();

    public static AssetDepreciationSchedule Create(string tenantId, AssetId assetId,
        decimal purchaseValue, decimal residualValue, int usefulLifeYears,
        DepreciationMethod method, DateOnly startDate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(purchaseValue);
        if (residualValue < 0 || residualValue >= purchaseValue) throw new ArgumentOutOfRangeException(nameof(residualValue));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(usefulLifeYears);

        return new AssetDepreciationSchedule(new DepreciationScheduleId(Guid.NewGuid()), tenantId,
            assetId, purchaseValue, residualValue, usefulLifeYears, method, startDate);
    }

    /// <summary>Compute and record depreciation for a given fiscal year.</summary>
    public DepreciationPeriodEntry RecordPeriod(int year)
    {
        if (_periods.Any(p => p.Year == year))
            throw new InvalidOperationException($"Period {year} already recorded.");

        var depreciableBase = PurchaseValue - ResidualValue;
        decimal amount = Method switch
        {
            DepreciationMethod.StraightLine => depreciableBase / UsefulLifeYears,
            DepreciationMethod.DecliningBalance => CurrentBookValue * (2m / UsefulLifeYears),
            _ => throw new NotSupportedException(),
        };

        // Never depreciate below residual value
        amount = Math.Min(amount, CurrentBookValue - ResidualValue);
        amount = Math.Max(amount, 0);

        CurrentBookValue -= amount;
        var entry = new DepreciationPeriodEntry(AssetId, year, amount, CurrentBookValue);
        _periods.Add(entry);
        return entry;
    }

    public decimal AccumulatedDepreciation => PurchaseValue - CurrentBookValue;
}

public sealed class DepreciationPeriodEntry
{
    public DepreciationPeriodEntry() { }

    public DepreciationPeriodEntry(AssetId assetId, int year, decimal amount, decimal bookValueAfter)
    {
        AssetId = assetId;
        Year = year;
        Amount = amount;
        BookValueAfter = bookValueAfter;
        RecordedAt = DateTimeOffset.UtcNow;
    }

    public AssetId AssetId { get; private set; } = null!;
    public int Year { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BookValueAfter { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
}
