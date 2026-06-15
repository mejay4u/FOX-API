namespace IdCard.Infrastructure.Options;

public sealed class IdCardOptions
{
    public const string SectionName = "IdCard";

    public const string DefaultBasePath = "IdCard";

    /// <summary>Root folder that contains Templates/, Assets/, and Fonts/ sub-folders.</summary>
    public string BasePath { get; set; } = DefaultBasePath;

    /// <summary>Template code used as fallback when a plan has no own JSON file (e.g. "0500").</summary>
    public string? TemplateAlias { get; set; }
}
