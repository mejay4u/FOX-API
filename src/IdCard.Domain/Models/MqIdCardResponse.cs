namespace IdCard.Domain.Models;

public sealed class MqIdCardResponse
{
    public bool IsSuccess { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    // Fields parsed from the IVR reply XML
    public string IvrCode { get; set; } = string.Empty;
    public string TransactionStatus { get; set; } = string.Empty;
    public string MessageStatus { get; set; } = string.Empty;
}
