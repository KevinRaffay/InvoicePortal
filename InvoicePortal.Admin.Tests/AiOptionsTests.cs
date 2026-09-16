using System.ComponentModel.DataAnnotations;
using InvoicePortal.Admin.Ai;

namespace InvoicePortal.Admin.Tests;

public class AiOptionsTests
{
    private static bool IsValid(AiOptions options)
        => Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true);

    [Theory]
    [InlineData("Mock", true)]
    [InlineData("Ollama", true)]
    [InlineData("AzureOpenAI", true)]
    [InlineData("OpenAI", false)]
    [InlineData("", false)]
    public void Only_registered_providers_are_valid(string provider, bool expected)
    {
        Assert.Equal(expected, IsValid(new AiOptions { Provider = provider }));
    }

    [Fact]
    public void Ollama_defaults_point_at_a_local_coder_model()
    {
        var o = new OllamaOptions();

        Assert.Equal("http://localhost:11434", o.Endpoint);
        Assert.StartsWith("qwen2.5-coder", o.Model);
        Assert.True(o.ContextLength >= 8192);
    }
}
