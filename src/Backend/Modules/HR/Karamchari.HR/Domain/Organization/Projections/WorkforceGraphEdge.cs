// -----------------------------------------------------------------------
// <copyright file="WorkforceGraphEdge.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Karamchari.HR.Domain.Organization.Projections;

public sealed class WorkforceGraphEdge
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid SourceNodeId { get; set; }
    public Guid TargetNodeId { get; set; }
    public WorkforceEdgeType Type { get; set; }
    public decimal? Weight { get; set; }
    public DateTimeOffset? EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}
