namespace InvoicePortal.Admin.Ai.Documents;

/// <summary>A seeded "customer document" (the analogue of the .docx/.xlsx files Foundry IQ indexed).</summary>
public sealed record SeedDocument(string Id, string FileName, string? CustomerName, string Content)
{
    public string Title => Path.GetFileNameWithoutExtension(FileName);
    public string SourcePath => $"customer documents/{FileName}";
}

public sealed record DocumentChunk(string DocumentId, int ChunkIndex, string Title, string? CustomerName, string SourcePath, string SourceUrl, string Content);

/// <summary>A retrieved, deduplicated source with its prompt label (S1, S2, ...). Same fields the reference sends to the model.</summary>
public sealed record GroundingSource(string Id, string Label, string Title, string? CustomerName, string SourcePath, string SourceUrl, string Content, double Score);

/// <summary>What the UI renders under the answer; mirrors the reference's <c>FoundryIQCitation</c> plus a snippet.</summary>
public sealed record Citation(string Id, string Label, string Title, string? CustomerName, string SourcePath, string SourceUrl, string Snippet);

public sealed record DocumentAnswer(string Answer, IReadOnlyList<Citation> Citations);
