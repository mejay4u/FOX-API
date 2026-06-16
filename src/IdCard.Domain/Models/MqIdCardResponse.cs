namespace IdCard.Domain.Models;

public sealed class MqIdCardResponse
{
    public bool IsSuccess { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Member? Member { get; set; }
}
