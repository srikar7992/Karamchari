// -----------------------------------------------------------------------
// <copyright file="CalibrationBoardProjection.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Calibration;

namespace Karamchari.Performance.Domain.ReadModels;

/// <summary>
/// Denormalized read model for the calibration board UI.
/// One row per calibration session. Panel members, bucket distribution, and action
/// counts are all pre-computed â€” no session graph traversal at query time.
/// </summary>
public sealed class CalibrationBoardProjection : Entity<Guid>, ITenantOwned
{
    private CalibrationBoardProjection() { /* EF materialization */ }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid CalibrationSessionId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ReviewCycleId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string SessionName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CalibrationSessionStatus Status { get; private set; }

    // Distribution counts
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int TotalEntries { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int TopPerformerCount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int HighPerformerCount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int MeetsExpectationsCount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int BelowExpectationsCount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int UnderPerformerCount { get; private set; }

    // Panel activity
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int TotalPanelMembers { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int PanelMembersJoined { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int AdjustmentsMade { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int EntriesPendingFinalization { get; private set; }

    public DateTimeOffset? FinalizedOnUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset LastRefreshedUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static CalibrationBoardProjection Create(
        string tenantId,
        Guid calibrationSessionId,
        Guid reviewCycleId,
        string sessionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionName);
        return new CalibrationBoardProjection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CalibrationSessionId = calibrationSessionId,
            ReviewCycleId = reviewCycleId,
            SessionName = sessionName,
            Status = CalibrationSessionStatus.Draft,
            LastRefreshedUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Refresh(
        CalibrationSessionStatus status,
        int totalEntries,
        int topPerformerCount, int highPerformerCount, int meetsExpectationsCount,
        int belowExpectationsCount, int underPerformerCount,
        int totalPanelMembers, int panelMembersJoined,
        int adjustmentsMade, int entriesPendingFinalization,
        DateTimeOffset? finalizedOnUtc)
    {
        Status = status;
        TotalEntries = totalEntries;
        TopPerformerCount = topPerformerCount;
        HighPerformerCount = highPerformerCount;
        MeetsExpectationsCount = meetsExpectationsCount;
        BelowExpectationsCount = belowExpectationsCount;
        UnderPerformerCount = underPerformerCount;
        TotalPanelMembers = totalPanelMembers;
        PanelMembersJoined = panelMembersJoined;
        AdjustmentsMade = adjustmentsMade;
        EntriesPendingFinalization = entriesPendingFinalization;
        FinalizedOnUtc = finalizedOnUtc;
        LastRefreshedUtc = DateTimeOffset.UtcNow;
    }
}
