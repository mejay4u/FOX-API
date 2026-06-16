using IdCard.Application.Interfaces;
using IdCard.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdCard.Infrastructure.Email;

/// <summary>
/// Loads HTML templates from {TemplatesPath}/{templateName}.html.
/// When templateName is "{lob}/{name}", tries the LOB sub-folder first,
/// then falls back to the root templates folder.
/// </summary>
public sealed class HtmlTemplateService : IEmailTemplateService
{
    private readonly string _root;
    private readonly ILogger<HtmlTemplateService> _logger;

    public HtmlTemplateService(IOptions<EmailOptions> options, ILogger<HtmlTemplateService> logger)
    {
        var configured = options.Value.TemplatesPath;
        _root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);
        _logger = logger;
    }

    public async Task<string> RenderAsync(
        string templateName,
        Dictionary<string, string> placeholders,
        CancellationToken ct = default)
    {
        var html = await LoadAsync(templateName, ct);

        foreach (var (key, value) in placeholders)
            html = html.Replace($"{{{key}}}", value ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        return html;
    }

    private async Task<string> LoadAsync(string templateName, CancellationToken ct)
    {
        // templateName may be "LOB/name" or just "name"
        var parts = templateName.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2)
        {
            var lobPath = Path.Combine(_root, parts[0], $"{parts[1]}.html");
            if (File.Exists(lobPath))
                return await File.ReadAllTextAsync(lobPath, ct);

            var globalPath = Path.Combine(_root, $"{parts[1]}.html");
            if (File.Exists(globalPath))
                return await File.ReadAllTextAsync(globalPath, ct);
        }
        else
        {
            var path = Path.Combine(_root, $"{parts[0]}.html");
            if (File.Exists(path))
                return await File.ReadAllTextAsync(path, ct);
        }

        _logger.LogWarning("Email template not found: {TemplateName} (root={Root})", templateName, _root);
        return string.Empty;
    }
}
