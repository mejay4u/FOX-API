using IdCard.Application.Interfaces;
using IdCard.Domain.Interfaces;
using IdCard.Infrastructure.Data;
using IdCard.Infrastructure.Email;
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

        // Member + provider data for local card rendering (swap for real DB service when ready)
        services.AddSingleton<IMemberDataService, MockMemberDataService>();
        services.AddSingleton<IProviderDataService, MockProviderDataService>();

        // ── IBM MQ ──────────────────────────────────────────────────────────
        services.Configure<MqOptions>(configuration.GetSection(MqOptions.SectionName));

        var mqEnabled = configuration
            .GetSection(MqOptions.SectionName)
            .GetValue<bool>(nameof(MqOptions.Enabled));

        if (mqEnabled)
            services.AddSingleton<IIdCardMqGateway, IbmMqGateway>();
        else
            services.AddSingleton<IIdCardMqGateway, NullIdCardMqGateway>();

        // ── Email ────────────────────────────────────────────────────────────
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IEmailTemplateService, HtmlTemplateService>();

        return services;
    }
}
