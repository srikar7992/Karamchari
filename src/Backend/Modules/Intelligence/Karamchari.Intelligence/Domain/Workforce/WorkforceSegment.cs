// -----------------------------------------------------------------------
// <copyright file="WorkforceSegment.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Workforce segment classification used to apply segment-specific risk thresholds.
/// Different industries have different baseline intensity norms; a fixed global threshold
/// would produce false positives in high-intensity segments and miss early signals in
/// knowledge-worker segments.
/// </summary>
public enum WorkforceSegment
{
    /// <summary>Default. Office workers, back-office functions.</summary>
    General,

    /// <summary>Fire, police, ambulance, security — high-intensity norm.</summary>
    EmergencyServices,

    /// <summary>Nurses, doctors, allied health — shift-intensive with regulated rest rules.</summary>
    Healthcare,

    /// <summary>Store staff, hospitality — variable hours, weekend-heavy patterns.</summary>
    Retail,

    /// <summary>Logistics, distribution, manufacturing floor — physical intensity norms.</summary>
    Warehouse,

    /// <summary>Engineers, developers — lower overtime tolerance; burnout detectable earlier.</summary>
    Technology
}
