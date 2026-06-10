// -----------------------------------------------------------------------
// <copyright file="SuccessionCoverageProjection.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Succession.Domain.Projections;

public sealed class SuccessionCoverageProjection : ITenantOwned
{
    public string TenantId { get; set; } = string.Empty;
    public Guid PositionId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public Guid? CurrentOccupantId { get; set; }
    public bool IsKeyPosition { get; set; }
    public int SuccessorCount { get; set; }
    public int ReadyNowCount { get; set; }
    public int ReadyNearTermCount { get; set; }
    public string VacancyRisk { get; set; } = "Low"; // Low, Medium, High, Critical
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}
