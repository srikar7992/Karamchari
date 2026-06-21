// -----------------------------------------------------------------------
// <copyright file="IntelligenceEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Contracts;
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

/// <summary>
/// Published when any source — burnout engine, attrition engine, external AI, performance engine —
/// detects a workforce risk signal for an employee.
/// Intelligence module consumes this event regardless of source, keeping risk detection decoupled.
/// </summary>
public sealed record RiskSignalRaisedIntegrationEventV1(
    string TenantId,
    Guid EmployeeId,
    string SignalType,
    decimal SignalValue,
    DateOnly SignalDate,
    string SourceModule,
    Guid CorrelationId) : IIntegrationEvent;
