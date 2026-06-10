// -----------------------------------------------------------------------
// <copyright file="ViolationStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Compliance.Domain.Violations;

public enum ViolationStatus
{
    Open,
    Investigating,
    Resolved,
    AcceptedRisk
}
