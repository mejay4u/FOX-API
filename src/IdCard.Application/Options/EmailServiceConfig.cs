namespace IdCard.Application.Options;

public sealed class EmailServiceConfig
{
    public const string SectionName = "Email";

    public string PortalName { get; set; } = "Member Portal";
    public string HelplinePhone { get; set; } = string.Empty;
    public string HelplineTty { get; set; } = string.Empty;

    // LOB-specific sender addresses — key = LOB name (e.g. "MEDICAL"), value = from email
    // Falls back to EmailOptions.FromAddress when LOB not found
    public Dictionary<string, string> LobFromAddresses { get; set; } = [];
}
