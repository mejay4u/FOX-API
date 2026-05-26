using IdCard.Domain.Models;

namespace IdCard.Domain.Interfaces;

public interface IBindingResolver
{
    /// <summary>
    /// Resolves a binding key to its string value using computed values,
    /// reflection-based object lookup, and AdditionalData fallback.
    /// </summary>
    string Resolve(string binding, IdCardContext context);
}
