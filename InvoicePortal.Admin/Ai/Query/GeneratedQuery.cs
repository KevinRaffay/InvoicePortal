namespace InvoicePortal.Admin.Ai.Query;

/// <summary>
/// The JSON contract the model must honour: <c>{"sql": "...", "paramValues": [...], "error": "..."}</c>.
/// Same shape as the customer-insights <c>QueryData</c> object.
/// </summary>
public sealed record GeneratedQuery(string? Sql, IReadOnlyList<object?> ParamValues, string? Error)
{
    public bool HasSql => !string.IsNullOrWhiteSpace(Sql);
}
