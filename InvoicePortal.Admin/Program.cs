using InvoicePortal.Admin.Ai;
using InvoicePortal.Admin.Components;
using InvoicePortal.Admin.Data;
using InvoicePortal.Admin.Services;
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

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
        }
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        // HTTP only inside the container; no HTTPS redirection.
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
