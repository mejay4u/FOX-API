using IdCard.Application.Interfaces;
using IdCard.Application.Models;
using IdCard.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdCard.Application.Services;

public sealed class EmailService : IEmailService
{
    private readonly IEmailSender _sender;
    private readonly IEmailTemplateService _templates;
    private readonly EmailServiceConfig _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IEmailSender sender,
        IEmailTemplateService templates,
        IOptions<EmailServiceConfig> config,
        ILogger<EmailService> logger)
    {
        _sender    = sender;
        _templates = templates;
        _config    = config.Value;
        _logger    = logger;
    }

    public Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
        => _sender.SendAsync(message, ct);

    public async Task<bool> SendIdCardRequestEmailAsync(
        string toEmail,
        string fromEmail,
        string memberName,
        string memberId,
        string planId,
        string lob,
        string memberReference,
        string ivrCode,
        string transactionStatus,
        CancellationToken ct = default)
    {
        var subject = $"{lob} {memberReference} portal: ID Card Request";

        var placeholders = new Dictionary<string, string>
        {
            ["UserName"]          = memberName,
            ["MemberId"]          = memberId,
            ["PlanId"]            = planId,
            ["LobName"]           = lob,
            ["MemberReference"]   = memberReference,
            ["HelplineNumber"]    = _config.HelplinePhone,
            ["HelplineTTY"]       = _config.HelplineTty,
            ["IvrCode"]           = ivrCode,
            ["TransactionStatus"] = transactionStatus
        };

        return await SendTemplatedAsync(toEmail, fromEmail, subject, $"{lob}/id-card-request", placeholders, ct);
    }

    public async Task<bool> SendActivationCodeEmailAsync(
        string toEmail,
        string fromEmail,
        string username,
        string activationCode,
        string lob,
        CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["UserName"]       = username,
            ["ActivationCode"] = activationCode,
            ["LobName"]        = lob,
            ["HelplineNumber"] = _config.HelplinePhone,
            ["HelplineTTY"]    = _config.HelplineTty
        };

        return await SendTemplatedAsync(
            toEmail, fromEmail, "Your Activation Code",
            $"{lob}/activation-code", placeholders, ct);
    }

    public async Task<bool> SendPasswordResetEmailAsync(
        string toEmail,
        string fromEmail,
        string username,
        string resetLink,
        string lob,
        CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["UserName"]       = username,
            ["ResetLink"]      = resetLink,
            ["LobName"]        = lob,
            ["HelplineNumber"] = _config.HelplinePhone,
            ["HelplineTTY"]    = _config.HelplineTty
        };

        return await SendTemplatedAsync(
            toEmail, fromEmail, "Password Reset Request",
            $"{lob}/password-reset", placeholders, ct);
    }

    public async Task<bool> SendWelcomeEmailAsync(
        string toEmail,
        string fromEmail,
        string username,
        string lob,
        CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["UserName"]       = username,
            ["LobName"]        = lob,
            ["HelplineNumber"] = _config.HelplinePhone,
            ["HelplineTTY"]    = _config.HelplineTty
        };

        return await SendTemplatedAsync(
            toEmail, fromEmail, "Welcome to the Member Portal",
            $"{lob}/welcome", placeholders, ct);
    }

    public async Task<bool> SendAccountUnlockEmailAsync(
        string toEmail,
        string fromEmail,
        string username,
        string unlockLink,
        string lob,
        CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["UserName"]       = username,
            ["UnlockLink"]     = unlockLink,
            ["LobName"]        = lob,
            ["HelplineNumber"] = _config.HelplinePhone,
            ["HelplineTTY"]    = _config.HelplineTty
        };

        return await SendTemplatedAsync(
            toEmail, fromEmail, "Your Account Has Been Unlocked",
            $"{lob}/account-unlock", placeholders, ct);
    }

    public async Task<bool> SendToMultipleAsync(
        IEnumerable<string> toEmails,
        string fromEmail,
        string subject,
        string templateName,
        Dictionary<string, string> placeholders,
        string lob,
        CancellationToken ct = default)
    {
        var body = await _templates.RenderAsync($"{lob}/{templateName}", placeholders, ct);
        return await _sender.SendAsync(new EmailMessage
        {
            From     = fromEmail,
            To       = toEmails.ToList(),
            Subject  = subject,
            HtmlBody = body
        }, ct);
    }

    private async Task<bool> SendTemplatedAsync(
        string toEmail,
        string fromEmail,
        string subject,
        string templateName,
        Dictionary<string, string> placeholders,
        CancellationToken ct)
    {
        var body = await _templates.RenderAsync(templateName, placeholders, ct);
        return await _sender.SendAsync(new EmailMessage
        {
            From     = fromEmail,
            To       = [toEmail],
            Subject  = subject,
            HtmlBody = body
        }, ct);
    }
}
