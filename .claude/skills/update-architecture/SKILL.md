---
name: update-architecture
description: Keep the Invoice Portal Admin architecture document current and regenerate its rendered HTML and PDF. Use this whenever the user asks to update, refresh, regenerate, review or check the architecture document, ARCHITECTURE.md, docs/ARCHITECTURE.html, docs/ARCHITECTURE.pdf, the data-flow or integration diagrams, or asks "is the architecture doc still accurate". Also use it after a change lands that the document describes, even if the user does not say "architecture" - for example a new AI provider or IChatClient registration, a new service or DI registration in Program.cs, telemetry or health-check wiring, a new table in the EF scaffold, a change to docker-compose.yml, appsettings or infra/, or a new test project. Do not use it for README-only edits or for writing code.
---

# Update the architecture document

Three files describe this system's architecture, and they must stay in step:

| File                        | Role                                                                                  |
|-----------------------------|---------------------------------------------------------------------------------------|
| `ARCHITECTURE.md`           | Source of record. Hand-written Markdown with Mermaid diagrams. Edit this one.         |
| `docs/ARCHITECTURE.html`    | Generated. Never edit by hand.                                                        |
| `docs/ARCHITECTURE.pdf`     | Generated from the HTML by headless Chromium. Never edit by hand.                     |

The generator is `docs/build.js` (Node 18+, `markdown-it` for Markdown, `puppeteer-core` for the PDF).
The HTML exists because most Markdown previewers do not draw Mermaid. The PDF exists for people who
receive the document rather than open the repo.

## Workflow

### 1. Find out what changed

Do not re-derive the whole document from scratch. Work out what moved since the doc was last touched:

```bash
git log -1 --format=%H -- ARCHITECTURE.md
git diff --stat <that-hash>..HEAD -- InvoicePortal.Admin InvoicePortal.DbInit InvoicePortal.Admin.Tests docker-compose.yml infra azure.yaml
```

If the doc has never been committed, or the user names the change, start from their description instead.
Then read only the files the diff touches. This table says which parts of the document each area feeds:

| Source area                                      | Sections to check                                  |
|--------------------------------------------------|----------------------------------------------------|
| `docker-compose.yml`, Dockerfiles, `.env`        | 1 System context, 8 Configuration                  |
| `InvoicePortal.Admin/Program.cs`                 | 3.2 DI and lifetimes                               |
| `Ai/AiServiceCollectionExtensions.cs`, `Ai/AiOptions.cs` | 3.2 DI, 7.1 seams, 7.2 providers, 7.5 configuration, 7.6 adding a provider, 9 security |
| `Ai/Chat/*`, `Ai/Query/*`, `Ai/Documents/*`      | 5.5 and 5.6 flows, 7.3 mock behaviour, 7.4 prompt contracts |
| `Telemetry/*`                                    | 3.x telemetry subsection, 8 Configuration, 2 packages |
| `Services/CrudService.cs`, `LookupService.cs`    | 5.2 to 5.4 flows, 6 database integration            |
| `Data/InvoicePortalDbContext.cs`, `Data/Entities/*` | 4 Data model (table list, soft delete, audit)   |
| `Components/Shared/*`, `Components/Pages/*`      | 3.3 shared CRUD, entry points named in 5.5 and 5.6 |
| `InvoicePortal.DbInit/*`                         | 5.1 provisioning, 6 database integration           |
| `azure.yaml`, `infra/*`, `.github/workflows/*`  | 1 System context, 8 Configuration (Azure hosting), 9 security, 11 limitations |
| `InvoicePortal.Admin.Tests/*`                    | 10 Testing                                         |
| `*.csproj`                                       | 2 Solution structure (package table)               |

Another session or teammate may be editing `ARCHITECTURE.md` at the same time. Re-read the file
immediately before editing and make targeted edits rather than rewriting whole sections, so their
work is not lost.

### 2. Edit ARCHITECTURE.md

Keep the existing eleven numbered sections and their order. Readers and the generated table of
contents depend on them. Add a subsection (`### 7.7 ...`) rather than a new top-level section unless a
genuinely new concern appears.

Write for a reader who knows .NET but has not seen this repo. Prefer a table to a paragraph when the
content is a list of components with properties. Name a file only when the reader needs to open it.

**Never copy secret values into the document.** The repo contains `.env` files with passwords and
keys. Refer to them by variable name only, as the document already does.

The generator uses `markdown-it` in CommonMark mode with tables, plus these conventions it relies on:

- One `#` heading (the title). `##` headings carry a leading `N.` number that becomes the section number
  and anchor. `###` headings are plain.
- A ```` ```mermaid ```` fence becomes a rendered diagram; any other fence is plain code.
- A `>` blockquote becomes a highlighted callout. If it starts with `**Label.**`, that becomes the label.
- Relative links are rewritten to work from `docs/`, so link as if from the repo root.
- Raw HTML is escaped, not rendered. Use Markdown only.

### 3. Mermaid rules

These come from what mermaid.js 11 actually rejects or mangles, not from taste:

- **No semicolons** inside sequence-diagram messages or notes. Mermaid treats `;` as a statement
  separator and the parser fails on the whole diagram. Use commas or "then".
- **No angle brackets or HTML entities in labels** except `<br/>` for a line break. Write generics
  as `CrudService(T)`, not `CrudService<T>` or `&lt;T&gt;`.
- **Quote flowchart node labels** that contain parentheses, colons or slashes: `A["app (port 8080)"]`.
- Keep a sequence diagram to roughly 20 messages and 8 participants. Split a longer flow into two
  diagrams; the PDF prints at Letter width and a taller one becomes unreadable.
- Participants and node ids must be simple identifiers. Put the human-readable name in the alias or label.

### 4. Rebuild HTML and PDF

Run from the `docs/` folder. The first time on a machine, `npm install` first.

```bash
npm run build:pdf
```

This writes both files and prints the diagram and section counts. The PDF step loads the HTML in headless
Edge or Chrome (auto-detected, or `PUPPETEER_EXECUTABLE_PATH`), waits for every diagram to draw and
**fails the build if any diagram has a Mermaid syntax error**, naming the diagram's first line. So a
successful `build:pdf` is also the render check; no separate browser step is needed. It needs network
access for mermaid.js and the fonts.

`npm run check` exits non-zero when the HTML is stale, which is useful before a commit or in CI.

### 5. Look at the PDF once

Open `docs/ARCHITECTURE.pdf` and check the first few pages and one sequence diagram: the table of
contents is on its own page, each numbered section starts on a new page, and no diagram or table is
clipped at the right edge. The Read tool renders PDF pages where poppler is installed; otherwise start
the `docs` entry from `.claude/launch.json` (a dependency-free static server, `docs/serve.cjs`) and open
`/ARCHITECTURE.pdf` in the browser pane. Fix the Markdown and rebuild if something is off. Do not iterate
on the CSS in `build.js` unless the layout itself is wrong.

### 6. Report

Tell the user what changed in the document, section by section, and confirm the HTML and PDF were
rebuilt. If the diff revealed something the document cannot describe truthfully yet (for example a
provider registered in code but not wired in configuration), say so rather than papering over it.
