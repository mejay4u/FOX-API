namespace IdCard.Application.Models;

public sealed class EmailAttachment
{
    public string FileName { get; init; } = string.Empty;
    public byte[] Content { get; init; } = [];
    public string ContentType { get; init; } = "application/octet-stream";
}
