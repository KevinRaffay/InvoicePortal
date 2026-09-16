using InvoicePortal.Admin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InvoicePortal.Admin.Ai.Documents;

/// <summary>
/// Mock Foundry IQ: seeds documents about real bottlers, chunks them, scores with BM25 plus a customer-name boost
/// (standing in for the semantic reranker), applies the score threshold, deduplicates by document
/// (like the reference's <c>buildGroundingSources</c>) and labels the survivors S1..Sn.
/// </summary>
public sealed class InMemoryDocumentRetriever : IDocumentRetriever
{
    private const double CustomerBoost = 2.0;

    private readonly AiOptions options;
    private readonly Lazy<Task<Index>> index;

    public InMemoryDocumentRetriever(IDbContextFactory<InvoicePortalDbContext> factory, IOptions<AiOptions> options, ILogger<InMemoryDocumentRetriever> logger)
    {
        this.options = options.Value;
        index = new Lazy<Task<Index>>(async () => Index.Build(await LoadBottlerNamesAsync(factory, logger)));
    }

    /// <summary>Test constructor: fixed customer names, no database.</summary>
    public InMemoryDocumentRetriever(IReadOnlyList<string> bottlerNames, AiOptions options)
    {
        this.options = options;
        index = new Lazy<Task<Index>>(() => Task.FromResult(Index.Build(bottlerNames)));
    }

    public async Task<IReadOnlyList<string>> CustomerNamesAsync() => (await index.Value).CustomerNames;

    public async Task<string> DefaultQuestionAsync(CancellationToken cancellationToken = default)
        => $"What supplies are associated with {(await index.Value).CustomerNames[0]}?";

    public async Task<IReadOnlyList<GroundingSource>> RetrieveAsync(string query, CancellationToken cancellationToken = default)
    {
        var idx = await index.Value;
        var queryTokens = Tokenizer.Tokenize(query);
        if (queryTokens.Count == 0)
        {
            return [];
        }

        var scored = new List<(DocumentChunk Chunk, double Score)>();
        for (var i = 0; i < idx.Chunks.Count; i++)
        {
            var chunk = idx.Chunks[i];
            var score = idx.Scorer.Score(queryTokens, i);
            if (chunk.CustomerName is not null)
            {
                var customerTokens = Tokenizer.Tokenize(chunk.CustomerName);
                if (customerTokens.Count > 0 && customerTokens.All(queryTokens.Contains))
                {
                    score += CustomerBoost;
                }
            }
            if (score >= options.RetrievalMinScore && !string.IsNullOrWhiteSpace(chunk.Content))
            {
                scored.Add((chunk, score));
            }
        }

        var sources = new List<GroundingSource>();
        var seenDocuments = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (chunk, score) in scored.OrderByDescending(s => s.Score).ThenBy(s => s.Chunk.DocumentId).ThenBy(s => s.Chunk.ChunkIndex))
        {
            if (!seenDocuments.Add(chunk.DocumentId))
            {
                continue;
            }
            var label = $"S{sources.Count + 1}";
            sources.Add(new GroundingSource($"{chunk.DocumentId}#{chunk.ChunkIndex}", label, chunk.Title, chunk.CustomerName, chunk.SourcePath, chunk.SourceUrl, chunk.Content, score));
            if (sources.Count >= options.RetrievalTopK)
            {
                break;
            }
        }
        return sources;
    }

    private static async Task<IReadOnlyList<string>> LoadBottlerNamesAsync(IDbContextFactory<InvoicePortalDbContext> factory, ILogger logger)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await using var db = await factory.CreateDbContextAsync(cts.Token);
            var names = await db.Bottlers.AsNoTracking()
                .OrderByDescending(b => b.Invoices.Count(i => !i.IsDeleted))
                .ThenBy(b => b.Name)
                .Select(b => b.Name)
                .Take(SeedDocuments.PlaceholderCount)
                .ToListAsync(cts.Token);
            if (names.Count > 0)
            {
                return names;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not load bottler names for the seeded documents; using fictional names.");
        }
        return SeedDocuments.FallbackNames;
    }

    private sealed class Index
    {
        public required IReadOnlyList<string> CustomerNames { get; init; }
        public required IReadOnlyList<DocumentChunk> Chunks { get; init; }
        public required Bm25Scorer Scorer { get; init; }

        public static Index Build(IReadOnlyList<string> bottlerNames)
        {
            var documents = SeedDocuments.Build(bottlerNames);
            var chunks = new List<DocumentChunk>();
            foreach (var doc in documents)
            {
                var pieces = TextChunker.Split(doc.Content);
                for (var i = 0; i < pieces.Count; i++)
                {
                    chunks.Add(new DocumentChunk(doc.Id, i, doc.Title, doc.CustomerName, doc.SourcePath, SourceUrl: string.Empty, pieces[i]));
                }
            }
            var names = documents.Where(d => d.CustomerName is not null).Select(d => d.CustomerName!).Distinct().ToList();
            return new Index
            {
                CustomerNames = names,
                Chunks = chunks,
                Scorer = new Bm25Scorer(chunks.Select(c => Tokenizer.Tokenize(c.Title + " " + c.Content))),
            };
        }
    }
}
