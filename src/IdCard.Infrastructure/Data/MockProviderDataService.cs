using IdCard.Application.Interfaces;
using IdCard.Domain.Models;

namespace IdCard.Infrastructure.Data;

/// <summary>
/// In-memory mock PCP/provider store.
/// Replace with a real data access implementation without touching any other layer.
/// </summary>
public sealed class MockProviderDataService : IProviderDataService
{
    private static readonly Dictionary<string, Provider> Providers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["PCP-9001"] = new Provider
            {
                ProviderId = "PCP-9001",
                Name       = "Dr. Sarah Mitchell, MD",
                Npi        = "1234567890",
                Phone      = "(312) 555-7700",
                Fax        = "(312) 555-7701",
                Address    = "900 N. Michigan Ave, Suite 400",
                City       = "Chicago",
                State      = "IL",
                ZipCode    = "60611",
                Specialty  = "Internal Medicine",
                GroupName  = "Chicago Medical Associates",
                GroupNpi   = "0987654321",
                TaxId      = "36-4521099"
            },
            ["PCP-9002"] = new Provider
            {
                ProviderId = "PCP-9002",
                Name       = "Dr. Kevin Torres, DDS",
                Npi        = "2345678901",
                Phone      = "(630) 555-8800",
                Fax        = "(630) 555-8801",
                Address    = "200 S. Washington St",
                City       = "Naperville",
                State      = "IL",
                ZipCode    = "60540",
                Specialty  = "General Dentistry",
                GroupName  = "Naperville Family Dental",
                GroupNpi   = "1098765432",
                TaxId      = "36-7812345"
            },
            ["PCP-9003"] = new Provider
            {
                ProviderId = "PCP-9003",
                Name       = "Dr. Lisa Chen, OD",
                Npi        = "3456789012",
                Phone      = "(847) 555-9900",
                Fax        = "(847) 555-9901",
                Address    = "555 Golf Rd, Suite 100",
                City       = "Schaumburg",
                State      = "IL",
                ZipCode    = "60173",
                Specialty  = "Optometry",
                GroupName  = "Vision Care Associates",
                GroupNpi   = "2109876543",
                TaxId      = "36-9023456"
            }
        };

    public Task<Provider> GetProviderAsync(string providerId, CancellationToken ct = default)
    {
        if (Providers.TryGetValue(providerId, out var provider))
            return Task.FromResult(provider);

        return Task.FromResult(new Provider
        {
            ProviderId = providerId,
            Name       = "Unassigned PCP",
            Npi        = "0000000000",
            Phone      = "N/A",
            Specialty  = "General Practice"
        });
    }
}
