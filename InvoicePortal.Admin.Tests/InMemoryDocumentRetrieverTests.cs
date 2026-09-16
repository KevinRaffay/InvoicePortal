using InvoicePortal.Admin.Ai;
using InvoicePortal.Admin.Ai.Documents;

namespace InvoicePortal.Admin.Tests;

public class InMemoryDocumentRetrieverTests
{
    private static readonly string[] Names = ["Northwind Beverages", "Contoso Bottling", "Fabrikam Distribution", "Adventure Works Cycles"];

    private static InMemoryDocumentRetriever Create(AiOptions? options = null) => new(Names, options ?? new AiOptions());

    [Fact]
    public async Task Default_question_names_the_first_customer()
    {
        Assert.Equal("What supplies are associated with Northwind Beverages?", await Create().DefaultQuestionAsync());
    }

    [Fact]
    public async Task Ranks_the_supplies_inventory_first_for_the_default_question()
    {
        var retriever = Create();
        var sources = await retriever.RetrieveAsync(await retriever.DefaultQuestionAsync());

        Assert.NotEmpty(sources);
        Assert.Equal("S1", sources[0].Label);
        Assert.Contains("Supplies and POS Inventory", sources[0].Title);
        Assert.Equal("Northwind Beverages", sources[0].CustomerName);
        Assert.StartsWith("customer documents/", sources[0].SourcePath);
    }

    [Fact]
    public async Task Labels_are_sequential_and_documents_are_not_repeated()
    {
        var sources = await Create().RetrieveAsync("Which invoice statuses exist and what is required for samples invoices?");

        Assert.Equal(sources.Select((_, i) => $"S{i + 1}"), sources.Select(s => s.Label));
        Assert.Equal(sources.Count, sources.Select(s => s.Id.Split('#')[0]).Distinct().Count());
        Assert.True(sources.Count <= new AiOptions().RetrievalTopK);
    }

    [Fact]
    public async Task Customer_name_in_the_question_boosts_that_customers_documents()
    {
        var sources = await Create().RetrieveAsync("Tell me about Contoso Bottling");

        Assert.NotEmpty(sources);
        Assert.Equal("Contoso Bottling", sources[0].CustomerName);
    }

    [Fact]
    public async Task Unrelated_questions_fall_below_the_threshold()
    {
        Assert.Empty(await Create().RetrieveAsync("zebra quantum saxophone"));
        Assert.Empty(await Create().RetrieveAsync("the and of"));
    }

    [Fact]
    public async Task Respects_top_k()
    {
        var sources = await Create(new AiOptions { RetrievalTopK = 2 }).RetrieveAsync("invoices program payer sales center samples co-op");
        Assert.Equal(2, sources.Count);
    }

    [Fact]
    public void Seed_documents_substitute_real_names_and_fall_back_when_missing()
    {
        var docs = SeedDocuments.Build(["Real Bottler"]);

        Assert.Contains(docs, d => d.FileName.StartsWith("Real Bottler ") && d.CustomerName == "Real Bottler");
        Assert.Contains(docs, d => d.CustomerName == SeedDocuments.FallbackNames[1]);
        Assert.DoesNotContain(docs, d => d.Content.Contains("{Bottler"));
        Assert.Contains(docs, d => d.CustomerName is null);
    }

    [Fact]
    public void Chunker_produces_overlapping_windows_of_at_most_the_requested_size()
    {
        var words = Enumerable.Range(1, 1000).Select(i => $"w{i}");
        var chunks = TextChunker.Split(string.Join(' ', words), chunkWords: 450, overlapWords: 60);

        Assert.Equal(3, chunks.Count);
        Assert.All(chunks, c => Assert.True(c.Split(' ').Length <= 450));
        Assert.StartsWith("w1 ", chunks[0]);
        Assert.StartsWith("w391 ", chunks[1]);
        Assert.StartsWith("w781 ", chunks[2]);
        Assert.EndsWith(" w1000", chunks[2]);
    }

    [Fact]
    public void Chunker_returns_one_chunk_for_short_text_and_none_for_blank()
    {
        Assert.Single(TextChunker.Split("just a few words"));
        Assert.Empty(TextChunker.Split("   "));
    }
}
