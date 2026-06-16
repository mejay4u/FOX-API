using IdCard.Application.Interfaces;
using IdCard.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IdCard.Infrastructure.Data;

/// <summary>
/// Fetches member data by sending an IBM MQ request and awaiting the reply.
/// Drop-in replacement for <see cref="MockMemberDataService"/>.
/// </summary>
public sealed class IbmMqMemberDataService : IMemberDataService
{
    private readonly IIdCardMqGateway _mqGateway;
    private readonly ILogger<IbmMqMemberDataService> _logger;

    public IbmMqMemberDataService(IIdCardMqGateway mqGateway, ILogger<IbmMqMemberDataService> logger)
    {
        _mqGateway = mqGateway;
        _logger    = logger;
    }

    public async Task<Member> GetMemberAsync(string memberId, CancellationToken ct = default)
    {
        var request = new MqIdCardRequest
        {
            MemberId     = memberId,
            SubscriberId = memberId   // caller can enrich if subscriber ID differs
        };

        var response = await _mqGateway.RequestMemberDataAsync(request, ct);

        if (!response.IsSuccess || response.Member is null)
        {
            _logger.LogWarning(
                "IBM MQ member lookup failed. MemberId={MemberId} Code={Code} Message={Message}",
                memberId, response.ErrorCode, response.ErrorMessage);

            throw new InvalidOperationException(
                $"IBM MQ member lookup failed for '{memberId}': {response.ErrorMessage}");
        }

        return response.Member;
    }
}
