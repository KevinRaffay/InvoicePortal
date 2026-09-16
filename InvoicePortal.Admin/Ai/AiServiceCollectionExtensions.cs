using InvoicePortal.Admin.Ai.Chat;
using InvoicePortal.Admin.Ai.Documents;
using InvoicePortal.Admin.Ai.Query;
using Microsoft.Extensions.AI;

namespace InvoicePortal.Admin.Ai;

public static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AI feature slice. Everything except the model and the retriever is provider-agnostic:
    /// the prompt builders, JSON parsing, SQL guard, citation validation and UI only see
    /// <see cref="IChatClient"/> and <see cref="IDocumentRetriever"/>.
    /// </summary>
    public static WebApplicationBuilder AddInvoicePortalAi(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<AiOptions>()
            .Bind(builder.Configuration.GetSection(AiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var provider = builder.Configuration[$"{AiOptions.SectionName}:Provider"] ?? "Mock";
        switch (provider)
        {
            case "Mock":
                // Deterministic fake: pattern-matched intents for NL->SQL, template answers for document Q&A.
                builder.Services.AddChatClient(sp => new MockChatClient(sp.GetRequiredService<ILogger<MockChatClient>>()))
                    .UseLogging();
                // In-memory "Foundry IQ": seeded document chunks + BM25 scoring + threshold + dedupe.
                builder.Services.AddSingleton<IDocumentRetriever, InMemoryDocumentRetriever>();
                break;

            // To use a real model, add a case here and change nothing else, e.g.:
            // case "AzureOpenAI":
            //     builder.Services.AddChatClient(new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            //         .GetChatClient(deployment).AsIChatClient()).UseLogging();
            //     builder.Services.AddSingleton<IDocumentRetriever, FoundryIqDocumentRetriever>();
            //     break;

            default:
                throw new InvalidOperationException($"Unsupported Ai:Provider '{provider}'. Only 'Mock' is registered.");
        }

        builder.Services.AddSingleton<SchemaDescriber>();
        builder.Services.AddScoped<AiRateLimiter>();
        builder.Services.AddScoped<IQueryGenerationService, QueryGenerationService>();
        builder.Services.AddScoped<ISqlQueryExecutor, SqlQueryExecutor>();
        builder.Services.AddScoped<IDocumentAnswerService, DocumentAnswerService>();

        return builder;
    }
}
