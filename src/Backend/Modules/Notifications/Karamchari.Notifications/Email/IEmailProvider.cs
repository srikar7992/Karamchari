namespace Karamchari.Notifications.Email;

/// <summary>
/// Abstraction over transactional email providers (SendGrid, SES, Mailgun).
/// Phase 1: NullEmailProvider (logs only). Phase 2: real provider registered via config.
/// </summary>
public interface IEmailProvider
{
    Task<EmailProviderResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
