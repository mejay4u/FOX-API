namespace IdCard.Application.Options;

public sealed class EmailServiceConfig
{
    public const string SectionName = "Email";

    public string PortalName { get; set; } = "Member Portal";
    public string HelplinePhone { get; set; } = string.Empty;
    public string HelplineTty { get; set; } = string.Empty;
}
