// -----------------------------------------------------------------------
// <copyright file="IntelligenceEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Intelligence.Contracts;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record StaleIntelligenceAlertEvent(
    Guid SignalId,
    string TenantId,
    string SignalType,
    string SubjectId,
    DateTimeOffset LastGeneratedAtUtc,
    string WarningMessage);
