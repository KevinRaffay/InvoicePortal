using InvoicePortal.Admin.Ai;
using InvoicePortal.Admin.Components;
using InvoicePortal.Admin.Data;
using InvoicePortal.Admin.Services;
using InvoicePortal.Admin.Telemetry;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Radzen;

namespace InvoicePortal.Admin;

// Explicit entry-point class (instead of top-level statements) so the compiler-generated
// global "Program" class does not shadow the scaffolded Data.Entities.Program entity.
public static class EntryPoint
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("InvoicePortal")
            ?? throw new InvalidOperationException(
                "Connection string 'InvoicePortal' is missing. Set the ConnectionStrings__InvoicePortal environment variable (see docker-compose.yml).");

        // OpenTelemetry traces, metrics and logs. Exporters are chosen by configuration: OTLP (Telemetry:OtlpEndpoint or
        // OTEL_EXPORTER_OTLP_ENDPOINT) and/or Azure Monitor (APPLICATIONINSIGHTS_CONNECTION_STRING, set by the Bicep template).
        // With neither set, nothing is exported. See Telemetry/TelemetryExtensions.cs.
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

        app.Run();
    }
}
