namespace InvoicePortal.Admin.Ai.Documents;

/// <summary>Word-window chunking with overlap, same defaults as the reference ingestion (450 words, 60 overlap).</summary>
public static class TextChunker
{
    public static IReadOnlyList<string> Split(string content, int chunkWords = 450, int overlapWords = 60)
    {
        if (chunkWords <= 0) throw new ArgumentOutOfRangeException(nameof(chunkWords));
        if (overlapWords < 0 || overlapWords >= chunkWords) throw new ArgumentOutOfRangeException(nameof(overlapWords));

        var words = content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
        {
            return [];
        }

        var chunks = new List<string>();
        var step = chunkWords - overlapWords;
        for (var start = 0; start < words.Length; start += step)
        {
            var take = Math.Min(chunkWords, words.Length - start);
            chunks.Add(string.Join(' ', words, start, take));
            if (start + take >= words.Length)
            {
                break;
            }
        }
        return chunks;
    }
}
