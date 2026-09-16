using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace InvoicePortal.Admin.Tests;

/// <summary>Returns a canned reply and records the messages it was sent.</summary>
internal sealed class StubChatClient(string reply) : IChatClient
{
    public List<ChatMessage> LastMessages { get; } = [];
    public int Calls { get; private set; }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Calls++;
        LastMessages.Clear();
        LastMessages.AddRange(messages);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
