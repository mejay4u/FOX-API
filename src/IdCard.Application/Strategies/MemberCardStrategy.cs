using System.Text;
using IdCard.Application.Interfaces;
using IdCard.Application.Models;
using IdCard.Domain.Interfaces;
using IdCard.Domain.Models;

namespace IdCard.Application.Strategies;

/// <summary>
/// Single catch-all strategy (TemplateCode = "*").
/// Enriches IdCardContext with QR payload and LOB-derived AdditionalData,
/// then delegates template path resolution to ITemplateResolver.
/// Zero LOB-specific branching — all layout decisions live in the JSON template.
/// </summary>
public sealed class MemberCardStrategy : IIdCardStrategy
{
    private readonly ITemplateResolver _templateResolver;

    public MemberCardStrategy(ITemplateResolver templateResolver)
        => _templateResolver = templateResolver;

    public StrategyResult Execute(IdCardContext context)
    {
        context.QrData = BuildQrData(context);
        EnrichAdditionalData(context);

        return new StrategyResult
        {
            Context = context,
            // TemplateAlias fallback is supplied by the resolver via IdCardOptions
            TemplatePath = _templateResolver.Resolve(context.Lob, context.TemplateCode)
        };
    }

    // -----------------------------------------------------------------
    // QR payload — encodes key identity fields as a URL query string
    // -----------------------------------------------------------------
    private static string BuildQrData(IdCardContext context)
    {
        var sb = new StringBuilder();
        sb.Append($"MemberId={Uri.EscapeDataString(context.Member.MemberId)}");
        sb.Append($"&LOB={Uri.EscapeDataString(context.Lob)}");
        sb.Append($"&Name={Uri.EscapeDataString($"{context.Member.FirstName} {context.Member.LastName}")}");
        sb.Append($"&Group={Uri.EscapeDataString(context.Member.GroupNumber)}");
        sb.Append($"&Plan={Uri.EscapeDataString(context.Member.PlanCode)}");
        return sb.ToString();
    }

    // -----------------------------------------------------------------
    // AdditionalData — computed/derived fields the template can bind to.
    // Strategy is the only place with LOB-awareness; templates are pure layout.
    // -----------------------------------------------------------------
    private static void EnrichAdditionalData(IdCardContext context)
    {
        var d = context.AdditionalData;
        var m = context.Member;
        var p = context.Provider;

        d.TryAdd("MemberName", $"{m.FirstName} {m.LastName}".Trim());
        d.TryAdd("SubscriberName", $"{m.FirstName} {m.LastName}".Trim());
        d.TryAdd("PcpName", p.Name);
        d.TryAdd("PcpPhone", p.Phone);
        d.TryAdd("PcpNpi", p.Npi);
        d.TryAdd("PcpAddress", $"{p.Address}, {p.City}, {p.State} {p.ZipCode}".Trim(',', ' '));
        d.TryAdd("PcpDetails", string.IsNullOrWhiteSpace(p.Name)
            ? string.Empty
            : $"{p.Name}\n{p.Phone}\nNPI: {p.Npi}");

        d.TryAdd("MemberAddress", $"{m.Address}\n{m.City}, {m.State} {m.ZipCode}".Trim());
        d.TryAdd("Copays", $"Office: {m.CopayOffice}  Specialist: {m.CopaySpecialist}\nUR: {m.CopayUrgentCare}  ER: {m.CopayER}");
        d.TryAdd("Deductible", $"Ind: {m.DeductibleIndividual} / Fam: {m.DeductibleFamily}");
        d.TryAdd("OutOfPocketMax", m.OutOfPocketMax);
        d.TryAdd("RxInfo", $"BIN: {m.RxBinNumber}  PCN: {m.RxPcnNumber}  Grp: {m.RxGroupNumber}");
        d.TryAdd("NetworkName", m.NetworkName);
        d.TryAdd("EffectiveDate", m.EffectiveDate);
        d.TryAdd("GroupNumber", m.GroupNumber);
        d.TryAdd("GroupName", m.GroupName);
        d.TryAdd("PlanName", m.PlanName);
    }
}
