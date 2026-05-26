using IdCard.Application.Interfaces;
using IdCard.Domain.Interfaces;
using IdCard.Domain.Models;

namespace IdCard.Application.Services;

/// <summary>
/// Orchestrates the full ID-card generation flow:
/// 1. Fetch Member + Provider data
/// 2. Build IdCardContext
/// 3. Strategy → enriches context + resolves template path
/// 4. Renderer → produces front/back PNG bytes
/// </summary>
public sealed class IdCardAggregator
{
    private readonly IMemberDataService _memberData;
    private readonly IProviderDataService _providerData;
    private readonly IIdCardStrategy _strategy;
    private readonly IIdCardRenderer _renderer;

    public IdCardAggregator(
        IMemberDataService memberData,
        IProviderDataService providerData,
        IIdCardStrategy strategy,
        IIdCardRenderer renderer)
    {
        _memberData   = memberData;
        _providerData = providerData;
        _strategy     = strategy;
        _renderer     = renderer;
    }

    public async Task<IdCardResult> GenerateAsync(string memberId, string lob, CancellationToken ct = default)
    {
        var member   = await _memberData.GetMemberAsync(memberId, ct);
        var provider = await _providerData.GetProviderAsync(member.PcpId, ct);

        var context = new IdCardContext
        {
            Lob          = lob.ToUpperInvariant(),
            TemplateCode = "*",
            Member       = member,
            Provider     = provider
        };

        var strategyResult = _strategy.Execute(context);

        return await _renderer.RenderAsync(strategyResult.TemplatePath, strategyResult.Context);
    }
}
