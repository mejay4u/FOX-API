using System.Text.Json.Serialization;

namespace IdCard.Infrastructure.Templates.Models;

public sealed class IdCardTemplate
{
    [JsonPropertyName("templateCode")]
    public string TemplateCode { get; set; } = "default";

    [JsonPropertyName("lob")]
    public string Lob { get; set; } = string.Empty;

    [JsonPropertyName("front")]
    public FrontConfig Front { get; set; } = new();

    [JsonPropertyName("back")]
    public BackConfig Back { get; set; } = new();
}
