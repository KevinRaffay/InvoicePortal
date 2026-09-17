using InvoicePortal.DbInit;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;

// One-shot database initialiser for the local SQL Server (Express) container. Idempotent -
// exits immediately if the database already exists. Two routes:
//
//   scripts (default)  Creates the database and applies the checked-in db/*.sql files: the
//                      schema plus synthetic demo data. Needs nothing outside the repository.
//   bacpac (opt-in)    Restores a bacpac mounted at BACPAC_PATH instead. Used when you have a
//                      real export to work against; see docker-compose.bacpac.yml.

const long MinimumMemoryKb = 2L * 1024 * 1024; // mssql container image minimum

var server = GetEnv("SQL_SERVER", "localhost");
var port = GetEnv("SQL_PORT", "1433");
var dbName = GetEnv("DB_NAME", "InvoicePortal");
var password = GetEnv("MSSQL_SA_PASSWORD", string.Empty);
if (password.Length == 0)
{
    Fail("MSSQL_SA_PASSWORD is not set.");
}
var bacpacPath = GetEnv("BACPAC_PATH", string.Empty);
var scriptsPath = GetEnv("SCRIPTS_PATH", "/app/db");

CheckMemory();

var useBacpac = bacpacPath.Length > 0;
if (useBacpac && !File.Exists(bacpacPath))
{
    Fail($"BACPAC_PATH is set to {bacpacPath} but no file is mounted there.");
}

IReadOnlyList<string> scripts = useBacpac ? [] : SqlScriptRunner.FindScripts(scriptsPath);
if (!useBacpac && scripts.Count == 0)
{
    Fail($"No .sql files found in {scriptsPath}. Expected the repository's db/ folder to be present in the image.");
}

var masterConnectionString = new SqlConnectionStringBuilder
{
    DataSource = $"{server},{port}",
    InitialCatalog = "master",
    UserID = "sa",
    Password = password,
    Encrypt = SqlConnectionEncryptOption.Mandatory,
    TrustServerCertificate = true,
    ConnectTimeout = 5,
}.ConnectionString;

await WaitForSqlServerAsync(masterConnectionString, TimeSpan.FromMinutes(3));

if (await DatabaseExistsAsync(masterConnectionString, dbName))
{
    Log($"Database '{dbName}' already present - nothing to do.");
    return 0;
}

if (!useBacpac)
{
    Log($"Building database '{dbName}' on {server},{port} from {scripts.Count} script(s) in {scriptsPath}...");
    var scriptsStarted = DateTime.UtcNow;

    try
    {
        await CreateDatabaseAsync(masterConnectionString, dbName);

        var dbConnectionString = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = dbName,
            ConnectTimeout = 30,
        }.ConnectionString;

        await SqlScriptRunner.RunAsync(dbConnectionString, scripts, Log);
        Log($"Database built in {DateTime.UtcNow - scriptsStarted:mm\\:ss}.");
        return 0;
    }
    catch (Exception ex)
    {
        Log($"Script run FAILED: {ex}");
        await TryDropDatabaseAsync(masterConnectionString, dbName);
        return 1;
    }
}

var sanitizedPath = Path.Combine(Path.GetTempPath(), "invoiceportal-sanitized.bacpac");
Log($"Sanitizing bacpac {bacpacPath} for on-premises import...");
BacpacSanitizer.Create(bacpacPath, sanitizedPath, Log);

Log($"Importing into database '{dbName}' on {server},{port} (this takes a few minutes)...");
var started = DateTime.UtcNow;

try
{
    var services = new DacServices(masterConnectionString);
    services.Message += (_, e) => Log($"  {e.Message}");
    services.ProgressChanged += (_, e) => Log($"  [{e.Status}] {e.Message}");

    using var package = BacPackage.Load(sanitizedPath);
    var options = new DacImportOptions
    {
        CommandTimeout = 0,
    };

    services.ImportBacpac(package, dbName, options);
    Log($"Import completed in {DateTime.UtcNow - started:mm\\:ss}.");
    return 0;
}
catch (Exception ex)
{
    Log($"Import FAILED: {ex}");
    await TryDropDatabaseAsync(masterConnectionString, dbName);
    return 1;
}
finally
{
    try { File.Delete(sanitizedPath); } catch { /* best effort */ }
}

static string GetEnv(string name, string fallback)
    => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

static void Log(string message) => Console.WriteLine($"[db-init {DateTime.UtcNow:HH:mm:ss}] {message}");

static string Fail(string message)
{
    Log(message);
    Environment.Exit(1);
    return string.Empty;
}

static void CheckMemory()
{
    const string memInfo = "/proc/meminfo";
    if (!File.Exists(memInfo))
    {
        return;
    }

    var line = File.ReadLines(memInfo).FirstOrDefault(l => l.StartsWith("MemTotal:", StringComparison.Ordinal));
    if (line is null)
    {
        return;
    }

    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length < 2 || !long.TryParse(parts[1], out var totalKb))
    {
        return;
    }

    Log($"Docker engine memory: {totalKb / 1024} MiB");
    if (totalKb < MinimumMemoryKb)
    {
        Fail("SQL Server needs at least 2 GB. Raise Docker Desktop -> Settings -> Resources -> Memory to 4 GB or more, then run `docker compose up` again.");
    }
}

static async Task WaitForSqlServerAsync(string connectionString, TimeSpan timeout)
{
    var deadline = DateTime.UtcNow + timeout;
    Exception? last = null;

    while (DateTime.UtcNow < deadline)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync();
            Log("SQL Server is ready.");
            return;
        }
        catch (Exception ex)
        {
            last = ex;
            Log($"Waiting for SQL Server... ({ex.GetType().Name}: {ex.Message})");
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    Fail($"SQL Server did not become ready within {timeout.TotalSeconds:0}s. Last error: {last?.Message}");
}

static async Task<bool> DatabaseExistsAsync(string connectionString, string dbName)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand("SELECT DB_ID(@name)", connection);
    command.Parameters.AddWithValue("@name", dbName);
    var result = await command.ExecuteScalarAsync();
    return result is not null && result != DBNull.Value;
}

static async Task CreateDatabaseAsync(string connectionString, string dbName)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new SqlCommand($"CREATE DATABASE [{dbName}];", connection) { CommandTimeout = 120 };
    await command.ExecuteNonQueryAsync();
}

static async Task TryDropDatabaseAsync(string connectionString, string dbName)
{
    try
    {
        if (!await DatabaseExistsAsync(connectionString, dbName))
        {
            return;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        var sql = $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{dbName}];";
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
        Log($"Dropped partially imported database '{dbName}' so the next run can retry.");
    }
    catch (Exception ex)
    {
        Log($"Could not drop partial database: {ex.Message}");
    }
}
