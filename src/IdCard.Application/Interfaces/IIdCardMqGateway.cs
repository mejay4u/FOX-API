using IdCard.Domain.Models;

namespace IdCard.Application.Interfaces;

public interface IIdCardMqGateway
{
    Task<MqIdCardResponse> RequestIdCardAsync(MqIdCardRequest request, CancellationToken ct = default);
}
