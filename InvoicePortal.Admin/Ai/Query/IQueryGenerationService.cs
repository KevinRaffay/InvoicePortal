namespace InvoicePortal.Admin.Ai.Query;

/// <summary>Turns a natural-language request into a guarded <see cref="GeneratedQuery"/> via the chat model.</summary>
public interface IQueryGenerationService
{
    Task<GeneratedQuery> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}
