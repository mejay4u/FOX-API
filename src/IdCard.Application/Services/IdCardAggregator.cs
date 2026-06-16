using IdCard.Application.Interfaces;
using IdCard.Domain.Interfaces;
using IdCard.Domain.Models;

namespace IdCard.Application.Services;

/// <summary>
/// Orchestrates the full ID-card generation flow:
/// 1. Fetch member data
/// 2. PUT ID card request to IBM MQ (PutIdCardRequestAsync)
/// 3. Poll OUTBOUND queue for transaction result (GetIdCardTransactionAsync)
/// 4. Fetch provider, build context, apply strategy, render
/// </summary>
public sealed class IdCardAggregator
{
    private readonly IIdCardMqGateway _mqGateway;
    private readonly IMemberDataService _memberData;
    private readonly IProviderDataService _providerData;
    private readonly IIdCardStrategy _strategy;
    private readonly IIdCardRenderer _renderer;

    public IdCardAggregator(
        IIdCardMqGateway mqGateway,
        IMemberDataService memberData,
        IProviderDataService providerData,
        IIdCardStrategy strategy,
        IIdCardRenderer renderer)
    {
        _mqGateway    = mqGateway;
        _memberData   = memberData;
        _providerData = providerData;
        _strategy     = strategy;
        _renderer     = renderer;
    }

    public async Task<IdCardResult> GenerateAsync(string memberId, string lob, CancellationToken ct = default)
    {
        // Step 1 — Fetch member (PlanCode needed for MQ request)
        var member = await _memberData.GetMemberAsync(memberId, ct);

        // Step 2 — PUT ID card request to IBM MQ INBOUND queue
        var putResult = await _mqGateway.PutIdCardRequestAsync(
            new MqIdCardRequest { MemberId = memberId, PlanId = member.PlanCode }, ct);

        if (!putResult.IsSuccess)
            throw new InvalidOperationException(
                $"IBM MQ PUT failed for '{memberId}': {putResult.ErrorMessage}");

        // Step 3 — Poll OUTBOUND queue for transaction result (GetIDCardTransaction pattern)
        var getResult = await _mqGateway.GetIdCardTransactionAsync(putResult.MessageId, memberId, ct);

        if (!getResult.IsSuccess)
            throw new InvalidOperationException(
                $"IBM MQ GET failed for '{memberId}': {getResult.ErrorMessage}");

        // Step 4 — Fetch provider, build context, apply strategy, render
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
