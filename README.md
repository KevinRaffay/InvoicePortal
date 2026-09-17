# Invoice Portal Admin (proof of concept)

Blazor Web App (.NET 10, Interactive Server) with Radzen components that manages a **local** copy
of the Invoice Portal database. Everything runs in Docker Compose:

| Service   | What it is                                                                                  |
|-----------|---------------------------------------------------------------------------------------------|
| `sql`     | SQL Server 2022 **Express** (`mcr.microsoft.com/mssql/server:2022-latest`, `MSSQL_PID=Express`) |
| `db-init` | One-shot .NET console app that builds the database from [`db/`](db/) - schema plus synthetic demo data |
| `app`     | The Blazor admin UI on <http://localhost:8080>                                              |
| `aspire-dashboard` | Optional (`--profile otel`): local OpenTelemetry sink with a UI on <http://localhost:18888> |

No connection to Azure is made by any of these. `.env.Development` in this folder is **not** read by
any code; it was only used once, read-only, to inspect the schema.

The database is checked in as SQL, so the repository stands on its own - no bacpac, no export, no
production data. See [db/README.md](db/README.md).

## Prerequisites

- Docker Desktop with Linux containers and **at least 4 GB of memory** (Settings -> Resources).
  The SQL Server image refuses to start below 2 GB; `db-init` checks and fails fast with a message.
- For host-side development only: .NET SDK 10.

## Run

```bash
docker compose up --build
```

Then open <http://localhost:8080>. The first run takes a few minutes for image pulls and the build;
the database itself is created in about a second. Subsequent runs skip it entirely, because
`db-init` exits immediately when the database already exists.

Rebuild the database from scratch:

```bash
docker compose down -v
```

## What `db-init` does

By default it creates the database and applies [`db/001_schema.sql`](db/001_schema.sql) and
[`db/002_seed_demo_data.sql`](db/002_seed_demo_data.sql) in order, on a single connection. If a
script fails it drops the partial database so the next run retries.

Set `BACPAC_PATH` and it takes the other route instead, restoring a bacpac with DacFx. Azure SQL
bacpacs contain objects an on-premises SQL Server cannot create: Entra ID (external) users, their
role memberships and grants, a database master key, a database scoped credential and the TDE flag.
`db-init` writes a sanitised **copy** of the bacpac (the original is never modified), removes those
elements from `model.xml`, rewrites the checksum in `Origin.xml`, and imports that.
`docker-compose.bacpac.yml` wires this up:

```bash
docker compose -f docker-compose.yml -f docker-compose.bacpac.yml up --build
```

Bacpacs are git-ignored and must stay that way: they hold production data and this repository is
public.

## Scope of the UI

- **Invoices**: full CRUD with a tabbed edit dialog, server-side paging/sorting/filtering, column
  picker, soft delete (`IsDeleted`) with a "Show deleted" toggle and Restore.
- **Lookups**: Bottlers, Payers, Sales Centers, Programs (soft delete), Currencies. Bottlers, Payers
  and Sales Centers use SAP-sourced ids, so Create asks for the Id and checks for duplicates.
- Companies, Countries and Brand Segment Categories are scaffolded for dropdowns only.
- **AI Insights** (`/ai/query`) and **Ask the documents** (header button, and per row on Bottlers): see
  [AI features (mocked)](#ai-features-mocked).

## Project layout

Architecture, data flows and integration points are described in [ARCHITECTURE.md](ARCHITECTURE.md)
(rendered copies with diagrams: [docs/ARCHITECTURE.html](docs/ARCHITECTURE.html) and
[docs/ARCHITECTURE.pdf](docs/ARCHITECTURE.pdf), regenerated with `npm run build:pdf` in `docs/`, Node 18+;
the `update-architecture` skill in `.claude/skills/` walks through keeping them current).

```
InvoicePortal.slnx
docker-compose.yml, .env            compose definition and local SA password (git-ignored)
db/                                 schema + synthetic demo data as SQL (see db/README.md)
InvoicePortal.DbInit/               builds the database from db/, or imports a bacpac (runs once)
InvoicePortal.Admin/
  Data/InvoicePortalDbContext.cs    EF Core scaffold output - never hand-edited
  Data/Entities/*.cs                EF Core scaffold output - never hand-edited
  Data/Entities/Partials/*.cs       partial classes: enum wrappers, display helpers, marker interfaces
  Data/InvoicePortalDbContext.Partial.cs
  Data/Enums/*.cs                   copied from the Invoice Portal backend domain project
  Services/CrudService.cs           generic list/insert/update/delete, soft delete, friendly SQL errors
  Services/LookupService.cs         cached dropdown sources
  Components/Shared/CrudPageBase.cs, EditDialogBase.cs   shared page / dialog behaviour
  Components/Pages/Invoices/, Components/Pages/Lookups/  one grid + one dialog per table
  Ai/                               AI slice: options, DI, mock model, NL-to-SQL guard/executor, document Q&A
  Telemetry/                        OpenTelemetry wiring (traces, metrics, logs), options, own ActivitySource/Meter
  Components/Pages/Ai/              AiQuery.razor (NL query + dynamic grid), AskDocumentsDialog.razor
InvoicePortal.Admin.Tests/          xUnit tests for the AI slice (no database needed)
```

### Re-scaffolding the model

Generated files are never edited by hand, so the scaffold can be re-run at any time against the
local container (the connection string is the one in `appsettings.Development.json`):

```bash
dotnet ef dbcontext scaffold "Server=localhost,1433;Database=InvoicePortal;User Id=sa;Password=<see .env>;Encrypt=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --project InvoicePortal.Admin -o Data/Entities -c InvoicePortalDbContext --context-dir Data --namespace InvoicePortal.Admin.Data.Entities --context-namespace InvoicePortal.Admin.Data --no-onconfiguring --data-annotations --force -t Invoices -t Bottlers -t Payers -t SalesCenters -t Programs -t Currencies -t Companies -t Countries -t BrandSegmentCategories
```

## AI features (mocked)

The AI features reproduce the two headline patterns of
[DanWahlin/customer-insights](https://github.com/DanWahlin/customer-insights) (an Angular + Express demo of
Azure AI Foundry models, Foundry IQ, Microsoft Graph and Azure Communication Services) inside this Blazor app,
with **every external service replaced by an in-process mock**. Nothing leaves the machine and no keys are needed.

| customer-insights                                    | Here                                                                 |
|------------------------------------------------------|----------------------------------------------------------------------|
| GPT model via Azure OpenAI                           | `MockChatClient` (default) or a local Ollama model, both behind `Microsoft.Extensions.AI.IChatClient` |
| `/generateSql`: NL -> SQL, read-only txn, dynamic grid | `/ai/query`: NL -> T-SQL, `SqlGuard` + rollback-only executor, Radzen dynamic grid |
| Foundry IQ knowledge-base retrieve + `[S#]` citations | `InMemoryDocumentRetriever` (seeded docs, BM25, threshold, dedupe) + `CitationSelector` |
| Chat help dialog                                     | "Ask the documents" dialog (header button; per-row on Bottlers, the "customer" here) |
| Email/SMS, Graph, phone (ACS)                        | not included                                                         |

What is real and what is fake:

- **Real**: the system prompts (schema generated from the EF model), JSON contract parsing, SQL validation
  (`Ai/Query/SqlGuard.cs`), execution (5 s command timeout, `LOCK_TIMEOUT`, 200-row cap, transaction always
  rolled back), retrieval -> prompt -> citation validation, rate limiting (10/min per circuit), feature flags.
- **Fake**: the model (`Ai/Chat/MockChatClient.cs` routes on the system prompt: `SqlIntentCatalog` maps a
  dozen regex intents to parameterised T-SQL; `MockAnswerSynthesizer` stitches the most relevant sentences of
  the retrieved chunks and cites them) and the index (`Ai/Documents/SeedDocuments.cs`: six fictional documents
  whose placeholders are filled with the four busiest bottler names from the local database at first use).

Configuration (`appsettings.json`, overridable with environment variables as in `docker-compose.yml`):

| Key                        | Default | Effect                                                       |
|----------------------------|---------|--------------------------------------------------------------|
| `Ai__Enabled`              | `true`  | Hides all AI entry points and makes `/ai/query` refuse when false |
| `Ai__DocumentChatEnabled`  | `true`  | Gates the "Ask the documents" dialog                         |
| `Ai__Provider`             | `Mock`  | `Mock` (offline fake), `Ollama` (local model, below) or `AzureOpenAI` (Azure, see `docs/azure-deploy.md`) |
| `Ai__Ollama__Endpoint`     | `http://localhost:11434` | Ollama base URL (`http://host.docker.internal:11434` from the app container) |
| `Ai__Ollama__Model`        | `qwen2.5-coder:7b` | Model tag; must already be pulled                   |
| `Ai__Ollama__TimeoutSeconds` | `120` | Per-call HTTP timeout                                          |
| `Ai__Ollama__ContextLength` | `8192` | Ollama `num_ctx`; the schema prompt needs more than the default |

The `AzureOpenAI` provider (Entra ID auth, no keys) is what the Azure deployment uses; a Foundry IQ knowledge
base would be another `IDocumentRetriever` implementation in place of the seeded index. Nothing else changes.

### Running a real local model (Ollama)

The `Ollama` provider replaces the fake model with a small model running on this machine, through the same
`IChatClient`. The retriever stays the seeded in-memory index. With a real model the suggestion chips are just
suggestions: free-form questions work, and bad SQL from the model is rejected by `SqlGuard` and shown as an
error rather than executed.

1. Install [Ollama for Windows](https://ollama.com/download) (or `winget install Ollama.Ollama`). It uses the
   NVIDIA GPU directly, no WSL needed. Then pull a coder-tuned model:

   ```bash
   ollama pull qwen2.5-coder:7b
   ```

   `qwen2.5-coder:7b` (about 4.7 GB) just fits a 6 GB GPU; use `qwen2.5-coder:3b` if it spills to CPU or memory
   is tight. Coder models are markedly better at text-to-SQL than general chat models of the same size.

2. Point the app at it.
   - Docker: set `AI_PROVIDER=Ollama` in `.env` (the default endpoint `http://host.docker.internal:11434`
     reaches Ollama on the host) and run `docker compose up -d app`.
   - Host-side: `$env:Ai__Provider='Ollama'` before `dotnet run --project InvoicePortal.Admin --launch-profile http`.

3. Expect the first query after a model load to take a few seconds on the GPU (the schema prompt is a few
   thousand tokens); Ollama caches the shared prompt prefix, so later queries are faster.

CPU-only alternative inside Docker (no host install): raise the Docker Desktop VM memory to about 12 GB
(Settings > Resources; a 7B model does not fit beside SQL Server in the default 4 GB), then

```bash
docker compose --profile ollama up -d
```

```bash
docker compose exec ollama ollama pull qwen2.5-coder:7b
```

and set `AI_OLLAMA_ENDPOINT=http://ollama:11434` with `AI_PROVIDER=Ollama` in `.env`. Without GPU passthrough
(which needs the WSL2 backend) expect 10 to 50 seconds per query depending on model size.

SQL Server caveat: unlike the PostgreSQL `READ ONLY` transaction the reference relies on, SQL Server has no
engine-level read-only transaction. Safety here is guard-based (single SELECT/WITH, keyword denylist, `@p0..`
parameters only, no comments or `@@` variables) plus timeout, lock timeout, row cap and rollback. For anything
beyond a local POC, point the executor at a `db_datareader` login as well.

Tests (no database needed):

```bash
dotnet test InvoicePortal.slnx
```

## Logging (Serilog)

All Admin `ILogger<T>` messages and direct Serilog events now use one Serilog pipeline. No changes are
needed in consuming services. The bootstrap logger writes to console; the fully configured logger takes
over at host construction, and shutdown flushes sinks. Forced process termination can lose buffered logs.

| Hosting | Log destinations |
|---------|------------------|
| Local .NET process or local Docker (including `Production`) | Console + rolling newline-delimited JSON files, optional OTLP logs for Aspire |
| Azure Container Apps / App Service | Console + Application Insights + Datadog, with each cloud sink enabled only when its credential is supplied; no rolling files |

Azure detection uses `CONTAINER_APP_NAME`, `WEBSITE_INSTANCE_ID`, or `WEBSITE_SITE_NAME`, **not** the
`Production` environment or the presence of cloud credentials. Override with `AppLogging__RunningInAzure`
(`true`/`false`) for other hosting such as Azure VMs/AKS. Leave it unset for automatic detection.

Local defaults: `logs/invoiceportal-YYYYMMDD.json` relative to the app's content root, daily rolling and
additional rolls at 10 MiB, retaining 14 **files**, not necessarily 14 days. A single oversized event may
exceed the size threshold. Configure `AppLogging__FilePath`, `AppLogging__FileSizeLimitBytes`, and
`AppLogging__RetainedFileCountLimit`. The default log directory is excluded from git, Docker build context,
and publish output. Use a persistent writable volume for local Docker if logs must survive container
replacement (default path `/app/logs`); custom paths require equivalent exclusions/permissions.

### Azure logging configuration

| Setting | Purpose |
|---------|---------|
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Already supplied by the Bicep template. `Telemetry__AzureMonitorConnectionString` takes precedence if set. |
| `DD_API_KEY` | Datadog API key, supplied as an Azure container/app secret reference or Key Vault reference. Never commit it. `AppLogging__DatadogApiKey` is an alternative. |
| `DD_SITE` | Datadog region, default `datadoghq.com`. Supported: `datadoghq.eu`, `us3.datadoghq.com`, `us5.datadoghq.com`, `ap1.datadoghq.com`, `ap2.datadoghq.com`, `ddog-gov.com`. `AppLogging__DatadogSite` takes precedence. Logs use HTTPS port 443. |
| `DD_SERVICE`, `DD_ENV`, `DD_VERSION` | Optional Datadog service/environment/version overrides; defaults match the app's service name, ASP.NET environment and assembly version. |
| `AppLogging__ApplicationInsightsEnabled`, `AppLogging__DatadogEnabled` | Independently disable a cloud sink; both default to `true`. Only effective in Azure. |
| `AppLogging__ConsoleEnabled` | Defaults to `true`. Disable when Azure console collection would be redundant with direct sinks. Bootstrap failures still reach console. |

The current infrastructure **does not provision Datadog or bind its API key**. Configure that secret reference
and site in your deployment configuration before expecting Datadog delivery, and ensure later IaC deployments
preserve them. No Azure deployment is performed by this logging change. Missing credentials produce a startup
warning instead of preventing the app from starting. Datadog failures emit a generic stderr diagnostic without
echoing credentials. Remote delivery is best-effort, asynchronously buffered (Datadog queue: 10,000 events),
not a durable audit log. Network failures/queue exhaustion or forced shutdown can lose logs.

### Telemetry conflicts, redundancy and privacy

- **Application Insights duplicate logs prevented:** Serilog is the only Application Insights log sender.
  The previous `AddAzureMonitorLogExporter` is removed. OpenTelemetry still exports traces and metrics.
  Do not add `AddApplicationInsightsTelemetry`, another Application Insights logger provider, or a second
  request/dependency auto-instrumentation agent alongside the current OpenTelemetry instrumentation.
- **Aspire preserved:** Serilog forwards once to the OpenTelemetry log provider via `writeToProviders`.
  Default Console/Debug/EventSource logging providers are removed so console output is not duplicated.
- **Collector duplication:** if your OTLP collector also forwards logs to Application Insights/Datadog,
  set `Telemetry__OtlpLogsEnabled=false` (or disable the corresponding direct Serilog sink). Traces/metrics
  continue. Azure startup emits a warning when OTLP logs are also configured.
- **Console/agent duplication:** Azure Container Apps already collects stdout into Log Analytics. An agent
  collecting stdout and forwarding it to Datadog can duplicate direct sink delivery. Choose one route;
  otherwise expect additional storage/ingestion cost. Azure startup warns when console logging is enabled.
- **Signals are not interchangeable:** a dependency span and an application log can describe the same
  operation. Exceptions may appear as both an OpenTelemetry span event and an Application Insights exception.
  Trace sampling does not sample Serilog logs. No Serilog request-logging middleware or Datadog APM profiler
  is added, avoiding another request-span pipeline. Datadog receives logs only; APM correlation requires
  separately configured compatible trace ingestion/remapping. Application Insights logs use captured
  Activity trace/span IDs and matching cloud role names for correlation.
- **SQL/privacy:** `Telemetry__RecordSqlText=false` protects spans, not log messages. EF
  `Microsoft.EntityFrameworkCore.Database.Command` logs are suppressed below `Fatal` by default (including
  failed-command SQL text). Other errors still surface through EF/query/circuit loggers. Do not enable
  sensitive-data logging or log invoice content, credentials, prompts, or completions. Arbitrary message
  text/exception details are **not automatically redacted**. Restrict file permissions and remote retention.
- **Configuration:** use `Serilog:MinimumLevel:Default` and `Serilog:MinimumLevel:Override` for levels;
  the former `Logging:LogLevel` settings no longer control the Serilog pipeline. Destinations under
  `Serilog:WriteTo` or `Serilog:AuditTo` are rejected at startup: code selects sinks by hosting to prevent
  duplicates and accidental local-to-cloud export.
- `Telemetry__Enabled=false` disables OpenTelemetry only. Serilog still writes local files or configured
  Azure sinks; cloud log settings are independent of trace/metric switches.

## Telemetry (OpenTelemetry)

The app emits OpenTelemetry traces (requests, SQL commands, outgoing HTTP, one span per chat-model call with
token usage, and its own `ai.query.generate` / `ai.query.execute` / `ai.documents.answer` spans), metrics
(ASP.NET Core, runtime, EF Core, chat tokens, `invoiceportal.ai.requests`) and optional Serilog-forwarded OTLP
logs with trace ids. **Local defaults do not export remotely** (console and files are still written).
Exporters switch on by configuration:

| Setting                                 | Effect                                                                  |
|-----------------------------------------|-------------------------------------------------------------------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` (or `Telemetry__OtlpEndpoint`) | Send traces, metrics and optionally logs over OTLP (gRPC; set `Telemetry__OtlpProtocol=http/protobuf` for HTTP) |
| `Telemetry__OtlpLogsEnabled` | `true`: enable the OTLP log route; `false` disables just OTLP logs, not traces/metrics |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` (or `Telemetry__AzureMonitorConnectionString`) | OpenTelemetry traces/metrics to Application Insights; Serilog logs to it only in Azure. The Azure template sets this. |
| `Telemetry__RecordSqlText`              | `false`: SQL statement text is **not** recorded on spans (the data is a production copy) |
| `Telemetry__RecordAiContent`            | `false`: prompts and completions are **not** recorded on chat spans      |
| `Telemetry__TraceSamplingRatio`         | `1.0`: head sampling for traces                                          |
| `Telemetry__Enabled`                    | `true`: set `false` to register no OpenTelemetry at all                  |

To look at it locally, start the standalone .NET Aspire Dashboard (in-memory, no storage) and point the app at it:

```bash
docker compose --profile otel up -d
```

Then set `OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889` in `.env`, run `docker compose up -d app`
and open <http://localhost:18888>. Host-side, use the launch profile that already points at the dashboard:

```bash
dotnet run --project InvoicePortal.Admin --launch-profile http-otel
```

The dashboard shows traces (a `/ai/query` request → `ai.query.generate` → `chat mock-invoice-portal`
→ `ai.query.execute` → the SQL command), the metrics above, and structured logs correlated by trace id.

## Deploy to Azure (azd + GitHub Actions)

`azure.yaml` and `infra/` follow the customer-insights pattern: the Azure Developer CLI provisions a Container
Apps environment, registry, managed identity, Blob storage for Data Protection keys, App Insights and Azure
OpenAI, then builds and deploys the container. The existing Azure SQL database is referenced, never created or
restored. `.github/workflows/azure-dev.yml` runs the same `azd provision` / `azd deploy` on pushes to `main`
with OIDC, and `ci.yml` builds, tests and validates on pull requests. Step-by-step, including the one-time SQL
user and Entra sign-in setup, is in [docs/azure-deploy.md](docs/azure-deploy.md).

## Host-side development

With the `sql` container running (`docker compose up -d sql db-init`):

```bash
dotnet run --project InvoicePortal.Admin --launch-profile http
```

`appsettings.Development.json` points at `localhost,1433` with the same throwaway SA password as `.env`.

## Troubleshooting

- `docker compose up` hangs at "Container invoiceportal-db-init Starting" while `docker ps` still works:
  the Docker Desktop engine has wedged on that container (seen once on the Hyper-V backend; `docker start`,
  `docker inspect` and `docker rm` on it all time out). Quit and reopen Docker Desktop, then run
  `docker compose up -d` again. The SQL data volume survives the restart.
- SQL container exits immediately: Docker Desktop memory is below 2 GB (see Prerequisites).

## Data caveat

**This repository is public.** Everything committed under [`db/`](db/) is invented for the demo -
no row is derived from, sampled from, or anonymised out of a production system.

A bacpac, if you use one, is a copy of production data (invoices, user emails). It lives only on
your disk and on the local Docker volume `mssql-data`; `.gitignore` excludes `*.bacpac` and
`.env*`. Do not commit either, and do not push the volume or the bacpac anywhere.
