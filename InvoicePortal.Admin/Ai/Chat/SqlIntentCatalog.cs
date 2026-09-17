using System.Text.Json;
using System.Text.RegularExpressions;
using InvoicePortal.Admin.Data.Enums;
using InvoicePortal.Admin.Services;

namespace InvoicePortal.Admin.Ai.Chat;

/// <summary>
/// The mock model's "understanding" of natural language: a small table of regex intents, each producing
/// parameterised T-SQL in exactly the JSON shape the real system prompt demands. First match wins.
/// </summary>
public static partial class SqlIntentCatalog
{
    public const string FallbackError =
        "I could not map that request to a supported query. Try totals by bottler, payer, currency, status, type, program, sales center, or a date range.";

    private const string InvoiceList =
        "SELECT TOP (200) i.Id, i.Number, i.InvoiceType, i.InvoiceDate, i.Amount, c.Code AS Currency, b.Name AS Bottler, p.Name AS Payer, i.ProgramId AS Program " +
        "FROM Invoices i JOIN Bottlers b ON b.Id = i.BottlerId JOIN Payers p ON p.Id = i.PayerId JOIN Currencies c ON c.Id = i.CurrencyId " +
        "WHERE i.IsDeleted = 0";

    public sealed record Intent(string Name, string Example, Regex Pattern, Func<Match, (string Sql, object?[] Params)> Build);

    /// <summary>Phrases guaranteed to hit an intent; the page shows them as suggestion chips.</summary>
    public static IReadOnlyList<string> Examples => Intents.Select(i => i.Example).ToList();

    public static readonly IReadOnlyList<Intent> Intents =
    [
        new("top-payers", "Top 5 payers by total amount",
            TopPayers(),
            m => ("SELECT TOP (@p0) p.Name AS Payer, p.City, p.State, COUNT(i.Id) AS InvoiceCount, SUM(i.Amount) AS TotalAmount " +
                  "FROM Invoices i JOIN Payers p ON p.Id = i.PayerId WHERE i.IsDeleted = 0 " +
                  "GROUP BY p.Name, p.City, p.State ORDER BY TotalAmount DESC", [ParseInt(m.Groups[1].Value, 5)])),

        new("top-bottlers", "Top 10 bottlers by invoice count",
            TopBottlers(),
            m => ("SELECT TOP (@p0) b.Name AS Bottler, b.SalesOrganization, COUNT(i.Id) AS InvoiceCount, SUM(i.Amount) AS TotalAmount " +
                  "FROM Invoices i JOIN Bottlers b ON b.Id = i.BottlerId WHERE i.IsDeleted = 0 " +
                  "GROUP BY b.Name, b.SalesOrganization ORDER BY InvoiceCount DESC", [ParseInt(m.Groups[1].Value, 10)])),

        new("bottler-invoices", "Show invoices for bottler Cascade",
            BottlerNamed(),
            m => (InvoiceList + " AND b.Name LIKE @p0 ORDER BY i.InvoiceDate DESC", [$"%{m.Groups["name"].Value.Trim()}%"])),

        new("last-months", "Invoices from the last 6 months",
            LastMonths(),
            m => (InvoiceList + " AND i.InvoiceDate >= DATEADD(month, -@p0, CAST(GETDATE() AS date)) ORDER BY i.InvoiceDate DESC", [ParseInt(m.Groups[1].Value, 6)])),

        new("monthly-in-year", "Monthly totals in 2025",
            InYear(),
            m => ("SELECT YEAR(i.InvoiceDate) AS [Year], MONTH(i.InvoiceDate) AS [Month], COUNT(i.Id) AS InvoiceCount, SUM(i.Amount) AS TotalAmount " +
                  "FROM Invoices i WHERE i.IsDeleted = 0 AND YEAR(i.InvoiceDate) = @p0 " +
                  "GROUP BY YEAR(i.InvoiceDate), MONTH(i.InvoiceDate) ORDER BY [Month]", [int.Parse(m.Groups[2].Value)])),

        new("by-status", "How many invoices are there per status?",
            ByStatus(),
            _ => ($"SELECT i.InvoiceStatus AS StatusCode, {StatusCase("i.InvoiceStatus")} AS Status, COUNT(i.Id) AS InvoiceCount, SUM(i.Amount) AS TotalAmount " +
                  "FROM Invoices i WHERE i.IsDeleted = 0 GROUP BY i.InvoiceStatus ORDER BY InvoiceCount DESC", [])),

        new("by-currency", "Count invoices per currency",
            ByCurrency(),
            _ => ("SELECT c.Code AS Currency, COUNT(i.Id) AS InvoiceCount, SUM(i.Amount) AS TotalAmount " +
                  "FROM Invoices i JOIN Currencies c ON c.Id = i.CurrencyId WHERE i.IsDeleted = 0 " +
                  "GROUP BY c.Code ORDER BY InvoiceCount DESC", [])),

        new("by-program", "Total amount by program",
            ByProgram(),
            _ => ("SELECT TOP (200) pr.Id AS Program, pr.Name AS ProgramName, b.Name AS Bottler, COUNT(i.Id) AS InvoiceCount, SUM(i.Amount) AS TotalAmount " +
                  "FROM Invoices i JOIN Programs pr ON pr.Id = i.ProgramId JOIN Bottlers b ON b.Id = i.BottlerId WHERE i.IsDeleted = 0 " +
                  "GROUP BY pr.Id, pr.Name, b.Name ORDER BY TotalAmount DESC", [])),

        new("by-sales-center", "Invoice totals per sales center",
            BySalesCenter(),
            _ => ("SELECT TOP (200) s.Name AS SalesCenter, p.Name AS Payer, COUNT(i.Id) AS InvoiceCount, SUM(i.Amount) AS TotalAmount " +
                  "FROM Invoices i JOIN SalesCenters s ON s.Id = i.SalesCenterId JOIN Payers p ON p.Id = i.PayerId WHERE i.IsDeleted = 0 " +
                  "GROUP BY s.Name, p.Name ORDER BY TotalAmount DESC", [])),

        new("by-type", "Average amount by invoice type",
            ByType(),
            _ => ("SELECT i.InvoiceType, COUNT(i.Id) AS InvoiceCount, AVG(i.Amount) AS AverageAmount, SUM(i.Amount) AS TotalAmount " +
                  "FROM Invoices i WHERE i.IsDeleted = 0 GROUP BY i.InvoiceType ORDER BY InvoiceCount DESC", [])),

        new("by-bottler", "Get the total invoice amount for all invoices. Group by bottler and include the country.",
            ByBottler(),
            _ => ("SELECT b.Name AS Bottler, co.Name AS Country, c.Code AS Currency, COUNT(i.Id) AS InvoiceCount, SUM(i.Amount) AS TotalAmount " +
                  "FROM Invoices i JOIN Bottlers b ON b.Id = i.BottlerId JOIN Countries co ON co.Id = b.CountryId JOIN Currencies c ON c.Id = b.CurrencyId " +
                  "WHERE i.IsDeleted = 0 GROUP BY b.Name, co.Name, c.Code ORDER BY TotalAmount DESC", [])),
    ];

    /// <summary>Returns the JSON the "model" would answer with for this user prompt.</summary>
    public static string Answer(string userPrompt)
    {
        foreach (var intent in Intents)
        {
            var match = intent.Pattern.Match(userPrompt);
            if (match.Success)
            {
                var (sql, parameters) = intent.Build(match);
                return JsonSerializer.Serialize(new { sql, paramValues = parameters });
            }
        }
        return JsonSerializer.Serialize(new { sql = (string?)null, paramValues = Array.Empty<object>(), error = FallbackError });
    }

    private static int ParseInt(string text, int fallback) => int.TryParse(text, out var n) && n > 0 ? Math.Min(n, 200) : fallback;

    private static string StatusCase(string column)
        => "CASE " + column + " " + string.Join(' ', EnumDisplay.Options<InvoiceStatus>().Select(o => $"WHEN {(int)o.Value} THEN '{o.Text.Replace("'", "''")}'")) + " ELSE 'Unknown' END";

    [GeneratedRegex(@"top\s+(\d+)\s+payers?", RegexOptions.IgnoreCase)] private static partial Regex TopPayers();
    [GeneratedRegex(@"top\s+(\d+)\s+bottlers?", RegexOptions.IgnoreCase)] private static partial Regex TopBottlers();
    [GeneratedRegex(@"(?:bottler\s+(?:named|called)|(?:for|of|from)\s+bottler)\s+(?<name>[^,.?]+)", RegexOptions.IgnoreCase)] private static partial Regex BottlerNamed();
    [GeneratedRegex(@"last\s+(\d+)\s+months?", RegexOptions.IgnoreCase)] private static partial Regex LastMonths();
    [GeneratedRegex(@"\b(in|for|during)\s+(20\d\d)\b", RegexOptions.IgnoreCase)] private static partial Regex InYear();
    [GeneratedRegex(@"\b(by|per|group)\b.*\bstatus", RegexOptions.IgnoreCase)] private static partial Regex ByStatus();
    [GeneratedRegex(@"currenc(y|ies)", RegexOptions.IgnoreCase)] private static partial Regex ByCurrency();
    [GeneratedRegex(@"\b(by|per|group)\b.*\bprograms?\b", RegexOptions.IgnoreCase)] private static partial Regex ByProgram();
    [GeneratedRegex(@"\b(by|per|group)\b.*\bsales\s*cent(er|re)s?\b", RegexOptions.IgnoreCase)] private static partial Regex BySalesCenter();
    [GeneratedRegex(@"\b(by|per|group)\b.*\b(invoice\s+)?types?\b", RegexOptions.IgnoreCase)] private static partial Regex ByType();
    [GeneratedRegex(@"\b(by|per|group)\b.*\bbottlers?\b", RegexOptions.IgnoreCase)] private static partial Regex ByBottler();
}
