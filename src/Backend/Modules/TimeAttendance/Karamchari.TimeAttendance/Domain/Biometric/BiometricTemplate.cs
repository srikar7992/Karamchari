// -----------------------------------------------------------------------
// <copyright file="BiometricTemplate.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Biometric;

/// <summary>
/// Stores the AES-256-GCM encrypted biometric template for one employee/modality.
/// Raw biometric data is never stored — only the extracted mathematical template.
/// Template blob is opaque; decrypted only inside the matching microservice using the tenant KMS key.
/// </summary>
public sealed class BiometricTemplate : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<Guid> _authorizedDeviceIds = [];

    private BiometricTemplate() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public BiometricModality Modality { get; private set; }

    /// <summary>
    /// AES-256-GCM ciphertext. Key lives in tenant KMS; never stored alongside the blob.
    /// Stored as Base64 string to avoid binary column type issues across databases.
    /// </summary>
    public string EncryptedTemplateBlob { get; private set; } = string.Empty;

    /// <summary>KMS key version used to encrypt. Needed for key rotation without re-enrollment.</summary>
    public string KmsKeyVersion { get; private set; } = string.Empty;

    /// <summary>Quality score 0-100 averaged across capture samples during enrollment.</summary>
    public int QualityScore { get; private set; }

    public BiometricMatchMode MatchMode { get; private set; }

    public DateTimeOffset EnrolledAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public bool IsActive => RevokedAtUtc is null;

    public Guid? EnrollmentSessionId { get; private set; }

    /// <summary>Device IDs authorized to receive this template (for on-device matching).</summary>
    public IReadOnlyList<Guid> AuthorizedDeviceIds => _authorizedDeviceIds.AsReadOnly();

    public static BiometricTemplate Enroll(
        string tenantId,
        Guid employeeId,
        BiometricModality modality,
        string encryptedTemplateBlob,
        string kmsKeyVersion,
        int qualityScore,
        BiometricMatchMode matchMode,
        Guid? enrollmentSessionId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedTemplateBlob);
        ArgumentException.ThrowIfNullOrWhiteSpace(kmsKeyVersion);

        if (qualityScore < 0 || qualityScore > 100)
            throw new ArgumentOutOfRangeException(nameof(qualityScore), "Quality score must be 0-100.");

        return new BiometricTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Modality = modality,
            EncryptedTemplateBlob = encryptedTemplateBlob,
            KmsKeyVersion = kmsKeyVersion,
            QualityScore = qualityScore,
            MatchMode = matchMode,
            EnrolledAtUtc = DateTimeOffset.UtcNow,
            EnrollmentSessionId = enrollmentSessionId,
        };
    }

    public void AuthorizeDevice(Guid deviceId)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot authorize device for a revoked template.");

        if (!_authorizedDeviceIds.Contains(deviceId))
            _authorizedDeviceIds.Add(deviceId);
    }

    public void RevokeDeviceAccess(Guid deviceId)
        => _authorizedDeviceIds.Remove(deviceId);

    public void RotateKey(string encryptedTemplateBlob, string newKmsKeyVersion)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot rotate key on a revoked template.");

        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedTemplateBlob);
        ArgumentException.ThrowIfNullOrWhiteSpace(newKmsKeyVersion);

        EncryptedTemplateBlob = encryptedTemplateBlob;
        KmsKeyVersion = newKmsKeyVersion;
    }

    /// <summary>
    /// GDPR Art. 17 / BIPA erasure. Zeroes the template blob; record kept for audit trail per retention policy.
    /// The enrollmentId and dates are retained so the audit log can reference what was erased.
    /// </summary>
    public void Revoke()
    {
        if (!IsActive)
            throw new InvalidOperationException("Template already revoked.");

        EncryptedTemplateBlob = string.Empty;
        RevokedAtUtc = DateTimeOffset.UtcNow;
        _authorizedDeviceIds.Clear();
    }
}
