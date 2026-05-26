namespace IdCard.Domain.Interfaces;

public interface IQrCodeService
{
    /// <summary>Returns PNG bytes for the given QR payload.</summary>
    byte[] Generate(string data);
}
