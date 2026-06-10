// -----------------------------------------------------------------------
// <copyright file="PayrollRunEnums.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.Runs;

public enum PayrollRunStatus
{
    Draft = 1,
    Calculating = 2,
    PendingApproval = 3,
    Approved = 4,
    Published = 5,
    Locked = 6
}
