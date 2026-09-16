using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using InvoicePortal.Admin.Ai.Documents;

namespace InvoicePortal.Admin.Ai.Chat;

/// <summary>
/// The mock model's grounded-answer behaviour: read the JSON sources out of the user message and stitch together
/// the sentences most relevant to the question, citing each with its [S#] label. Deterministic, no randomness.
/// </summary>
public static partial class MockAnswerSynthesizer
{
    private sealed record Source(string Label, string? Title, string? CustomerName, string? SourcePath, string? Content);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceBoundary();

    public static string Synthesize(string userMessage)
    {
        var question = ExtractQuestion(userMessage);
        var sources = ExtractSources(userMessage);
        if (sources.Count == 0)
        {
            return "I could not find that in the indexed documents.";
        }

        var questionTokens = Tokenizer.Tokenize(question).ToHashSet(StringComparer.Ordinal);
        var sb = new StringBuilder();

        var primary = sources[0];
        foreach (var sentence in BestSentences(primary.Content, questionTokens, 2))
        {
            sb.Append(sentence).Append(" [").Append(primary.Label).Append("] ");
        }

        if (sources.Count > 1)
        {
            var secondary = sources[1];
            var extra = BestSentences(secondary.Content, questionTokens, 1).FirstOrDefault();
            if (extra is not null)
            {
                sb.AppendLine().AppendLine().Append(extra).Append(" [").Append(secondary.Label).Append(']');
            }
        }

        return sb.ToString().Trim();
    }

    private static string ExtractQuestion(string message)
    {
        const string prefix = "Question:";
        var start = message.IndexOf(prefix, StringComparison.Ordinal);
        if (start < 0)
        {
            return message;
        }
        var rest = message[(start + prefix.Length)..];
        var end = rest.IndexOf('\n');
        return (end < 0 ? rest : rest[..end]).Trim();
    }

    private static List<Source> ExtractSources(string message)
    {
        var header = message.IndexOf(DocumentAnswerService.SourcesHeader, StringComparison.Ordinal);
        if (header < 0)
        {
            return [];
        }
        var json = message[(header + DocumentAnswerService.SourcesHeader.Length)..];
        var start = json.IndexOf('[');
        var end = json.LastIndexOf(']');
        if (start < 0 || end <= start)
        {
            return [];
        }
        try
        {
            return JsonSerializer.Deserialize<List<Source>>(json[start..(end + 1)], JsonOptions)?
                .Where(s => !string.IsNullOrWhiteSpace(s.Label) && !string.IsNullOrWhiteSpace(s.Content))
                .ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IEnumerable<string> BestSentences(string? content, HashSet<string> questionTokens, int take)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }
        var sentences = SentenceBoundary().Split(content.Trim()).Where(s => s.Length > 0).ToList();
        return sentences
            .Select((text, index) => (text, index, overlap: Tokenizer.Tokenize(text).Count(questionTokens.Contains)))
            .OrderByDescending(s => s.overlap)
            .ThenBy(s => s.index)
            .Take(take)
            .OrderBy(s => s.index)
            .Select(s => s.text);
    }
}
