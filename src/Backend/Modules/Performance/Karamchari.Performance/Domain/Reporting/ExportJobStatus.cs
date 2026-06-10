// -----------------------------------------------------------------------
// <copyright file="ExportJobStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.Reporting;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ExportJobStatus
{
    Queued = 1,
    Processing = 2,
    Ready = 3,
    Failed = 4,
    Cancelled = 5,
}
