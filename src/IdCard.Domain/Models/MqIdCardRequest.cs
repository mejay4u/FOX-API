namespace IdCard.Domain.Models;

public sealed class MqIdCardRequest
{
    public string MemberId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
}
