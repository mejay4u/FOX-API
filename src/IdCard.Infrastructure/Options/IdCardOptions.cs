namespace IdCard.Infrastructure.Options;

public sealed class IdCardOptions
{
    public const string SectionName = "IdCard";

    public const string DefaultBasePath = "IdCard";

    /// <summary>Root folder that contains Templates/, Assets/, and Fonts/ sub-folders.</summary>
    public string BasePath { get; set; } = DefaultBasePath;

    /// <summary>Maps plan codes that have no own template file to a shared canonical code.</summary>
    public Dictionary<string, string> TemplateAliases { get; set; } = new();
}
