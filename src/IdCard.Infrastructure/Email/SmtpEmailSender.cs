using System.Net;
using System.Net.Mail;
using IdCard.Application.Interfaces;
using IdCard.Application.Models;
using IdCard.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdCard.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opts;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _opts   = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            using var smtp = new SmtpClient(_opts.SmtpHost, _opts.SmtpPort)
            {
                Credentials = new NetworkCredential(_opts.SmtpUser, _opts.SmtpPassword),
                EnableSsl   = _opts.EnableSsl
            };

            using var mail = new MailMessage
            {
                From       = new MailAddress(_opts.FromAddress, _opts.FromName),
                Subject    = message.Subject,
                Body       = message.HtmlBody,
                IsBodyHtml = true
            };

            foreach (var to  in message.To)  mail.To.Add(to);
            foreach (var cc  in message.Cc)  mail.CC.Add(cc);
            foreach (var bcc in message.Bcc) mail.Bcc.Add(bcc);

            if (message.ReplyTo is not null)
                mail.ReplyToList.Add(new MailAddress(message.ReplyTo));

            foreach (var att in message.Attachments)
                mail.Attachments.Add(new Attachment(
                    new MemoryStream(att.Content), att.FileName, att.ContentType));

            await smtp.SendMailAsync(mail, ct);

            _logger.LogInformation(
                "Email sent. Subject={Subject} To={To}",
                message.Subject, string.Join(", ", message.To));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email. Subject={Subject} To={To}",
                message.Subject, string.Join(", ", message.To));
            return false;
        }
    }
}
