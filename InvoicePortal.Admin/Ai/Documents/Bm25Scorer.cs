namespace InvoicePortal.Admin.Ai.Documents;

/// <summary>Plain BM25 over pre-tokenised chunks. Stands in for Azure AI Search's ranking + semantic reranker.</summary>
public sealed class Bm25Scorer
{
    private const double K1 = 1.2;
    private const double B = 0.75;

    private readonly List<Dictionary<string, int>> termFrequencies = [];
    private readonly Dictionary<string, int> documentFrequencies = new(StringComparer.Ordinal);
    private readonly List<int> lengths = [];
    private readonly double averageLength;

    public Bm25Scorer(IEnumerable<IReadOnlyList<string>> documents)
    {
        foreach (var tokens in documents)
        {
            var tf = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var t in tokens)
            {
                tf[t] = tf.TryGetValue(t, out var c) ? c + 1 : 1;
            }
            foreach (var term in tf.Keys)
            {
                documentFrequencies[term] = documentFrequencies.TryGetValue(term, out var d) ? d + 1 : 1;
            }
            termFrequencies.Add(tf);
            lengths.Add(tokens.Count);
        }
        averageLength = lengths.Count == 0 ? 0 : lengths.Average();
    }

    public int Count => termFrequencies.Count;

    public double Score(IReadOnlyList<string> queryTokens, int documentIndex)
    {
        if (documentIndex < 0 || documentIndex >= Count || averageLength == 0)
        {
            return 0;
        }

        var tf = termFrequencies[documentIndex];
        var length = lengths[documentIndex];
        double score = 0;
        foreach (var term in queryTokens.Distinct())
        {
            if (!tf.TryGetValue(term, out var f))
            {
                continue;
            }
            var n = documentFrequencies[term];
            var idf = Math.Log(1 + (Count - n + 0.5) / (n + 0.5));
            score += idf * (f * (K1 + 1)) / (f + K1 * (1 - B + B * length / averageLength));
        }
        return score;
    }
}
