using System.Data;
using System.Data.Common;
using InvoicePortal.Admin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InvoicePortal.Admin.Ai.Query;

/// <summary>
/// ADO.NET execution over the EF connection. SQL Server has no READ ONLY transaction (the reference app used
/// PostgreSQL's), so safety comes from <see cref="SqlGuard"/>, a short command timeout, LOCK_TIMEOUT,
/// a row cap, and a transaction that is always rolled back.
/// </summary>
public sealed class SqlQueryExecutor(IDbContextFactory<InvoicePortalDbContext> factory, IOptions<AiOptions> options) : ISqlQueryExecutor
{
    public async Task<QueryResult> ExecuteAsync(GeneratedQuery query, CancellationToken cancellationToken = default)
    {
        var sql = SqlGuard.Validate(query.Sql, query.ParamValues);
        var maxRows = options.Value.MaxRows;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = options.Value.CommandTimeoutSeconds;
            // The only semicolon in the batch is ours, added after validation.
            command.CommandText = "SET LOCK_TIMEOUT 3000; " + sql;

            for (var i = 0; i < query.ParamValues.Count; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@p{i}";
                parameter.Value = query.ParamValues[i] ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken);
            var columns = ReadColumns(reader);
            var rows = new List<IDictionary<string, object>>();
            var truncated = false;

            while (await reader.ReadAsync(cancellationToken))
            {
                if (rows.Count >= maxRows)
                {
                    truncated = true;
                    break;
                }

                var row = new Dictionary<string, object>(columns.Count, StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < columns.Count; i++)
                {
                    row[columns[i].Name] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                }
                rows.Add(row);
            }

            return new QueryResult(sql, query.ParamValues, columns, rows, truncated);
        }
        finally
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    private static List<QueryColumn> ReadColumns(DbDataReader reader)
    {
        var columns = new List<QueryColumn>(reader.FieldCount);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"Column{i + 1}";
            }
            var unique = name;
            for (var n = 2; !seen.Add(unique); n++)
            {
                unique = $"{name}_{n}";
            }

            // Nullable-wrap value types so null cells do not break the grid's dynamic sort expressions.
            var type = reader.GetFieldType(i);
            if (type.IsValueType && Nullable.GetUnderlyingType(type) is null)
            {
                type = typeof(Nullable<>).MakeGenericType(type);
            }
            columns.Add(new QueryColumn(unique, type));
        }
        return columns;
    }
}
