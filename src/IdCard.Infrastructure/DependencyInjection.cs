using IdCard.Application.Interfaces;
using IdCard.Domain.Interfaces;
using IdCard.Infrastructure.Data;
using IdCard.Infrastructure.Messaging;
using IdCard.Infrastructure.Options;
using IdCard.Infrastructure.QrCode;
using IdCard.Infrastructure.Rendering;
using IdCard.Infrastructure.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace IdCard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── IdCard rendering ────────────────────────────────────────────────
        services.Configure<IdCardOptions>(opts =>
        {
            var section = configuration.GetSection(IdCardOptions.SectionName);
            opts.BasePath = section[nameof(IdCardOptions.BasePath)] ?? IdCardOptions.DefaultBasePath;
        });

        services.AddSingleton<ITemplateResolver, TemplateResolver>();
        services.AddSingleton<IBindingResolver, BindingResolver>();
        services.AddSingleton<IQrCodeService, QrCodeService>();
        services.AddSingleton<IIdCardRenderer, SkiaIdCardRenderer>();

        // ── IBM MQ ──────────────────────────────────────────────────────────
        services.Configure<MqOptions>(configuration.GetSection(MqOptions.SectionName));

        var mqEnabled = configuration
            .GetSection(MqOptions.SectionName)
            .GetValue<bool>(nameof(MqOptions.Enabled));

        if (mqEnabled)
        {
            services.AddSingleton<IIdCardMqGateway, IbmMqGateway>();
            services.AddSingleton<IMemberDataService, IbmMqMemberDataService>();
        }
        else
        {
            // Mock data — swap for real repositories without touching any other layer
            services.AddSingleton<IMemberDataService, MockMemberDataService>();
        }

        services.AddSingleton<IProviderDataService, MockProviderDataService>();

        return services;
    }
}
