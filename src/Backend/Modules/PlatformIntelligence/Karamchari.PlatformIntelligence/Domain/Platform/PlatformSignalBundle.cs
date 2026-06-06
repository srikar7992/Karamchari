namespace Karamchari.PlatformIntelligence.Domain.Platform;

public sealed record PlatformSignalBundle(
    string TenantId,
    decimal ComplianceScore,
    int OpenCriticalViolations,
    int OpenHighViolations,
    decimal CoverageFragilityScore,
    int CriticalFragilitySites,
    decimal BurnoutPressureScore,
    decimal AttritionPressureScore,
    int TalentRiskCriticalCount,
    decimal ConfidenceScore
);
