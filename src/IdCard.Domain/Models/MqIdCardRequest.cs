namespace IdCard.Domain.Models;

public sealed class MqIdCardRequest
{
    public string MemberId { get; set; } = string.Empty;
    public string SubscriberId { get; set; } = string.Empty;
    public string Lob { get; set; } = string.Empty;
}
