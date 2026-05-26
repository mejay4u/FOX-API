namespace IdCard.Domain.Models;

public sealed class Provider
{
    public string ProviderId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Npi { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Fax { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string GroupNpi { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
}
