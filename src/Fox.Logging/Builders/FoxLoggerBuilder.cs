using Fox.Logging.Options;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Dynatrace;

namespace Fox.Logging.Builders;

/// <summary>
/// Translates <see cref="FoxLoggingOptions"/> into a configured Serilog <see cref="LoggerConfiguration"/>.
/// Each sink is attached only when its Enabled flag is true, so sinks are switched purely via config.
/// </summary>
public static class FoxLoggerBuilder
{
    /// <summary>Builds a standalone configuration (useful for tests or non-host scenarios).</summary>
    public static LoggerConfiguration Build(FoxLoggingOptions options, IServiceProvider? services = null) =>
        Configure(new LoggerConfiguration(), options, services);

    /// <summary>Applies the Fox pipeline onto an existing configuration (used by UseSerilog).</summary>
    public static LoggerConfiguration Configure(
        LoggerConfiguration configuration, FoxLoggingOptions options, IServiceProvider? services = null)
    {
        configuration
            .MinimumLevel.Is(ParseLevel(options.MinimumLevel))
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("ApplicationName", options.ApplicationName);

        if (!string.IsNullOrWhiteSpace(options.Environment))
            configuration.Enrich.WithProperty("Environment", options.Environment);

        foreach (var (source, level) in options.Overrides)
            configuration.MinimumLevel.Override(source, ParseLevel(level));

        AddConsole(configuration, options);
        AddJsonFile(configuration, options);
        AddApplicationInsights(configuration, options, services);
        AddDynatrace(configuration, options);

        return configuration;
    }

    private static void AddConsole(LoggerConfiguration configuration, FoxLoggingOptions options)
    {
        var sink = options.Console;
        if (!sink.Enabled) return;

        var level = ParseLevel(sink.MinimumLevel, options.MinimumLevel);
        if (sink.UseJsonFormat)
            configuration.WriteTo.Console(new CompactJsonFormatter(), restrictedToMinimumLevel: level);
        else
            configuration.WriteTo.Console(outputTemplate: sink.OutputTemplate, restrictedToMinimumLevel: level);
    }

    private static void AddJsonFile(LoggerConfiguration configuration, FoxLoggingOptions options)
    {
        var sink = options.JsonFile;
        if (!sink.Enabled) return;

        configuration.WriteTo.File(
            formatter: new CompactJsonFormatter(),
            path: sink.Path,
            restrictedToMinimumLevel: ParseLevel(sink.MinimumLevel, options.MinimumLevel),
            rollingInterval: Enum.TryParse<RollingInterval>(sink.RollingInterval, ignoreCase: true, out var interval)
                ? interval
                : RollingInterval.Day,
            fileSizeLimitBytes: sink.FileSizeLimitBytes,
            retainedFileCountLimit: sink.RetainedFileCountLimit,
            rollOnFileSizeLimit: sink.RollOnFileSizeLimit,
            shared: sink.Shared);
    }

    private static void AddApplicationInsights(
        LoggerConfiguration configuration, FoxLoggingOptions options, IServiceProvider? services)
    {
        var sink = options.ApplicationInsights;
        if (!sink.Enabled) return;

        if (string.IsNullOrWhiteSpace(sink.ConnectionString))
            throw new InvalidOperationException(
                "FoxLogging:ApplicationInsights:ConnectionString is required when the Application Insights sink is enabled.");

        // Reuse the app's TelemetryConfiguration when AddApplicationInsightsTelemetry was registered,
        // so logs correlate with requests/dependencies; otherwise create a standalone one.
        var telemetryConfiguration =
            services?.GetService(typeof(TelemetryConfiguration)) as TelemetryConfiguration
            ?? new TelemetryConfiguration { ConnectionString = sink.ConnectionString };
        telemetryConfiguration.ConnectionString = sink.ConnectionString;

        var converter = sink.TelemetryConverter.Equals("Events", StringComparison.OrdinalIgnoreCase)
            ? TelemetryConverter.Events
            : TelemetryConverter.Traces;

        configuration.WriteTo.ApplicationInsights(
            telemetryConfiguration,
            converter,
            restrictedToMinimumLevel: ParseLevel(sink.MinimumLevel, options.MinimumLevel));
    }

    private static void AddDynatrace(LoggerConfiguration configuration, FoxLoggingOptions options)
    {
        var sink = options.Dynatrace;
        if (!sink.Enabled) return;

        if (string.IsNullOrWhiteSpace(sink.AccessToken) || string.IsNullOrWhiteSpace(sink.IngestUrl))
            throw new InvalidOperationException(
                "FoxLogging:Dynatrace:AccessToken and FoxLogging:Dynatrace:IngestUrl are required when the Dynatrace sink is enabled.");

        configuration.WriteTo.Dynatrace(
            accessToken: sink.AccessToken,
            ingestUrl: sink.IngestUrl,
            applicationId: options.ApplicationName,
            hostName: sink.HostName ?? System.Environment.MachineName,
            restrictedToMinimumLevel: ParseLevel(sink.MinimumLevel, options.MinimumLevel));
    }

    private static LogEventLevel ParseLevel(string? level, string? fallback = null) =>
        Enum.TryParse<LogEventLevel>(level, ignoreCase: true, out var parsed)
            ? parsed
            : Enum.TryParse<LogEventLevel>(fallback, ignoreCase: true, out var parsedFallback)
                ? parsedFallback
                : LogEventLevel.Information;
}
