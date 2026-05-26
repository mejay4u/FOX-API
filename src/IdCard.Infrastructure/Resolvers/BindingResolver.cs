using System.Reflection;
using IdCard.Domain.Interfaces;
using IdCard.Domain.Models;

namespace IdCard.Infrastructure.Resolvers;

/// <summary>
/// Resolves template binding keys to string values.
///
/// Resolution order (no switch-case anywhere):
///   1. Computed dictionary  — derived values built once per context (MemberName, PcpDetails, …)
///   2. Reflection lookup    — "Member.FirstName", "Provider.Npi", etc.
///   3. AdditionalData       — strategy-populated LOB-specific values
///   4. Empty string         — safe fallback; never throws on missing keys
/// </summary>
public sealed class BindingResolver : IBindingResolver
{
    // Static map: prefix string → accessor lambda (no switch-case, O(1) lookup)
    private static readonly Dictionary<string, Func<IdCardContext, object?>> ObjectAccessors =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Member"]   = ctx => ctx.Member,
            ["Provider"] = ctx => ctx.Provider,
        };

    public string Resolve(string binding, IdCardContext context)
    {
        if (string.IsNullOrWhiteSpace(binding))
            return string.Empty;

        // 1. Computed (pre-built per call — strategy may have already populated AdditionalData)
        var computed = BuildComputedValues(context);
        if (computed.TryGetValue(binding, out var computedValue))
            return computedValue;

        // 2. Reflection: "Member.FirstName", "Provider.Npi", "Member.DeductibleIndividual", …
        if (binding.Contains('.'))
        {
            var reflectionResult = ResolveViaReflection(binding, context);
            if (!string.IsNullOrEmpty(reflectionResult))
                return reflectionResult;
        }

        // 3. AdditionalData (strategy-enriched LOB fields)
        if (context.AdditionalData.TryGetValue(binding, out var additional))
            return additional;

        return string.Empty;
    }

    // ──────────────────────────────────────────────────────────────────
    // Computed values — derived strings that combine multiple raw fields
    // ──────────────────────────────────────────────────────────────────
    private static Dictionary<string, string> BuildComputedValues(IdCardContext ctx)
    {
        var m = ctx.Member;
        var p = ctx.Provider;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MemberName"]     = $"{m.FirstName} {m.LastName}".Trim(),
            ["FullName"]       = $"{m.FirstName} {m.LastName}".Trim(),
            ["SubscriberName"] = $"{m.FirstName} {m.LastName}".Trim(),
            ["PcpName"]        = p.Name,
            ["PcpPhone"]       = p.Phone,
            ["PcpNpi"]         = p.Npi,
            ["PcpDetails"]     = string.IsNullOrWhiteSpace(p.Name)
                                    ? string.Empty
                                    : $"{p.Name}\n{p.Phone}\nNPI: {p.Npi}",
            ["MemberAddress"]  = $"{m.Address}\n{m.City}, {m.State} {m.ZipCode}".Trim(),
            ["RxInfo"]         = $"BIN: {m.RxBinNumber}  PCN: {m.RxPcnNumber}  Grp: {m.RxGroupNumber}",
            ["Copays"]         = $"Office: {m.CopayOffice} | Specialist: {m.CopaySpecialist}\n" +
                                 $"UR: {m.CopayUrgentCare} | ER: {m.CopayER}",
            ["Deductible"]     = $"Ind: {m.DeductibleIndividual} / Fam: {m.DeductibleFamily}",
            ["EffectiveDate"]  = m.EffectiveDate,
            ["PlanName"]       = m.PlanName,
            ["GroupNumber"]    = m.GroupNumber,
            ["GroupName"]      = m.GroupName,
            ["NetworkName"]    = m.NetworkName,
            ["MemberId"]       = m.MemberId,
            ["SubscriberId"]   = m.SubscriberId,
        };
    }

    // ──────────────────────────────────────────────────────────────────
    // Reflection-based lookup for "Object.Property[.NestedProperty]"
    // ──────────────────────────────────────────────────────────────────
    private static string ResolveViaReflection(string binding, IdCardContext context)
    {
        var dotIndex  = binding.IndexOf('.');
        var prefix    = binding[..dotIndex];
        var propPath  = binding[(dotIndex + 1)..];

        if (!ObjectAccessors.TryGetValue(prefix, out var accessor))
            return string.Empty;

        var root = accessor(context);
        return root is null ? string.Empty : WalkPropertyPath(root, propPath);
    }

    private static string WalkPropertyPath(object root, string path)
    {
        object? current = root;
        foreach (var segment in path.Split('.'))
        {
            if (current is null) return string.Empty;
            var prop = current.GetType().GetProperty(
                segment, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            current = prop?.GetValue(current);
        }
        return current?.ToString() ?? string.Empty;
    }
}
