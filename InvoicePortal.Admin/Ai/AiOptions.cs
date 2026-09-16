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

    /// <summary>Only "Mock" is registered. A real provider is one extra case in AiServiceCollectionExtensions.</summary>
    [RegularExpression("^(Mock)$", ErrorMessage = "Ai:Provider must be 'Mock' (the only registered provider).")]
    public string Provider { get; set; } = "Mock";

    [Range(1, 20000)] public int MaxPromptLength { get; set; } = 4000;
    [Range(1, 1000)] public int RateLimitPerMinute { get; set; } = 10;
    [Range(1, 5000)] public int MaxRows { get; set; } = 200;
    [Range(1, 60)] public int CommandTimeoutSeconds { get; set; } = 5;
    [Range(1, 50)] public int RetrievalTopK { get; set; } = 5;

    /// <summary>Analogue of the Foundry IQ reranker threshold: chunks scoring below this are dropped.</summary>
    [Range(0, 100)] public double RetrievalMinScore { get; set; } = 1.0;
}
