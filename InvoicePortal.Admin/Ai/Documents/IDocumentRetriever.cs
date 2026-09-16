namespace InvoicePortal.Admin.Ai.Documents;

/// <summary>
/// The Foundry IQ seam. The real implementation would POST to an Azure AI Search knowledge base
/// (<c>/knowledgebases('{kb}')/retrieve</c>); the mock scores seeded chunks in memory.
/// Either way the caller gets deduplicated, labelled grounding sources.
/// </summary>
public interface IDocumentRetriever
{
    Task<IReadOnlyList<GroundingSource>> RetrieveAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>Suggested first question, e.g. "What supplies are associated with {customer}?".</summary>
    Task<string> DefaultQuestionAsync(CancellationToken cancellationToken = default);
}
