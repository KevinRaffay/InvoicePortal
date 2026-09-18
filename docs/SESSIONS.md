# How this project was built: sessions and prompts

The Invoice Portal Admin proof of concept was built with Claude Code over three days, 15–17 September 2026,
in ten sessions. This file is the record of that work: every prompt the author typed, the choices made in
Claude's clarification dialogs, what each session produced, and the commits it landed in.

It was reconstructed from the Claude Code transcripts stored on the author's machine, so it is complete for
sessions run there. Times are Pacific (UTC−7), matching `git log`. Prompts are quoted as typed, including typos,
except for the redactions below.

**Redactions.** This is a public repository. Connection strings, credentials, internal environment names and
server addresses have been replaced with bracketed placeholders such as `[the dev environment]`. Two long pastes
(a `dotnet run` crash log and a laptop hardware inventory) are summarised in brackets instead of reproduced.
Nothing else was altered.

**Conventions**

- `>` blockquote: a prompt, verbatim.
- `[answered: …]`: options picked in a Claude clarification dialog (the "ask user question" step of plan mode).
- `[command: …]` / `[skill: …]`: a slash command typed, or a repo skill Claude invoked.
- `[pasted …]`: a long paste, summarised.

## Timeline

| # | Date (PT) | Session focus | Prompts | Shipped |
|---|---|---|---|---|
| 1 | Sep 15, 08:20–15:47 | Kickoff: read the schema, build the Blazor + Radzen CRUD app, containerise it | 7 (+1 dialog) | `3417ee4 init` |
| 2 | Sep 15 22:32 → Sep 16 07:54 | AI features patterned on customer-insights, everything external mocked | 1 (+1 dialog, +`/compact`) | `02b241a llm` |
| 3 | Sep 16, 07:58–17:32 | Architecture document, HTML/PDF toolchain, a skill to repeat it, executive deck | 9 | `01d7078 Add docs`, `d31b90a presentation` |
| 4 | Sep 16, 07:59–10:30 | Local LLM on the laptop, WSL or not, Azure deployment with azd + GitHub Actions | 8 | folded into `428ba54` |
| 5 | Sep 16, 10:13–11:34 | OpenTelemetry | 1 | `428ba54 telemetry and aspire` |
| 6 | Sep 16, 10:18 | Is Aspire useful here? | 1 | advisory only |
| 7 | Sep 16, 14:52–15:02 | Port already in use, no telemetry showing | 4 | advisory only |
| 8 | Sep 16, 17:31 | Is there a use for Microsoft Agent Framework? | 1 | advisory only |
| 9 | Sep 17, 11:28–16:25 | Database checked in as SQL, README quickstart, per-request logging | 4 (+1 dialog) | `3ac575a`, `ce2b7e8`, PR #1, PR #2, `81b7e2f` |
| 10 | Sep 17, 16:36 | How am I connected to GitHub? | 1 | advisory only |
| 11 | Sep 17, 16:59 | This document | 1 (+1 dialog) | `docs/SESSIONS.md` |

Forty typed prompts in total, plus four clarification dialogs and one `/compact`. Two commits have no local
transcript; see [Work not captured in transcripts](#work-not-captured-in-transcripts).

## Session 1: Kickoff (Sep 15, 08:20–15:47)

**Goal.** Turn an existing invoice database into a working admin app, then make it run anywhere with Docker.

**Prompts**

1. 08:20
   > 1.  Use the connection in `.env.Development` to read the schema of the database
   > 2. Build a CRUD app using Blazor with Radzen components to manage that database
2. 08:24
   > use the connection string for [the dev environment]
3. 08:33 `[answered: table scope = "Reference/master data only"; delete behaviour = "Soft-delete where flag exists"; target = ".NET 10"; sign-in = "No sign-in, local dev tool"]`
4. 09:34
   > implement this plan
5. 09:43
   > switch to auto mode
6. 13:13
   > Try again
7. 15:44
   > is this error: Cannot start Docker Compose application. Reason: compose [start] exit status 1. could not find db-init: not found
8. 15:45
   > run it

A first attempt at 08:20 (a separate three-minute session) was interrupted twice while it probed the wrong
connection and was abandoned; the session above superseded it.

**Decisions.** Reference and master data plus invoices in scope, soft delete wherever the table has a flag,
.NET 10, no authentication for a local tool. Claude planned in plan mode and asked the four questions above
before writing code; the author then switched to auto mode for the long implementation run.

**Outcome.** Schema read through the read-only SQL MCP server. A .NET 10 Blazor Web App (global interactive
server render mode) with Radzen CRUD pages for the lookups and invoices, built on a scaffolded EF Core model.
A Docker Compose stack of `sql` (SQL Server 2022 Express), `db-init` and `app`, with `db-init` sanitising and
restoring a database backup through DacFx and no-oping when the database already exists. The afternoon was spent
un-wedging a stuck `db-init` container; the session ended with the stack healthy and the Invoices grid serving
on port 8080.

**Commit.** `3417ee4 init` (Sep 16, 08:11).

## Session 2: AI features patterned on customer-insights (Sep 15 22:32 → Sep 16 07:54)

**Goal.** Demonstrate the AI patterns of [DanWahlin/customer-insights](https://github.com/DanWahlin/customer-insights)
inside this app without calling any external service.

**Prompts**

1. 22:32
   > Analyze https://github.com/DanWahlin/customer-insights.  Design a plan to emulate the same functionality to use ai but do not implement actual calls to Foundry IQ, Microsoft Graph, and ACS, stub those out with mocks.  the goal is to demonstrate how a user can use ai in blazor-demo the same way it is used in https://github.com/DanWahlin/customer-insights
2. 22:44 `[answered: LLM = "Mock IChatClient too"; scope = "Just NL query + document Q&A (features 1 and 3)"; "customer" entity = "Bottler"]`
3. Sep 16, 07:54 `[command: /compact]`

**Decisions.** Mock the model as well as the services, so the demo runs offline with no keys. Only two of the
reference's five features: natural-language query over invoice data with a dynamic grid, and "Ask the documents"
with `[S#]` citations. Bottler plays the role of the reference's Customer.

**Outcome.** An `Ai/` slice behind `Microsoft.Extensions.AI.IChatClient`: schema-aware prompt, JSON contract
parsing, a SQL guard (single SELECT, keyword denylist, `@p0…` parameters only), a rollback-only executor with
timeouts and a row cap, and a Radzen grid whose columns come from the result set. A seeded document index with
BM25 retrieval and a citation validator standing in for Foundry IQ. A deterministic mock model that routes on
the system prompt. An xUnit project (70 tests, no database needed) and a README section mapping each reference
feature to its local counterpart.

**Commit.** `02b241a llm` (Sep 16, 09:01).

## Session 3: Architecture document, toolchain, skill, executive deck (Sep 16, 07:58–17:32)

**Goal.** Explain the system to other people: first as a document with diagrams, then as a 30-minute deck.

**Prompts**

1. 07:58
   > create an architectural document explaining this application. detail the data flow and integration points to database and ai services.
2. 09:13
   > I can't view the diagrams in preview. Is there another way?
3. 09:44
   > can you create the html page and add it to the repo?
4. 09:58
   > shouldn't i use a skill to repeat this process?
5. 10:00
   > yes, build both
6. 10:23
   > i also want a pdf generated
7. 10:25
   > i see you prefer python tooling.  can't this all be done with js libraries?
8. 10:26
   > use js
9. 16:09
   > I have to create a presentation to IT executives describing this system. I will present the information about the overall architecture, and then describe the ui and ai integration. Another developer will discuss all the database functionalty. Create a powerpoint deck with high level information that can be presented by me and my database colleague. This should be a 30 minute presentation. Run the application to show screenshots or, if possible, record animations of the UI with the ai components. Use a friendly casual tone. Don't get into the weeds. high level only

**Decisions.** Mermaid diagrams that would not render in the editor preview led to a rendered HTML copy in the
repo. "Build both" created the `update-architecture` skill and its build script together. The author rejected a
Python rendering pipeline in favour of JavaScript so the docs toolchain matches a Node-based team.

**Outcome.** `ARCHITECTURE.md` as the source of truth, rendered by `docs/build.js` (markdown-it plus
puppeteer-core driving the locally installed browser, failing on any Mermaid syntax error) into
`docs/ARCHITECTURE.html` and `docs/ARCHITECTURE.pdf`, with `docs/package.json` scripts and a dependency-free
static server. The `update-architecture` skill so the document can be refreshed after later changes. Finally
`docs/exec-briefing/InvoicePortalAdmin-ExecBriefing.pptx`: twenty slides in four chapters with casual speaker
notes timed at 8, 12, 8 and 2 minutes, illustrated with screenshots and a GIF captured from the running app.

**Commits.** `01d7078 Add docs` (Sep 16, 09:51), `d31b90a presentation` (Sep 17, 11:09).

## Session 4: Local LLM, WSL, Azure deployment with azd (Sep 16, 07:59–10:30)

Same transcript as session 2, resumed the next morning.

**Goal.** Could a real model replace the mock on the laptop, and what would it take to run this in Azure?

**Prompts**

1. 07:59
   > can this application run using a small local llm running in the container?  Isn't the llm used to convert plain language queries into sql?
2. 08:15
   > Here are my laptop specs, evaluTE for performance: `[pasted memory, CPU and GPU inventory: 32 GB DDR5 in one slot, 14-core i7, 6 GB NVIDIA laptop GPU]`
3. 08:22
   > how do i setup wsl
4. 08:24
   > let's do this without wsl.  do i have to download ollama?
5. 09:52
   > explain the prerequisites to deploy this app to azure.  what needs to be provisioned?
6. 09:57
   > i want to use github actions
7. 10:04
   > isn't az cli the better way?  the https://github.com/DanWahlin/customer-insights does, and this app is patterned after that
8. 10:07
   > go ahead, set it up with azd.

**Decisions.** The laptop's Docker Desktop runs the Hyper-V backend with a 4 GB VM and no WSL, so GPU
passthrough to containers was not available; the answer was Ollama installed on Windows using the GPU, reached
from the container over `host.docker.internal`. For Azure, the author first asked for GitHub Actions, then
checked the reference repo: it uses the Azure Developer CLI with Bicep, not raw `az` commands and no workflows.
The final choice was to follow the reference (azd + Bicep) and have GitHub Actions run azd.

**Outcome.** An `Ollama` provider behind the same `IChatClient`, an optional CPU-only Ollama sidecar in
Compose, JSON response mode and few-shot examples in the SQL prompt, and readable transport errors. Then
`azure.yaml`, `infra/main.bicep` and `infra/resources.bicep` provisioning Container Apps (sticky sessions,
one replica), a container registry, a managed identity, Blob storage for Data Protection keys, Log Analytics
with Application Insights, Azure OpenAI with a `gpt-4.1-mini` deployment and optional Entra sign-in;
`.github/workflows/azure-dev.yml` and `ci.yml`; `docs/azure-deploy.md`; and the code the host needs
(`/healthz`, Blob-persisted keys, an `AzureOpenAI` provider). The existing Azure SQL database is referenced,
never created or restored. Nothing was provisioned from the session.

**Commit.** Folded into `428ba54 telemetry and aspire` (Sep 16, 12:13) together with session 5.

## Session 5: OpenTelemetry (Sep 16, 10:13–11:34)

**Prompt**

1. 10:13
   > add opentelemetry

**Outcome.** Two words drove eighty minutes of autonomous work: `Telemetry/TelemetryExtensions.cs` and
`TelemetryOptions.cs` wiring one pipeline for traces, metrics and logs across ASP.NET Core, HttpClient,
SqlClient, the runtime, EF Core and the chat client, with health probes and static assets excluded and a sampler
that drops plumbing spans. Verified end to end against a local Aspire Dashboard through a new `http-otel` launch
profile, with SQL text confirmed not to leak. 108 tests passing. The architecture document was regenerated.

**Commit.** `428ba54 telemetry and aspire` (Sep 16, 12:13).

## Session 6: Is Aspire useful? (Sep 16, 10:18)

Opened as a side question while session 5 was running.

> is aspire useful for this app

**Outcome.** Advisory only: marginal for this app as it stands. Aspire would replace Compose with an AppHost,
add a dashboard and bundle telemetry, health checks and HttpClient resilience as service defaults; only that
last point was relevant to the logging branch in progress. No code changed.

## Session 7: Port in use and missing telemetry (Sep 16, 14:52–15:02)

**Prompts**

1. 14:52
   > fix this: `[pasted dotnet run output: Serilog startup JSON followed by "Failed to bind to address http://127.0.0.1:5098: address already in use" and the stack trace]`
2. 14:54 the same paste again
3. 14:57
   > how do kill this process: Failed to bind to address http://127.0.0.1:5098
4. 15:01
   > i don't see telemetry in http://localhost:18888/structuredlogs

**Outcome.** Two stale app processes were holding the port and were killed; the author got a reusable
PowerShell one-liner for next time. The telemetry question had a configuration cause: plain `dotnet run` uses the
`http` launch profile, which sets no OTLP endpoint, so no exporter is registered. Use `http-otel`.

## Session 8: Microsoft Agent Framework? (Sep 16, 17:31)

> is there a use for Microsoft Agent Framework

**Outcome.** Advisory only: not for the current feature set, since both AI features are single-shot calls with
no tools or memory. It builds on the same `Microsoft.Extensions.AI` abstractions the app already uses, so adopting
it later would be cheap if the AI slice grows toward tool calling or multi-step agents.

## Session 9: Database in the repo, quickstart, request logging (Sep 17, 11:28–16:25)

**Goal.** Make the repository self-contained for a public audience, then finish the logging branch.

**Prompts**

1. 11:28
   > Add the database to the repo
2. 11:31 `[answered: what to commit = "Schema + synthetic demo data"]`
3. 12:18
   > Update the README.MD with a quickstart to run the app locally with Docker and Visual Studio Code.  Include pre-requirements
4. 12:08 `[skill: update-architecture]` invoked by Claude after the database change
5. 15:57
   > http://localhost:8080/invoices is running from the container but I don't see any telemetry in http://localhost:18888/structuredlogs
6. 16:09
   > enable request logging so I can see per-request logs

**Decisions.** Commit the schema and synthetic seed data as SQL rather than any copy of real data; keep the
backup import as an opt-in override. Request logging was implemented as `ILogger` records rather than Serilog's
request-logging middleware, because Serilog is registered as a logger provider here.

**Outcome.** `db/001_schema.sql`, `db/002_seed_demo_data.sql` and `db/README.md`, with `db-init` running the
scripts by default through a new `SqlScriptRunner.cs` and importing a backup only when `BACPAC_PATH` is set via
`docker-compose.bacpac.yml`. A README quickstart for Docker and VS Code with prerequisites. One log record per HTTP
request at Information, Warning or Error by status, carrying trace and span ids so the dashboard links the record
to its trace. An inaccurate claim that OTLP logs were "forwarded by Serilog" was corrected in the architecture
document and a code comment.

**Commits and PRs.** `3ac575a Add the database to the repo as SQL` (12:15), `ce2b7e8 Add a Docker + VS Code
quickstart to the README` (15:13), PR #1 "Logging" merged 15:21, PR #2 "Change label color" merged 15:36,
`81b7e2f Log one record per HTTP request` (16:25).

## Session 10: How am I connected to GitHub? (Sep 17, 16:36)

> how am i connected to github

**Outcome.** Read-only diagnostic: three independent HTTPS paths (Git Credential Manager for `git`, the `gh`
CLI's own OAuth token, and the GitHub MCP connector), no SSH keys, nothing stored in the repository.

## Session 11: This document (Sep 17, 16:59)

Same transcript as sessions 2 and 4.

> Create a markdown file summarizing all the prompts and sessions used for this project

`[answered: location = "docs/SESSIONS.md"; detail = "Verbatim prompts + outcome"]`

Claude ran in plan mode, used three read-only agents to extract the prompts from the ten transcripts, mapped them
to `git log` and the pull requests, and wrote this file.

## Work not captured in transcripts

Two pieces of work have no Claude Code transcript on the author's machine, so their prompts are not recorded here:

- **Serilog logging**, commit `fa927eb serilog` (Sep 16, 15:05): `Logging/AppLoggingOptions.cs`,
  `Logging/LoggingExtensions.cs`, `Logging/CorrelatedTraceTelemetryConverter.cs`, `AppLoggingTests.cs` and the
  README "Logging (Serilog)" section.
- **The formal executive overview deck** in `docs/presentation/` (pptx, PDF, storyboard, speaker notes and
  presenter guide), committed with the exec briefing in `d31b90a presentation` (Sep 17, 11:09).

## Patterns that worked

- **Plan mode with a clarification dialog before anything large.** Sessions 1, 2, 9 and 11 each began with a plan
  and three or four multiple-choice questions, so scope was fixed before code was written.
- **Short prompts, long runs.** "add opentelemetry", "implement this plan" and "Add the database to the repo" each
  produced hours of verified work. The detail lived in the codebase and the reference repo, not in the prompt.
- **Pointing at a reference implementation.** Naming customer-insights in sessions 2 and 4 gave Claude a concrete
  pattern to emulate and a way to settle tooling debates ("isn't az cli the better way?").
- **Pushing back on tooling choices.** "use js" and "let's do this without wsl" redirected work in one line each;
  Claude adjusted rather than defending its first choice.
- **Asking for a skill when a process will repeat.** The architecture document is regenerated by a repo skill
  instead of by re-explaining the steps.
- **Side sessions for yes/no questions.** Aspire, Agent Framework and the GitHub connection were each a one-minute
  session that left the main thread untouched.
- **Pasting the error.** Sessions 1 and 7 pasted the failing output as the whole prompt, which was enough.
