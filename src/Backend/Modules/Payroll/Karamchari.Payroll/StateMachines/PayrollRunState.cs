// -----------------------------------------------------------------------
// <copyright file="PayrollRunState.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MassTransit;

namespace Karamchari.Payroll.StateMachines;

/// <summary>
/// Represents the persistent state of a payroll run saga.
/// </summary>
public class PayrollRunState : SagaStateMachineInstance
{
    /// <summary>
    /// Gets or sets the unique correlation identifier for the saga instance.
    /// </summary>
    public Guid CorrelationId { get; set; } // The MassTransit primary key
    /// <summary>
    /// Gets or sets the current state of the saga (e.g., Calculating, Completed).
    /// </summary>
    public string CurrentState { get; set; } = null!;

    // Business Data
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = null!;
    /// <summary>
    /// Gets or sets the name of the payroll period.
    /// </summary>
    public string PeriodName { get; set; } = null!; // e.g., "March 2027"
    /// <summary>
    /// Gets or sets the date and time the payroll run was started.
    /// </summary>
    public DateTime StartedAt { get; set; }
    /// <summary>
    /// Gets or sets the date and time the payroll run was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // Trackers for distributed workers
    /// <summary>
    /// Gets or sets the total number of employees to be processed in this run.
    /// </summary>
    public int TotalEmployeesToProcess { get; set; }
    /// <summary>
    /// Gets or sets the total number of employees already processed.
    /// </summary>
    public int ProcessedEmployees { get; set; }

    // Summary Totals
    /// <summary>Gets or sets the total gross payout.</summary>
    public decimal TotalGross { get; set; }
    /// <summary>Gets or sets the total net payout.</summary>
    public decimal TotalNet { get; set; }

    // Audit Info
    /// <summary>Gets or sets the user who locked the run.</summary>
    public string? LockedBy { get; set; }
    /// <summary>Gets or sets the date and time the run was locked.</summary>
    public DateTime? LockedAt { get; set; }
}
