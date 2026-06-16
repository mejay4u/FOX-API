using IdCard.Domain.Models;

namespace IdCard.Application.Interfaces;

public interface IIdCardMqGateway
{
    Task<MqIdCardResponse> RequestMemberDataAsync(MqIdCardRequest request, CancellationToken ct = default);
}
