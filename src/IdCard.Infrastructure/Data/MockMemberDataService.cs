using IdCard.Application.Interfaces;
using IdCard.Domain.Models;

namespace IdCard.Infrastructure.Data;

/// <summary>
/// In-memory mock member store.
/// Replace with a real data access implementation without touching any other layer.
/// </summary>
public sealed class MockMemberDataService : IMemberDataService
{
    private static readonly Dictionary<string, Member> Members =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["MED-001"] = new Member
            {
                MemberId             = "MED-001",
                SubscriberId         = "SUB-001",
                FirstName            = "Jayesh",
                LastName             = "Shelar",
                DateOfBirth          = "01/15/1985",
                Gender               = "M",
                RelationshipCode     = "18",
                GroupNumber          = "GRP-78452",
                GroupName            = "TechCorp Inc.",
                PlanName             = "Premier PPO 2000",
                PlanCode             = "PPO2000",
                EffectiveDate        = "01/01/2026",
                TerminationDate      = "12/31/2026",
                Address              = "123 Main Street",
                City                 = "Chicago",
                State                = "IL",
                ZipCode              = "60601",
                Phone                = "(312) 555-0100",
                Email                = "jayesh.shelar@outlook.com",
                PcpId                = "PCP-9001",
                CopayOffice          = "$20",
                CopaySpecialist      = "$45",
                CopayUrgentCare      = "$65",
                CopayER              = "$250",
                DeductibleIndividual = "$1,500",
                DeductibleFamily     = "$3,000",
                OutOfPocketMax       = "$5,000",
                RxBinNumber          = "610502",
                RxPcnNumber          = "MEDADV",
                RxGroupNumber        = "RX78452",
                NetworkName          = "Blue Preferred Network"
            },
            ["DEN-001"] = new Member
            {
                MemberId             = "DEN-001",
                SubscriberId         = "SUB-002",
                FirstName            = "Priya",
                LastName             = "Patel",
                DateOfBirth          = "06/22/1990",
                Gender               = "F",
                RelationshipCode     = "18",
                GroupNumber          = "GRP-88100",
                GroupName            = "HealthCo LLC",
                PlanName             = "Delta Dental PPO",
                PlanCode             = "DENPPO",
                EffectiveDate        = "01/01/2026",
                TerminationDate      = "12/31/2026",
                Address              = "456 Oak Avenue",
                City                 = "Naperville",
                State                = "IL",
                ZipCode              = "60540",
                Phone                = "(630) 555-0200",
                Email                = "priya.patel@example.com",
                PcpId                = "PCP-9002",
                CopayOffice          = "Preventive: 100%",
                CopaySpecialist      = "Basic: 80%",
                DeductibleIndividual = "$50",
                DeductibleFamily     = "$150",
                OutOfPocketMax       = "$1,000",
                NetworkName          = "Delta Dental Network"
            },
            ["VIS-001"] = new Member
            {
                MemberId             = "VIS-001",
                SubscriberId         = "SUB-003",
                FirstName            = "Arjun",
                LastName             = "Kumar",
                DateOfBirth          = "09/10/1978",
                Gender               = "M",
                RelationshipCode     = "18",
                GroupNumber          = "GRP-55200",
                GroupName            = "Vision Plus Corp",
                PlanName             = "VSP Basic Vision",
                PlanCode             = "VSPBASIC",
                EffectiveDate        = "01/01/2026",
                TerminationDate      = "12/31/2026",
                Address              = "789 Elm Street",
                City                 = "Schaumburg",
                State                = "IL",
                ZipCode              = "60173",
                Phone                = "(847) 555-0300",
                Email                = "arjun.kumar@example.com",
                PcpId                = "PCP-9003",
                CopayOffice          = "$10",
                CopaySpecialist      = "$25",
                DeductibleIndividual = "$0",
                OutOfPocketMax       = "$200",
                NetworkName          = "VSP Provider Network"
            }
        };

    public Task<Member> GetMemberAsync(string memberId, CancellationToken ct = default)
    {
        if (Members.TryGetValue(memberId, out var member))
            return Task.FromResult(member);

        // Return a generic demo member for any unknown ID
        return Task.FromResult(new Member
        {
            MemberId             = memberId,
            SubscriberId         = memberId,
            FirstName            = "Demo",
            LastName             = "Member",
            DateOfBirth          = "01/01/1990",
            GroupNumber          = "GRP-00000",
            GroupName            = "Demo Group",
            PlanName             = "Demo Plan",
            PlanCode             = "DEMO",
            EffectiveDate        = "01/01/2026",
            TerminationDate      = "12/31/2026",
            PcpId                = "PCP-9001",
            CopayOffice          = "$20",
            CopaySpecialist      = "$45",
            CopayUrgentCare      = "$65",
            CopayER              = "$250",
            DeductibleIndividual = "$1,500",
            DeductibleFamily     = "$3,000",
            OutOfPocketMax       = "$5,000",
            RxBinNumber          = "000000",
            RxPcnNumber          = "DEMO",
            RxGroupNumber        = "DEMO00",
            NetworkName          = "Demo Network"
        });
    }
}
