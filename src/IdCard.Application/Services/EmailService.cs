using IdCard.Application.Interfaces;
using IdCard.Application.Models;
using Microsoft.Extensions.Logging;

namespace IdCard.Application.Services;

public sealed class EmailService : IEmailService
{
    private readonly IEmailSender _sender;
    private readonly IEmailTemplateService _templates;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IEmailSender sender,
        IEmailTemplateService templates,
        ILogger<EmailService> logger)
    {
        _sender    = sender;
        _templates = templates;
        _logger    = logger;
    }

    public Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
        => _sender.SendAsync(message, ct);

    public async Task<bool> SendIdCardRequestEmailAsync(
        string toEmail,
        string memberName,
        string memberId,
        string planId,
        string lob,
        string ivrCode,
        string transactionStatus,
        CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["MemberName"]        = memberName,
            ["MemberId"]          = memberId,
            ["PlanId"]            = planId,
            ["IvrCode"]           = ivrCode,
            ["TransactionStatus"] = transactionStatus
        };

        return await SendTemplatedAsync(
            toEmail, "ID Card Request Confirmation",
            $"{lob}/id-card-request", placeholders, ct);
    }

    public async Task<bool> SendActivationCodeEmailAsync(
        string toEmail,
        string username,
        string activationCode,
        string lob,
        CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["Username"]       = username,
            ["ActivationCode"] = activationCode
        };

        return await SendTemplatedAsync(
            toEmail, "Your Activation Code",
            $"{lob}/activation-code", placeholders, ct);
    }

    public async Task<bool> SendPasswordResetEmailAsync(
        string toEmail,
        string username,
        string resetLink,
        string lob,
        CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["Username"]  = username,
            ["ResetLink"] = resetLink
        };

        return await SendTemplatedAsync(
            toEmail, "Password Reset Request",
            $"{lob}/password-reset", placeholders, ct);
    }

    public async Task<bool> SendWelcomeEmailAsync(
        string toEmail,
        string username,
        string lob,
        CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["Username"] = username
        };

        return await SendTemplatedAsync(
            toEmail, "Welcome to the Member Portal",
            $"{lob}/welcome", placeholders, ct);
    }

    public async Task<bool> SendAccountUnlockEmailAsync(
        string toEmail,
        string username,
        string unlockLink,
        string lob,
        CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["Username"]   = username,
            ["UnlockLink"] = unlockLink
        };

        return await SendTemplatedAsync(
            toEmail, "Your Account Has Been Unlocked",
            $"{lob}/account-unlock", placeholders, ct);
    }

    public async Task<bool> SendToMultipleAsync(
        IEnumerable<string> toEmails,
        string subject,
        string templateName,
        Dictionary<string, string> placeholders,
        string lob,
        CancellationToken ct = default)
    {
        var body = await _templates.RenderAsync($"{lob}/{templateName}", placeholders, ct);
        return await _sender.SendAsync(new EmailMessage
        {
            To       = toEmails.ToList(),
            Subject  = subject,
            HtmlBody = body
        }, ct);
    }

    private async Task<bool> SendTemplatedAsync(
        string toEmail,
        string subject,
        string templateName,
        Dictionary<string, string> placeholders,
        CancellationToken ct)
    {
        var body = await _templates.RenderAsync(templateName, placeholders, ct);
        return await _sender.SendAsync(new EmailMessage
        {
            To       = [toEmail],
            Subject  = subject,
            HtmlBody = body
        }, ct);
    }
}
