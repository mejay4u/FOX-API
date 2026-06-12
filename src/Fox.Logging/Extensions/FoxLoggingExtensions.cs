using Fox.Logging.Builders;
using Fox.Logging.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Fox.Logging.Extensions;

/// <summary>
/// Entry points for wiring Fox.Logging into any .NET 8+ host.
/// Typical usage in Program.cs:
/// <code>
/// builder.AddFoxLogging();
/// ...
/// app.UseFoxRequestLogging();
/// </code>
/// </summary>
public static class FoxLoggingExtensions
{
    /// <summary>
    /// Reads the "FoxLogging" section, builds the Serilog pipeline and replaces the
    /// default logging providers. Works for WebApplicationBuilder (minimal APIs).
    /// </summary>
    public static WebApplicationBuilder AddFoxLogging(
        this WebApplicationBuilder builder,
        Action<FoxLoggingOptions>? configure = null)
    {
        builder.Services.AddFoxLoggingOptions(builder.Configuration, configure);
        builder.Host.AddFoxLogging(configure);
        return builder;
    }

    /// <summary>
    /// Same wiring for generic hosts (worker services, classic Startup-based apps).
    /// </summary>
    public static IHostBuilder AddFoxLogging(
        this IHostBuilder hostBuilder,
        Action<FoxLoggingOptions>? configure = null)
    {
        return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            var options = BindOptions(context.Configuration, configure);
            if (string.IsNullOrWhiteSpace(options.Environment))
                options.Environment = context.HostingEnvironment.EnvironmentName;

            FoxLoggerBuilder.Configure(loggerConfiguration, options, services);
        });
    }

    /// <summary>
    /// Registers FoxLoggingOptions for DI so consumers can inject IOptions&lt;FoxLoggingOptions&gt;.
    /// Called automatically by AddFoxLogging(WebApplicationBuilder); exposed for advanced scenarios.
    /// </summary>
    public static IServiceCollection AddFoxLoggingOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<FoxLoggingOptions>? configure = null)
    {
        var optionsBuilder = services
            .AddOptions<FoxLoggingOptions>()
            .Bind(configuration.GetSection(FoxLoggingOptions.SectionName));

        if (configure is not null)
            optionsBuilder.Configure(configure);

        return services;
    }

    /// <summary>
    /// Adds Serilog HTTP request logging middleware (one structured event per request)
    /// when FoxLogging:EnableRequestLogging is true. Place it early in the pipeline.
    /// </summary>
    public static WebApplication UseFoxRequestLogging(this WebApplication app)
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
    /// <code>Log.Logger = FoxLoggingExtensions.CreateBootstrapLogger();</code>
    /// </summary>
    public static Serilog.ILogger CreateBootstrapLogger() =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

    internal static FoxLoggingOptions BindOptions(
        IConfiguration configuration, Action<FoxLoggingOptions>? configure)
    {
        var options = new FoxLoggingOptions();
        configuration.GetSection(FoxLoggingOptions.SectionName).Bind(options);
        configure?.Invoke(options);
        return options;
    }
}
