using System.Text;
using System.Xml;

namespace IdCard.Infrastructure.Messaging.Xml;

internal static class IdCardRequestXmlBuilder
{
    public static string Build(string memberId, string subscriberId, string lob, string environment)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssffff");

        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        });

        writer.WriteStartDocument();
        writer.WriteStartElement("IdCardRequest");

        writer.WriteElementString("MEMBERID", memberId);
        writer.WriteElementString("CPPPMID", subscriberId);
        writer.WriteElementString("LOB", lob);
        writer.WriteElementString("ENVIRONMNT", environment);
        writer.WriteElementString("TIMESTAMP", timestamp);

        writer.WriteEndElement();
        writer.WriteEndDocument();

        return sb.ToString();
    }
}
