namespace IdCard.Infrastructure.Options;

public sealed class IdCardOptions
{
    public const string SectionName = "IdCard";

    public const string DefaultBasePath = "IdCard";

    /// <summary>Root folder that contains Templates/, Assets/, and Fonts/ sub-folders.</summary>
    public string BasePath { get; set; } = DefaultBasePath;

    /// <summary>Fallback template code when no specific template file exists for a plan code.</summary>
    public string? TemplateAlias { get; set; }
}
