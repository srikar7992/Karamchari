// -----------------------------------------------------------------------
// <copyright file="EmployeeDtos.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Api.BFF.Employee;

// â”€â”€ My Goals â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record MyGoalsResponse(
    IReadOnlyList<MyGoalItem> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record MyGoalItem(
    Guid GoalId,
    Guid CycleId,
    string Title,
    string? Description,
    string Type,
    decimal TargetValue,
    string? Unit,
    decimal CurrentProgress,
    decimal? FinalScore,
    string Status,
    decimal Weight);

// â”€â”€ My Reviews â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record MyReviewsResponse(
    IReadOnlyList<ReviewInboxItemDto> Pending,
    int PendingCount);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ReviewInboxItemDto(
    Guid AssignmentId,
    Guid RevieweeEmployeeId,
    string RevieweeDisplayName,
    string CycleName,
    string ReviewerRole,
    DateTimeOffset Deadline,
    bool IsOverdue,
    int Priority);

// â”€â”€ My Skills â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record MySkillsResponse(
    IReadOnlyList<SkillInventoryItemDto> Items,
    int SkillsWithGap);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record SkillInventoryItemDto(
    Guid SkillId,
    string SkillName,
    string Category,
    string Domain,
    string CurrentProficiency,
    string? TargetProficiency,
    int? ProficiencyGap,
    DateTimeOffset LastAssessedOn);
