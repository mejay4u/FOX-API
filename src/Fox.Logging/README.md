# Fox.Logging

Config-driven Serilog logging library for any **.NET 8+** API or worker service.
All sinks are switched on/off purely through `appsettings.json` — no code changes needed
to move between local development (console / JSON file) and cloud (Application Insights / Dynatrace).

## Sinks

| Sink | Config key | Use case |
|---|---|---|
| Console | `FoxLogging:Console` | Local machine, container stdout |
| JSON File | `FoxLogging:JsonFile` | Rolling compact-JSON files at any file location |
| Application Insights | `FoxLogging:ApplicationInsights` | Azure cloud telemetry |
| Dynatrace | `FoxLogging:Dynatrace` | Dynatrace Log Monitoring v2 ingest |

## Installation

Project reference (until packaged):

```xml
<ProjectReference Include="..\Fox.Logging\Fox.Logging.csproj" />
```

Or as NuGet package after packing:

```bash
dotnet pack src/Fox.Logging -c Release
dotnet add package Fox.Logging --source ./src/Fox.Logging/bin/Release
```

## Usage (Program.cs)

```csharp
using Fox.Logging.Extensions;
using Serilog;

Log.Logger = FoxLoggingExtensions.CreateBootstrapLogger(); // optional: capture startup errors

var builder = WebApplication.CreateBuilder(args);
builder.AddFoxLogging();          // reads the "FoxLogging" section

var app = builder.Build();
app.UseFoxRequestLogging();       // one structured event per HTTP request

app.Run();
```

Worker services / generic host: `hostBuilder.AddFoxLogging()`.

## Configuration (appsettings.json)

```json
{
  "FoxLogging": {
    "ApplicationName": "IdCard.API",
    "MinimumLevel": "Information",
    "EnableRequestLogging": true,
    "Overrides": {
      "Microsoft.AspNetCore": "Warning",
      "System": "Warning"
    },
    "Console": {
      "Enabled": true,
      "UseJsonFormat": false
    },
    "JsonFile": {
      "Enabled": true,
      "Path": "logs/idcard-.json",
      "RollingInterval": "Day",
      "FileSizeLimitBytes": 52428800,
      "RetainedFileCountLimit": 31
    },
    "ApplicationInsights": {
      "Enabled": false,
      "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=...",
      "TelemetryConverter": "Traces"
    },
    "Dynatrace": {
      "Enabled": false,
      "AccessToken": "dt0c01.XXXX",
      "IngestUrl": "https://{env-id}.live.dynatrace.com/api/v2/logs/ingest"
    }
  }
}
```

### Switching sinks per environment

Put environment-specific blocks in `appsettings.Development.json` / `appsettings.Production.json`,
or override with environment variables, e.g.:

```
FoxLogging__ApplicationInsights__Enabled=true
FoxLogging__ApplicationInsights__ConnectionString=...
FoxLogging__Console__Enabled=false
```

Each sink also supports its own `MinimumLevel`, e.g. console at `Debug` while
Application Insights only receives `Warning` and above.

### Notes

- Secrets (AppInsights connection string, Dynatrace token) should come from
  user-secrets, Key Vault, or environment variables — not committed JSON.
- If your app also calls `AddApplicationInsightsTelemetry()`, the sink reuses that
  `TelemetryConfiguration` so logs correlate with requests and dependencies.
- Options are registered for DI: inject `IOptions<FoxLoggingOptions>` anywhere.
