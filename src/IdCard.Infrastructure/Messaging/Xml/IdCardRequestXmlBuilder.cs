namespace IdCard.Infrastructure.Messaging.Xml;

internal static class IdCardRequestXmlBuilder
{
    private const string TemplatePath = "App_Data/xml/id-card-request.xml";

    /// <summary>
    /// Loads the XML template from disk and substitutes all placeholders,
    /// matching the MEM.Next MemberCardAggregator replacement chain pattern.
    /// </summary>
    public static string Build(
        string memberId,
        string subscriberId,
        string lob,
        string environment,
        string contentRootPath)
    {
        var templateFile = Path.Combine(contentRootPath, TemplatePath);
        var strXml = File.ReadAllText(templateFile);

        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssffff");

        var strXml1 = strXml.Replace("{MEMBERID}", memberId);
        var strXml2 = strXml1.Replace("{CPPPMID}", subscriberId);
        var strXml3 = strXml2.Replace("{LOB}", lob);
        var strXml4 = strXml3.Replace("{ENVIRONMNT}", environment);
        var strXml5 = strXml4.Replace("{TIMESTAMP}", timestamp);

        return strXml5;
    }
}
