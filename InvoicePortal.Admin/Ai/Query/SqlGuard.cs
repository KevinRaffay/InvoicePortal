using System.Text.RegularExpressions;

namespace InvoicePortal.Admin.Ai.Query;

/// <summary>
/// Defence in depth for model-generated SQL, adapted from the reference app's <c>normalizeReadOnlyQuery</c> and
/// <c>getSQLFromNLP</c> checks to T-SQL: single SELECT/WITH statement, no comments, keyword denylist,
/// primitive parameters named @p0..@pN. The executor adds a command timeout and a row cap on top.
/// </summary>
public static partial class SqlGuard
{
    public const int MaxParameters = 50;

    [GeneratedRegex(@"^\s*(select|with)\b", RegexOptions.IgnoreCase)]
    private static partial Regex StartsWithSelect();

    [GeneratedRegex(@"\b(insert|update|delete|merge|drop|alter|create|truncate|exec|execute|grant|revoke|deny|backup|restore|shutdown|dbcc|waitfor|openrowset|openquery|opendatasource|bulk|into|xp_\w+|sp_\w+)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DeniedKeyword();

    [GeneratedRegex(@"@@?(\w+)")]
    private static partial Regex ParameterReference();

    /// <summary>Trims whitespace and at most one trailing semicolon (models love to add one).</summary>
    public static string Normalize(string sql)
    {
        var s = sql.Trim();
        if (s.EndsWith(';'))
        {
            s = s[..^1].TrimEnd();
        }
        return s;
    }

    /// <summary>Throws <see cref="QueryRejectedException"/> unless the statement is a single, parameterised SELECT.</summary>
    public static string Validate(string? sql, IReadOnlyList<object?> paramValues)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new QueryRejectedException("Missing SQL command object.");
        }

        var normalized = Normalize(sql);

        if (normalized.Contains(';') || normalized.Contains("--") || normalized.Contains("/*") || !StartsWithSelect().IsMatch(normalized))
        {
            throw new QueryRejectedException("Only a single SELECT query is allowed.");
        }

        var denied = DeniedKeyword().Match(normalized);
        if (denied.Success)
        {
            throw new QueryRejectedException($"The query uses a disallowed keyword: {denied.Groups[1].Value.ToUpperInvariant()}.");
        }

        if (paramValues.Count > MaxParameters)
        {
            throw new QueryRejectedException($"Too many parameters (max {MaxParameters}).");
        }

        foreach (var value in paramValues)
        {
            if (!IsPrimitive(value))
            {
                throw new QueryRejectedException("Only primitive parameter values are allowed.");
            }
        }

        foreach (Match m in ParameterReference().Matches(normalized))
        {
            if (m.Value.StartsWith("@@"))
            {
                throw new QueryRejectedException($"System variables such as {m.Value} are not allowed.");
            }

            var name = m.Groups[1].Value;
            if (name.Length < 2 || name[0] != 'p' || !int.TryParse(name[1..], out var index))
            {
                throw new QueryRejectedException($"Unexpected parameter name @{name}. Use @p0, @p1, ...");
            }

            if (index < 0 || index >= paramValues.Count)
            {
                throw new QueryRejectedException($"Parameter @{name} is referenced but not supplied.");
            }
        }

        return normalized;
    }

    public static bool IsPrimitive(object? value) => value is null or string or bool or byte or sbyte or short or ushort
        or int or uint or long or ulong or float or double or decimal or DateTime or DateOnly;
}
