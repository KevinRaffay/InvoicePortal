using System.Text.RegularExpressions;

namespace InvoicePortal.Admin.Ai.Documents;

public static partial class Tokenizer
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a", "an", "the", "and", "or", "of", "to", "in", "on", "for", "with", "by", "at", "from", "as", "is", "are",
        "was", "were", "be", "been", "it", "its", "this", "that", "these", "those", "what", "which", "who", "how",
        "when", "where", "does", "do", "did", "can", "could", "should", "would", "will", "about", "there", "their",
        "they", "them", "we", "our", "you", "your", "me", "my", "i", "s", "associated", "tell", "please", "show",
    };

    [GeneratedRegex(@"[^\p{L}\p{Nd}]+")]
    private static partial Regex NonWord();

    public static IReadOnlyList<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return NonWord().Split(text.ToLowerInvariant())
            .Where(t => t.Length > 1 && !StopWords.Contains(t))
            .ToList();
    }
}
