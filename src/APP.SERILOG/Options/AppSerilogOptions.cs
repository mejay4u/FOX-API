namespace App.Serilog.Options;

/// <summary>
/// Root options bound from the "AppSerilog" configuration section.
/// Enable/disable individual sinks purely via configuration.
/// </summary>
public sealed class AppSerilogOptions
{
    /// <summary>Configuration section name expected in appsettings.json.</summary>
    public const string SectionName = "AppSerilog";

    /// <summary>Logical application name stamped on every log event.</summary>
    public string ApplicationName { get; set; } = "Application";

    /// <summary>Deployment environment stamped on every log event (e.g. Development, Production).</summary>
    public string? Environment { get; set; }

    /// <summary>Global minimum level: Verbose, Debug, Information, Warning, Error, Fatal.</summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>Per-namespace level overrides, e.g. { "Microsoft.AspNetCore": "Warning" }.</summary>
    public Dictionary<string, string> Overrides { get; set; } = new()
    {
        ["Microsoft.AspNetCore"] = "Warning",
        ["System"] = "Warning"
    };

    /// <summary>Enable Serilog request logging middleware (UseAppSerilogRequestLogging).</summary>
    public bool EnableRequestLogging { get; set; } = true;

    public ConsoleSinkOptions Console { get; set; } = new();
    public JsonFileSinkOptions JsonFile { get; set; } = new();
    public ApplicationInsightsSinkOptions ApplicationInsights { get; set; } = new();
    public DynatraceSinkOptions Dynatrace { get; set; } = new();
}

/// <summary>Base for every sink: each can be switched on/off and have its own minimum level.</summary>
public abstract class SinkOptionsBase
{
    /// <summary>Master switch for this sink.</summary>
    public bool Enabled { get; set; }

    /// <summary>Optional sink-specific minimum level; falls back to the global level when null.</summary>
    public string? MinimumLevel { get; set; }
}

/// <summary>Local machine console output (human-readable by default).</summary>
public sealed class ConsoleSinkOptions : SinkOptionsBase
{
    /// <summary>When true, writes compact JSON to the console instead of plain text.</summary>
    public bool UseJsonFormat { get; set; }

    public string OutputTemplate { get; set; } =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}      {Message:lj}{NewLine}{Exception}";
}

/// <summary>Rolling JSON file on disk at a configurable location.</summary>
public sealed class JsonFileSinkOptions : SinkOptionsBase
{
    /// <summary>Target path; rolling suffix is appended automatically (e.g. logs/app-20260612.json).</summary>
    public string Path { get; set; } = "logs/log-.json";

    /// <summary>Rolling interval: Infinite, Year, Month, Day, Hour, Minute.</summary>
    public string RollingInterval { get; set; } = "Day";

    /// <summary>Max size per file in bytes before rolling. Null = unlimited.</summary>
    public long? FileSizeLimitBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>How many rolled files to keep. Null = keep all.</summary>
    public int? RetainedFileCountLimit { get; set; } = 31;

    /// <summary>Roll to a new file when the size limit is reached.</summary>
    public bool RollOnFileSizeLimit { get; set; } = true;

    /// <summary>Allow multiple processes to share the file.</summary>
    public bool Shared { get; set; }
}

/// <summary>Azure Application Insights sink.</summary>
public sealed class ApplicationInsightsSinkOptions : SinkOptionsBase
{
    /// <summary>Full Application Insights connection string (preferred over instrumentation key).</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Send events as Traces (default) or Events.</summary>
    public string TelemetryConverter { get; set; } = "Traces";
}

/// <summary>Dynatrace log ingest sink (Log Monitoring v2 API).</summary>
public sealed class DynatraceSinkOptions : SinkOptionsBase
{
    /// <summary>Dynatrace API token with logs.ingest scope.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Ingest endpoint, e.g. https://{your-env-id}.live.dynatrace.com/api/v2/logs/ingest</summary>
    public string? IngestUrl { get; set; }

    /// <summary>Optional host name reported to Dynatrace; defaults to the machine name.</summary>
    public string? HostName { get; set; }
}
