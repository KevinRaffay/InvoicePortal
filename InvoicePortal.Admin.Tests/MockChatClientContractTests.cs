using InvoicePortal.Admin.Ai;
using InvoicePortal.Admin.Ai.Chat;
using InvoicePortal.Admin.Ai.Documents;
using InvoicePortal.Admin.Ai.Query;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InvoicePortal.Admin.Tests;

/// <summary>
/// The key contract: the fake model must produce exactly what the real prompt asks a real model for,
/// so the real parsing / guard / citation code is exercised end to end.
/// </summary>
public class MockChatClientContractTests
{
    private static readonly string[] Names = ["Northwind Beverages", "Contoso Bottling", "Fabrikam Distribution", "Adventure Works Cycles"];

    private static MockChatClient CreateChat() => new(NullLogger<MockChatClient>.Instance);

    private static QueryGenerationService CreateQueryService()
    {
        var opts = Options.Create(new AiOptions());
        return new QueryGenerationService(CreateChat(), new SchemaDescriber(), new AiRateLimiter(opts), opts);
    }

    private static DocumentAnswerService CreateAnswerService()
    {
        var opts = Options.Create(new AiOptions());
        return new DocumentAnswerService(CreateChat(), new InMemoryDocumentRetriever(Names, opts.Value), new AiRateLimiter(opts), opts);
    }

    public static TheoryData<string> Examples => new(SqlIntentCatalog.Examples);

    [Theory]
    [MemberData(nameof(Examples))]
    public async Task Every_example_phrase_yields_sql_that_passes_the_guard(string phrase)
    {
        var generated = await CreateQueryService().GenerateAsync(phrase);

        Assert.True(generated.HasSql, $"No SQL for '{phrase}': {generated.Error}");
        var normalized = SqlGuard.Validate(generated.Sql, generated.ParamValues);
        Assert.Contains("IsDeleted = 0", normalized);
    }

    [Fact]
    public async Task Parameterised_intents_bind_their_values()
    {
        var top = await CreateQueryService().GenerateAsync("Top 7 payers by total amount");
        Assert.Equal([7L], top.ParamValues);
        Assert.Contains("TOP (@p0)", top.Sql);

        var months = await CreateQueryService().GenerateAsync("Invoices from the last 3 months");
        Assert.Equal([3L], months.ParamValues);

        var bottler = await CreateQueryService().GenerateAsync("Show invoices for bottler Coca Cola Consolidated");
        Assert.Equal(["%Coca Cola Consolidated%"], bottler.ParamValues);
        Assert.Contains("LIKE @p0", bottler.Sql);

        var year = await CreateQueryService().GenerateAsync("Monthly totals in 2024");
        Assert.Equal([2024L], year.ParamValues);
    }

    [Fact]
    public async Task Unknown_phrases_yield_the_fallback_error()
    {
        var generated = await CreateQueryService().GenerateAsync("Please write a poem about invoices");

        Assert.False(generated.HasSql);
        Assert.Equal(SqlIntentCatalog.FallbackError, generated.Error);
    }

    [Fact]
    public async Task Sql_reply_is_fenced_json_so_extraction_is_exercised()
    {
        var response = await CreateChat().GetResponseAsync(
        [
            new ChatMessage(ChatRole.System, "You convert natural language into safe " + QueryGenerationService.SystemPromptMarker + " queries."),
            new ChatMessage(ChatRole.User, "Count invoices per currency"),
        ]);
        var reply = response.Text;

        Assert.StartsWith("```json", reply);
        Assert.EndsWith("```", reply.TrimEnd());
    }

    [Fact]
    public async Task Default_document_question_is_answered_with_citations()
    {
        var service = CreateAnswerService();
        var answer = await service.AskAsync(await service.DefaultQuestionAsync());

        Assert.Contains("[S1]", answer.Answer);
        Assert.NotEmpty(answer.Citations);
        Assert.Equal("S1", answer.Citations[0].Label);
        Assert.Contains("Supplies and POS Inventory", answer.Citations[0].Title);
        Assert.Contains("coolers", answer.Answer);
    }

    [Fact]
    public async Task Unmatched_document_question_returns_no_citations_instead_of_failing()
    {
        var answer = await CreateAnswerService().AskAsync("zebra quantum saxophone");

        Assert.Empty(answer.Citations);
        Assert.Equal("No indexed documents matched your question.", answer.Answer);
    }

    [Fact]
    public void Answer_synthesizer_without_sources_produces_no_label_so_the_citation_check_fails()
    {
        var reply = MockAnswerSynthesizer.Synthesize("Question: anything\n\nno sources here");

        Assert.DoesNotMatch(@"\[S\d+\]", reply);
        Assert.Throws<ModelResponseException>(() => CitationSelector.Select(reply, []));
    }

    [Fact]
    public void Answer_synthesizer_cites_only_supplied_labels()
    {
        IReadOnlyList<GroundingSource> sources =
        [
            new("a#0", "S1", "Doc A", "Contoso Bottling", "customer documents/a.docx", "", "Contoso Bottling stores coolers. Coolers are counted monthly. Nothing else.", 3),
            new("b#0", "S2", "Doc B", null, "customer documents/b.docx", "", "Coolers need gaskets. Unrelated sentence.", 2),
        ];
        var reply = MockAnswerSynthesizer.Synthesize(DocumentAnswerService.BuildUserMessage("How many coolers does Contoso Bottling store?", sources));

        var citations = CitationSelector.Select(reply, sources);
        Assert.Equal(["S1", "S2"], citations.Select(c => c.Label));
        Assert.Contains("Contoso Bottling stores coolers.", reply);
    }
}
