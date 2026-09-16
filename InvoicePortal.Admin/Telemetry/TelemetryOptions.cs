using System.ComponentModel.DataAnnotations;

namespace InvoicePortal.Admin.Telemetry;

/// <summary>
/// Bound from the <c>Telemetry</c> configuration section (env vars <c>Telemetry__*</c>). The exporters are
/// chosen by what is set: an OTLP endpoint (local Aspire Dashboard, a collector, Jaeger, Grafana, ...) and/or
/// an Application Insights connection string (set by <c>infra/resources.bicep</c> in Azure). With neither,
/// the SDK still runs so activities and metrics exist in-process, but nothing leaves the machine.
/// </summary>
public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    /// <summary>Master switch. When false no OpenTelemetry provider is registered at all.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Forward Serilog logs through OTLP. Turn off when a collector would duplicate direct cloud sinks.</summary>
    public bool OtlpLogsEnabled { get; set; } = true;

    /// <summary><c>service.name</c> on every exported resource.</summary>
    [Required]
    public string ServiceName { get; set; } = "InvoicePortal.Admin";

    /// <summary>
    /// OTLP endpoint, e.g. <c>http://localhost:4317</c> (gRPC) or <c>http://localhost:4318</c> (HTTP/protobuf).
    /// Falls back to the standard <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable when empty.
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// <c>grpc</c> or <c>http/protobuf</c>. Falls back to <c>OTEL_EXPORTER_OTLP_PROTOCOL</c>, then to gRPC.
    /// </summary>
    [RegularExpression("^(|grpc|http/protobuf)$", ErrorMessage = "Telemetry:OtlpProtocol must be 'grpc' or 'http/protobuf'.")]
    public string? OtlpProtocol { get; set; }

    /// <summary>
    /// Application Insights connection string. Falls back to <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c>,
    /// which is what the Azure template sets on the container.
    /// </summary>
    public string? AzureMonitorConnectionString { get; set; }

    /// <summary>
    /// Whether SQL statement text is recorded on database spans. Off by default: generated SQL from the AI
    /// feature and the CRUD queries can contain literal values from the production copy of the data.
    /// </summary>
    public bool RecordSqlText { get; set; }

    /// <summary>
    /// Whether prompts and completions are recorded on chat spans. Off by default for the same reason;
    /// the prompts embed the schema and document content.
    /// </summary>
    public bool RecordAiContent { get; set; }

    /// <summary>Head sampling ratio for traces, 0.0 to 1.0. Root spans only; children follow their parent.</summary>
    [Range(0.0, 1.0)]
    public double TraceSamplingRatio { get; set; } = 1.0;
}
