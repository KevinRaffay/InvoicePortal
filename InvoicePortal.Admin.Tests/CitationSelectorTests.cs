using InvoicePortal.Admin.Ai;
using InvoicePortal.Admin.Ai.Documents;

namespace InvoicePortal.Admin.Tests;

public class CitationSelectorTests
{
    private static GroundingSource Source(int n, string? customer = "Northwind Beverages")
        => new($"doc{n}#0", $"S{n}", $"Document {n}", customer, $"customer documents/Document {n}.docx", string.Empty, new string('x', 300), 1.0);

    private static readonly IReadOnlyList<GroundingSource> TwoSources = [Source(1), Source(2, null)];

    [Fact]
    public void Returns_cited_sources_in_first_use_order_without_duplicates()
    {
        var citations = CitationSelector.Select("Second [S2] then first [S1] and again [S2].", TwoSources);

        Assert.Equal(["S2", "S1"], citations.Select(c => c.Label));
    }

    [Fact]
    public void Maps_source_fields_and_truncates_the_snippet()
    {
        var citation = Assert.Single(CitationSelector.Select("Only one [S1].", TwoSources));

        Assert.Equal("doc1#0", citation.Id);
        Assert.Equal("Document 1", citation.Title);
        Assert.Equal("Northwind Beverages", citation.CustomerName);
        Assert.Equal("customer documents/Document 1.docx", citation.SourcePath);
        Assert.Equal(243, citation.Snippet.Length);
        Assert.EndsWith("...", citation.Snippet);
    }

    [Fact]
    public void Throws_when_the_answer_cites_nothing()
    {
        var ex = Assert.Throws<ModelResponseException>(() => CitationSelector.Select("No labels here.", TwoSources));
        Assert.Equal("The grounded answer did not cite a source.", ex.Message);
    }

    [Theory]
    [InlineData("See [S9].")]
    [InlineData("See [S0].")]
    [InlineData("Valid [S1] and invalid [S3].")]
    public void Throws_when_the_answer_cites_an_unknown_source(string answer)
    {
        var ex = Assert.Throws<ModelResponseException>(() => CitationSelector.Select(answer, TwoSources));
        Assert.Equal("The grounded answer cited an unknown source.", ex.Message);
    }

    [Fact]
    public void Ignores_lookalike_labels()
    {
        Assert.Throws<ModelResponseException>(() => CitationSelector.Select("[S] [Sx] (S1) S1", TwoSources));
    }
}
