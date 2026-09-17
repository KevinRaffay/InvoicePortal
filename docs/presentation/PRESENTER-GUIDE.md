# Presenter guide — Invoice Portal Admin

## Purpose and evidence status

- Audience: IT executives; explain the system, user experience, data foundation and AI boundaries—not an investment or deployment approval request.
- Format: 16 main slides + four untimed appendices; exactly **30:00**, including a five-minute demonstration and five-minute discussion.
- **No live demo is verified, no display data is approved, and human rehearsal is not completed.** Runtime provider and cloud deployment remain unverified.
- Source-level integration is known; production controls are proposed acceptance work. Do not claim measured performance, accuracy, savings, certification or successful runtime execution.
- This guide authorizes no application edits, database changes, imports, container restarts or deployments.
- [storyboard.json](storyboard.json) contains the narrative: titles, takeaways, cards, layout choices, owners, seconds, full notes and source references. [build.mjs](build.mjs) contains diagram labels, synthetic wireframe examples and display-title overrides.
- Follow the storyboard where the earlier session plan differs: slides 3 and 17–19 use synthetic wireframes, not planned screenshots.

## Owners and exact running order

**Primary** is the user's primary presenter; **Database** is the database colleague. Use role labels until presenters are assigned.
Primary owns the added production-readiness slides **13–15, exactly 5:00**; Database owns **7–10, uninterrupted 8:00**.

| Slide | Clock | Duration | Owner | Topic |
|---|---|---|---|---|
| 1 | 00:00–00:30 | 0:30 | Primary | Invoice operations and meeting purpose |
| 2 | 00:30–02:00 | 1:30 | Primary | How the system fits together |
| 3 | 02:00–03:00 | 1:00 | Primary | Illustrative invoice workspace |
| 4 | 03:00–04:00 | 1:00 | Primary | AI Insights reporting flow |
| 5 | 04:00–05:00 | 1:00 | Primary | Document answers and sources |
| 6 | 05:00–10:00 | 5:00 | Primary | Read-only find, ask, verify walkthrough |
| 7 | 10:00–12:00 | 2:00 | Database | Business data and relationships |
| 8 | 12:00–14:00 | 2:00 | Database | CRUD, lookups and SQL-backed UI |
| 9 | 14:00–16:00 | 2:00 | Database | Integrity, lifecycle and environments |
| 10 | 16:00–18:00 | 2:00 | Database | AI reporting database controls |
| 11 | 18:00–19:30 | 1:30 | Primary | Hosting and operational visibility |
| 12 | 19:30–20:00 | 0:30 | Primary | Capability recap and transition |
| 13 | 20:00–21:30 | 1:30 | Primary | Azure OpenAI production target |
| 14 | 21:30–23:30 | 2:00 | Primary | Reporting and document data controls |
| 15 | 23:30–25:00 | 1:30 | Primary | Evidence gates and operating ownership |
| 16 | 25:00–30:00 | 5:00 | Both | Discussion and close |
| 17–20 | Within an existing slot only | 0:00 added | Primary / Both | Illustrative fallbacks / technical references |

Arithmetic: original Primary slides 7:00 + Database 8:00 + added Primary slides 5:00 + demo 5:00 + discussion 5:00 = **30:00**.
Use a shared elapsed timer. Shorten commentary, not the colleague's block or discussion; appendices never extend the meeting.

### Milestones and handoffs

- **05:00:** Primary starts slide 6 and the five-minute demo timer; use illustrations immediately if preflight is incomplete.
- **10:00:** “You have seen the experience; our database colleague will explain the data foundation and controls behind it.” Yield the floor.
- **18:00:** Database: “That is the data foundation and its current control boundary. I will hand back to the primary presenter for hosting and the production-readiness decisions.”
- **20:00:** Primary: “The integration exists; what separates this demonstration from production?” Start slides 13–15; retain their full five minutes.
- **25:00:** Open discussion. Primary routes UI, AI and hosting questions; Database handles relationships, integrity, SQL scope and schema ownership.
- **30:00:** Close with unresolved decisions and role owners—not delivery commitments or permission to deploy.

## Demo preflight — approval required before showing runtime

- Obtain data-owner approval for dashboard counts, identities, invoice fields, currency aggregates, generated SQL and document excerpts. Counts alone are not automatically public.
- Approve the audience, screen-sharing scope and any recording. Hide secrets, endpoint details, emails, submitter information and unrelated windows.
- Bacpac metadata sanitation is **not business-data anonymization**. Seeded fictional documents can still contain customer names loaded from SQL.
- Verify safe access to the existing environment without restarting it. Pre-open approved screens and vet the record, document question and excerpt.
- Verify and disclose the **active provider shown on AI Insights**; configuration defaults do not establish runtime state. Never silently switch providers.
- If Mock: “This is deterministic offline intent matching, not a live generative-model demonstration.” Do not imply Mock removes SQL-data exposure risks.
- Before remote model calls, obtain explicit consent/authorization from the responsible data owner under organizational policy for the inputs and provider destination.
- Explain inputs: reporting sends schema and question; document Q&A sends question and selected document content/metadata. Query rows currently return to the UI, not a second model summarization call.
- Review all logging destinations before entering sensitive text: the mock logs a truncated reporting prompt at Information level; telemetry switches are not universal redaction.
- Without approved data, input consent, safe access or a verified provider, use the synthetic appendix walkthrough and mark the live-demo prerequisite unmet.

## Five-minute read-only demonstration script — slide 6

“Read-only” describes presenter actions, **not database-enforced read-only permissions**. No save, add, delete, restore, import, deployment or mutation-rejection experiment is permitted.

| Demo clock | Meeting clock | Action and spoken cue |
|---|---|---|
| 0:00–0:45 | 05:00–05:45 | Orient on the approved dashboard and invoice workspace. Identify the visible AI provider before asking anything; disclose Mock explicitly if active. |
| 0:45–2:00 | 05:45–07:00 | Filter and sort the invoice grid; open only the vetted record. Show its tabs without sensitive submitter details. “We are inspecting the editing workflow, not making a business change.” Close without saving. |
| 2:00–3:30 | 07:00–08:30 | In AI Insights, select exactly **Count invoices per currency** rather than the page's initial default question. Run only after approval; inspect currency groups and briefly expand generated SQL. |
| 3:30–4:40 | 08:30–09:40 | Open document Q&A with its pre-vetted suggested question. “These are seeded fictional documents.” Read a short answer, identify one recognized citation and expand its excerpt to compare support. |
| 4:40–5:00 | 09:40–10:00 | Recap “find, ask, verify” without claiming success for any skipped step. Deliver the Database handoff and yield at 10:00. |

- The currency mock returns **InvoiceCount and TotalAmount**: currency totals need approval too. Never sum different currencies into a grand total; another provider may generate different columns.
- Reporting rows are proposed-query results to inspect, not an authoritative financial conclusion. Defer SQL guard and permissions detail to Database.
- A recognized citation is a review aid, not proof of correctness, freshness or access permission. Customer relevance is not authorization.
- If any UI, query or model takes **more than 15 seconds**, stop retrying and switch to appendices 17–19 for the remaining slot. Do not troubleshoot infrastructure on stage.
- Say: “These are illustrative synthetic wireframes, not screenshots of a successful run.” Do not imply a stalled request succeeded; do not restart the timer.

## Appendix fallback and evidence labels

- **17 — Invoice workspace:** native editable panels, table cells and tab labels with synthetic placeholders. Trace filter → inspect → close; no record loading or saving is asserted.
- **18 — AI reporting:** native editable question, SQL panel and mock-shaped result grid. Placeholder counts and amounts are invented; no query execution or SQL accuracy is evidenced.
- **19 — Document answer:** native editable invented question, answer, citation badge and excerpt. The synthetic source is not a repository document or retrieved model output.
- **17–19 are explicitly synthetic, native editable wireframes—not screenshots, runtime evidence, approved real data or a completed live demo.** Do not present them as captured results.
- **20 — Technical sources and limitations:** Both presenters may consult it within demo/discussion time; Primary owns application/AI/platform references, Database owns schema/SQL/lifecycle references.
- If all five demo minutes are illustrative, announce that substitution at the start and retain “live rehearsal and data approval outstanding” in the follow-up.

## Production-readiness decisions — Primary, slides 13–15

Retain the implemented `IChatClient` / `IDocumentRetriever` seams and Azure OpenAI client/identity wiring; no rewrite or agent-platform migration is implied.
Label the production architecture **TARGET / REQUIRED**, not deployed. Existing templates and integration code are not an operating-service assessment.

| Decision owners | Required decision / evidence before acceptance |
|---|---|
| Security + Platform + data owners | Approved users, mandatory authentication and application roles; model/version, deployment type, processing location, capacity and policy-appropriate network/egress controls; consent and logging/privacy review. |
| DBA + App | Separate least-privilege AI read identity; curated reporting views/object grants; enforced user/customer/column scope and matching schema-prompt exposure. Guard, parameters and rollback remain defense in depth. |
| Data owners + App | Approved document sources; ACL-aware ingestion/retrieval, provenance, freshness and deletion. Select a durable retrieval index; a managed search product is an option, not a prerequisite. |
| App / AI + business data owners + Security | Approved-data evaluations for SQL/business accuracy, grounding, citations, unauthorized access, leakage, adversarial inputs, content-safety behavior and refusals; agreed human-review practice. |
| Operations + Platform + Finance; DBA for SQL recovery | Availability and recovery objectives, monitoring/runbooks, capacity and spending controls; demonstrated recovery, rollback and authenticated smoke tests before rollout. No schedule or cost estimate is asserted. |

**Critical path:** governance and data-boundary decisions → SQL identity isolation and document preparation in parallel → both relevant data paths plus an evaluated Azure OpenAI deployment → limited pilot → operational evidence and owner signoff → broader rollout.

Go-live criteria to agree and evidence, not claims of completion:
- Mandatory user authorization and tested data boundaries; current conditional Easy Auth and shared CRUD/query identity are insufficient.
- Evaluated outputs and human review, with approved model processing conditions; do not infer processing location from resource region or assume model availability/quota.
- Authenticated-user/concurrency and token limits beyond circuit throttling; monitor throttling, latency and failures, with bounded backoff and graceful failure. Budget alerts are not hard spending caps.
- Dependency readiness distinct from liveness; authenticated business smoke tests, scans, evaluation gates and deployment rollback rehearsal. A login redirect is not a successful business transaction.
- Reconnect and rolling-update evidence for process-local Blazor state; persisted Data Protection keys do not preserve circuits. Establish objectives before choosing replica/state strategy.
- DBA-owned SQL backups and demonstrated restore against approved recovery objectives; this is future acceptance evidence, **not an instruction to restore during the demo**.

## Build, notes, source map and optional rendering

- Deliverables: [editable PowerPoint](Invoice-Portal-Executive-Overview.pptx), [PDF handout](Invoice-Portal-Executive-Overview.pdf), and [complete speaker notes](SPEAKER-NOTES.md).
- From the docs directory, run `npm ci`, then `npm run build:presentation`, then `npm run check:presentation`. Only presentation outputs are built; no application, database or cloud operations are performed.
- Regenerate from the storyboard and generator rather than editing generated notes as a second source. The generator also writes an ignored layout manifest used by validation.
- The PPTX contains per-slide speaker notes with narration, timing, owner, caveats and transitions, plus the source map. Appendix 20 supplies consolidated technical context. The PDF contains slides only; use the separate notes document for narration.
- In desktop PowerPoint, use Presenter View for notes and elapsed time, or Normal view's Notes pane to review each slide. Share the slide-show window, not the private notes display.
- `check:presentation` validates the PPTX package, slide/timing structure, source paths, notes, editable text and slide bounds. These checks cannot establish runtime success, data approval or human delivery quality.
- Optional: `npm run render:presentation` uses installed desktop PowerPoint via PowerShell to export PDF/PNGs. It requires desktop access; do not assume it works in a headless session.
- Rendering also produces an ignored contact sheet and a validation report bound to the PPTX SHA-256. Rerun rendering after rebuilding; an older PDF or report does not validate a newer deck.

### Artifact validation completed — 2026-09-16

- Build and package validation passed: 20 slides, 16:9, 20 notes sections, source references, native editable text, bounds and exact timing/ownership.
- Desktop PowerPoint successfully opened the deck and exported the PDF plus 20 slide images; measured text-overflow checks passed.
- All slides were reviewed on the rendered contact sheet; the production target and data-controls slides were also inspected at full resolution.
- UI visuals remain labeled synthetic wireframes, not screenshots. No live demonstration, data approval, application integration test or cloud deployment validation is implied by these artifact checks.

## Human rehearsal and delivery checklist — not done

- [ ] Primary and Database review their notes, terminology, source references and implemented-versus-proposed labels.
- [ ] Data owner approves every displayed identity, aggregate and excerpt; remote-input consent and logging review are recorded.
- [ ] Active provider is verified, disclosed and rehearsed without changing providers or data.
- [ ] Safe runtime walkthrough succeeds within five minutes, or illustrative substitution and unmet prerequisites are explicitly recorded.
- [ ] Both presenters rehearse 10:00 / 18:00 handoffs, the Primary-owned 20:00–25:00 section, and the exact 30:00 close.
- [ ] Open the delivered deck on the presenting machine and confirm fonts, native objects, Presenter View and projection readability; artifact checks above do not replace venue rehearsal.
- [ ] Rehearse the >15-second fallback and Presenter View sharing; preserve the database block and five-minute discussion.
- [ ] Discussion uses 25:00–26:30 operational fit, 26:30–28:00 governance, 28:00–29:30 readiness evidence, and 29:30–30:00 owner/action recap.
- [ ] Record unresolved production decisions, evidence owners and rehearsal gaps without inventing budgets, dates, deployment status or approval.