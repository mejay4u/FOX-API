using System.Net;
using System.Net.Mail;
using IdCard.Application.Interfaces;
using IdCard.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdCard.Infrastructure.Email;

public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _opts;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _opts   = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendIdCardRequestEmailAsync(
        string toEmail,
        string memberName,
        string memberId,
        string planId,
        string ivrCode,
        string transactionStatus,
        CancellationToken ct = default)
    {
        try
        {
            var subject = $"{_opts.PortalName}: ID Card Request Confirmation — {memberName}";
            var body    = BuildBody(memberName, memberId, planId, ivrCode, transactionStatus);

            using var smtp = new SmtpClient(_opts.SmtpHost, _opts.SmtpPort)
            {
                Credentials = new NetworkCredential(_opts.SmtpUser, _opts.SmtpPassword),
                EnableSsl   = _opts.EnableSsl
            };

            using var mail = new MailMessage
            {
                From       = new MailAddress(_opts.FromAddress, _opts.FromName),
                Subject    = subject,
                Body       = body,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            await smtp.SendMailAsync(mail, ct);

            _logger.LogInformation(
                "ID card request email sent. MemberId={MemberId} To={Email} IVRCode={IvrCode}",
                memberId, toEmail, ivrCode);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send ID card request email. MemberId={MemberId} To={Email}",
                memberId, toEmail);

            return false;
        }
    }

    private string BuildBody(
        string memberName, string memberId, string planId, string ivrCode, string transactionStatus)
    {
        return $"""
            <html>
            <body style="font-family: Arial, sans-serif; font-size: 14px;">
              <p>Dear {memberName},</p>
              <p>Your ID card request has been received and processed.</p>
              <table border="1" cellpadding="6" cellspacing="0" style="border-collapse:collapse;">
                <tr><td><strong>Member ID</strong></td><td>{memberId}</td></tr>
                <tr><td><strong>Plan ID</strong></td><td>{planId}</td></tr>
                <tr><td><strong>Transaction Code</strong></td><td>{ivrCode}</td></tr>
                <tr><td><strong>Status</strong></td><td>{transactionStatus}</td></tr>
              </table>
              <p>If you have any questions, please contact member support.</p>
              <p>Thank you,<br/>{_opts.FromName}</p>
            </body>
            </html>
            """;
    }
}
