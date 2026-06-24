namespace IdCard.Infrastructure.Messaging.Xml;

internal static class IdCardRequestXmlBuilder
{
    private const string TemplatePath = "App_Data/xml/id-card-request.xml";

    public static string Build(
        string memberId,
        string planId,
        string environment,
        string contentRootPath)
    {
        var templateFile = Path.Combine(contentRootPath, TemplatePath);
        var strXml = File.ReadAllText(templateFile);

        var now = DateTime.Now;

        var strXml1 = strXml.Replace("#MEMBERID#",    memberId);
        var strXml2 = strXml1.Replace("#PLANID#",     planId);
        var strXml3 = strXml2.Replace("#ENVIRONMENT#", environment);
        var strXml4 = strXml3.Replace("#TIMESTAMP#",  now.ToString("yyyyMMddHHmmssffff"));
        var strXml5 = strXml4.Replace("#DATE#",       now.ToString("yyyyMMdd"));
        var strXml6 = strXml5.Replace("#TIME#",       now.ToString("HHmmss"));

        return strXml6;
    }
}
