using IdCard.Domain.Models;

namespace IdCard.Application.Models;

public sealed class StrategyResult
{
    public IdCardContext Context { get; set; } = new();
    public string TemplatePath { get; set; } = string.Empty;
}
