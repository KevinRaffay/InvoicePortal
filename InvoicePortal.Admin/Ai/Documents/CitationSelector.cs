using System.Text.RegularExpressions;

namespace InvoicePortal.Admin.Ai.Documents;

/// <summary>
/// Port of the reference's <c>selectCitations</c>: keep only the sources the answer actually cites, in first-use order,
/// and reject answers that cite nothing or cite a label that was not supplied.
/// </summary>
public static partial class CitationSelector
{
    [GeneratedRegex(@"\[S(\d+)\]")]
    public static partial Regex LabelPattern();

    public static IReadOnlyList<Citation> Select(string? answer, IReadOnlyList<GroundingSource> sources)
    {
        var indexes = LabelPattern().Matches(answer ?? string.Empty)
            .Select(m => int.Parse(m.Groups[1].Value))
            .Distinct()
            .ToList();

        if (indexes.Count == 0)
        {
            throw new ModelResponseException("The grounded answer did not cite a source.");
        }

        if (indexes.Any(i => i < 1 || i > sources.Count))
        {
            throw new ModelResponseException("The grounded answer cited an unknown source.");
        }

        return indexes.Select(i =>
        {
            var s = sources[i - 1];
            var snippet = s.Content.Length > 240 ? s.Content[..240].TrimEnd() + "..." : s.Content;
            return new Citation(s.Id, s.Label, s.Title, s.CustomerName, s.SourcePath, s.SourceUrl, snippet);
        }).ToList();
    }
}
