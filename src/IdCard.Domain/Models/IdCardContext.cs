namespace IdCard.Domain.Models;

public sealed class IdCardContext
{
    public string Lob { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = "*";
    public Member Member { get; set; } = new();
    public Provider Provider { get; set; } = new();
    public Dictionary<string, string> AdditionalData { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>QR code payload — plain string, no Skia types.</summary>
    public string QrData { get; set; } = string.Empty;
}
