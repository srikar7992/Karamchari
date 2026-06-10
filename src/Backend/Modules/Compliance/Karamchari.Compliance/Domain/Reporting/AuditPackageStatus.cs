// -----------------------------------------------------------------------
// <copyright file="AuditPackageStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Compliance.Domain.Reporting;

public enum AuditPackageStatus
{
    Pending,
    Generating,
    Ready,
    Expired,
    Failed
}
