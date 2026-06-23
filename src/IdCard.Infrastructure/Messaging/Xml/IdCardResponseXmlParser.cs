using IdCard.Domain.Models;
using IdCard.Infrastructure.Messaging.Models;
using System.Xml.Serialization;

namespace IdCard.Infrastructure.Messaging.Xml;

internal static class IdCardResponseXmlParser
{
    private static readonly XmlSerializer _serializer =
        new(typeof(MemberIdCardTransaction));

    public static MqIdCardResponse Parse(string responseXml, string correlationId)
    {
        try
        {
            MemberIdCardTransaction transaction;
            using (var reader = new StringReader(responseXml))
                transaction = (MemberIdCardTransaction)_serializer.Deserialize(reader)!;

            // Mirror reference: ReturnCode == 0 && IVRCode == 0 → success
            if (transaction.ReturnCode == 0 && transaction.IVRCode == 0)
            {
                return new MqIdCardResponse
                {
                    IsSuccess         = true,
                    MessageId         = correlationId,
                    IvrCode           = transaction.IVRCode.ToString(),
                    TransactionStatus = transaction.Status,
                    MessageStatus     = transaction.MessageStatus
                };
            }

            return new MqIdCardResponse
            {
                IsSuccess         = false,
                MessageId         = correlationId,
                ErrorCode         = $"IVR_{transaction.IVRCode}",
                ErrorMessage      = transaction.MessageStatus,
                IvrCode           = transaction.IVRCode.ToString(),
                TransactionStatus = transaction.Status,
                MessageStatus     = transaction.MessageStatus
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
