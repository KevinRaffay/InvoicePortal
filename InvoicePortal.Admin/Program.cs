using InvoicePortal.Admin.Ai;
using InvoicePortal.Admin.Components;
using InvoicePortal.Admin.Data;
using InvoicePortal.Admin.Logging;
using InvoicePortal.Admin.Services;
using InvoicePortal.Admin.Telemetry;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Serilog;

namespace InvoicePortal.Admin;

// Explicit entry-point class (instead of top-level statements) so the compiler-generated
// global "Program" class does not shadow the scaffolded Data.Entities.Program entity.
public static class EntryPoint
{
    public static async Task Main(string[] args)
    {
        // Console-only until configuration is available; never guess an Azure destination at bootstrap.
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
        try
        {
            await RunApplicationAsync(args);
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Admin terminated unexpectedly");
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static async Task RunApplicationAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddInvoicePortalLogging();

        var connectionString = builder.Configuration.GetConnectionString("InvoicePortal")
            ?? throw new InvalidOperationException(
                "Connection string 'InvoicePortal' is missing. Set the ConnectionStrings__InvoicePortal environment variable (see docker-compose.yml).");

        // OpenTelemetry owns traces/metrics and optional OTLP logs (e.g. Aspire).
        // Serilog owns files, console, Application Insights logs and Datadog logs; no duplicate Azure Monitor log exporter.
        builder.AddInvoicePortalTelemetry();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddRadzenComponents();

        // Factory rather than a scoped context: Blazor Server circuits live for the whole session.
        builder.Services.AddDbContextFactory<InvoicePortalDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3)));

        builder.Services.AddScoped(typeof(CrudService<>));
        builder.Services.AddScoped<LookupService>();

        // AI features (NL query, document Q&A). Provider "Mock" needs no keys or network; see Ai/AiServiceCollectionExtensions.cs.
        builder.AddInvoicePortalAi();

        builder.Services.AddHealthChecks();

        // In Azure (infra/resources.bicep sets DataProtection__BlobUri) the Data Protection key ring lives in Blob
        // Storage so antiforgery tokens and circuits survive container restarts. Locally the default file store is used.
        var dataProtectionBlobUri = builder.Configuration["DataProtection:BlobUri"];
        if (!string.IsNullOrWhiteSpace(dataProtectionBlobUri))
        {
            builder.Services.AddDataProtection()
                .SetApplicationName("InvoicePortal.Admin")
                .PersistKeysToAzureBlobStorage(new Uri(dataProtectionBlobUri), new DefaultAzureCredential());
        }

        var app = builder.Build();
        var logging = app.Services.GetRequiredService<AppLoggingOptions>();
        foreach (var warning in logging.GetWarnings(app.Configuration))
            app.Logger.LogWarning("{LoggingConfigurationWarning}", warning);
        app.Logger.LogInformation("Logging configured: Azure={RunningInAzure}, File={FileEnabled}, ApplicationInsights={ApplicationInsightsEnabled}, Datadog={DatadogEnabled}",
            logging.IsAzure(app.Configuration), !logging.IsAzure(app.Configuration),
            logging.UseApplicationInsights(app.Configuration), logging.UseDatadog(app.Configuration));

        // First in the pipeline, so one record per request carries the status code the client
        // actually received - after the exception handler and the status-code re-execute below.
        if (logging.RequestLoggingEnabled)
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
        }
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        // HTTP only inside the container; no HTTPS redirection.
        app.UseAntiforgery();

        app.MapHealthChecks("/healthz");
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        await app.RunAsync();
    }
}
