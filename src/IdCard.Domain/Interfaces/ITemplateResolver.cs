namespace IdCard.Domain.Interfaces;

public interface ITemplateResolver
{
    /// <summary>
    /// Resolves the JSON template file path for the given LOB and template code.
    /// Resolution order: {templateCode}.json → {fallbackCode}.json → default.json
    /// </summary>
    string Resolve(string lob, string templateCode, string? fallbackCode = null);
}
