namespace IdCard.Domain.Models;

public sealed class IdCardResult
{
    public byte[] FrontImageBytes { get; set; } = [];
    public byte[] BackImageBytes { get; set; } = [];
}
