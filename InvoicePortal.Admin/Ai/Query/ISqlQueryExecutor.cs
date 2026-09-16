namespace InvoicePortal.Admin.Ai.Query;

/// <summary>Runs a guarded, read-only, row-capped SELECT and returns dictionary rows for a dynamic grid.</summary>
public interface ISqlQueryExecutor
{
    Task<QueryResult> ExecuteAsync(GeneratedQuery query, CancellationToken cancellationToken = default);
}
