// -----------------------------------------------------------------------
// <copyright file="BudgetStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Compensation.Domain;

/// <summary>Lifecycle status of an increment budget pool.</summary>
public enum BudgetStatus
{
    Draft = 1,
    Approved = 2,
    Allocated = 3,
    Closed = 4,
}
