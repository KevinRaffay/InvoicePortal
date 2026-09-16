using System.Text.Json;

namespace InvoicePortal.Admin.Ai.Query;

/// <summary>
/// Models frequently wrap JSON in ```json fences or add prose around it. This finds the outermost object
/// and parses it; anything else is a <see cref="ModelResponseException"/>.
/// </summary>
public static class JsonExtraction
{
    public static JsonDocument ExtractObject(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ModelResponseException("The model returned an empty response.");
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new ModelResponseException("The model response did not contain a JSON object.");
        }

        try
        {
            return JsonDocument.Parse(text[start..(end + 1)]);
        }
        catch (JsonException)
        {
            throw new ModelResponseException("The model response was not valid JSON.");
        }
    }
}
