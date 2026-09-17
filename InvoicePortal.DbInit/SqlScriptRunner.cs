using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace InvoicePortal.DbInit;

/// <summary>
/// Applies the checked-in <c>db/*.sql</c> files to a freshly created database, in filename order.
/// This is the route used when no bacpac is mounted, so the repository builds a working demo
/// database on its own.
/// </summary>
internal static partial class SqlScriptRunner
{
    // A batch separator is a line containing only GO, optionally followed by a line comment.
    // The trailing \r? matters: the scripts are committed with CRLF endings on Windows.
    [GeneratedRegex(@"^[\t ]*GO[\t ]*(?:--[^\r\n]*)?\r?$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex BatchSeparator();

    public static IReadOnlyList<string> FindScripts(string directory)
        => Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*.sql").OrderBy(Path.GetFileName, StringComparer.Ordinal).ToArray()
            : [];

    public static async Task RunAsync(string connectionString, IReadOnlyList<string> scriptPaths, Action<string> log)
    {
        // One connection for all files: the scripts rely on session state (SET NOEXEC, IDENTITY_INSERT).
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var path in scriptPaths)
        {
            var batches = BatchSeparator()
                .Split(await File.ReadAllTextAsync(path))
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .ToArray();

            log($"Applying {Path.GetFileName(path)} ({batches.Length} batch{(batches.Length == 1 ? "" : "es")})...");

            foreach (var batch in batches)
            {
                await using var command = new SqlCommand(batch, connection) { CommandTimeout = 0 };
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
