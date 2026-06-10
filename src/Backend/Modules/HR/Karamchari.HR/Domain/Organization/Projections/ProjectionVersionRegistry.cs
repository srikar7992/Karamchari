// -----------------------------------------------------------------------
// <copyright file="ProjectionVersionRegistry.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Karamchari.HR.Domain.Organization.Projections;

public sealed class ProjectionVersionRegistry
{
    public string ProjectionName { get; set; } = string.Empty;
    public string ActiveVersion { get; set; } = string.Empty; // "A" or "B"
    public DateTimeOffset UpdatedUtc { get; set; }
}
