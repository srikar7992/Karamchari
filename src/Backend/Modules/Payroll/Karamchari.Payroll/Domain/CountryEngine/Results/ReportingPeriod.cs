// -----------------------------------------------------------------------
// <copyright file="ReportingPeriod.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.CountryEngine.Results;

using Karamchari.Payroll.Services.Statutory;

/// <summary>
/// Identifies the pay period for which compliance reports are being generated.
/// </summary>
public sealed record ReportingPeriod(
    DateOnly StartDate,
    DateOnly EndDate,
    FinancialYear FinancialYear
);
