using IdCard.Domain.Interfaces;
using IdCard.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdCard.Infrastructure.Resolvers;

/// <summary>
/// Resolves the JSON template file path for a given LOB + templateCode.
/// Resolution order:
///   1. Templates/{LOB}/{templateCode}.json
///   2. Templates/{LOB}/{fallbackCode}.json  (supplied by caller)
///   3. Templates/{LOB}/default.json
/// Throws FileNotFoundException if none exist.
/// </summary>
public sealed class TemplateResolver : ITemplateResolver
{
    private readonly string _templatesRoot;
    private readonly ILogger<TemplateResolver> _logger;

    public TemplateResolver(IOptions<IdCardOptions> options, ILogger<TemplateResolver> logger)
    {
        _templatesRoot = Path.Combine(options.Value.BasePath, "Templates");
        _logger = logger;
    }

    public string Resolve(string lob, string templateCode, string? fallbackCode = null)
    {
        var normalizedLob      = lob.ToUpperInvariant();
        var normalizedCode     = templateCode.Trim('*').ToUpperInvariant();
        var normalizedFallback = fallbackCode?.ToUpperInvariant();

        // 1. Try specific template
        if (!string.IsNullOrEmpty(normalizedCode))
        {
            var specific = Path.Combine(_templatesRoot, normalizedLob, $"{normalizedCode}.json");
            if (File.Exists(specific))
            {
                _logger.LogDebug("Template resolved: {Path}", specific);
                return specific;
            }

            // 2. Try caller-supplied fallback (e.g. 0530/0540 → 0500)
            if (!string.IsNullOrEmpty(normalizedFallback))
            {
                var fallback = Path.Combine(_templatesRoot, normalizedLob, $"{normalizedFallback}.json");
                if (File.Exists(fallback))
                {
                    _logger.LogDebug("Template resolved (fallback {Fallback}): {Path}", normalizedFallback, fallback);
                    return fallback;
                }
            }
        }

        // 3. Default
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
