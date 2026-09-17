# Invoice Portal — complete speaker notes

Generated from [storyboard.json](storyboard.json). Edit the storyboard, then rebuild. Wireframes are illustrative; no live run or data approval is implied.

## 1. Invoice operations, with AI-assisted insight

**Owner:** Primary • **Duration:** 0:30 • **Run:** 0:00–0:30

Primary presenter, 00:00–00:30. Today is a system overview, not an investment approval request. We will connect the administration experience to its database and two AI workflows, then distinguish implemented integration from production readiness. I own the experience, architecture and operating discussion; our database colleague owns an uninterrupted eight-minute data section. We finish with discussion, not a promised ROI or delivery date.

**Source evidence**

- [InvoicePortal.Admin/Program.cs](../../InvoicePortal.Admin/Program.cs)
- [InvoicePortal.Admin/Components/Pages/Home.razor](../../InvoicePortal.Admin/Components/Pages/Home.razor)
- [InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs](../../InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs)

## 2. How the system fits together

**Owner:** Primary • **Duration:** 1:30 • **Run:** 0:30–2:00

Primary presenter, 00:30–02:00. Read the diagram from the browser inward. This is a .NET 10 Blazor Interactive Server application: the browser exchanges interactions over a SignalR circuit while components and service calls run on the server. Radzen supplies the grids and dialogs. The browser is not a direct SQL client.

Services obtain an EF Core context from IDbContextFactory for each operation rather than holding one database context for the whole user session. Show SQL as the operational data store. Add two branches from the service layer: IChatClient supplies model responses, and IDocumentRetriever supplies document context. These are application interfaces, not evidence of a hosted agent platform or enterprise search connector. Keep all boxes and connectors editable. The same application supports local and proposed Azure hosting views, which we will distinguish later. Next, consider what the user sees.

**Source evidence**

- [InvoicePortal.Admin/InvoicePortal.Admin.csproj](../../InvoicePortal.Admin/InvoicePortal.Admin.csproj)
- [InvoicePortal.Admin/Program.cs](../../InvoicePortal.Admin/Program.cs)
- [InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs](../../InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs)
- [InvoicePortal.Admin/Services/CrudService.cs](../../InvoicePortal.Admin/Services/CrudService.cs)

## 3. One workspace for invoice administration — ILLUSTRATIVE

**Owner:** Primary • **Duration:** 1:00 • **Run:** 2:00–3:00

Primary presenter, 02:00–03:00. This is an illustrative editable wireframe, not a screenshot; all displayed record content is synthetic. The real portal offers dashboard counts, invoice filtering, sorting, paging, column selection and tabbed editing. Reference-data pages support related administration. Soft delete and restore exist for applicable entities, but we will not exercise those operations in the demonstration. Payment and SAP-related fields represent stored state, not implemented payment or export integrations. Reserve the detailed navigation for the five-minute walkthrough. This sketch establishes the workspace without displaying customer identities, amounts or submitter information.

**Source evidence**

- [InvoicePortal.Admin/Components/Pages/Home.razor](../../InvoicePortal.Admin/Components/Pages/Home.razor)
- [InvoicePortal.Admin/Components/Pages/Invoices/Invoices.razor](../../InvoicePortal.Admin/Components/Pages/Invoices/Invoices.razor)
- [InvoicePortal.Admin/Components/Pages/Invoices/InvoiceDialog.razor](../../InvoicePortal.Admin/Components/Pages/Invoices/InvoiceDialog.razor)
- [InvoicePortal.Admin/Services/CrudService.cs](../../InvoicePortal.Admin/Services/CrudService.cs)

## 4. AI Insights: a question becomes a report

**Owner:** Primary • **Duration:** 1:00 • **Run:** 3:00–4:00

Primary presenter, 03:00–04:00. AI Insights turns a question into a proposed database query, not an authoritative financial conclusion. The prompt includes the schema description and user question. The response is parsed as structured SQL and parameters, then checked and executed by application code. Rows currently return to the UI; there is no automatic second model call to summarize those results. With a remote provider, schema and prompt content cross the model boundary. Mock uses deterministic intent matching, not live generation. Our database colleague will explain why the guard and rollback are useful but not database-enforced read-only access.

**Source evidence**

- [InvoicePortal.Admin/Ai/Query/QueryGenerationService.cs](../../InvoicePortal.Admin/Ai/Query/QueryGenerationService.cs)
- [InvoicePortal.Admin/Ai/Query/SqlGuard.cs](../../InvoicePortal.Admin/Ai/Query/SqlGuard.cs)
- [InvoicePortal.Admin/Ai/Query/SqlQueryExecutor.cs](../../InvoicePortal.Admin/Ai/Query/SqlQueryExecutor.cs)
- [InvoicePortal.Admin/Components/Pages/Ai/AiQuery.razor](../../InvoicePortal.Admin/Components/Pages/Ai/AiQuery.razor)
- [InvoicePortal.Admin/Ai/Chat/MockChatClient.cs](../../InvoicePortal.Admin/Ai/Chat/MockChatClient.cs)

## 5. Document answers with visible sources

**Owner:** Primary • **Duration:** 1:00 • **Run:** 4:00–5:00

Primary presenter, 04:00–05:00. Document Q&A retrieves seeded fictional content from an in-memory BM25 index, sends selected passages with the question, and displays recognized citations. The document text is synthetic, but customer names may be loaded from SQL, so seeded does not mean universally anonymous. Customer relevance is not authorization. No enterprise document repository connector is implemented. Citations make review possible; they do not prove that an answer faithfully represents a source. Both AI workflows share the provider interface, and source defaults do not establish the running provider. Next, we will find, ask and verify without changing records.

**Source evidence**

- [InvoicePortal.Admin/Ai/Documents/InMemoryDocumentRetriever.cs](../../InvoicePortal.Admin/Ai/Documents/InMemoryDocumentRetriever.cs)
- [InvoicePortal.Admin/Ai/Documents/SeedDocuments.cs](../../InvoicePortal.Admin/Ai/Documents/SeedDocuments.cs)
- [InvoicePortal.Admin/Ai/Documents/DocumentAnswerService.cs](../../InvoicePortal.Admin/Ai/Documents/DocumentAnswerService.cs)
- [InvoicePortal.Admin/Ai/Documents/CitationSelector.cs](../../InvoicePortal.Admin/Ai/Documents/CitationSelector.cs)
- [InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs](../../InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs)

## 6. Live walkthrough: find, ask, verify

**Owner:** Primary • **Duration:** 5:00 • **Run:** 5:00–10:00

Primary presenter, 05:00–10:00. Rehearsal prerequisite, not completed evidence: pre-open approved screens, verify the visible provider, and obtain approval for every identity and aggregate displayed. No live model, database or cloud operation was performed to create this storyboard. If approvals or safe runtime access are unavailable, announce an illustrative walkthrough instead; appendices 17–19 are editable synthetic wireframes, not captured results or proof that the application ran.

Demo 0:00–0:45: Here is the dashboard and the invoice workspace. Before asking anything, I will identify the provider shown on AI Insights. If it is Mock, say explicitly: This is deterministic offline intent matching; the integration runs, but this is not a live generative-model demonstration. Do not silently switch providers or expose endpoint details or secrets.

Demo 0:45–2:00: I will narrow the invoice list with a filter, change the sort, and open a record to show its tabs. We are inspecting the editing workflow, not making a business change. Close without saving. Do not add, delete or restore records, and do not show sensitive submitter fields.

Demo 2:00–3:30: Select the rehearsed example, Count invoices per currency, rather than using the page's initial default question. We can inspect the resulting currency groups and expand generated SQL if useful. The mock catalog also returns currency-level amounts, so aggregate data approval is still necessary. Do not combine currencies into a grand total. State that rows return to the grid, not a model summarization step. Defer SQL safety detail to the database colleague.

Demo 3:30–4:40: Open document Q&A using its suggested question only after vetting the customer name. These are seeded fictional documents. Read a short answer, point to one recognized citation, and expand its excerpt. The question and selected content reach the configured model; a citation is a review aid, not a correctness guarantee.

If any screen or model stalls for roughly fifteen seconds, stop retrying and use appendices 17–19 within the same five-minute slot. Say: These are illustrative synthetic wireframes, not screenshots of a successful run. Do not troubleshoot cloud services or execute mutation-rejection demonstrations in front of the audience.

Demo 4:40–5:00: We have followed find, ask and verify without changing data. Exact handoff: You have seen the experience; our database colleague will explain the data foundation and controls behind it. Yield the floor at 10:00 and preserve the uninterrupted eight-minute database block.

**Source evidence**

- [InvoicePortal.Admin/Components/Pages/Home.razor](../../InvoicePortal.Admin/Components/Pages/Home.razor)
- [InvoicePortal.Admin/Components/Pages/Invoices/Invoices.razor](../../InvoicePortal.Admin/Components/Pages/Invoices/Invoices.razor)
- [InvoicePortal.Admin/Components/Pages/Invoices/InvoiceDialog.razor](../../InvoicePortal.Admin/Components/Pages/Invoices/InvoiceDialog.razor)
- [InvoicePortal.Admin/Components/Pages/Ai/AiQuery.razor](../../InvoicePortal.Admin/Components/Pages/Ai/AiQuery.razor)
- [InvoicePortal.Admin/Ai/Chat/SqlIntentCatalog.cs](../../InvoicePortal.Admin/Ai/Chat/SqlIntentCatalog.cs)
- [InvoicePortal.Admin/Components/Pages/Ai/AskDocumentsDialog.razor](../../InvoicePortal.Admin/Components/Pages/Ai/AskDocumentsDialog.razor)
- [InvoicePortal.Admin/Ai/Documents/InMemoryDocumentRetriever.cs](../../InvoicePortal.Admin/Ai/Documents/InMemoryDocumentRetriever.cs)

## 7. The business data behind the portal

**Owner:** Database • **Duration:** 2:00 • **Run:** 10:00–12:00

Database presenter, 10:00–12:00. I will own the next eight minutes, starting with the business meaning rather than a table-by-table tour. Place Invoice at the center of this editable domain diagram. It holds the amount, currency, dates and lifecycle fields, and links the transaction to bottler, payer, sales center and program context. Surround those relationships with reference domains such as currency, country, company and brand segment category. This is a simplified relationship view, not an exhaustive schema or a claim that every reference table joins directly to Invoice.

The distinction matters when someone asks a reporting question. Which party is being grouped? Which status definition is intended? Are amounts grouped within one currency? A technically valid query can still answer the wrong business question. Payment flags and SAP-related fields describe stored state; their presence does not establish payment processing or an export connector. The model was scaffolded from an existing database, so the portal is a consumer of that schema, not its migration owner. Next I will connect these relationships to the grids and edit dialogs.

**Source evidence**

- [InvoicePortal.Admin/Data/InvoicePortalDbContext.cs](../../InvoicePortal.Admin/Data/InvoicePortalDbContext.cs)
- [InvoicePortal.Admin/Data/Entities/Invoice.cs](../../InvoicePortal.Admin/Data/Entities/Invoice.cs)
- [InvoicePortal.Admin/Data/Entities/Bottler.cs](../../InvoicePortal.Admin/Data/Entities/Bottler.cs)
- [InvoicePortal.Admin/Data/Entities/Payer.cs](../../InvoicePortal.Admin/Data/Entities/Payer.cs)
- [InvoicePortal.Admin/Data/Entities/SalesCenter.cs](../../InvoicePortal.Admin/Data/Entities/SalesCenter.cs)
- [InvoicePortal.Admin/Data/Entities/Program.cs](../../InvoicePortal.Admin/Data/Entities/Program.cs)

## 8. How database functionality reaches users

**Owner:** Database • **Duration:** 2:00 • **Run:** 12:00–14:00

Database presenter, 12:00–14:00. A grid interaction becomes a service request containing filtering, ordering and paging choices. CrudService builds an EF query, obtains the matching count, and reads the requested page from SQL. Normal grid reads use no-tracking queries. This is server-side invoice paging, not a claim that all invoices are downloaded to the browser. By contrast, the AI report grid works over its already bounded result collection.

Each operation creates and disposes its own context through the registered factory, which suits a long-lived Blazor circuit. The registration is a regular context factory; do not describe it as pooled merely because a service comment says so. LookupService projects identifiers and display labels and caches choices briefly within the circuit. It supports dependent choices, including payer by bottler and sales center by payer, without making those relevance filters an authorization boundary.

When an edit is saved, the service handles a detached entity and leaves computed fields alone. SQL still enforces its constraints, and common database exceptions become user feedback. These reusable services reduce repeated UI plumbing; they are not a replacement for business authorization or a full workflow engine. Next, examine integrity and lifecycle.

**Source evidence**

- [InvoicePortal.Admin/Program.cs](../../InvoicePortal.Admin/Program.cs)
- [InvoicePortal.Admin/Services/CrudService.cs](../../InvoicePortal.Admin/Services/CrudService.cs)
- [InvoicePortal.Admin/Services/LookupService.cs](../../InvoicePortal.Admin/Services/LookupService.cs)
- [InvoicePortal.Admin/Components/Shared/CrudPageBase.cs](../../InvoicePortal.Admin/Components/Shared/CrudPageBase.cs)
- [InvoicePortal.Admin/Components/Pages/Invoices/Invoices.razor](../../InvoicePortal.Admin/Components/Pages/Invoices/Invoices.razor)
- [InvoicePortal.Admin/Components/Pages/Ai/AiQuery.razor](../../InvoicePortal.Admin/Components/Pages/Ai/AiQuery.razor)

## 9. Integrity, lifecycle, and environments

**Owner:** Database • **Duration:** 2:00 • **Run:** 14:00–16:00

Database presenter, 14:00–16:00. The existing schema supplies relationships, required values, indexes and uniqueness rules. SQL computes the administrative and bottler status projections; the application should not overwrite those computed fields. Created and updated timestamps help describe record lifecycle, and CrudService updates the modification timestamp when applicable. They do not provide a complete, immutable history of who changed every field.

Soft-delete behavior depends on the entity implementing the supported abstraction. Normal service lists exclude those deleted records unless requested, and restore clears the flag. Other entity types can be physically removed, subject to relationships. This is not a universal database-wide soft-delete policy, nor does it automatically constrain AI-generated SQL.

Keep the environment diagrams separate. Local DbInit prepares a copy of bacpac metadata for SQL Server import; it removes Azure-specific security metadata and adjusts package compatibility. It does not anonymize business rows. Production-derived names, emails and amounts therefore still require approved handling. The Azure templates point to an existing Azure SQL database rather than creating its business schema. DBA ownership must cover schema changes, access and recovery. We have not run imports, inspected records or verified any cloud deployment for this presentation.

**Source evidence**

- [InvoicePortal.Admin/Data/InvoicePortalDbContext.cs](../../InvoicePortal.Admin/Data/InvoicePortalDbContext.cs)
- [InvoicePortal.Admin/Data/Entities/Invoice.cs](../../InvoicePortal.Admin/Data/Entities/Invoice.cs)
- [InvoicePortal.Admin/Services/CrudService.cs](../../InvoicePortal.Admin/Services/CrudService.cs)
- [InvoicePortal.DbInit/Program.cs](../../InvoicePortal.DbInit/Program.cs)
- [InvoicePortal.DbInit/BacpacSanitizer.cs](../../InvoicePortal.DbInit/BacpacSanitizer.cs)
- [docker-compose.yml](../../docker-compose.yml)
- [infra/resources.bicep](../../infra/resources.bicep)

## 10. Database controls for AI-assisted reporting

**Owner:** Database • **Duration:** 2:00 • **Run:** 16:00–18:00

Database presenter, 16:00–18:00. The current path checks the structured response and applies a regex-based SQL guard before execution. The guard requires SELECT or WITH intent, rejects comments and multiple statements, blocks listed keywords, and validates supplied parameter references. Parameters are bound by the executor. These checks reduce risk; they are not a complete SQL parser, object allowlist, or proof that every possible query is safe.

Execution adds command and lock timeouts, a returned-row cap, and a transaction that is always rolled back. The row cap bounds returned data, not all database work. Rate limiting is per Blazor circuit. Neither rollback nor SELECT intent establishes database-enforced read-only access. The executor uses the same context factory as CRUD, and the supplied SQL identity script grants both reader and writer roles.

Production therefore needs a separate AI read identity, preferably object-specific grants over approved reporting views, plus enforced user, customer and column scope. Broad db_datareader membership can still expose too much. Test business correctness as well as denied access. Exact handoff: That is the data foundation and its current control boundary. I will hand back to the primary presenter for hosting and the production-readiness decisions.

**Source evidence**

- [InvoicePortal.Admin/Ai/Query/QueryGenerationService.cs](../../InvoicePortal.Admin/Ai/Query/QueryGenerationService.cs)
- [InvoicePortal.Admin/Ai/Query/SqlGuard.cs](../../InvoicePortal.Admin/Ai/Query/SqlGuard.cs)
- [InvoicePortal.Admin/Ai/Query/SqlQueryExecutor.cs](../../InvoicePortal.Admin/Ai/Query/SqlQueryExecutor.cs)
- [InvoicePortal.Admin/Ai/AiRateLimiter.cs](../../InvoicePortal.Admin/Ai/AiRateLimiter.cs)
- [InvoicePortal.Admin/Program.cs](../../InvoicePortal.Admin/Program.cs)
- [infra/sql/create-app-user.sql](../../infra/sql/create-app-user.sql)

## 11. Hosting and operational visibility

**Owner:** Primary • **Duration:** 1:30 • **Run:** 18:00–19:30

Primary presenter, 18:00–19:30. Local Compose describes SQL Server, import tooling and the app, with optional Aspire and Ollama services. Separately, Azure templates describe Container Apps, an image registry, managed identity, existing Azure SQL, optional Azure OpenAI, Blob-backed Data Protection and Application Insights. These are provisionable definitions, not a verified deployment.

Current logging separates responsibilities: OpenTelemetry owns traces and metrics; Serilog owns console and local rolling files, plus conditional Azure Application Insights and Datadog log sinks. Optional OTLP forwarding needs duplicate-export review. Do not claim every destination is active or prompts are universally redacted.

Easy Auth is conditional and application role enforcement is absent. The template fixes one replica. Persisted Data Protection keys do not preserve in-memory component state across process restarts. The health endpoint is basic liveness, not proof of database or model readiness. These distinctions shape the production work rather than negating the implemented foundation.

**Source evidence**

- [docker-compose.yml](../../docker-compose.yml)
- [infra/resources.bicep](../../infra/resources.bicep)
- [InvoicePortal.Admin/Program.cs](../../InvoicePortal.Admin/Program.cs)
- [InvoicePortal.Admin/Logging/LoggingExtensions.cs](../../InvoicePortal.Admin/Logging/LoggingExtensions.cs)
- [InvoicePortal.Admin/Logging/AppLoggingOptions.cs](../../InvoicePortal.Admin/Logging/AppLoggingOptions.cs)
- [InvoicePortal.Admin/Telemetry/TelemetryExtensions.cs](../../InvoicePortal.Admin/Telemetry/TelemetryExtensions.cs)
- [InvoicePortal.Admin/Telemetry/TelemetryOptions.cs](../../InvoicePortal.Admin/Telemetry/TelemetryOptions.cs)

## 12. What the demonstration establishes

**Owner:** Primary • **Duration:** 0:30 • **Run:** 19:30–20:00

Primary presenter, 19:30–20:00. The source establishes the administration flows and both AI integration paths. A rehearsed demonstration can illustrate them; wireframes alone do not verify a live run. We have not established production accuracy, security certification, measured performance or financial return. The integration exists; what separates this demonstration from production? The next five minutes address that question without proposing a rewrite.

**Source evidence**

- [InvoicePortal.Admin/Services/CrudService.cs](../../InvoicePortal.Admin/Services/CrudService.cs)
- [InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs](../../InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs)
- [InvoicePortal.Admin/Ai/Query/QueryGenerationService.cs](../../InvoicePortal.Admin/Ai/Query/QueryGenerationService.cs)
- [InvoicePortal.Admin/Ai/Documents/DocumentAnswerService.cs](../../InvoicePortal.Admin/Ai/Documents/DocumentAnswerService.cs)

## 13. Azure OpenAI: retain the foundation, harden the boundary

**Owner:** Primary • **Duration:** 1:30 • **Run:** 20:00–21:30

Primary presenter, 20:00–21:30. This is a target, not a deployed architecture. Retain IChatClient and IDocumentRetriever; neither a rewrite nor a Foundry agent migration is required. Draw approved users through mandatory Entra authentication and application roles into the Blazor app. Branch to Azure OpenAI using managed identity, and to distinct CRUD, approved AI reporting and retrieval paths. Separate identities may access one existing SQL database; two databases are not required.

The client and identity wiring exist, but Easy Auth is conditional and app authorization is absent. Disabling model access keys is a foundation, not end-user authorization. Security and Platform must approve model version, deployment type, processing location and capacity against privacy, quality, latency and cost requirements. The current template specifies GlobalStandard and public model access, not a private regional-processing guarantee. Private connectivity and egress controls follow organizational policy. No model availability, quota or deployment has been verified here; guidance references are in appendix 20.

**Source evidence**

- [InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs](../../InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs)
- [InvoicePortal.Admin/Program.cs](../../InvoicePortal.Admin/Program.cs)
- [infra/resources.bicep](../../infra/resources.bicep)
- [infra/sql/create-app-user.sql](../../infra/sql/create-app-user.sql)

## 14. Two AI workflows, two production data controls

**Owner:** Primary • **Duration:** 2:00 • **Run:** 21:30–23:30

Primary presenter, 21:30–23:30. For reporting, the DBA and application team must implement a separate AI read identity and approved reporting views with enforced user, customer and column scope. Prefer approved object grants where db_datareader would be too broad. Limit the schema prompt to that authorized surface. Keep the existing guard, parameter binding and rollback as defense in depth, not authorization.

For document Q&A, data owners and the application team need approved sources, access-control-aware ingestion and retrieval, provenance, freshness and deletion handling. Replace seeded content for real business use. A customer-name relevance boost is not permission. A durable index behind IDocumentRetriever is needed; a managed search service is an option, not a mandatory product choice.

Both paths expose model inputs: schema and questions for reporting; questions and selected document content for Q&A. Current query result rows go to the UI, not a second model summarization call. Telemetry content switches do not redact every log: MockChatClient logs a truncated reporting prompt at Information level. Review all sinks. Evaluate SQL accuracy, unauthorized access, adversarial prompts, groundedness, citation correctness, content-safety behavior and refusals using approved data. Citations and platform filters do not replace access controls or human review. Bacpac metadata sanitation is not anonymization.

**Source evidence**

- [InvoicePortal.Admin/Ai/Query/QueryGenerationService.cs](../../InvoicePortal.Admin/Ai/Query/QueryGenerationService.cs)
- [InvoicePortal.Admin/Ai/Query/SqlQueryExecutor.cs](../../InvoicePortal.Admin/Ai/Query/SqlQueryExecutor.cs)
- [InvoicePortal.Admin/Components/Pages/Ai/AiQuery.razor](../../InvoicePortal.Admin/Components/Pages/Ai/AiQuery.razor)
- [infra/sql/create-app-user.sql](../../infra/sql/create-app-user.sql)
- [InvoicePortal.Admin/Ai/Documents/DocumentAnswerService.cs](../../InvoicePortal.Admin/Ai/Documents/DocumentAnswerService.cs)
- [InvoicePortal.Admin/Ai/Documents/InMemoryDocumentRetriever.cs](../../InvoicePortal.Admin/Ai/Documents/InMemoryDocumentRetriever.cs)
- [InvoicePortal.Admin/Ai/Chat/MockChatClient.cs](../../InvoicePortal.Admin/Ai/Chat/MockChatClient.cs)
- [InvoicePortal.Admin/Logging/LoggingExtensions.cs](../../InvoicePortal.Admin/Logging/LoggingExtensions.cs)
- [InvoicePortal.DbInit/BacpacSanitizer.cs](../../InvoicePortal.DbInit/BacpacSanitizer.cs)

## 15. Go live through evidence, not configuration alone

**Owner:** Primary • **Duration:** 1:30 • **Run:** 23:30–25:00

Primary presenter, 23:30–25:00. Governance decisions come first; identity isolation and document preparation can then proceed in parallel. A limited pilot depends on both relevant data paths and an evaluated Azure OpenAI deployment. Broader rollout needs owner signoff, not just configuration.

Require authenticated-user concurrency and token budgets beyond circuit throttling. Monitor throttling, latency and failures; use bounded backoff, graceful failure and budget alerts, recognizing alerts are not hard spending caps. Separate liveness from dependency readiness; require authenticated smoke tests, scans, evaluation gates and deployment rollback rehearsal. The existing smoke check accepts a login redirect, which does not verify an authenticated business transaction.

Agree availability objectives before changing the single-replica, process-local design. Rehearse reconnects and rolling updates; SignalR does not automatically migrate component state. Assign existing SQL backup and restore ownership and demonstrate approved recovery objectives. No budget, date or SLA is asserted. The Azure OpenAI connection is already implemented; production readiness depends on controlled data access, evaluated outputs, and an owned operating model.

**Source evidence**

- [.github/workflows/ci.yml](../../.github/workflows/ci.yml)
- [.github/workflows/azure-dev.yml](../../.github/workflows/azure-dev.yml)
- [InvoicePortal.Admin/Program.cs](../../InvoicePortal.Admin/Program.cs)
- [InvoicePortal.Admin/Ai/AiRateLimiter.cs](../../InvoicePortal.Admin/Ai/AiRateLimiter.cs)
- [InvoicePortal.Admin/Telemetry/TelemetryExtensions.cs](../../InvoicePortal.Admin/Telemetry/TelemetryExtensions.cs)
- [infra/resources.bicep](../../infra/resources.bicep)

## 16. Discussion

**Owner:** Both • **Duration:** 5:00 • **Run:** 25:00–30:00

Both presenters, 25:00–30:00. Primary facilitates and owns UI, AI integration, hosting and production questions. Database owns business relationships, SQL integrity, reporting access and schema ownership. This is five minutes of audience discussion, not another monologue or permission to deploy.

Discussion 0:00–1:30: Which operational task should a limited pilot prove first, and what would an acceptable answer look like? Separate an administration requirement from a reporting or document requirement. If payment processing or export automation comes up, explain that current fields track status; connectors are not established by this implementation. Do not translate feature interest into a savings claim.

Discussion 1:30–3:00: Who owns the underlying records and documents, who should be allowed to see them, and which outputs require review? Route SQL scope to the database presenter and identity, logging and document access to the primary presenter. Remind the audience that citations help inspection, not truth certification. A displayed synthetic example is not a live production-data result.

Discussion 3:00–4:30: What evidence would Security, Platform, data owners and Operations need before allowing broader use? Discuss authorization tests, evaluated outputs, recovery and spending controls as proposed acceptance conditions. If asked whether the cloud service is deployed, or how much it costs, state that neither deployment state nor a workload budget was verified. Do not improvise model availability, pricing or performance figures.

Discussion 4:30–5:00: Summarize the questions raised, identify role owners for follow-up and distinguish unresolved decisions from commitments. Close at 30:00. Appendices are untimed references used only within an existing demo or discussion slot; they do not extend the meeting. If the audience has no questions, use one operational-fit question and one governance question, then close with the three production gates.

**Source evidence**

- [InvoicePortal.Admin/Data/Entities/Invoice.cs](../../InvoicePortal.Admin/Data/Entities/Invoice.cs)
- [InvoicePortal.Admin/Ai/Documents/CitationSelector.cs](../../InvoicePortal.Admin/Ai/Documents/CitationSelector.cs)
- [infra/sql/create-app-user.sql](../../infra/sql/create-app-user.sql)
- [infra/resources.bicep](../../infra/resources.bicep)
- [.github/workflows/ci.yml](../../.github/workflows/ci.yml)

## 17. Invoice workspace — ILLUSTRATIVE wireframe

**Owner:** Primary • **Duration:** 0:00 • **Run:** Untimed appendix

Primary presenter, untimed appendix; use only inside the existing demonstration slot. Say: This is an illustrative editable wireframe with synthetic placeholders, not a screenshot. It shows where an operator filters the grid, opens the detail dialog and accesses assistance. Draw native editable panels, table cells and tab labels; do not embed an image or simulate live controls. Keep placeholders visibly synthetic and omit customer names, emails, amounts and record identifiers.

Trace find, inspect and close without claiming a record was loaded or saved. The source UI implements filtering, sorting, paging and column selection; the tabs are taken from the actual dialog. Payment and SAP fields are state, not proof of integration. If this substitutes for the live step, explicitly record that runtime demonstration and data approval remain unmet prerequisites. Move to the reporting wireframe without restarting the timer.

**Source evidence**

- [InvoicePortal.Admin/Components/Pages/Home.razor](../../InvoicePortal.Admin/Components/Pages/Home.razor)
- [InvoicePortal.Admin/Components/Pages/Invoices/Invoices.razor](../../InvoicePortal.Admin/Components/Pages/Invoices/Invoices.razor)
- [InvoicePortal.Admin/Components/Pages/Invoices/InvoiceDialog.razor](../../InvoicePortal.Admin/Components/Pages/Invoices/InvoiceDialog.razor)
- [InvoicePortal.Admin/Components/Layout/NavMenu.razor](../../InvoicePortal.Admin/Components/Layout/NavMenu.razor)

## 18. AI reporting — ILLUSTRATIVE wireframe

**Owner:** Primary • **Duration:** 0:00 • **Run:** Untimed appendix

Primary presenter, untimed appendix; consume only the remaining reporting-demo time. Say: This is an illustrative editable wireframe, not a screenshot or query result. Draw the question box, generated-SQL panel and table using native editable shapes. The sample column labels reflect the deterministic mock's currency intent, which includes TotalAmount as well as count. Placeholder cells contain no real amounts or measured counts; a different provider may generate different columns.

Explain the path from schema and question through a proposed query, validation, execution and display. Result rows currently return to the UI, not an automatic model summarization step. Do not claim that a displayed guard proves least-privilege SQL permissions, and do not describe the report as an exported artifact. For a future live rehearsal, replace the page's default prompt with this approved example, identify the active provider honestly, and obtain approval even for currency-level aggregates. No runtime provider, successful execution or SQL accuracy is asserted here.

**Source evidence**

- [InvoicePortal.Admin/Components/Pages/Ai/AiQuery.razor](../../InvoicePortal.Admin/Components/Pages/Ai/AiQuery.razor)
- [InvoicePortal.Admin/Ai/Chat/SqlIntentCatalog.cs](../../InvoicePortal.Admin/Ai/Chat/SqlIntentCatalog.cs)
- [InvoicePortal.Admin/Ai/Query/QueryGenerationService.cs](../../InvoicePortal.Admin/Ai/Query/QueryGenerationService.cs)
- [InvoicePortal.Admin/Ai/Query/SqlQueryExecutor.cs](../../InvoicePortal.Admin/Ai/Query/SqlQueryExecutor.cs)

## 19. Cited document answer — ILLUSTRATIVE wireframe

**Owner:** Primary • **Duration:** 0:00 • **Run:** Untimed appendix

Primary presenter, untimed appendix; use within the document-demo slot. Say: This is an illustrative editable wireframe with invented sample text, not a screenshot, retrieved document or model output. The synthetic question, answer and excerpt exist only to explain the display pattern. Render the answer, S1 badge and expandable-excerpt panel as native editable objects. Do not imply the synthetic source is a file in the repository or a linked enterprise document.

The application retrieves seeded content and passes selected passages to the configured model. Citation selection retains recognized source labels and the dialog lets users inspect excerpts. A recognized label does not verify that the answer is supported, current or authorized. Real runtime seeded text can include names drawn from SQL, so vet the default question and every displayed excerpt before rehearsal. No enterprise ingestion, ACL enforcement or source freshness process is established. Return to the demo handoff at its scheduled time; this appendix is not evidence that the live Q&A succeeded.

**Source evidence**

- [InvoicePortal.Admin/Components/Pages/Ai/AskDocumentsDialog.razor](../../InvoicePortal.Admin/Components/Pages/Ai/AskDocumentsDialog.razor)
- [InvoicePortal.Admin/Ai/Documents/DocumentAnswerService.cs](../../InvoicePortal.Admin/Ai/Documents/DocumentAnswerService.cs)
- [InvoicePortal.Admin/Ai/Documents/CitationSelector.cs](../../InvoicePortal.Admin/Ai/Documents/CitationSelector.cs)
- [InvoicePortal.Admin/Ai/Documents/InMemoryDocumentRetriever.cs](../../InvoicePortal.Admin/Ai/Documents/InMemoryDocumentRetriever.cs)
- [InvoicePortal.Admin/Ai/Documents/SeedDocuments.cs](../../InvoicePortal.Admin/Ai/Documents/SeedDocuments.cs)

## 20. Technical sources and production limitations

**Owner:** Both • **Duration:** 0:00 • **Run:** Untimed appendix

Both presenters, untimed reference appendix. Current source review: 2026-09-16. Primary owns application, AI and platform references; Database owns schema, SQL and lifecycle references. These are source-level observations, not a runtime, cloud, security or performance assessment. No app build, application tests, imports, model calls or deployments were performed for this storyboard. Do not infer a test-pass claim from the existence of tests or CI.

Application + data references: InvoicePortal.Admin/Program.cs registers .NET Blazor services and a regular IDbContextFactory, not a pooled factory despite an outdated CrudService summary comment. InvoicePortal.Admin/Components/Pages/Invoices/Invoices.razor and InvoiceDialog.razor establish grid and tab behavior. InvoicePortal.Admin/Services/CrudService.cs and LookupService.cs establish paging, detached edits, lookup caching and entity-dependent deletion. InvoicePortal.Admin/Data/InvoicePortalDbContext.cs and Data/Entities/Invoice.cs establish modeled state. Stored SAP and payment fields do not implement export or payment integration. Timestamps are not a full audit trail; the scaffold is not EF migration ownership. InvoicePortal.DbInit/BacpacSanitizer.cs modifies metadata in a package copy, not business-data anonymity.

AI + privacy references: InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs registers Mock, Ollama and AzureOpenAI behind IChatClient plus seeded IDocumentRetriever. Query/QueryGenerationService.cs sends schema and user question; Components/Pages/Ai/AiQuery.razor sends returned rows to the grid, with no second model summarization. Query/SqlGuard.cs is regex defense in depth; Query/SqlQueryExecutor.cs shares CRUD's factory and adds timeouts, returned-row limits and rollback. infra/sql/create-app-user.sql grants reader and writer roles to one identity, not a separate AI reader. Production needs authorized views, object grants and user/customer/column enforcement, including schema exposure controls.

Document and logging limits: InvoicePortal.Admin/Ai/Documents/InMemoryDocumentRetriever.cs uses seeded BM25 and a customer-name boost, not authorization. DocumentAnswerService.cs sends selected content and metadata to the model; CitationSelector.cs recognizes supplied labels, not semantic correctness. Production needs approved sources, ACL-aware ingestion/retrieval, ownership, freshness, deletion and provenance. A managed search index is optional; the retriever interface can accommodate alternatives. Chat/MockChatClient.cs logs a truncated reporting prompt at Information level. Logging/LoggingExtensions.cs and Telemetry/TelemetryOptions.cs must be reviewed together: disabling content telemetry is not blanket redaction. Seeded document text can include actual database customer names.

Platform + assurance references: infra/resources.bicep describes conditional Easy Auth, optional Azure OpenAI with local keys disabled, public network access and GlobalStandard deployment, one Container Apps replica, and Blob-backed Data Protection. These are template settings, not observed cloud state. Persisted keys do not persist circuit component state. InvoicePortal.Admin/Program.cs maps basic /healthz without dependency checks. Logging/LoggingExtensions.cs routes Serilog logs; Telemetry/TelemetryExtensions.cs owns traces and metrics and optional OTLP log forwarding. Review duplicate exports and sink destinations. .github/workflows/ci.yml defines build/test, Bicep and container checks; .github/workflows/azure-dev.yml defines OIDC deployment and a smoke check accepting 200 or 302, not an authenticated end-to-end business check.

Production checklist, proposed rather than implemented: Security, Platform and DBA approve users, roles, objects, model/version, processing location, quota and network policy before production data exposure. App/AI and data owners evaluate SQL accuracy, groundedness, citation correctness, adversarial prompts, unauthorized access, leakage, content-safety behavior, refusals and human-review practice. Operations, Platform and Finance agree availability, workload budgets and recovery objectives. Add global authenticated-user/concurrency and token controls; monitor 429s, latency and errors; respect SDK retry behavior with bounded backoff; provide graceful failure and runbooks. Budget alerts are not hard spending caps. Require dependency readiness, authenticated smoke tests, scans, evaluation gates and rollback rehearsal. Validate reconnects and rolling updates; SignalR alone does not migrate Blazor state. Agree SQL backup/restore responsibility and demonstrate approved RTO/RPO. No price, SLA, date, measured savings or acceptance signoff is asserted.

Official guidance rechecked on 2026-09-16, kept in notes rather than workspace-path sources: Microsoft Learn data processing and deployment-location guidance, https://learn.microsoft.com/en-us/azure/foundry/responsible-ai/openai/data-privacy ; Azure OpenAI Entra and managed-identity guidance, https://learn.microsoft.com/en-us/azure/foundry-classic/openai/how-to/managed-identity ; Container Apps authentication and application claims/authorization guidance, https://learn.microsoft.com/en-us/azure/container-apps/authentication . Processing location depends on deployment type; do not equate resource region with guaranteed processing region. Identity authentication does not define per-user data access. Verify current model/version, capacity, terms and organizational policy before a pilot rather than quoting unverified prices or regional availability.

Plan reconciliation: all 16 main slide durations and the four untimed appendices are preserved. Slides 3 and 17–19 intentionally use illustrative editable synthetic wireframes instead of planned sanitized screenshots; they cannot establish live-demo success. Logging narration reflects current Serilog/OpenTelemetry ownership and conditional Datadog routing. Correct misleading code comments about a pooled context factory and restart-surviving circuits in the spoken narrative without modifying source. The currency mock includes TotalAmount, while the query-prompt example shows count only; the illustrative report is explicitly mock-shaped. Runtime provider, approved display data, presenter review and live rehearsal remain unverified.

**Source evidence**

- [InvoicePortal.Admin/Program.cs](../../InvoicePortal.Admin/Program.cs)
- [InvoicePortal.Admin/Components/Pages/Invoices/Invoices.razor](../../InvoicePortal.Admin/Components/Pages/Invoices/Invoices.razor)
- [InvoicePortal.Admin/Components/Pages/Invoices/InvoiceDialog.razor](../../InvoicePortal.Admin/Components/Pages/Invoices/InvoiceDialog.razor)
- [InvoicePortal.Admin/Components/Pages/Ai/AiQuery.razor](../../InvoicePortal.Admin/Components/Pages/Ai/AiQuery.razor)
- [InvoicePortal.Admin/Services/CrudService.cs](../../InvoicePortal.Admin/Services/CrudService.cs)
- [InvoicePortal.Admin/Services/LookupService.cs](../../InvoicePortal.Admin/Services/LookupService.cs)
- [InvoicePortal.Admin/Data/InvoicePortalDbContext.cs](../../InvoicePortal.Admin/Data/InvoicePortalDbContext.cs)
- [InvoicePortal.Admin/Data/Entities/Invoice.cs](../../InvoicePortal.Admin/Data/Entities/Invoice.cs)
- [InvoicePortal.DbInit/BacpacSanitizer.cs](../../InvoicePortal.DbInit/BacpacSanitizer.cs)
- [InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs](../../InvoicePortal.Admin/Ai/AiServiceCollectionExtensions.cs)
- [InvoicePortal.Admin/Ai/Query/QueryGenerationService.cs](../../InvoicePortal.Admin/Ai/Query/QueryGenerationService.cs)
- [InvoicePortal.Admin/Ai/Query/SqlGuard.cs](../../InvoicePortal.Admin/Ai/Query/SqlGuard.cs)
- [InvoicePortal.Admin/Ai/Query/SqlQueryExecutor.cs](../../InvoicePortal.Admin/Ai/Query/SqlQueryExecutor.cs)
- [InvoicePortal.Admin/Ai/Documents/InMemoryDocumentRetriever.cs](../../InvoicePortal.Admin/Ai/Documents/InMemoryDocumentRetriever.cs)
- [InvoicePortal.Admin/Ai/Documents/DocumentAnswerService.cs](../../InvoicePortal.Admin/Ai/Documents/DocumentAnswerService.cs)
- [InvoicePortal.Admin/Ai/Documents/CitationSelector.cs](../../InvoicePortal.Admin/Ai/Documents/CitationSelector.cs)
- [InvoicePortal.Admin/Ai/Chat/MockChatClient.cs](../../InvoicePortal.Admin/Ai/Chat/MockChatClient.cs)
- [InvoicePortal.Admin/Ai/Chat/SqlIntentCatalog.cs](../../InvoicePortal.Admin/Ai/Chat/SqlIntentCatalog.cs)
- [InvoicePortal.Admin/Logging/LoggingExtensions.cs](../../InvoicePortal.Admin/Logging/LoggingExtensions.cs)
- [InvoicePortal.Admin/Telemetry/TelemetryExtensions.cs](../../InvoicePortal.Admin/Telemetry/TelemetryExtensions.cs)
- [InvoicePortal.Admin/Telemetry/TelemetryOptions.cs](../../InvoicePortal.Admin/Telemetry/TelemetryOptions.cs)
- [infra/resources.bicep](../../infra/resources.bicep)
- [infra/sql/create-app-user.sql](../../infra/sql/create-app-user.sql)
- [.github/workflows/ci.yml](../../.github/workflows/ci.yml)
- [.github/workflows/azure-dev.yml](../../.github/workflows/azure-dev.yml)
- [InvoicePortal.Admin.Tests/SqlGuardTests.cs](../../InvoicePortal.Admin.Tests/SqlGuardTests.cs)
- [InvoicePortal.Admin.Tests/QueryGenerationServiceTests.cs](../../InvoicePortal.Admin.Tests/QueryGenerationServiceTests.cs)
- [InvoicePortal.Admin.Tests/InMemoryDocumentRetrieverTests.cs](../../InvoicePortal.Admin.Tests/InMemoryDocumentRetrieverTests.cs)
- [InvoicePortal.Admin.Tests/AppLoggingTests.cs](../../InvoicePortal.Admin.Tests/AppLoggingTests.cs)
