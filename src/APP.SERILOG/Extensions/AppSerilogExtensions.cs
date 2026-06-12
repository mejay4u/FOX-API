using App.Serilog.Builders;
using App.Serilog.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace App.Serilog.Extensions;

/// <summary>
/// Entry points for wiring App.Serilog into any .NET 8+ host.
/// Typical usage in Program.cs:
/// <code>
/// builder.AddAppSerilog();
/// ...
/// app.UseAppSerilogRequestLogging();
/// </code>
/// </summary>
public static class AppSerilogExtensions
{
    /// <summary>
    /// Reads the "AppSerilog" section, builds the Serilog pipeline and replaces the
    /// default logging providers. Works for WebApplicationBuilder (minimal APIs).
    /// </summary>
    public static WebApplicationBuilder AddAppSerilog(
        this WebApplicationBuilder builder,
        Action<AppSerilogOptions>? configure = null)
    {
        builder.Services.AddAppSerilogOptions(builder.Configuration, configure);
        builder.Host.AddAppSerilog(configure);
        return builder;
    }

    /// <summary>
    /// Same wiring for generic hosts (worker services, classic Startup-based apps).
    /// </summary>
    public static IHostBuilder AddAppSerilog(
        this IHostBuilder hostBuilder,
        Action<AppSerilogOptions>? configure = null)
    {
        return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            var options = BindOptions(context.Configuration, configure);
            if (string.IsNullOrWhiteSpace(options.Environment))
                options.Environment = context.HostingEnvironment.EnvironmentName;

            AppSerilogBuilder.Configure(loggerConfiguration, options, services);
        });
    }

    /// <summary>
    /// Registers AppSerilogOptions for DI so consumers can inject IOptions&lt;AppSerilogOptions&gt;.
    /// Called automatically by AddAppSerilog(WebApplicationBuilder); exposed for advanced scenarios.
    /// </summary>
    public static IServiceCollection AddAppSerilogOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AppSerilogOptions>? configure = null)
    {
        var optionsBuilder = services
            .AddOptions<AppSerilogOptions>()
            .Bind(configuration.GetSection(AppSerilogOptions.SectionName));

        if (configure is not null)
            optionsBuilder.Configure(configure);

        return services;
    }

    /// <summary>
    /// Adds Serilog HTTP request logging middleware (one structured event per request)
    /// when AppSerilog:EnableRequestLogging is true. Place it early in the pipeline.
    /// </summary>
    public static WebApplication UseAppSerilogRequestLogging(this WebApplication app)
    {
        var options = BindOptions(app.Configuration, configure: null);
        if (!options.EnableRequestLogging)
            return app;

        app.UseSerilogRequestLogging(o =>
        {
            o.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            o.GetLevel = (httpContext, _, ex) =>
                ex is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError
                    ? LogEventLevel.Error
                    : httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest
                        ? LogEventLevel.Warning
                        : LogEventLevel.Information;
            o.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            };
        });

        return app;
    }

    /// <summary>
    /// Creates a minimal console logger for capturing startup failures before the host is built:
    /// <code>Log.Logger = AppSerilogExtensions.CreateBootstrapLogger();</code>
    /// </summary>
    public static global::Serilog.ILogger CreateBootstrapLogger() =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

    internal static AppSerilogOptions BindOptions(
        IConfiguration configuration, Action<AppSerilogOptions>? configure)
    {
        var options = new AppSerilogOptions();
        configuration.GetSection(AppSerilogOptions.SectionName).Bind(options);
        configure?.Invoke(options);
        return options;
    }
}
