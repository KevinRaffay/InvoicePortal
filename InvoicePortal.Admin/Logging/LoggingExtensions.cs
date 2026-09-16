using System.Reflection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Datadog.Logs;

namespace InvoicePortal.Admin.Logging;

public static class LoggingExtensions
{
    /// <summary>Call before AddInvoicePortalTelemetry: only the subsequently registered OTLP provider is forwarded to.</summary>
    public static WebApplicationBuilder AddInvoicePortalLogging(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var options = configuration.GetSection(AppLoggingOptions.SectionName).Get<AppLoggingOptions>() ?? new();
        options.Validate(configuration);
        builder.Services.AddSingleton(options);

        // Remove Console/Debug/EventSource providers. Serilog owns console and all remote application logs.
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog((services, logger) =>
        {
            var service = configuration["Telemetry:ServiceName"] ?? "InvoicePortal.Admin";
            var version = typeof(LoggingExtensions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
            logger.MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                // RecordSqlText controls spans, NOT ILogger. Suppress EF SQL (including error command text) separately.
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Fatal)
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", service)
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .Enrich.WithProperty("Version", version)
                .Enrich.WithProperty("MachineName", Environment.MachineName);

            if (options.ConsoleEnabled)
                logger.WriteTo.Console(new RenderedCompactJsonFormatter());

            if (!options.IsAzure(configuration))
            {
                logger.WriteTo.File(new RenderedCompactJsonFormatter(),
                    Path.GetFullPath(options.FilePath, builder.Environment.ContentRootPath),
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: options.FileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: options.RetainedFileCountLimit,
                    shared: true);
            }
            else
            {
                if (options.UseApplicationInsights(configuration))
                    logger.WriteTo.ApplicationInsights(options.GetApplicationInsightsConnectionString(configuration)!,
                        new CorrelatedTraceTelemetryConverter(service, Environment.MachineName));

                if (options.UseDatadog(configuration))
                    logger.WriteTo.DatadogLogs(options.GetDatadogApiKey(configuration)!,
                        source: "csharp", service: configuration["DD_SERVICE"] ?? service,
                        host: Environment.MachineName,
                        tags: [$"env:{configuration["DD_ENV"] ?? builder.Environment.EnvironmentName}",
                            $"version:{configuration["DD_VERSION"] ?? version}"],
                        configuration: new DatadogConfiguration(url: options.GetDatadogIntakeUrl(configuration), useSSL: true, useTCP: false),
                        queueLimit: 10000,
                        // Do not recursively log into this sink or expose credentials in exception messages.
                        exceptionHandler: _ => Console.Error.WriteLine("Datadog log delivery failed. Check the API key, site and network connectivity."));
            }
        }, writeToProviders: true);
        return builder;
    }
}