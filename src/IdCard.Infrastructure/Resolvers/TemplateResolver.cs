using IdCard.Domain.Interfaces;
using IdCard.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdCard.Infrastructure.Resolvers;

/// <summary>
/// Resolves the JSON template file path for a given LOB + templateCode.
/// Resolution order:
///   1. Templates/{LOB}/{templateCode}.json
///   2. Templates/{LOB}/default.json
/// Throws FileNotFoundException if neither exists.
/// No Strategy dependency — pure path logic.
/// </summary>
public sealed class TemplateResolver : ITemplateResolver
{
    private readonly string _templatesRoot;
    private readonly string? _templateAlias;
    private readonly ILogger<TemplateResolver> _logger;

    public TemplateResolver(IOptions<IdCardOptions> options, ILogger<TemplateResolver> logger)
    {
        _templatesRoot = Path.Combine(options.Value.BasePath, "Templates");
        _templateAlias = options.Value.TemplateAlias?.ToUpperInvariant();
        _logger = logger;
    }

    public string Resolve(string lob, string templateCode)
    {
        var normalizedLob  = lob.ToUpperInvariant();
        var normalizedCode = templateCode.Trim('*').ToUpperInvariant();

        // Try specific template first (skip when code is catch-all "*")
        if (!string.IsNullOrEmpty(normalizedCode))
        {
            var specific = Path.Combine(_templatesRoot, normalizedLob, $"{normalizedCode}.json");
            if (File.Exists(specific))
            {
                _logger.LogDebug("Template resolved: {Path}", specific);
                return specific;
            }

            // Try alias template (e.g. 0530/0540 → 0500)
            if (!string.IsNullOrEmpty(_templateAlias))
            {
                var alias = Path.Combine(_templatesRoot, normalizedLob, $"{_templateAlias}.json");
                if (File.Exists(alias))
                {
                    _logger.LogDebug("Template resolved (alias {Alias}): {Path}", _templateAlias, alias);
                    return alias;
                }
            }
        }

        // Fallback to default
        var defaultPath = Path.Combine(_templatesRoot, normalizedLob, "default.json");
        if (File.Exists(defaultPath))
        {
            _logger.LogDebug("Template resolved (default): {Path}", defaultPath);
            return defaultPath;
        }

        throw new FileNotFoundException(
            $"No template found for LOB='{lob}', TemplateCode='{templateCode}'. " +
            $"Searched: {defaultPath}");
    }
}
