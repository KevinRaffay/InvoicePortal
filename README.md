# Invoice Portal Admin (proof of concept)

Blazor Web App (.NET 10, Interactive Server) with Radzen components that manages a **local** copy
of the Invoice Portal database. Everything runs in Docker Compose:

| Service   | What it is                                                                                  |
|-----------|---------------------------------------------------------------------------------------------|
| `sql`     | SQL Server 2022 **Express** (`mcr.microsoft.com/mssql/server:2022-latest`, `MSSQL_PID=Express`) |
| `db-init` | One-shot .NET console app that restores `mec-invoiceportal-prod.bacpac` into `sql` (DacFx)  |
| `app`     | The Blazor admin UI on <http://localhost:8080>                                              |
| `aspire-dashboard` | Optional (`--profile otel`): local OpenTelemetry sink with a UI on <http://localhost:18888> |

No connection to Azure is made by any of these. `.env.Development` in this folder is **not** read by
any code; it was only used once, read-only, to inspect the schema.

## Prerequisites

- Docker Desktop with Linux containers and **at least 4 GB of memory** (Settings -> Resources).
  The SQL Server image refuses to start below 2 GB; `db-init` checks and fails fast with a message.
- `mec-invoiceportal-prod.bacpac` in this folder (it is git-ignored).
- For host-side development only: .NET SDK 10.

## Run

```bash
docker compose up --build
```

Then open <http://localhost:8080>. The first run takes a few minutes (image pulls, build, and the
bacpac import, roughly one minute for ~744k rows). Subsequent runs skip the import because `db-init`
exits immediately when the database already exists.

Reset the database to the bacpac contents:

```bash
docker compose down -v
```

## What `db-init` does

Azure SQL bacpacs contain objects an on-premises SQL Server cannot create: Entra ID (external) users,
their role memberships and grants, a database master key, a database scoped credential and the TDE
flag. `db-init` writes a sanitised **copy** of the bacpac (the original is never modified), removes
those elements from `model.xml`, rewrites the checksum in `Origin.xml`, and imports it with
`Microsoft.SqlServer.DacFx`. If the import fails it drops the partial database so the next run retries.

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
InvoicePortal.DbInit/               bacpac sanitiser + importer (runs once)
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

## Telemetry (OpenTelemetry)

The app emits OpenTelemetry traces (requests, SQL commands, outgoing HTTP, one span per chat-model call with
token usage, and its own `ai.query.generate` / `ai.query.execute` / `ai.documents.answer` spans), metrics
(ASP.NET Core, runtime, EF Core, chat tokens, `invoiceportal.ai.requests`) and logs with trace ids. **By default
nothing is exported.** Exporters switch on by configuration:

| Setting                                 | Effect                                                                  |
|-----------------------------------------|-------------------------------------------------------------------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` (or `Telemetry__OtlpEndpoint`) | Send everything over OTLP (gRPC; set `Telemetry__OtlpProtocol=http/protobuf` for HTTP) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` (or `Telemetry__AzureMonitorConnectionString`) | Send everything to Application Insights. The Azure template sets this. |
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

The bacpac is a copy of production data (invoices, user emails). It lives only on the local Docker
volume `mssql-data`; `.gitignore` excludes `*.bacpac` and `.env*`. Do not push the volume or the
bacpac anywhere.
