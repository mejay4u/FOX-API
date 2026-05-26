using IdCard.Domain.Interfaces;
using QRCoder;

namespace IdCard.Infrastructure.QrCode;

/// <summary>Generates QR code PNG bytes from a string payload using QRCoder.</summary>
public sealed class QrCodeService : IQrCodeService
{
    public byte[] Generate(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return [];

        using var generator = new QRCodeGenerator();
        using var qrData    = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode    = new PngByteQRCode(qrData);

        // 10 pixels per module → compact but readable when rendered at 80-100px on card
        return qrCode.GetGraphic(10);
    }
}
