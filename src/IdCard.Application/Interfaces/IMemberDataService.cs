using IdCard.Domain.Models;

namespace IdCard.Application.Interfaces;

public interface IMemberDataService
{
    Task<Member> GetMemberAsync(string memberId, CancellationToken ct = default);
}
