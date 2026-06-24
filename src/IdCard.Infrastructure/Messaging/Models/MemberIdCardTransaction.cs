using System.Xml.Serialization;

namespace IdCard.Infrastructure.Messaging.Models;

[XmlRoot("IVR")]
public sealed class MemberIdCardTransaction
{
    [XmlElement("ReturnCode")]
    public int ReturnCode { get; set; }

    [XmlElement("IVRCode")]
    public int IVRCode { get; set; }

    [XmlElement("Status")]
    public string Status { get; set; } = string.Empty;

    [XmlElement("MessageStatus")]
    public string MessageStatus { get; set; } = string.Empty;
}
