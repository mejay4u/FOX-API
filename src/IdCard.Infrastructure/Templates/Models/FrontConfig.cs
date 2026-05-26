using System.Text.Json.Serialization;

namespace IdCard.Infrastructure.Templates.Models;

public sealed class FrontConfig
{
    /// <summary>Asset file name relative to Assets/{LOB}/{TemplateCode}/</summary>
    [JsonPropertyName("backgroundAsset")]
    public string BackgroundAsset { get; set; } = "front.png";

    [JsonPropertyName("elements")]
    public List<TemplateElement> Elements { get; set; } = [];
}
