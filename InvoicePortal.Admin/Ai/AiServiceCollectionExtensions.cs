using InvoicePortal.Admin.Ai.Chat;
using InvoicePortal.Admin.Ai.Documents;
using InvoicePortal.Admin.Ai.Query;
using InvoicePortal.Admin.Telemetry;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using OllamaSharp;

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

        // One "chat" span and token-usage metrics per model call, for every provider (Telemetry/TelemetryExtensions.cs
        // subscribes to the source). Prompts and completions are only recorded when Telemetry:RecordAiContent is true.
        var recordAiContent = builder.Configuration.GetValue<bool>($"{TelemetryOptions.SectionName}:RecordAiContent");
        ChatClientBuilder UseTelemetry(ChatClientBuilder chat) =>
            chat.UseOpenTelemetry(sourceName: InvoicePortalTelemetry.AiSourceName, configure: c => c.EnableSensitiveData = recordAiContent);

        var provider = builder.Configuration[$"{AiOptions.SectionName}:Provider"] ?? "Mock";
        switch (provider)
        {
            case "Mock":
                // Deterministic fake: pattern-matched intents for NL->SQL, template answers for document Q&A.
                UseTelemetry(builder.Services.AddChatClient(sp => new MockChatClient(sp.GetRequiredService<ILogger<MockChatClient>>())))
                    .UseLogging();
                break;

            case "Ollama":
                // A real small model running locally (Ollama on the host GPU, or the optional CPU sidecar in
                // docker-compose). Same prompts, parsing, guards and UI as the mock; only the model changes.
                var ollama = builder.Configuration.GetSection($"{AiOptions.SectionName}:Ollama").Get<OllamaOptions>() ?? new OllamaOptions();
                UseTelemetry(builder.Services.AddChatClient(_ => new OllamaApiClient(
                        new HttpClient { BaseAddress = new Uri(ollama.Endpoint), Timeout = TimeSpan.FromSeconds(ollama.TimeoutSeconds) },
                        ollama.Model))
                    .Use(inner => new ModelTransportChatClient(inner, ollama.Endpoint, ollama.Model))
                    .ConfigureOptions(options =>
                    {
                        // Ollama's default context window is too small for the schema prompt; num_ctx is an Ollama option.
                        options.AdditionalProperties ??= [];
                        options.AdditionalProperties["num_ctx"] = ollama.ContextLength;
                    }))
                    .UseLogging();
                break;

            case "AzureOpenAI":
                // Provisioned by infra/resources.bicep. Entra ID auth only (the resource has local keys disabled):
                // DefaultAzureCredential uses the container's managed identity (AZURE_CLIENT_ID) or `az login` locally.
                var aoai = builder.Configuration.GetSection($"{AiOptions.SectionName}:AzureOpenAI").Get<AzureOpenAIOptions>() ?? new AzureOpenAIOptions();
                if (string.IsNullOrWhiteSpace(aoai.Endpoint))
                {
                    throw new InvalidOperationException("Ai:AzureOpenAI:Endpoint is required when Ai:Provider is 'AzureOpenAI'.");
                }

                UseTelemetry(builder.Services.AddChatClient(_ => new AzureOpenAIClient(new Uri(aoai.Endpoint), new DefaultAzureCredential())
                        .GetChatClient(aoai.Deployment)
                        .AsIChatClient())
                    .Use(inner => new ModelTransportChatClient(inner, aoai.Endpoint, aoai.Deployment)))
                    .UseLogging();
                break;

            default:
                throw new InvalidOperationException($"Unsupported Ai:Provider '{provider}'. Registered providers: Mock, Ollama, AzureOpenAI.");
        }

        // The "Foundry IQ" side stays in-process for every provider: seeded document chunks + BM25 scoring +
        // threshold + dedupe. A real knowledge base would be another IDocumentRetriever implementation.
        builder.Services.AddSingleton<IDocumentRetriever, InMemoryDocumentRetriever>();

        builder.Services.AddSingleton<SchemaDescriber>();
        builder.Services.AddScoped<AiRateLimiter>();
        builder.Services.AddScoped<IQueryGenerationService, QueryGenerationService>();
        builder.Services.AddScoped<ISqlQueryExecutor, SqlQueryExecutor>();
        builder.Services.AddScoped<IDocumentAnswerService, DocumentAnswerService>();

        return builder;
    }
}
