using IdCard.Domain.Models;

namespace IdCard.Application.Interfaces;

public interface IProviderDataService
{
    Task<Provider> GetProviderAsync(string providerId, CancellationToken ct = default);
}
