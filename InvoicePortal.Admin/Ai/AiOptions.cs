using System.ComponentModel.DataAnnotations;

namespace InvoicePortal.Admin.Ai;

/// <summary>
/// Bound from the "Ai" configuration section (appsettings.json, overridable with Ai__* environment variables,
/// e.g. <c>Ai__Enabled=false</c> in docker-compose.yml). Mirrors the feature flags of the customer-insights
/// reference app (aiEnabled / foundryIQEnabled) plus the guard limits its API enforced.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Master switch: hides every AI entry point and makes the AI page refuse to run.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gates the "Ask the documents" dialog (the Foundry IQ analogue).</summary>
    public bool DocumentChatEnabled { get; set; } = true;

    /// <summary>
    /// "Mock" (default; deterministic fake, offline) or "Ollama" (a real local model behind the same
    /// <c>IChatClient</c>). A cloud provider is one extra case in AiServiceCollectionExtensions.
    /// </summary>
    [Required, RegularExpression("^(Mock|Ollama)$", ErrorMessage = "Ai:Provider must be 'Mock' or 'Ollama'.")]
    public string Provider { get; set; } = "Mock";

    /// <summary>Settings for the Ollama provider (ignored when Provider is Mock).</summary>
    public OllamaOptions Ollama { get; set; } = new();

    [Range(1, 20000)] public int MaxPromptLength { get; set; } = 4000;
    [Range(1, 1000)] public int RateLimitPerMinute { get; set; } = 10;
    [Range(1, 5000)] public int MaxRows { get; set; } = 200;
    [Range(1, 60)] public int CommandTimeoutSeconds { get; set; } = 5;
    [Range(1, 50)] public int RetrievalTopK { get; set; } = 5;

    /// <summary>Analogue of the Foundry IQ reranker threshold: chunks scoring below this are dropped.</summary>
    [Range(0, 100)] public double RetrievalMinScore { get; set; } = 1.0;
}

/// <summary>
/// Where the local model runs. Ollama on the Windows host (uses the GPU) is reached from the app container via
/// <c>http://host.docker.internal:11434</c>; from a host-side <c>dotnet run</c> via <c>http://localhost:11434</c>.
/// </summary>
public sealed class OllamaOptions
{
    [Required, Url] public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>Must already be pulled (<c>ollama pull qwen2.5-coder:7b</c>). Coder-tuned models do best at text-to-SQL.</summary>
    [Required] public string Model { get; set; } = "qwen2.5-coder:7b";

    /// <summary>Per-call HTTP timeout. CPU-only inference of the schema prompt can take tens of seconds.</summary>
    [Range(5, 600)] public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Ollama's default context is too small for the schema prompt plus a reply; this sets num_ctx.</summary>
    [Range(1024, 131072)] public int ContextLength { get; set; } = 8192;
}
