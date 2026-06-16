using IdCard.Application.Interfaces;
using IdCard.Application.Models;
using IdCard.Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace IdCard.Infrastructure.Email;

public sealed class MailKitEmailSender : IEmailSender
{
    private readonly EmailOptions _opts;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IOptions<EmailOptions> options, ILogger<MailKitEmailSender> logger)
    {
        _opts   = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(_opts.FromName, _opts.FromAddress));

            foreach (var to  in message.To)  mime.To.Add(MailboxAddress.Parse(to));
            foreach (var cc  in message.Cc)  mime.Cc.Add(MailboxAddress.Parse(cc));
            foreach (var bcc in message.Bcc) mime.Bcc.Add(MailboxAddress.Parse(bcc));

            if (message.ReplyTo is not null)
                mime.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));

            mime.Subject = message.Subject;

            var builder = new BodyBuilder
            {
                HtmlBody = string.IsNullOrWhiteSpace(message.HtmlBody) ? null : message.HtmlBody,
                TextBody = string.IsNullOrWhiteSpace(message.TextBody) ? null : message.TextBody
            };

            foreach (var att in message.Attachments)
                await builder.Attachments.AddAsync(att.FileName, new MemoryStream(att.Content),
                    ContentType.Parse(att.ContentType), ct);

            mime.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _opts.SmtpHost,
                _opts.SmtpPort,
                _opts.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                ct);

            if (!string.IsNullOrWhiteSpace(_opts.SmtpUser))
                await client.AuthenticateAsync(_opts.SmtpUser, _opts.SmtpPassword, ct);

            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(quit: true, ct);

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
