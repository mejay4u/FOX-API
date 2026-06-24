using IdCard.Application.Models;

namespace IdCard.Application.Interfaces;

/// <summary>
/// High-level email use-case service.
/// Each method maps to one business event and knows which template + placeholders to use.
/// Add new methods here as new email types are needed — no other layer changes required.
/// </summary>
public interface IEmailService
{
    // ── Generic ──────────────────────────────────────────────────────────────

    /// <summary>Send a fully-built message directly (advanced / ad-hoc use).</summary>
    Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default);

    // ── ID Card ───────────────────────────────────────────────────────────────

    Task<bool> SendIdCardRequestEmailAsync(
        string toEmail,
        string fromEmail,
        string memberName,
        string memberId,
        string planId,
        string lob,
        string memberReference,
        string ivrCode,
        string transactionStatus,
        CancellationToken ct = default);

    // ── Member Account ────────────────────────────────────────────────────────

    Task<bool> SendActivationCodeEmailAsync(
        string toEmail,
        string fromEmail,
        string username,
        string activationCode,
        string lob,
        CancellationToken ct = default);

    Task<bool> SendPasswordResetEmailAsync(
        string toEmail,
        string fromEmail,
        string username,
        string resetLink,
        string lob,
        CancellationToken ct = default);

    Task<bool> SendWelcomeEmailAsync(
        string toEmail,
        string fromEmail,
        string username,
        string lob,
        CancellationToken ct = default);

    Task<bool> SendAccountUnlockEmailAsync(
        string toEmail,
        string fromEmail,
        string username,
        string unlockLink,
        string lob,
        CancellationToken ct = default);

    // ── Multi-recipient ───────────────────────────────────────────────────────

    Task<bool> SendToMultipleAsync(
        IEnumerable<string> toEmails,
        string fromEmail,
        string subject,
        string templateName,
        Dictionary<string, string> placeholders,
        string lob,
        CancellationToken ct = default);
}
