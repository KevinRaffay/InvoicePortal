using InvoicePortal.Admin.Ai;
using InvoicePortal.Admin.Ai.Query;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace InvoicePortal.Admin.Tests;

public class QueryGenerationServiceTests
{
    private static QueryGenerationService Create(StubChatClient chat, AiOptions? options = null)
    {
        var opts = Options.Create(options ?? new AiOptions());
        return new QueryGenerationService(chat, new SchemaDescriber(), new AiRateLimiter(opts), opts);
    }

    [Fact]
    public async Task Parses_fenced_json_reply()
    {
        var chat = new StubChatClient("```json\n{\"sql\": \"SELECT TOP (@p0) Name FROM Bottlers\", \"paramValues\": [5]}\n```");
        var result = await Create(chat).GenerateAsync("top 5 bottlers");

        Assert.Equal("SELECT TOP (@p0) Name FROM Bottlers", result.Sql);
        Assert.Equal([5L], result.ParamValues);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task Parses_raw_json_reply_with_mixed_primitive_params()
    {
        var chat = new StubChatClient("{\"sql\": \"SELECT 1\", \"paramValues\": [\"a\", 2.5, true, null]}");
        var result = await Create(chat).GenerateAsync("anything");

        Assert.Equal(["a", 2.5m, true, null], result.ParamValues);
    }

    [Fact]
    public async Task Surfaces_model_error_without_sql()
    {
        var chat = new StubChatClient("{\"sql\": null, \"paramValues\": [], \"error\": \"Cannot answer with a SELECT.\"}");
        var result = await Create(chat).GenerateAsync("delete everything");

        Assert.False(result.HasSql);
        Assert.Equal("Cannot answer with a SELECT.", result.Error);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{\"sql\": 42}")]
    [InlineData("{\"sql\": \"SELECT 1\", \"paramValues\": {\"a\": 1}}")]
    [InlineData("{\"sql\": \"SELECT 1\", \"paramValues\": [[1]]}")]
    [InlineData("{}")]
    public async Task Rejects_malformed_replies(string reply)
    {
        await Assert.ThrowsAsync<ModelResponseException>(() => Create(new StubChatClient(reply)).GenerateAsync("x"));
    }

    [Fact]
    public async Task Rejects_prompts_over_the_length_limit()
    {
        var service = Create(new StubChatClient("{}"), new AiOptions { MaxPromptLength = 10 });
        var ex = await Assert.ThrowsAsync<QueryRejectedException>(() => service.GenerateAsync(new string('x', 11)));
        Assert.Contains("at most 10 characters", ex.Message);
    }

    [Fact]
    public async Task Rejects_empty_prompts()
    {
        await Assert.ThrowsAsync<QueryRejectedException>(() => Create(new StubChatClient("{}")).GenerateAsync("   "));
    }

    [Fact]
    public async Task Enforces_the_per_minute_rate_limit()
    {
        var chat = new StubChatClient("{\"sql\": \"SELECT 1\", \"paramValues\": []}");
        var service = Create(chat, new AiOptions { RateLimitPerMinute = 3 });

        for (var i = 0; i < 3; i++)
        {
            await service.GenerateAsync("ok");
        }
        await Assert.ThrowsAsync<AiRateLimitException>(() => service.GenerateAsync("one too many"));
        Assert.Equal(3, chat.Calls);
    }

    [Fact]
    public async Task System_prompt_contains_the_schema_and_domain_notes()
    {
        var chat = new StubChatClient("{\"sql\": \"SELECT 1\", \"paramValues\": []}");
        await Create(chat).GenerateAsync("hello");

        var system = Assert.Single(chat.LastMessages, m => m.Role == ChatRole.System).Text;
        Assert.Contains(QueryGenerationService.SystemPromptMarker, system);
        Assert.Contains("Invoices(", system);
        Assert.Contains("IsDeleted", system);
        Assert.Contains("Bottlers(", system);
        Assert.Contains("17=Completed", system);
        Assert.Contains("[From] and [To]", system);

        var user = Assert.Single(chat.LastMessages, m => m.Role == ChatRole.User).Text;
        Assert.Equal("hello", user);
    }

    [Fact]
    public async Task Asks_the_model_for_deterministic_json_output()
    {
        var chat = new StubChatClient("{\"sql\": \"SELECT 1\", \"paramValues\": []}");
        await Create(chat).GenerateAsync("hello");

        Assert.NotNull(chat.LastOptions);
        Assert.Equal(0, chat.LastOptions.Temperature);
        Assert.Same(ChatResponseFormat.Json, chat.LastOptions.ResponseFormat);
    }
}
