using System.Text.Json.Serialization;

namespace IdCard.Infrastructure.Templates.Models;

public sealed class TemplateElement
{
    /// <summary>"text" or "image"</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    // ── Text-only fields ────────────────────────────────────────────────

    /// <summary>Bold label rendered above (or inline with) the bound value.</summary>
    [JsonPropertyName("header")]
    public string? Header { get; set; }

    /// <summary>Binding key resolved by IBindingResolver (e.g. "MemberName", "Member.MemberId").</summary>
    [JsonPropertyName("binding")]
    public string? Binding { get; set; }

    /// <summary>Literal text with no data binding (e.g. "PCP Details:").</summary>
    [JsonPropertyName("staticText")]
    public string? StaticText { get; set; }

    [JsonPropertyName("fontSize")]
    public float FontSize { get; set; } = 12f;

    /// <summary>When true the element's value font is bold (header is always bold).</summary>
    [JsonPropertyName("bold")]
    public bool Bold { get; set; }

    /// <summary>Render Header and Value on the same line ("Header: Value").</summary>
    [JsonPropertyName("inline")]
    public bool Inline { get; set; }

    /// <summary>Word-wrap the value inside Width.</summary>
    [JsonPropertyName("wrap")]
    public bool Wrap { get; set; }

    /// <summary>Hex color override, e.g. "#333333". Defaults to black when absent.</summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    // ── Layout ──────────────────────────────────────────────────────────

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("width")]
    public float Width { get; set; } = 200f;

    [JsonPropertyName("height")]
    public float Height { get; set; } = 20f;
}
