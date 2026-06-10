// -----------------------------------------------------------------------
// <copyright file="FlightRiskDriverType.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Specifies the type of driver contributing to an employee's flight risk score.
/// </summary>
public enum FlightRiskDriverType
{
    /// <summary>Compensation increment is below the target threshold.</summary>
    LowCompRange,
    /// <summary>Employee has spent high tenure in the current role without transition.</summary>
    HighTenureInRole,
    /// <summary>Employee recently experienced a manager change.</summary>
    RecentManagerChange,
    /// <summary>Employee calibrated performance composite score is low.</summary>
    LowPerformanceTrend
}

