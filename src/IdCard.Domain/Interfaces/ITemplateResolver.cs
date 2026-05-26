namespace IdCard.Domain.Interfaces;

public interface ITemplateResolver
{
    /// <summary>
    /// Resolves the JSON template file path for the given LOB and template code.
    /// Falls back to default.json when a specific template is absent.
    /// </summary>
    string Resolve(string lob, string templateCode);
}
