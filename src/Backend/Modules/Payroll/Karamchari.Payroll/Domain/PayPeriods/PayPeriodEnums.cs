// -----------------------------------------------------------------------
// <copyright file="PayPeriodEnums.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.PayPeriods;

public enum PayrollFrequency
{
    Weekly = 1,
    Fortnightly = 2,
    Monthly = 3
}

public enum PayPeriodStatus
{
    Open = 1,
    Calculating = 2,
    PendingApproval = 3,
    Approved = 4,
    Paid = 5,
    Locked = 6
}
