// -----------------------------------------------------------------------
// <copyright file="PromotionStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.Promotions;

/// <summary>
/// Authoritative business status for promotion recommendations.
/// </summary>
public enum PromotionStatus
{
    /// <summary>Recommendation is being drafted.</summary>
    Draft = 1,

    /// <summary>Recommendation has been approved and is authoritative truth.</summary>
    Approved = 2,

    /// <summary>Recommendation was rejected.</summary>
    Rejected = 3,

    /// <summary>Recommendation was withdrawn by the nominator.</summary>
    Withdrawn = 4
}
