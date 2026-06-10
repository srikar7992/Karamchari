// -----------------------------------------------------------------------
// <copyright file="ProfessionalTaxCalculator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Domain.DeductionRules;

namespace Karamchari.Payroll.Services.Deductions;

/// <summary>
/// Professional Tax (PT) slabs by Indian state code.
/// Rates as per applicable state schedules. Returns 0 for unknown state codes.
/// </summary>
public sealed class ProfessionalTaxCalculator : IDeductionCalculator
{
    public StatutoryDeductionType DeductionType => StatutoryDeductionType.ProfessionalTax;

    public decimal Calculate(DeductionContext context)
    {
        return context.StateCode?.ToUpperInvariant() switch
        {
            "KA" or "KARNATAKA" => KarnatakaPt(context.GrossPay),
            "MH" or "MAHARASHTRA" => MaharashtraPt(context.GrossPay, context.Month),
            "AP" or "ANDHRA PRADESH" => AndhraPt(context.GrossPay),
            "TS" or "TELANGANA" => TelanganaaPt(context.GrossPay),
            "WB" or "WEST BENGAL" => WestBengalPt(context.GrossPay),
            "TN" or "TAMIL NADU" => TamilNaduPt(context.GrossPay),
            "GJ" or "GUJARAT" => GujaratPt(context.GrossPay),
            "MP" or "MADHYA PRADESH" => MpPt(context.GrossPay),
            "OR" or "OD" or "ODISHA" => OdishaPt(context.GrossPay),
            "AS" or "ASSAM" => AssamPt(context.GrossPay),
            _ => DefaultPt(context.GrossPay),
        };
    }

    // Karnataka: ≤15k=0, 15001-17999=150, ≥18k=200
    private static decimal KarnatakaPt(decimal gross) =>
        gross switch
        {
            <= 15_000m => 0m,
            <= 17_999m => 150m,
            _ => 200m,
        };

    // Maharashtra: ≤7500=0, 7501-10000=175, >10000=200 (Feb=300)
    private static decimal MaharashtraPt(decimal gross, int month) =>
        gross switch
        {
            <= 7_500m => 0m,
            <= 10_000m => 175m,
            _ => month == 2 ? 300m : 200m,
        };

    // Andhra Pradesh: ≤15k=0, 15001-20000=150, >20000=200
    private static decimal AndhraPt(decimal gross) =>
        gross switch
        {
            <= 15_000m => 0m,
            <= 20_000m => 150m,
            _ => 200m,
        };

    // Telangana: same slabs as AP
    private static decimal TelanganaaPt(decimal gross) => AndhraPt(gross);

    // West Bengal: ≤10k=0, 10001-15000=110, 15001-25000=130, 25001-40000=150, >40000=200
    private static decimal WestBengalPt(decimal gross) =>
        gross switch
        {
            <= 10_000m => 0m,
            <= 15_000m => 110m,
            <= 25_000m => 130m,
            <= 40_000m => 150m,
            _ => 200m,
        };

    // Tamil Nadu: ≤21k=0, >21k=208.33 (~2500/year)
    private static decimal TamilNaduPt(decimal gross) =>
        gross <= 21_000m ? 0m : 208m;

    // Gujarat: ≤5999=0, 6000-8999=80, 9000-11999=150, ≥12000=200
    private static decimal GujaratPt(decimal gross) =>
        gross switch
        {
            <= 5_999m => 0m,
            <= 8_999m => 80m,
            <= 11_999m => 150m,
            _ => 200m,
        };

    // Madhya Pradesh: ≤18750=0, >18750=208 (~2500/year)
    private static decimal MpPt(decimal gross) =>
        gross <= 18_750m ? 0m : 208m;

    // Odisha: ≤13333=0, 13334-25000=125, >25000=200
    private static decimal OdishaPt(decimal gross) =>
        gross switch
        {
            <= 13_333m => 0m,
            <= 25_000m => 125m,
            _ => 200m,
        };

    // Assam: ≤10000=0, >10000=208
    private static decimal AssamPt(decimal gross) =>
        gross <= 10_000m ? 0m : 208m;

    // Default fallback for states without specific slabs
    private static decimal DefaultPt(decimal gross) =>
        gross > 10_000m ? 200m : 0m;
}
