using IdCard.Application.Interfaces;
using IdCard.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IdCard.Infrastructure.Messaging;

/// <summary>
/// No-op gateway used when IbmMq:Enabled=false (local dev / unit tests).
/// Always returns success without touching IBM MQ.
/// </summary>
internal sealed class NullIdCardMqGateway : IIdCardMqGateway
{
    private readonly ILogger<NullIdCardMqGateway> _logger;

    public NullIdCardMqGateway(ILogger<NullIdCardMqGateway> logger) => _logger = logger;

    public Task<MqIdCardResponse> PutIdCardRequestAsync(MqIdCardRequest request, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "IBM MQ disabled (IbmMq:Enabled=false). Skipping PUT for MemberId={MemberId}",
            request.MemberId);

        return Task.FromResult(new MqIdCardResponse { IsSuccess = true, MessageId = "NULL" });
    }

    public Task<MqIdCardResponse> GetIdCardTransactionAsync(
        string correlationId, string memberId, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "IBM MQ disabled (IbmMq:Enabled=false). Skipping GET for MemberId={MemberId}",
            memberId);

        return Task.FromResult(new MqIdCardResponse { IsSuccess = true, MessageId = correlationId });
    }
}
