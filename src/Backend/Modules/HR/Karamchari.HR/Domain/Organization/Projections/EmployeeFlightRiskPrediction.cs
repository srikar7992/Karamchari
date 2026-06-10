// -----------------------------------------------------------------------
// <copyright file="EmployeeFlightRiskPrediction.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Karamchari.HR.Domain.Organization.Projections;

/// <summary>
/// Read model representing the flight risk prediction score and drivers computed for an employee.
/// </summary>
public sealed class EmployeeFlightRiskPrediction
{
    /// <summary>Gets or sets the tenant identifier associated with the employee.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique identifier of the employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Gets or sets the overall flight risk score, typically between 0.05 and 0.95.</summary>
    public decimal RiskScore { get; set; }

    /// <summary>Gets or sets the timestamp of when this prediction was computed in UTC.</summary>
    public DateTimeOffset LastUpdatedUtc { get; set; }

    /// <summary>Gets or sets the collection of contributing flight risk drivers.</summary>
    public List<FlightRiskDriverProjection> Drivers { get; set; } = [];
}

