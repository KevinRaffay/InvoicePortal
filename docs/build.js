#!/usr/bin/env node
/**
 * Render ARCHITECTURE.md (repo root) to docs/ARCHITECTURE.html and, with --pdf, docs/ARCHITECTURE.pdf.
 *
 *   npm run build          HTML only
 *   npm run build:pdf      HTML + PDF (needs a Chromium-based browser, see findBrowser)
 *   npm run check          exit 1 if ARCHITECTURE.html is stale (for CI or a pre-commit hook)
 *
 * Markdown is converted with markdown-it. Mermaid fences become <pre class="mermaid"> blocks drawn in the
 * browser by mermaid.js from jsdelivr, so the HTML needs network access the first time it is opened. The PDF
 * step loads the HTML in headless Chromium, waits for every diagram to render, fails if any diagram has a
 * Mermaid syntax error, then prints to Letter with page numbers.
 */

import { existsSync, readFileSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import MarkdownIt from "markdown-it";

const here = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(here, "..");
const SOURCE = path.join(ROOT, "ARCHITECTURE.md");
const HTML_OUT = path.join(here, "ARCHITECTURE.html");
const PDF_OUT = path.join(here, "ARCHITECTURE.pdf");
const MERMAID_VERSION = "11.4.1";

// ------------------------------------------------------------------------------------------------
// Markdown -> HTML
// ------------------------------------------------------------------------------------------------

const slug = (text) => text.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "") || "section";

/** Links in the Markdown are relative to the repo root; the HTML lives one level down in docs/. */
const rewriteHref = (href) => (/^(https?:|#|mailto:|\/)/.test(href) ? href : "../" + href);

function createRenderer(state) {
  const md = new MarkdownIt({ html: false, linkify: false, typographer: false });
  const esc = md.utils.escapeHtml;
  const defaultRender = (tokens, idx, options, env, self) => self.renderToken(tokens, idx, options);

  // Pre-pass: strip the "N." prefix from level-2 headings (kept in token.meta) and mark the title heading.
  md.core.ruler.push("architecture_headings", (mdState) => {
    const tokens = mdState.tokens;
    for (let i = 0; i < tokens.length; i++) {
      const tok = tokens[i];
      if (tok.type !== "heading_open") continue;
      const inline = tokens[i + 1];
      const close = tokens[i + 2];
      if (tok.tag === "h1") {
        state.title = inline.content;
        tok.meta = { skip: true };
        close.meta = { skip: true };
        inline.children = [];
        inline.content = "";
      } else if (tok.tag === "h2") {
        const m = /^(\d+)\.\s+(.*)$/.exec(inline.content);
        const num = m ? m[1] : "";
        const label = m ? m[2] : inline.content;
        if (m && inline.children.length) {
          inline.children[0].content = inline.children[0].content.replace(/^\d+\.\s+/, "");
          inline.content = label;
        }
        tok.meta = { num, label, id: slug(label) };
      }
    }
  });

  const openSection = (id) => {
    const out = (state.sectionOpen ? "</section>\n" : "") + `<section id="${id}">\n`;
    state.sectionOpen = true;
    return out;
  };

  md.renderer.rules.heading_open = (tokens, idx) => {
    const tok = tokens[idx];
    if (tok.meta?.skip) return openSection("summary");
    if (tok.tag === "h2") {
      const { num, label, id } = tok.meta;
      state.toc.push({ num, label, id });
      state.sawH2 = true;
      return openSection(id) + `<h2><span class="num">${esc(num)}</span>`;
    }
    return `<${tok.tag}>`;
  };
  md.renderer.rules.heading_close = (tokens, idx) => (tokens[idx].meta?.skip ? "" : `</${tokens[idx].tag}>\n`);

  md.renderer.rules.paragraph_open = (tokens, idx) => {
    if (tokens[idx].hidden) return "";
    if (!state.sawH2 && !state.ledeDone) {
      state.ledeDone = true;
      return '<p class="lede">';
    }
    return "<p>";
  };
  md.renderer.rules.paragraph_close = (tokens, idx) => (tokens[idx].hidden ? "" : "</p>\n");

  md.renderer.rules.fence = (tokens, idx) => {
    const tok = tokens[idx];
    const info = tok.info.trim().toLowerCase();
    const code = esc(tok.content.replace(/\n$/, ""));
    if (info === "mermaid") {
      state.diagrams += 1;
      return `<figure class="diagram"><pre class="mermaid">\n${code}\n</pre></figure>\n`;
    }
    return `<pre class="code"><code>${code}</code></pre>\n`;
  };

  md.renderer.rules.table_open = () => '<div class="table-wrap"><table>\n';
  md.renderer.rules.table_close = () => "</table></div>\n";
  md.renderer.rules.ordered_list_open = () => '<ol class="steps">\n';
  md.renderer.rules.blockquote_open = () => '<div class="callout">\n';
  md.renderer.rules.blockquote_close = () => "</div>\n";
  md.renderer.rules.hr = () => "";

  md.renderer.rules.link_open = (tokens, idx, options, env, self) => {
    const tok = tokens[idx];
    const href = tok.attrGet("href");
    if (href) tok.attrSet("href", rewriteHref(href));
    return defaultRender(tokens, idx, options, env, self);
  };

  return md;
}

function renderBody(markdown) {
  const state = { title: "Architecture", toc: [], sectionOpen: false, sawH2: false, ledeDone: false, diagrams: 0 };
  const md = createRenderer(state);
  let body = md.render(markdown);
  if (state.sectionOpen) body += "</section>\n";

  // A callout that opens with **Label.** gets that label pulled out above the text.
  body = body.replace(
    /<div class="callout">\n<p><strong>([^<]*?)\.?<\/strong>\s*/g,
    '<div class="callout"><span class="label">$1</span><p>',
  );
  return { body, state };
}

function tocHtml(toc) {
  const items = toc
    .map(({ num, label, id }) => `<li><a href="#${id}"><span>${num}</span>${label}</a></li>`)
    .join("\n      ");
  return `<nav class="toc" aria-label="Contents"><h2>Contents</h2><ol>\n      ${items}\n    </ol></nav>`;
}

// ------------------------------------------------------------------------------------------------
// Page template
// ------------------------------------------------------------------------------------------------

const page = ({ title, toc, body, generated }) => `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="color-scheme" content="light dark">
<!--
  GENERATED FILE. Do not edit by hand.
  Source: ARCHITECTURE.md in the repo root. Rebuild with: npm run build (in docs/), or npm run build:pdf for the PDF too.
  Generated ${generated}. Diagrams are drawn client-side by mermaid.js ${MERMAID_VERSION} from jsdelivr.
-->
<title>${title}</title>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:wght@400;500;600&family=IBM+Plex+Mono:wght@400;500&display=swap">
<style>
  :root {
    --paper: #f5f7fa; --surface: #ffffff; --ink: #1b2430; --ink-soft: #55616f; --line: #d6dce4;
    --accent: #2c5c8a; --accent-soft: #e4edf6; --code-bg: #eaeff5; --warn-bg: #fbf3e3; --warn-line: #d9a33a;
    --sans: "IBM Plex Sans", "Segoe UI", system-ui, -apple-system, sans-serif;
    --mono: "IBM Plex Mono", Consolas, "SFMono-Regular", Menlo, monospace;
  }
  @media (prefers-color-scheme: dark) {
    :root:not([data-theme="light"]) {
      --paper: #11161c; --surface: #171e26; --ink: #e2e7ed; --ink-soft: #9aa6b3; --line: #2b343f;
      --accent: #7fb0e0; --accent-soft: #1d2d3f; --code-bg: #1d252f; --warn-bg: #2a2418; --warn-line: #c8973a;
    }
  }
  :root[data-theme="dark"] {
    --paper: #11161c; --surface: #171e26; --ink: #e2e7ed; --ink-soft: #9aa6b3; --line: #2b343f;
    --accent: #7fb0e0; --accent-soft: #1d2d3f; --code-bg: #1d252f; --warn-bg: #2a2418; --warn-line: #c8973a;
  }
  html, body { margin: 0; }
  img { max-width: 100%; }
  body { background: var(--paper); color: var(--ink); font-family: var(--sans); font-size: 15.5px; line-height: 1.6;
         padding-inline: 20px; padding-block: 0 56px; }
  a { color: var(--accent); text-decoration: none; }
  a:hover, a:focus-visible { text-decoration: underline; }
  :focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
  .page { max-width: 1180px; margin: 0 auto; display: grid; grid-template-columns: 1fr; gap: 32px; }
  @media (min-width: 1080px) {
    .page { grid-template-columns: 220px minmax(0, 1fr); }
    .toc { position: sticky; top: 0; align-self: start; padding-top: 40px; max-height: 100vh; overflow-y: auto; }
  }
  header.masthead { grid-column: 1 / -1; padding-top: 40px; border-bottom: 1px solid var(--line); padding-bottom: 24px; }
  .eyebrow { font-family: var(--mono); font-size: 12px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--accent); margin: 0 0 8px; }
  h1 { font-size: clamp(28px, 4vw, 38px); font-weight: 600; line-height: 1.15; margin: 0 0 12px; text-wrap: balance; letter-spacing: -0.01em; }
  .meta { color: var(--ink-soft); font-size: 14px; display: flex; flex-wrap: wrap; gap: 6px 20px; margin: 0; }
  .meta code { font-size: 13px; }
  .toc { font-size: 13.5px; }
  .toc h2 { font-family: var(--mono); font-size: 11.5px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--ink-soft); margin: 0 0 10px; font-weight: 500; display: block; }
  .toc ol { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 6px; }
  .toc li { margin: 0; }
  .toc li a { display: grid; grid-template-columns: 22px 1fr; gap: 6px; color: var(--ink); line-height: 1.35; }
  .toc li a span { font-family: var(--mono); color: var(--ink-soft); font-size: 12px; padding-top: 2px; }
  @media (max-width: 1079px) {
    .toc ol { flex-direction: row; flex-wrap: wrap; gap: 8px 16px; }
    .toc li a { grid-template-columns: auto 1fr; }
  }
  main { min-width: 0; }
  main > section { padding-block: 28px 8px; }
  main > section + section { border-top: 1px solid var(--line); }
  h2 { font-size: 23px; font-weight: 600; margin: 0 0 14px; display: grid; grid-template-columns: 34px 1fr; gap: 8px; align-items: baseline; text-wrap: balance; }
  h2 .num { font-family: var(--mono); font-size: 15px; color: var(--accent); font-weight: 500; }
  h3 { font-size: 17px; font-weight: 600; margin: 26px 0 8px; }
  p { margin: 0 0 14px; max-width: 76ch; }
  .lede { font-size: 16.5px; max-width: 80ch; }
  ul, ol.steps { margin: 0 0 16px; padding-left: 22px; max-width: 76ch; }
  li { margin-bottom: 6px; }
  strong { font-weight: 600; }
  code { font-family: var(--mono); font-size: 0.88em; background: var(--code-bg); padding: 1px 5px; border-radius: 3px; }
  pre.code { font-family: var(--mono); font-size: 13px; line-height: 1.55; background: var(--code-bg); border: 1px solid var(--line);
             border-radius: 4px; padding: 14px 16px; overflow-x: auto; margin: 0 0 16px; }
  pre.code code { background: none; padding: 0; font-size: inherit; }
  .table-wrap { overflow-x: auto; margin: 0 0 18px; border: 1px solid var(--line); border-radius: 4px; background: var(--surface); }
  table { border-collapse: collapse; width: 100%; font-size: 14px; min-width: 560px; }
  th, td { text-align: left; vertical-align: top; padding: 9px 12px; border-bottom: 1px solid var(--line); }
  th { font-family: var(--mono); font-size: 11.5px; letter-spacing: 0.06em; text-transform: uppercase; color: var(--ink-soft); font-weight: 500; background: var(--paper); }
  tr:last-child td { border-bottom: 0; }
  figure.diagram { margin: 0 0 18px; background: var(--surface); border: 1px solid var(--line); border-radius: 4px; padding: 12px 14px 10px; overflow-x: auto; }
  figure.diagram pre.mermaid { margin: 0; font-family: var(--mono); font-size: 13px; }
  .callout { border-left: 3px solid var(--warn-line); background: var(--warn-bg); padding: 12px 16px; border-radius: 0 4px 4px 0; margin: 0 0 16px; max-width: 80ch; }
  .callout p:last-child { margin-bottom: 0; }
  .callout .label { font-family: var(--mono); font-size: 11.5px; letter-spacing: 0.06em; text-transform: uppercase; color: var(--warn-line); display: block; margin-bottom: 4px; }
  @media (prefers-reduced-motion: no-preference) { html { scroll-behavior: smooth; } }

  /* Print / PDF: single column, one section per page, nothing clipped. */
  @media print {
    body { background: #fff; color: #1b2430; font-size: 10.5pt; line-height: 1.5; padding: 0; }
    .page { display: block; max-width: none; }
    header.masthead { padding-top: 0; }
    .toc { position: static; padding: 16px 0 0; max-height: none; break-after: page; }
    .toc ol { flex-direction: column; gap: 4px; }
    .toc li a { grid-template-columns: 22px 1fr; }
    main > section { border-top: 0; padding-block: 0 8px; }
    main > section + section { break-before: page; }
    h2, h3 { break-after: avoid; }
    h2 { font-size: 18pt; }
    h3 { font-size: 12.5pt; }
    p, ul, ol.steps, .lede, .callout { max-width: none; }
    figure.diagram, .table-wrap, pre.code, .callout, tr { break-inside: avoid; }
    figure.diagram, .table-wrap, pre.code { overflow: visible; }
    figure.diagram svg { max-width: 100% !important; height: auto !important; }
    table { min-width: 0; font-size: 9pt; }
    th, td { padding: 6px 8px; }
    pre.code { font-size: 8.5pt; white-space: pre-wrap; word-break: break-word; }
    a { color: inherit; }
  }
</style>
</head>
<body>
<div class="page">
  <header class="masthead">
    <p class="eyebrow">Architecture</p>
    <h1>${title}</h1>
    <p class="meta">
      <span>Source of record: <code>ARCHITECTURE.md</code> in the repo root</span>
      <span>Generated ${generated} by <code>docs/build.js</code></span>
    </p>
  </header>
  ${toc}
  <main>
${body}  </main>
</div>
<script src="https://cdn.jsdelivr.net/npm/mermaid@${MERMAID_VERSION}/dist/mermaid.min.js"></script>
<script>
  (function () {
    if (!window.mermaid) { return; }
    var dark = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
    mermaid.initialize({
      startOnLoad: true,
      theme: dark ? "dark" : "neutral",
      securityLevel: "loose",
      fontFamily: '"IBM Plex Sans", "Segoe UI", system-ui, sans-serif',
      flowchart: { htmlLabels: true, curve: "basis" },
      sequence: { mirrorActors: false, useMaxWidth: true }
    });
  })();
</script>
</body>
</html>
`;

// ------------------------------------------------------------------------------------------------
// PDF via headless Chromium
// ------------------------------------------------------------------------------------------------

/** Use PUPPETEER_EXECUTABLE_PATH if set, otherwise the first installed Edge or Chrome we can find. */
function findBrowser() {
  const fromEnv = process.env.PUPPETEER_EXECUTABLE_PATH;
  if (fromEnv && existsSync(fromEnv)) return fromEnv;

  const candidates = [
    // Windows
    "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe",
    "C:/Program Files/Microsoft/Edge/Application/msedge.exe",
    "C:/Program Files/Google/Chrome/Application/chrome.exe",
    "C:/Program Files (x86)/Google/Chrome/Application/chrome.exe",
    // macOS
    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
    "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
    // Linux (GitHub Actions ubuntu runners ship google-chrome)
    "/usr/bin/google-chrome",
    "/usr/bin/google-chrome-stable",
    "/usr/bin/microsoft-edge",
    "/usr/bin/chromium-browser",
    "/usr/bin/chromium",
  ];
  const found = candidates.find((p) => existsSync(p));
  if (!found) {
    throw new Error(
      "No Chromium-based browser found for the PDF step. Install Edge or Chrome, or set PUPPETEER_EXECUTABLE_PATH.",
    );
  }
  return found;
}

async function renderPdf(title, expectedDiagrams) {
  const { default: puppeteer } = await import("puppeteer-core");
  const executablePath = findBrowser();
  const browser = await puppeteer.launch({ executablePath, headless: true });
  try {
    const page = await browser.newPage();
    await page.emulateMediaFeatures([{ name: "prefers-color-scheme", value: "light" }]);
    await page.goto(pathToFileURL(HTML_OUT).href, { waitUntil: "networkidle0", timeout: 60_000 });

    // Mermaid draws after load. Wait until every figure holds an SVG, then make sure none is an error placeholder.
    await page.waitForFunction(
      (n) => document.querySelectorAll("figure.diagram svg").length >= n,
      { timeout: 30_000 },
      expectedDiagrams,
    );
    const broken = await page.evaluate(() =>
      [...document.querySelectorAll("figure.diagram")]
        .filter((f) => /Syntax error/i.test(f.textContent || ""))
        .map((f) => (f.querySelector("pre.mermaid")?.textContent || "").trim().split("\n")[0]),
    );
    if (broken.length) {
      throw new Error(`Mermaid syntax error in ${broken.length} diagram(s): ${broken.join("; ")}`);
    }
    await page.evaluate(() => document.fonts.ready);

    await page.emulateMediaType("print");
    await page.pdf({
      path: PDF_OUT,
      format: "Letter",
      printBackground: true,
      displayHeaderFooter: true,
      headerTemplate: "<span></span>",
      footerTemplate:
        `<div style="font-family: Consolas, Menlo, monospace; font-size: 8px; color: #6b7480; width: 100%; padding: 0 0.65in; display: flex; justify-content: space-between;">` +
        `<span>${title}</span><span><span class="pageNumber"></span> / <span class="totalPages"></span></span></div>`,
      margin: { top: "0.6in", right: "0.65in", bottom: "0.8in", left: "0.65in" },
    });
    return executablePath;
  } finally {
    await browser.close();
  }
}

// ------------------------------------------------------------------------------------------------
// Entry point
// ------------------------------------------------------------------------------------------------

const rel = (p) => path.relative(ROOT, p).replaceAll("\\", "/");
const stripDate = (s) => s.replace(/Generated \d{4}-\d{2}-\d{2}/g, "Generated");

async function main(argv) {
  const wantPdf = argv.includes("--pdf");
  const checkOnly = argv.includes("--check");

  const markdown = readFileSync(SOURCE, "utf8");
  const { body, state } = renderBody(markdown);
  const html = page({
    title: state.title,
    toc: tocHtml(state.toc),
    body,
    generated: new Date().toISOString().slice(0, 10),
  });

  if (checkOnly) {
    if (!existsSync(HTML_OUT)) {
      console.error(`${rel(HTML_OUT)} is missing. Run: npm run build (in docs/)`);
      return 1;
    }
    if (stripDate(readFileSync(HTML_OUT, "utf8")) !== stripDate(html)) {
      console.error(`${rel(HTML_OUT)} is stale. Run: npm run build (in docs/)`);
      return 1;
    }
    console.log(`${rel(HTML_OUT)} is up to date.`);
    return 0;
  }

  writeFileSync(HTML_OUT, html, "utf8");
  console.log(`Wrote ${rel(HTML_OUT)} (${html.length.toLocaleString()} bytes, ${state.diagrams} diagrams, ${state.toc.length} sections).`);

  if (wantPdf) {
    const browser = await renderPdf(state.title, state.diagrams);
    console.log(`Wrote ${rel(PDF_OUT)} via ${browser}.`);
  }
  return 0;
}

main(process.argv.slice(2)).then(
  (code) => process.exit(code),
  (err) => {
    console.error(err.message || err);
    process.exit(1);
  },
);
