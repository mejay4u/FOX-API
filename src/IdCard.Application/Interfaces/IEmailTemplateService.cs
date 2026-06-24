namespace IdCard.Application.Interfaces;

/// <summary>
/// Loads an HTML template by name and replaces {Placeholder} tokens with provided values.
/// Template resolution: templateName → Infrastructure/Email/Templates/{templateName}.html
/// LOB-specific templates: pass "{lob}/template-name" to resolve LOB sub-folder first,
/// falling back to the global template if not found.
/// </summary>
public interface IEmailTemplateService
{
    Task<string> RenderAsync(string templateName, Dictionary<string, string> placeholders, CancellationToken ct = default);
}
