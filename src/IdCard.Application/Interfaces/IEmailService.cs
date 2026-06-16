namespace IdCard.Application.Interfaces;

public interface IEmailService
{
    Task<bool> SendIdCardRequestEmailAsync(
        string toEmail,
        string memberName,
        string memberId,
        string planId,
        string ivrCode,
        string transactionStatus,
        CancellationToken ct = default);
}
