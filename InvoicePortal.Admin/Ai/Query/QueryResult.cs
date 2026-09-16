namespace InvoicePortal.Admin.Ai.Query;

public sealed record QueryColumn(string Name, Type ClrType)
{
    public bool IsNumeric => Type.GetTypeCode(Nullable.GetUnderlyingType(ClrType) ?? ClrType) is
        TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
        or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;
}

/// <summary>Result of a guarded, read-only execution. Rows are dictionaries so the grid can build columns dynamically.</summary>
public sealed record QueryResult(
    string Sql,
    IReadOnlyList<object?> ParamValues,
    IReadOnlyList<QueryColumn> Columns,
    IReadOnlyList<IDictionary<string, object>> Rows,
    bool Truncated);
