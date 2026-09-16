using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using InvoicePortal.Admin.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace InvoicePortal.Admin.Tests;

// Serilog's host integration replaces the process-wide static logger.
[CollectionDefinition("Serilog", DisableParallelization = true)]
public class SerilogCollection;

[Collection("Serilog")]
public class AppLoggingTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings.Select(x =>
            new KeyValuePair<string, string?>(x.Key, x.Value))).Build();

    [Theory]
    [InlineData("CONTAINER_APP_NAME")]
    [InlineData("WEBSITE_INSTANCE_ID")]
    [InlineData("WEBSITE_SITE_NAME")]
    public void Azure_host_markers_enable_cloud_routing(string marker)
    {
        Assert.True(new AppLoggingOptions().IsAzure(Config((marker, "test"))));
    }

    [Fact]
    public void Local_production_containers_and_credentials_do_not_imply_Azure()
    {
        var config = Config(("ASPNETCORE_ENVIRONMENT", "Production"), ("DOTNET_RUNNING_IN_CONTAINER", "true"),
            ("APPLICATIONINSIGHTS_CONNECTION_STRING", "present"), ("DD_API_KEY", "present"));
        var options = new AppLoggingOptions();
        Assert.False(options.IsAzure(config));
        Assert.False(options.UseApplicationInsights(config));
        Assert.False(options.UseDatadog(config));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Explicit_host_override_wins(bool azure)
    {
        Assert.Equal(azure, new AppLoggingOptions { RunningInAzure = azure }
            .IsAzure(Config(("CONTAINER_APP_NAME", "test"))));
    }

    [Fact]
    public void Azure_destinations_require_credentials_and_respect_switches()
    {
        var options = new AppLoggingOptions { RunningInAzure = true };
        Assert.False(options.UseApplicationInsights(Config()));
        Assert.False(options.UseDatadog(Config()));
        var config = Config(("APPLICATIONINSIGHTS_CONNECTION_STRING", "configured"), ("DD_API_KEY", "configured"));
        Assert.True(options.UseApplicationInsights(config));
        Assert.True(options.UseDatadog(config));
        options.ApplicationInsightsEnabled = false;
        options.DatadogEnabled = false;
        Assert.False(options.UseApplicationInsights(config));
        Assert.False(options.UseDatadog(config));
    }

    [Fact]
    public void Configuration_overrides_standard_environment_keys()
    {
        var config = Config(("Telemetry:AzureMonitorConnectionString", "preferred"),
            ("APPLICATIONINSIGHTS_CONNECTION_STRING", "fallback"), ("DD_API_KEY", "fallback"), ("DD_SITE", "datadoghq.com"));
        var options = new AppLoggingOptions { DatadogApiKey = "preferred", DatadogSite = "datadoghq.eu" };
        Assert.Equal("preferred", options.GetApplicationInsightsConnectionString(config));
        Assert.Equal("preferred", options.GetDatadogApiKey(config));
        Assert.Equal("https://http-intake.logs.datadoghq.eu", options.GetDatadogIntakeUrl(config));
    }

    [Theory]
    [InlineData("https://example.org")]
    [InlineData("datadoghq.com.attacker.example")]
    public void Datadog_rejects_untrusted_destinations(string site)
    {
        Assert.Throws<ValidationException>(() => new AppLoggingOptions { DatadogSite = site }.GetDatadogIntakeUrl(Config()));
    }

    [Fact]
    public void Local_retention_is_bounded_and_invalid_limits_are_rejected()
    {
        Assert.Throws<ValidationException>(() => new AppLoggingOptions { FileSizeLimitBytes = 0 }.Validate(Config()));
        Assert.Throws<ValidationException>(() => new AppLoggingOptions { RetainedFileCountLimit = 0 }.Validate(Config()));
        Assert.Throws<ValidationException>(() => new AppLoggingOptions { FilePath = "" }.Validate(Config()));
        new AppLoggingOptions().Validate(Config());
    }

    [Theory]
    [InlineData("WriteTo")]
    [InlineData("AuditTo")]
    public void Configured_sinks_cannot_bypass_host_routing(string section)
    {
        Assert.Throws<ValidationException>(() => new AppLoggingOptions().Validate(
            Config(($"Serilog:{section}:0:Name", "ApplicationInsights"))));
    }

    [Fact]
    public void Missing_credentials_and_duplicate_routes_are_reported_without_secret_values()
    {
        var options = new AppLoggingOptions { RunningInAzure = true };
        var warnings = options.GetWarnings(Config(("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:18889"))).ToArray();
        Assert.Contains(warnings, w => w.Contains("Application Insights logs are disabled"));
        Assert.Contains(warnings, w => w.Contains("Datadog logs are disabled"));
        Assert.Contains(warnings, w => w.Contains("console collection"));
        Assert.Contains(warnings, w => w.Contains("OTLP logs are also enabled"));
        Assert.DoesNotContain(options.GetWarnings(Config(("Telemetry:OtlpLogsEnabled", "false"))), w => w.Contains("OTLP logs"));
        Assert.Empty(new AppLoggingOptions().GetWarnings(Config()));
    }

    [Fact]
    public void ApplicationInsights_converter_uses_captured_trace_and_span_for_logs_and_exceptions()
    {
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        foreach (var exception in new Exception?[] { null, new InvalidOperationException("test exception") })
        {
            var logEvent = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, exception,
                new MessageTemplateParser().Parse("probe"), [], traceId, spanId);
            var telemetry = Assert.Single(new CorrelatedTraceTelemetryConverter("test-service", "test-instance")
                .Convert(logEvent, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(traceId.ToHexString(), telemetry.Context.Operation.Id);
            Assert.Equal(spanId.ToHexString(), telemetry.Context.Operation.ParentId);
            Assert.Equal("test-service", telemetry.Context.Cloud.RoleName);
            Assert.Equal("test-instance", telemetry.Context.Cloud.RoleInstance);
            if (exception is null) Assert.IsType<TraceTelemetry>(telemetry);
            else Assert.IsType<ExceptionTelemetry>(telemetry);
        }
    }

    [Fact]
    public async Task Local_host_writes_structured_file_and_forwards_once_to_OpenTelemetry()
    {
        var directory = Path.Combine(Path.GetTempPath(), "invoiceportal-logging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var builder = CreateBuilder(directory);
            builder.AddInvoicePortalLogging();
            var exporter = new CaptureExporter();
            builder.Logging.AddOpenTelemetry(o => o.AddProcessor(new SimpleLogRecordExportProcessor(exporter)));
            await using (var app = builder.Build())
            {
                var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("LoggingTest");
                using var activity = new Activity("logging-test").SetIdFormat(ActivityIdFormat.W3C).Start();
                using var scope = logger.BeginScope(new Dictionary<string, object> { ["CorrelationProbe"] = "scope-value" });
                logger.LogInformation("File and OTLP probe {ProbeId}", 42);
                var exported = Assert.Single(exporter.Events, e => e.Body.Contains("File and OTLP probe"));
                Assert.Equal(activity.TraceId, exported.TraceId);
            }
            var path = Assert.Single(Directory.GetFiles(directory, "*.json"));
            var line = Assert.Single(File.ReadAllLines(path), l => l.Contains("File and OTLP probe"));
            using var json = JsonDocument.Parse(line);
            Assert.Equal(42, json.RootElement.GetProperty("ProbeId").GetInt32());
            Assert.Equal("scope-value", json.RootElement.GetProperty("CorrelationProbe").GetString());
            Assert.True(json.RootElement.TryGetProperty("@tr", out _));
        }
        finally
        {
            await Log.CloseAndFlushAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task File_sink_rolls_on_size_and_limits_retained_files()
    {
        var directory = Path.Combine(Path.GetTempPath(), "invoiceportal-rolling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var builder = CreateBuilder(directory);
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppLogging:FileSizeLimitBytes"] = "256", ["AppLogging:RetainedFileCountLimit"] = "2"
            });
            builder.AddInvoicePortalLogging();
            await using (var app = builder.Build())
            {
                var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("RollingTest");
                for (var i = 0; i < 10; i++) logger.LogInformation("Rolling {Index} {Payload}", i, new string('x', 300));
            }
            Assert.Equal(2, Directory.GetFiles(directory, "*.json").Length);
        }
        finally
        {
            await Log.CloseAndFlushAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    private static WebApplicationBuilder CreateBuilder(string directory)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Configuration.Sources.Clear(); // Never pick up real credentials or endpoints from the developer's machine.
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppLogging:RunningInAzure"] = "false",
            ["AppLogging:ConsoleEnabled"] = "false",
            ["AppLogging:FilePath"] = Path.Combine(directory, "test-.json")
        });
        return builder;
    }

    private sealed class CaptureExporter : BaseExporter<LogRecord>
    {
        public List<(string Body, ActivityTraceId TraceId)> Events { get; } = [];
        public override ExportResult Export(in Batch<LogRecord> batch)
        {
            foreach (var record in batch) Events.Add((record.Body ?? "", record.TraceId));
            return ExportResult.Success;
        }
    }
}