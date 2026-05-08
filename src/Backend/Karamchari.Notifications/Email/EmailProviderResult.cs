namespace Karamchari.Notifications.Email;

/// <summary>Result returned by IEmailProvider.SendAsync.</summary>
public sealed record EmailProviderResult(
    bool Success,
    string? ProviderMessageId,
    string? FailureReason)
{
    public static EmailProviderResult Ok(string providerMessageId) =>
        new(true, providerMessageId, null);

    public static EmailProviderResult Fail(string reason) =>
        new(false, null, reason);
}
