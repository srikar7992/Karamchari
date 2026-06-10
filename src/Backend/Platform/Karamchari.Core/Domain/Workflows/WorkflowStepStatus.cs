// -----------------------------------------------------------------------
// <copyright file="WorkflowStepStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Domain.Workflows;

/// <summary>
/// Canonical statuses for a single step within a workflow instance.
/// </summary>
public enum WorkflowStepStatus
{
    /// <summary>Step is waiting for action.</summary>
    Pending = 1,
    /// <summary>Step has been approved.</summary>
    Approved = 2,
    /// <summary>Step has been rejected.</summary>
    Rejected = 3,
    /// <summary>Step has been skipped (e.g. by condition or parallel completion).</summary>
    Skipped = 4
}
