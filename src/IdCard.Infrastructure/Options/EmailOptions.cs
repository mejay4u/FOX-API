namespace IdCard.Infrastructure.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Member Portal";
    public string PortalName { get; set; } = "Member Portal";
    public string HelplinePhone { get; set; } = string.Empty;
    public string HelplineTty { get; set; } = string.Empty;
    public string TemplatesPath { get; set; } = "App_Data/Email/Templates";
}
