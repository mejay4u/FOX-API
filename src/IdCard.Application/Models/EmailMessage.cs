namespace IdCard.Application.Models;

public sealed class EmailMessage
{
    public List<string> To { get; init; } = [];
    public List<string> Cc { get; init; } = [];
    public List<string> Bcc { get; init; } = [];
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string TextBody { get; init; } = string.Empty;
    public string? ReplyTo { get; init; }
    public List<EmailAttachment> Attachments { get; init; } = [];
}
