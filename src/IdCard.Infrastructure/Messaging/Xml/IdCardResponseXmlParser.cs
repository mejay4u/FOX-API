using IdCard.Domain.Models;
using System.Xml.Linq;

namespace IdCard.Infrastructure.Messaging.Xml;

internal static class IdCardResponseXmlParser
{
    public static MqIdCardResponse Parse(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root!;

            var status = root.Element("Status")?.Value ?? string.Empty;
            if (!string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                return new MqIdCardResponse
                {
                    IsSuccess    = false,
                    ErrorCode    = root.Element("ErrorCode")?.Value ?? "UNKNOWN",
                    ErrorMessage = root.Element("ErrorMessage")?.Value ?? "MQ returned non-success status"
                };
            }

            var m = root.Element("Member");
            if (m is null)
            {
                return new MqIdCardResponse
                {
                    IsSuccess    = false,
                    ErrorCode    = "NO_MEMBER",
                    ErrorMessage = "Response did not contain member data"
                };
            }

            return new MqIdCardResponse
            {
                IsSuccess = true,
                Member = new Member
                {
                    MemberId             = Str(m, "MemberId"),
                    SubscriberId         = Str(m, "SubscriberId"),
                    FirstName            = Str(m, "FirstName"),
                    LastName             = Str(m, "LastName"),
                    DateOfBirth          = Str(m, "DateOfBirth"),
                    Gender               = Str(m, "Gender"),
                    RelationshipCode     = Str(m, "RelationshipCode"),
                    GroupNumber          = Str(m, "GroupNumber"),
                    GroupName            = Str(m, "GroupName"),
                    PlanName             = Str(m, "PlanName"),
                    PlanCode             = Str(m, "PlanCode"),
                    EffectiveDate        = Str(m, "EffectiveDate"),
                    TerminationDate      = Str(m, "TerminationDate"),
                    Address              = Str(m, "Address"),
                    City                 = Str(m, "City"),
                    State                = Str(m, "State"),
                    ZipCode              = Str(m, "ZipCode"),
                    Phone                = Str(m, "Phone"),
                    Email                = Str(m, "Email"),
                    PcpId                = Str(m, "PcpId"),
                    CopayOffice          = Str(m, "CopayOffice"),
                    CopaySpecialist      = Str(m, "CopaySpecialist"),
                    CopayUrgentCare      = Str(m, "CopayUrgentCare"),
                    CopayER              = Str(m, "CopayER"),
                    DeductibleIndividual = Str(m, "DeductibleIndividual"),
                    DeductibleFamily     = Str(m, "DeductibleFamily"),
                    OutOfPocketMax       = Str(m, "OutOfPocketMax"),
                    RxBinNumber          = Str(m, "RxBinNumber"),
                    RxPcnNumber          = Str(m, "RxPcnNumber"),
                    RxGroupNumber        = Str(m, "RxGroupNumber"),
                    NetworkName          = Str(m, "NetworkName")
                }
            };
        }
        catch (Exception ex)
        {
            return new MqIdCardResponse
            {
                IsSuccess    = false,
                ErrorCode    = "PARSE_ERROR",
                ErrorMessage = $"Failed to parse MQ response: {ex.Message}"
            };
        }
    }

    private static string Str(XElement parent, string name) =>
        parent.Element(name)?.Value ?? string.Empty;
}
