using IdCard.Domain.Models;

namespace IdCard.Application.Interfaces;

public interface IIdCardMqGateway
{
    /// <summary>PUT the ID card request XML to the INBOUND queue.</summary>
    Task<MqIdCardResponse> PutIdCardRequestAsync(MqIdCardRequest request, CancellationToken ct = default);

    /// <summary>
    /// Poll the OUTBOUND reply queue for the transaction result.
    /// Retries every second up to MaxPollAttempts (mirrors GetIDCardTransaction in MemberCardAggregator).
    /// </summary>
    Task<MqIdCardResponse> GetIdCardTransactionAsync(string correlationId, string memberId, CancellationToken ct = default);
}
