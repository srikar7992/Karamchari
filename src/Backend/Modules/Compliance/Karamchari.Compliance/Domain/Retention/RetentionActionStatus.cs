// -----------------------------------------------------------------------
// <copyright file="RetentionActionStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Compliance.Domain.Retention;

public enum RetentionActionStatus
{
    Scheduled,
    Executing,
    Completed,
    Skipped,     // legal hold prevented execution
    Failed
}
