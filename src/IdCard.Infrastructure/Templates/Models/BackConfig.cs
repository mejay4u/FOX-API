using System.Text.Json.Serialization;

namespace IdCard.Infrastructure.Templates.Models;

public sealed class BackConfig
{
    /// <summary>Asset file name relative to Assets/{LOB}/{TemplateCode}/</summary>
    [JsonPropertyName("backgroundAsset")]
    public string BackgroundAsset { get; set; } = "back.png";
}
