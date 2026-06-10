// -----------------------------------------------------------------------
// <copyright file="AdjustmentEnums.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.Adjustments;

public enum AdjustmentType
{
    Credit = 1,
    Debit = 2
}

public enum AdjustmentStatus
{
    Pending = 1,
    Applied = 2,
    Cancelled = 3
}
