using IdCard.Domain.Models;
using System.Xml.Linq;

namespace IdCard.Infrastructure.Messaging.Xml;

internal static class IdCardResponseXmlParser
{
    /// <summary>
    /// Parses the IVR reply XML returned from the OUTBOUND MQ queue.
    /// Expected structure:
    ///   &lt;IVR&gt;
    ///     &lt;IVRCode&gt;00&lt;/IVRCode&gt;
    ///     &lt;Status&gt;S&lt;/Status&gt;
    ///     &lt;MessageStatus&gt;...&lt;/MessageStatus&gt;
    ///   &lt;/IVR&gt;
    /// </summary>
    public static MqIdCardResponse Parse(string xml, string correlationId)
    {
        try
        {
            var doc  = XDocument.Parse(xml);
            var root = doc.Root!;

            var ivrCode   = root.Element("IVRCode")?.Value       ?? string.Empty;
            var status    = root.Element("Status")?.Value        ?? string.Empty;
            var msgStatus = root.Element("MessageStatus")?.Value ?? string.Empty;

            return new MqIdCardResponse
            {
                IsSuccess         = true,
                MessageId         = correlationId,
                IvrCode           = ivrCode,
                TransactionStatus = status,
                MessageStatus     = msgStatus
            };
        }
        catch (Exception ex)
        {
            return new MqIdCardResponse
            {
                IsSuccess    = false,
                ErrorCode    = "PARSE_ERROR",
                ErrorMessage = $"Failed to parse IVR reply: {ex.Message}"
            };
        }
    }
}
