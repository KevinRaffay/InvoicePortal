using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace InvoicePortal.Admin.Ai.Query;

/// <summary>
/// Real prompt construction and response validation (the mock only replaces <see cref="IChatClient"/>).
/// Mirrors the reference app's <c>getSQLFromNLP</c>: schema-aware system prompt, JSON-only reply,
/// primitive parameters, max 50, prompt length limit, rate limit.
/// </summary>
public sealed class QueryGenerationService(
    IChatClient chat,
    SchemaDescriber schemaDescriber,
    AiRateLimiter rateLimiter,
    IOptions<AiOptions> options) : IQueryGenerationService
{
    /// <summary>The mock chat client routes on this marker; a real model just follows the instructions.</summary>
    public const string SystemPromptMarker = "T-SQL (SQL Server) SELECT";

    public string BuildSystemPrompt() =>
        $$"""
        You convert natural language into safe {{SystemPromptMarker}} queries and return only a JSON object.

        Tables and columns:
        {{schemaDescriber.Describe()}}
        Rules:
        - Produce exactly one SELECT (or WITH ... SELECT) statement. No INSERT, UPDATE, DELETE, DDL, EXEC or comments.
        - Never modify data or schema. Never use system tables, INFORMATION_SCHEMA or system stored procedures.
        - Use parameters named @p0, @p1, ... for every literal value and return their values, in order, in "paramValues".
        - Parameter values must be primitives (string, number, boolean or null).
        - Never return more than 200 rows; use TOP (200) when listing rows. Alias every aggregate column.
        - If the request cannot be answered with a SELECT, return {"error": "<why>"}.

        Response shape: {"sql": "<T-SQL>", "paramValues": [<values>]}
        """;

    public async Task<GeneratedQuery> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var trimmed = prompt?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > options.Value.MaxPromptLength)
        {
            throw new QueryRejectedException($"The prompt must be a nonempty string of at most {options.Value.MaxPromptLength:N0} characters.");
        }

        rateLimiter.Acquire();

        var response = await chat.GetResponseAsync(
            [new ChatMessage(ChatRole.System, BuildSystemPrompt()), new ChatMessage(ChatRole.User, trimmed)],
            new ChatOptions { Temperature = 0 },
            cancellationToken);

        return Parse(response.Text);
    }

    /// <summary>Validates the JSON contract; exposed for tests.</summary>
    public static GeneratedQuery Parse(string? responseText)
    {
        using var doc = JsonExtraction.ExtractObject(responseText);
        var root = doc.RootElement;

        string? error = root.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;

        string? sql = null;
        if (root.TryGetProperty("sql", out var s))
        {
            sql = s.ValueKind switch
            {
                JsonValueKind.String => s.GetString(),
                JsonValueKind.Null => null,
                _ => throw new ModelResponseException("The model returned an invalid query object."),
            };
        }

        var paramValues = new List<object?>();
        if (root.TryGetProperty("paramValues", out var p) && p.ValueKind != JsonValueKind.Null)
        {
            if (p.ValueKind != JsonValueKind.Array)
            {
                throw new ModelResponseException("The model returned an invalid query object.");
            }

            foreach (var item in p.EnumerateArray())
            {
                paramValues.Add(item.ValueKind switch
                {
                    JsonValueKind.String => item.GetString(),
                    JsonValueKind.Number => item.TryGetInt64(out var l) ? (object)l : item.GetDecimal(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => throw new ModelResponseException("Only primitive parameter values are allowed."),
                });
            }
        }

        if (paramValues.Count > SqlGuard.MaxParameters)
        {
            throw new ModelResponseException($"Too many parameters (max {SqlGuard.MaxParameters}).");
        }

        if (string.IsNullOrWhiteSpace(sql) && string.IsNullOrWhiteSpace(error))
        {
            throw new ModelResponseException("The model returned neither a query nor an error.");
        }

        return new GeneratedQuery(sql, paramValues, error);
    }
}
