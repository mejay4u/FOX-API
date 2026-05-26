using IdCard.Application.Interfaces;
using IdCard.Application.Services;
using IdCard.Application.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace IdCard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IdCardAggregator>();
        services.AddScoped<IIdCardStrategy, MemberCardStrategy>();
        return services;
    }
}
