using System.Runtime.CompilerServices;
using InvoicePortal.Admin.Ai.Documents;
using InvoicePortal.Admin.Ai.Query;
using Microsoft.Extensions.AI;

namespace InvoicePortal.Admin.Ai.Chat;

/// <summary>
/// Deterministic stand-in for a hosted model. It looks at the system prompt to decide which "skill" is being asked for
/// (NL-to-SQL or grounded document answer) and honours the same output contract a real model would be instructed to.
/// The NL-to-SQL reply is wrapped in a ```json fence on purpose, so the real <see cref="JsonExtraction"/> path is exercised.
/// </summary>
public sealed class MockChatClient(ILogger<MockChatClient> logger) : IChatClient
{
    public const string ModelId = "mock-invoice-portal";

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var list = messages.ToList();
        var system = string.Join("\n", list.Where(m => m.Role == ChatRole.System).Select(m => m.Text));
        var user = list.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;

        string reply;
        if (system.Contains(QueryGenerationService.SystemPromptMarker, StringComparison.Ordinal))
        {
            reply = "```json\n" + SqlIntentCatalog.Answer(user) + "\n```";
            logger.LogInformation("MockChatClient: NL-to-SQL for \"{Prompt}\"", Truncate(user));
        }
        else if (system.Contains(DocumentAnswerService.SystemPromptMarker, StringComparison.Ordinal))
        {
            reply = MockAnswerSynthesizer.Synthesize(user);
            logger.LogInformation("MockChatClient: grounded answer, {Length} chars", reply.Length);
        }
        else
        {
            reply = "This is a mock model. It only knows how to write invoice SQL and answer from supplied documents.";
        }

        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, reply))
        {
            ModelId = ModelId,
            ResponseId = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text) { ModelId = ModelId, ResponseId = response.ResponseId };
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }

    private static string Truncate(string s) => s.Length <= 80 ? s : s[..80] + "...";
}
