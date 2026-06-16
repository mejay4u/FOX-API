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

    public Task<MqIdCardResponse> RequestIdCardAsync(MqIdCardRequest request, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "IBM MQ is disabled (IbmMq:Enabled=false). Skipping MQ request for MemberId={MemberId}",
            request.MemberId);

        return Task.FromResult(new MqIdCardResponse { IsSuccess = true, MessageId = "NULL" });
    }
}
