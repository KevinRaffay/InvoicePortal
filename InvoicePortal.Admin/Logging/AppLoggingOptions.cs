using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace InvoicePortal.Admin.Logging;

/// <summary>Sink routing, separate from Serilog's level configuration. Never log this object: it contains credentials.</summary>
public sealed class AppLoggingOptions
{
    public const string SectionName = "AppLogging";

    public bool? RunningInAzure { get; set; }
    public bool ConsoleEnabled { get; set; } = true;
    public bool ApplicationInsightsEnabled { get; set; } = true;
    public string FilePath { get; set; } = "logs/invoiceportal-.json";
    public long FileSizeLimitBytes { get; set; } = 10 * 1024 * 1024;
    public int RetainedFileCountLimit { get; set; } = 14;
    public bool DatadogEnabled { get; set; } = true;
    public string? DatadogApiKey { get; set; }
    public string? DatadogSite { get; set; }

    /// <summary>One log record per HTTP request (see <see cref="RequestLoggingMiddleware"/>). Set false to turn it off.</summary>
    public bool RequestLoggingEnabled { get; set; } = true;

    public bool IsAzure(IConfiguration configuration) => RunningInAzure ??
        (!string.IsNullOrWhiteSpace(configuration["CONTAINER_APP_NAME"])
         || !string.IsNullOrWhiteSpace(configuration["WEBSITE_INSTANCE_ID"])
         || !string.IsNullOrWhiteSpace(configuration["WEBSITE_SITE_NAME"]));

    public string? GetApplicationInsightsConnectionString(IConfiguration configuration) =>
        FirstNonEmpty(configuration["Telemetry:AzureMonitorConnectionString"],
            configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);

    public string? GetDatadogApiKey(IConfiguration configuration) =>
        FirstNonEmpty(DatadogApiKey, configuration["DD_API_KEY"]);

    public string GetDatadogIntakeUrl(IConfiguration configuration)
    {
        var site = FirstNonEmpty(DatadogSite, configuration["DD_SITE"]) ?? "datadoghq.com";
        // Restrict destinations so a typo cannot send credentials to an arbitrary host.
        if (site is not ("datadoghq.com" or "us3.datadoghq.com" or "us5.datadoghq.com"
            or "datadoghq.eu" or "ap1.datadoghq.com" or "ap2.datadoghq.com" or "ddog-gov.com"))
            throw new ValidationException("AppLogging:DatadogSite (or DD_SITE) must be a supported Datadog site hostname.");
        return $"https://http-intake.logs.{site}";
    }

    public bool UseApplicationInsights(IConfiguration configuration) =>
        IsAzure(configuration) && ApplicationInsightsEnabled && GetApplicationInsightsConnectionString(configuration) is not null;

    public bool UseDatadog(IConfiguration configuration) =>
        IsAzure(configuration) && DatadogEnabled && GetDatadogApiKey(configuration) is not null;

    public void Validate(IConfiguration configuration)
    {
        if (configuration.GetSection("Serilog:WriteTo").Exists()
            || configuration.GetSection("Serilog:AuditTo").Exists())
            throw new ValidationException("Configure destinations through AppLogging, not Serilog:WriteTo/AuditTo; sinks are selected by hosting to prevent duplicate logs and unintended cloud export.");
        if (!IsAzure(configuration) && (string.IsNullOrWhiteSpace(FilePath)
            || FileSizeLimitBytes <= 0 || RetainedFileCountLimit <= 0))
            throw new ValidationException("Local logging requires a file path, positive file size, and positive retained file count.");
        if (UseDatadog(configuration))
            _ = GetDatadogIntakeUrl(configuration);
    }

    public IEnumerable<string> GetWarnings(IConfiguration configuration)
    {
        if (!IsAzure(configuration))
            yield break;
        if (ApplicationInsightsEnabled && !UseApplicationInsights(configuration))
            yield return "Azure logging: Application Insights logs are disabled because APPLICATIONINSIGHTS_CONNECTION_STRING (or Telemetry:AzureMonitorConnectionString) is missing.";
        if (DatadogEnabled && !UseDatadog(configuration))
            yield return "Azure logging: Datadog logs are disabled because DD_API_KEY (or AppLogging:DatadogApiKey) is missing. Supply it through a secret reference, not source control.";
        if (ConsoleEnabled)
            yield return "Azure logging: console collection by Container Apps/agents can store a second copy of Serilog sink logs. Disable AppLogging:ConsoleEnabled or the collector's application-log forwarding if redundant.";
        if (configuration.GetValue("Telemetry:Enabled", true)
            && configuration.GetValue("Telemetry:OtlpLogsEnabled", true)
            && FirstNonEmpty(configuration["Telemetry:OtlpEndpoint"], configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]) is not null)
            yield return "Azure logging: OTLP logs are also enabled. If the collector forwards to Application Insights or Datadog, set Telemetry:OtlpLogsEnabled=false to avoid duplicate logs; traces and metrics are unaffected.";
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}