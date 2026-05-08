using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Calibration;

namespace Karamchari.Performance.Domain.ReadModels;

/// <summary>
/// Denormalized read model for the calibration board UI.
/// One row per calibration session. Panel members, bucket distribution, and action
/// counts are all pre-computed — no session graph traversal at query time.
/// </summary>
public sealed class CalibrationBoardProjection : Entity<Guid>, ITenantOwned
{
    private CalibrationBoardProjection() { /* EF materialization */ }

    public string TenantId { get; private set; } = string.Empty;
    public Guid CalibrationSessionId { get; private set; }
    public Guid ReviewCycleId { get; private set; }
    public string SessionName { get; private set; } = string.Empty;
    public CalibrationSessionStatus Status { get; private set; }

    // Distribution counts
    public int TotalEntries { get; private set; }
    public int TopPerformerCount { get; private set; }
    public int HighPerformerCount { get; private set; }
    public int MeetsExpectationsCount { get; private set; }
    public int BelowExpectationsCount { get; private set; }
    public int UnderPerformerCount { get; private set; }

    // Panel activity
    public int TotalPanelMembers { get; private set; }
    public int PanelMembersJoined { get; private set; }
    public int AdjustmentsMade { get; private set; }
    public int EntriesPendingFinalization { get; private set; }

    public DateTimeOffset? FinalizedOnUtc { get; private set; }
    public DateTimeOffset LastRefreshedUtc { get; private set; }

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
