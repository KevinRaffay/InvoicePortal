using Microsoft.Extensions.AI;
using OllamaSharp.Models.Exceptions;

namespace InvoicePortal.Admin.Ai.Chat;

/// <summary>
/// Wraps a real model client so transport failures (server not running, model not pulled, timeout) surface as
/// <see cref="AiException"/> with an actionable message instead of a generic error in the UI.
/// </summary>
public sealed class ModelTransportChatClient(IChatClient inner, string endpoint, string model) : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ModelResponseException($"The local model endpoint {endpoint} could not be reached ({ex.Message}). Is Ollama running?");
        }
        catch (OllamaException ex)
        {
            throw new ModelResponseException($"Ollama rejected the request for model '{model}': {ex.Message}. Has the model been pulled?");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ModelResponseException($"The local model '{model}' timed out. CPU-only inference can be slow; raise Ai:Ollama:TimeoutSeconds or use a smaller model.");
        }
    }
}
