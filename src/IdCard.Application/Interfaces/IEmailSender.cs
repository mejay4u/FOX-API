using IdCard.Application.Models;

namespace IdCard.Application.Interfaces;

/// <summary>
/// Low-level transport: sends a fully-formed EmailMessage via SMTP (or any provider).
/// Swap MailKit for SendGrid / SES without touching any other layer.
/// </summary>
public interface IEmailSender
{
    Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default);
}
