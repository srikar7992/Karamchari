namespace Karamchari.TimeAttendance.Domain.IoT;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Maps a physical biometric ID (e.g. "1004" from a fingerprint scanner) to a Karamchari EmployeeId.
/// </summary>
public sealed class BiometricMapping : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    /// <summary>Gets the biometric hardware identifier.</summary>
    public string BiometricId { get; private set; } = string.Empty;

    /// <summary>Gets the internal EmployeeId.</summary>
    public Guid EmployeeId { get; private set; }

    private BiometricMapping() { }

    public static BiometricMapping Create(string tenantId, string biometricId, Guid employeeId)
    {
        return new BiometricMapping
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BiometricId = biometricId,
            EmployeeId = employeeId
        };
    }
}
