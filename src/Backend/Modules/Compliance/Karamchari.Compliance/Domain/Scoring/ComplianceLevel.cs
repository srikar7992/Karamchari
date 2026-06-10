// -----------------------------------------------------------------------
// <copyright file="ComplianceLevel.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Compliance.Domain.Scoring;

public enum ComplianceLevel
{
    Excellent,  // 90-100
    Good,       // 80-89
    Warning,    // 70-79
    Risk,       // 60-69
    Critical    // < 60
}
