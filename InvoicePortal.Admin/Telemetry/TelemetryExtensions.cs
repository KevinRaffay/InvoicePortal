using System.Reflection;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace InvoicePortal.Admin.Telemetry;

public static class TelemetryExtensions
{
    /// <summary>
    /// Registers OpenTelemetry traces, metrics and logs for the admin app.
    /// <list type="bullet">
    /// <item>Traces: incoming requests (minus health checks and static assets), outgoing HTTP (Ollama, Azure OpenAI,
    /// Blob Storage), SQL Server commands issued by EF Core and the guarded executor, one span per chat-model call,
    /// and the app's own spans in <see cref="InvoicePortalTelemetry.Source"/>.</item>
    /// <item>Metrics: ASP.NET Core, Kestrel, HttpClient, .NET runtime, EF Core, chat token usage and the app's own counters.</item>
    /// <item>Logs: Serilog forwards to the OTLP provider for Aspire/collectors when enabled.
    /// Application Insights and Datadog logs are sent by Serilog only.</item>
    /// </list>
    /// Exporters depend on configuration: OTLP when <c>Telemetry:OtlpEndpoint</c> (or <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>)
    /// is set, Azure Monitor when <c>Telemetry:AzureMonitorConnectionString</c> (or <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c>)
    /// is set. Both can be active at once. With neither, nothing is exported and nothing leaves the machine.
    /// </summary>
    public static WebApplicationBuilder AddInvoicePortalTelemetry(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<TelemetryOptions>()
            .Bind(builder.Configuration.GetSection(TelemetryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = builder.Configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>() ?? new TelemetryOptions();
        if (!options.Enabled)
        {
            return builder;
        }

        var otlpEndpoint = FirstNonEmpty(options.OtlpEndpoint, builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        var otlpProtocol = ParseOtlpProtocol(FirstNonEmpty(options.OtlpProtocol, builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]));
        var azureMonitor = FirstNonEmpty(options.AzureMonitorConnectionString, builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);

        var otel = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: options.ServiceName,
                    serviceVersion: Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(
                [
                    new KeyValuePair<string, object>("deployment.environment.name", builder.Environment.EnvironmentName),
                ]));

        otel.WithTracing(tracing =>
        {
            tracing
                .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.TraceSamplingRatio)))
                // Before the exporters: un-records Blazor's per-hub-call plumbing spans (see the class remarks).
                .AddProcessor(new NoiseFilteringProcessor())
                .AddSource(InvoicePortalTelemetry.SourceName)
                .AddSource(InvoicePortalTelemetry.AiSourceName)
                .AddAspNetCoreInstrumentation(o =>
                {
                    o.RecordException = true;
                    // Container probes and Blazor's framework/static files would otherwise dominate the trace list.
                    o.Filter = context => !IsNoiseRequest(context.Request.Path);
                })
                .AddHttpClientInstrumentation(o => o.RecordException = true)
                .AddSqlClientInstrumentation(o =>
                {
                    o.RecordException = true;
                    // The instrumentation records db.query.text on every command. Strip it unless Telemetry:RecordSqlText is on:
                    // the CRUD queries and AI-generated SQL run against a copy of production data. The span keeps its name,
                    // db.query.summary (operation + table) and timings, which is enough to see what ran and how long it took.
                    if (!options.RecordSqlText)
                    {
                        o.EnrichWithSqlCommand = (activity, _) =>
                        {
                            activity.SetTag("db.query.text", null);
                            activity.SetTag("db.statement", null);
                        };
                    }
                });

            if (otlpEndpoint is not null)
            {
                tracing.AddOtlpExporter(o => ConfigureOtlp(o, otlpEndpoint, otlpProtocol));
            }
            if (azureMonitor is not null)
            {
                tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = azureMonitor);
            }
        });

        otel.WithMetrics(metrics =>
        {
            metrics
                .AddMeter(InvoicePortalTelemetry.SourceName)
                .AddMeter(InvoicePortalTelemetry.AiSourceName)
                // EF Core 9+ publishes query, connection and save-changes metrics under its own meter name.
                .AddMeter("Microsoft.EntityFrameworkCore")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddRuntimeInstrumentation();

            if (otlpEndpoint is not null)
            {
                metrics.AddOtlpExporter(o => ConfigureOtlp(o, otlpEndpoint, otlpProtocol));
            }
            if (azureMonitor is not null)
            {
                metrics.AddAzureMonitorMetricExporter(o => o.ConnectionString = azureMonitor);
            }
        });

        if (options.OtlpLogsEnabled && otlpEndpoint is not null)
        {
            otel.WithLogging(
                logging => logging.AddOtlpExporter(o => ConfigureOtlp(o, otlpEndpoint, otlpProtocol)),
                loggerOptions =>
                {
                    loggerOptions.IncludeFormattedMessage = true;
                    loggerOptions.IncludeScopes = true;
                    loggerOptions.ParseStateValues = true;
                });
        }

        return builder;
    }

    /// <summary>Paths excluded from request tracing: the health probe and Blazor's framework and static assets.</summary>
    public static bool IsNoiseRequest(PathString path) =>
        path.StartsWithSegments("/healthz")
        || path.StartsWithSegments("/_framework")
        || path.StartsWithSegments("/_content")
        || path.StartsWithSegments("/_blazor")
        || path.StartsWithSegments("/lib")
        || path.Value is { } value && (value.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                                     || value.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                                     || value.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)
                                     || value.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
                                     || value.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

    private static void ConfigureOtlp(OtlpExporterOptions o, string endpoint, OtlpExportProtocol protocol)
    {
        o.Endpoint = new Uri(endpoint);
        o.Protocol = protocol;
    }

    private static OtlpExportProtocol ParseOtlpProtocol(string? value) =>
        string.Equals(value, "http/protobuf", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}
