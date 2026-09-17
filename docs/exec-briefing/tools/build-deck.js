// Invoice Portal Admin — IT executive briefing deck (30 min). Builds InvoicePortalAdmin-ExecBriefing.pptx
const pptxgen = require("pptxgenjs");
const React = require("react");
const { renderToStaticMarkup } = require("react-dom/server");
const Fi = require("react-icons/fi");
const sharp = require("sharp");
const path = require("path");

const SHOTS = path.join(__dirname, "..", "media");
const OUT = path.join(__dirname, "..", "InvoicePortalAdmin-ExecBriefing.pptx");

// ---- palette (invoices + trust: deep ink, paper white, amber accent) -------------------
const C = {
  ink: "13233A", // dominant dark
  ink2: "1E3556", // dark surface
  paper: "FFFFFF",
  paper2: "F3F5F8", // light card
  text: "1B2430",
  muted: "6C7A8A",
  steel: "3D6B9A", // secondary
  amber: "F0A429", // accent
  amberDeep: "C77F12",
  line: "D9DFE7",
  ok: "3C9D6E",
};
const HEAD = "Cambria";
const BODY = "Calibri";

// ---- icon rendering -------------------------------------------------------------------
async function iconPng(name, color, size = 256) {
  const Icon = Fi[name];
  if (!Icon) throw new Error("Unknown icon " + name);
  const svg = renderToStaticMarkup(React.createElement(Icon, { color: "#" + color, size, strokeWidth: 1.75 }));
  const buf = await sharp(Buffer.from(svg)).png().toBuffer();
  return "image/png;base64," + buf.toString("base64");
}

// ---- helpers ---------------------------------------------------------------------------
const shadow = () => ({ type: "outer", blur: 10, offset: 3, angle: 90, color: "000000", opacity: 0.18 });
const W = 13.333, H = 7.5;

function bg(slide, color) {
  slide.background = { color };
}

function title(slide, text, opts = {}) {
  slide.addText(text, {
    x: 0.6, y: 0.45, w: W - 1.2, h: 0.9, fontFace: HEAD, fontSize: 34, bold: true,
    color: opts.color || C.ink, isTextBox: true, margin: 0, valign: "middle", ...opts,
  });
}

function kicker(slide, text, opts = {}) {
  slide.addText(text, {
    x: 0.6, y: 1.3, w: W - 1.2, h: 0.5, fontFace: BODY, fontSize: 16, color: opts.color || C.muted,
    isTextBox: true, margin: 0, italic: true,
  });
}

function footer(slide, n, dark = false) {
  slide.addText(`Invoice Portal Admin  ·  ${n}`, {
    x: 0.6, y: H - 0.5, w: 6, h: 0.3, fontFace: BODY, fontSize: 10, color: dark ? "8FA3BA" : C.muted,
    isTextBox: true, margin: 0,
  });
}

// Framed screenshot: white rounded card with shadow, image inset. Keeps the 16:10 aspect.
function framed(slide, img, x, y, w, opts = {}) {
  const h = w * 0.625;
  slide.addShape("roundRect", {
    x, y, w, h, fill: { color: C.paper }, line: { color: C.line, width: 0.75 }, rectRadius: 0.08, shadow: shadow(),
  });
  slide.addImage({ path: img, x: x + 0.08, y: y + 0.08, w: w - 0.16, h: h - 0.16, ...opts });
  return h;
}

// Icon inside an amber circle, with a bold header and a one-line description beside it.
async function iconRow(slide, icon, head, desc, x, y, w, opts = {}) {
  const circle = opts.circle || 0.62;
  slide.addShape("ellipse", { x, y, w: circle, h: circle, fill: { color: opts.circleColor || C.amber }, line: { color: opts.circleColor || C.amber } });
  slide.addImage({ data: await iconPng(icon, opts.iconColor || C.ink), x: x + circle * 0.22, y: y + circle * 0.22, w: circle * 0.56, h: circle * 0.56 });
  slide.addText(head, {
    x: x + circle + 0.2, y: y - 0.04, w: w - circle - 0.2, h: 0.36, fontFace: BODY, fontSize: opts.headSize || 16, bold: true,
    color: opts.color || C.text, isTextBox: true, margin: 0, valign: "top",
  });
  slide.addText(desc, {
    x: x + circle + 0.2, y: y + 0.3, w: w - circle - 0.2, h: opts.descH || 0.6, fontFace: BODY, fontSize: opts.descSize || 13,
    color: opts.descColor || C.muted, isTextBox: true, margin: 0, valign: "top",
  });
}

// Light card with icon circle, title and body text.
async function card(slide, icon, head, body, x, y, w, h, opts = {}) {
  slide.addShape("roundRect", { x, y, w, h, fill: { color: opts.fill || C.paper2 }, line: { color: opts.fill || C.paper2 }, rectRadius: 0.1 });
  const circle = 0.7;
  slide.addShape("ellipse", { x: x + 0.3, y: y + 0.3, w: circle, h: circle, fill: { color: C.amber }, line: { color: C.amber } });
  slide.addImage({ data: await iconPng(icon, C.ink), x: x + 0.3 + circle * 0.22, y: y + 0.3 + circle * 0.22, w: circle * 0.56, h: circle * 0.56 });
  slide.addText(head, { x: x + 0.3, y: y + 1.15, w: w - 0.6, h: 0.4, fontFace: BODY, fontSize: 17, bold: true, color: C.text, isTextBox: true, margin: 0 });
  slide.addText(body, { x: x + 0.3, y: y + 1.6, w: w - 0.6, h: h - 1.8, fontFace: BODY, fontSize: 13.5, color: C.muted, isTextBox: true, margin: 0, valign: "top" });
}

// Arrow connector between boxes (right-pointing block arrow, subtle).
function arrow(slide, x, y, w = 0.5, h = 0.3, color = C.muted) {
  slide.addShape("rightArrow", { x, y, w, h, fill: { color }, line: { color } });
}

// Box for architecture diagram.
function box(slide, text, sub, x, y, w, h, opts = {}) {
  slide.addShape("roundRect", {
    x, y, w, h, fill: { color: opts.fill || C.paper }, line: { color: opts.line || C.line, width: opts.dashed ? 1.25 : 1, dashType: opts.dashed ? "dash" : "solid" },
    rectRadius: 0.1, shadow: opts.noShadow ? undefined : shadow(),
  });
  slide.addText(text, { x: x + 0.15, y: y + 0.12, w: w - 0.3, h: 0.4, fontFace: BODY, fontSize: 15, bold: true, color: opts.color || C.text, isTextBox: true, margin: 0, align: "center" });
  if (sub) slide.addText(sub, { x: x + 0.15, y: y + 0.5, w: w - 0.3, h: h - 0.6, fontFace: BODY, fontSize: 11.5, color: opts.subColor || C.muted, isTextBox: true, margin: 0, align: "center", valign: "top" });
}

function divider(pres, n, eyebrow, big, small, who) {
  const s = pres.addSlide();
  bg(s, C.ink);
  s.addShape("ellipse", { x: 9.2, y: -1.6, w: 6.4, h: 6.4, fill: { color: C.ink2 }, line: { color: C.ink2 } });
  s.addShape("ellipse", { x: 10.9, y: 4.3, w: 2.2, h: 2.2, fill: { color: C.amber }, line: { color: C.amber } });
  s.addText(eyebrow, { x: 0.8, y: 2.1, w: 8, h: 0.4, fontFace: BODY, fontSize: 14, color: C.amber, bold: true, charSpacing: 4, isTextBox: true, margin: 0 });
  s.addText(big, { x: 0.8, y: 2.5, w: 8.5, h: 1.3, fontFace: HEAD, fontSize: 48, bold: true, color: C.paper, isTextBox: true, margin: 0, valign: "middle" });
  s.addText(small, { x: 0.8, y: 3.85, w: 8, h: 0.8, fontFace: BODY, fontSize: 18, color: "C9D4E1", isTextBox: true, margin: 0 });
  if (who) s.addText(who, { x: 0.8, y: 4.7, w: 8, h: 0.4, fontFace: BODY, fontSize: 14, color: "8FA3BA", italic: true, isTextBox: true, margin: 0 });
  footer(s, n, true);
  return s;
}

// ---- build ------------------------------------------------------------------------------
(async () => {
  const pres = new pptxgen();
  pres.layout = "LAYOUT_WIDE";
  pres.title = "Invoice Portal Admin – IT briefing";
  pres.author = "Kevin Raffay";
  let n = 0;

  // 1. Title ------------------------------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.ink);
    s.addShape("ellipse", { x: 8.6, y: -2.2, w: 7.6, h: 7.6, fill: { color: C.ink2 }, line: { color: C.ink2 } });
    framed(s, path.join(SHOTS, "home.png"), 7.15, 2.35, 5.6);
    s.addText("INVOICE PORTAL ADMIN", { x: 0.8, y: 1.9, w: 6.2, h: 0.4, fontFace: BODY, fontSize: 14, color: C.amber, bold: true, charSpacing: 4, isTextBox: true, margin: 0 });
    s.addText("A friendlier front door\nto our invoice data", { x: 0.8, y: 2.3, w: 6.3, h: 1.9, fontFace: HEAD, fontSize: 42, bold: true, color: C.paper, isTextBox: true, margin: 0, valign: "top" });
    s.addText("A proof of concept: modern UI, safe data access, and a first look at AI over our own numbers.", {
      x: 0.8, y: 4.3, w: 6.0, h: 0.9, fontFace: BODY, fontSize: 17, color: "C9D4E1", isTextBox: true, margin: 0,
    });
    s.addText([
      { text: "Kevin Raffay", options: { bold: true, color: C.paper } },
      { text: "   architecture, UI and AI", options: { color: "8FA3BA" } },
    ], { x: 0.8, y: 5.45, w: 6.2, h: 0.35, fontFace: BODY, fontSize: 14, isTextBox: true, margin: 0, valign: "top" });
    s.addText([
      { text: "[Database colleague]", options: { bold: true, color: C.paper } },
      { text: "   the data side", options: { color: "8FA3BA" } },
    ], { x: 0.8, y: 5.85, w: 6.2, h: 0.35, fontFace: BODY, fontSize: 14, isTextBox: true, margin: 0, valign: "top" });
    s.addText("IT leadership briefing  ·  September 2026", { x: 0.8, y: 6.5, w: 6, h: 0.3, fontFace: BODY, fontSize: 11, color: "8FA3BA", isTextBox: true, margin: 0 });
    s.addNotes(
      "Welcome. Keep this to 30 minutes. Set the tone: this is a proof of concept we built to answer two questions. " +
      "Can we give ops a modern, safe way to look at and fix Invoice Portal data? And can we try AI on our own data without any risk to production? " +
      "Spoiler: yes to both, and we'll show it live-ish with screenshots and short clips. Introduce your database colleague and say they'll take the last third.",
    );
  }

  // 2. Agenda -----------------------------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "The next 30 minutes");
    kicker(s, "Three short chapters, then your questions.");
    const items = [
      ["FiLayers", "The big picture", "What we built, why, and what it runs on.", "Kevin · 8 min"],
      ["FiMonitor", "UI and AI in action", "A tour of the screens and the two AI features.", "Kevin · 12 min"],
      ["FiDatabase", "The data side", "Where the data comes from and how we keep it safe.", "[Colleague] · 8 min"],
    ];
    let x = 0.6;
    for (const [icon, head, body, who] of items) {
      await card(s, icon, head, body, x, 2.2, 3.9, 3.4);
      s.addText(who, { x: x + 0.3, y: 4.95, w: 3.3, h: 0.35, fontFace: BODY, fontSize: 12.5, bold: true, color: C.amberDeep, isTextBox: true, margin: 0 });
      x += 4.12;
    }
    s.addText("Then: questions and what's next  ·  2 min", { x: 0.6, y: 5.95, w: 8, h: 0.4, fontFace: BODY, fontSize: 14, color: C.muted, isTextBox: true, margin: 0, italic: true });
    footer(s, n);
    s.addNotes("One breath on the agenda. Tell them they can interrupt any time; the deck is a guide, not a script.");
  }

  // 3. Why -------------------------------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "Why we built it");
    kicker(s, "Three itches, one small app.");
    const rows = [
      ["FiSearch", "Ops needs to see and fix records", "Invoices, bottlers, payers, programs. Today that means SQL scripts or waiting on a developer."],
      ["FiShield", "Without touching production", "A full copy of the real data, running on one laptop or one container host. Break it, reset it, no harm done."],
      ["FiZap", "And a cheap way to try AI", "Same real data, same screens. Ask questions in plain English and see what happens, with guardrails on."],
    ];
    let y = 2.15;
    for (const [icon, head, desc] of rows) {
      await iconRow(s, icon, head, desc, 0.6, y, 6.0, { descH: 0.9 });
      y += 1.45;
    }
    framed(s, path.join(SHOTS, "home.png"), 7.1, 2.2, 5.6);
    s.addText("38,592 invoices, 459 bottlers, 447 payers: the real shape of the data, locally.", {
      x: 7.1, y: 5.85, w: 5.6, h: 0.5, fontFace: BODY, fontSize: 12, color: C.muted, italic: true, isTextBox: true, margin: 0, align: "center",
    });
    footer(s, n);
    s.addNotes(
      "The origin story in plain terms. Ops has to look things up and occasionally fix a record; that currently means someone writing SQL against production. " +
      "We wanted a screen for that, and we wanted to practice on a copy so nobody has to be brave. " +
      "The AI part was the bonus: if we already have a safe sandbox with real data, it is the perfect place to test what AI can and cannot do for us.",
    );
  }

  // 4. Big picture architecture ---------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "The big picture");
    kicker(s, "Everything runs in a handful of containers on one machine. No cloud required.");

    // Compose group background
    s.addShape("roundRect", { x: 3.55, y: 2.15, w: 9.2, h: 3.6, fill: { color: C.paper2 }, line: { color: C.paper2 }, rectRadius: 0.12 });
    s.addText("Docker Compose (one host)", { x: 3.8, y: 2.25, w: 5, h: 0.3, fontFace: BODY, fontSize: 11.5, bold: true, color: C.muted, charSpacing: 2, isTextBox: true, margin: 0 });

    box(s, "Browser", "Ops user, any modern browser", 0.6, 3.05, 2.3, 1.15);
    arrow(s, 3.0, 3.48, 0.45, 0.3);
    box(s, "Web app", "Blazor Server + Radzen\nGrids, dialogs, AI screens", 3.6, 2.85, 3.0, 1.55, { fill: C.ink, color: C.paper, subColor: "C9D4E1", line: C.ink });
    arrow(s, 6.72, 3.48, 0.45, 0.3);
    box(s, "SQL Server Express", "Full copy of the Invoice Portal database\nDurable Docker volume", 7.3, 2.85, 3.0, 1.55);
    box(s, "One-shot importer", "Restores the production backup on first run, then exits", 7.3, 4.55, 3.0, 1.05);
    // importer writes up into SQL Server
    s.addShape("line", { x: 8.8, y: 4.42, w: 0, h: 0.13, line: { color: C.muted, width: 1.5, beginArrowType: "triangle" } });
    box(s, "AI model", "Optional. Built-in mock today.\nLocal or Azure OpenAI later.", 10.5, 2.85, 2.1, 1.55, { dashed: true, line: C.steel, noShadow: true, color: C.steel });

    s.addText("Production backup file (.bacpac)", { x: 3.6, y: 4.75, w: 3.4, h: 0.3, fontFace: BODY, fontSize: 12, color: C.text, bold: true, isTextBox: true, margin: 0 });
    s.addText("Copied once to the laptop. Never written back anywhere.", { x: 3.6, y: 5.05, w: 3.5, h: 0.5, fontFace: BODY, fontSize: 11.5, color: C.muted, isTextBox: true, margin: 0 });
    arrow(s, 6.85, 4.9, 0.4, 0.28, "B8C2CE");

    s.addText("Start it with one command. Reset it with one command. The AI box is a plug, not a dependency.", {
      x: 0.6, y: 6.15, w: 12, h: 0.45, fontFace: BODY, fontSize: 14, color: C.text, isTextBox: true, margin: 0, italic: true,
    });
    footer(s, n);
    s.addNotes(
      "Walk left to right. A browser talks to a web app. The web app talks to a SQL Server that is a full copy of the real database. " +
      "A tiny helper restores the production backup the first time and then gets out of the way. " +
      "The AI box is dashed on purpose: today it is a built-in stand-in that needs no network. When we want a real model, local or Azure, we flip a setting. " +
      "Everything here starts with 'docker compose up' and resets with 'docker compose down'.",
    );
  }

  // 5. Stack ------------------------------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "What it's made of");
    kicker(s, "Boring, well-supported Microsoft pieces. That is a feature.");
    const tiles = [
      ["FiCode", ".NET 10 and Blazor", "Our standard stack. C# end to end, one team can own it."],
      ["FiGrid", "Radzen components", "Grids, dialogs and forms that look finished on day one."],
      ["FiDatabase", "EF Core + SQL Server", "The data model is generated from the real schema, not hand-typed."],
      ["FiBox", "Docker Compose", "Same containers on a laptop, a build agent or Azure."],
      ["FiCpu", "Microsoft.Extensions.AI", "One AI interface; swap the model without touching the app."],
      ["FiActivity", "OpenTelemetry", "Traces and metrics built in, so we can see what it is doing."],
    ];
    let i = 0;
    for (const [icon, head, body] of tiles) {
      const col = i % 3, row = Math.floor(i / 3);
      await card(s, icon, head, body, 0.6 + col * 4.12, 2.1 + row * 2.25, 3.9, 2.05);
      i++;
    }
    footer(s, n);
    s.addNotes(
      "Nothing exotic. That is deliberate: the interesting part of this project is the AI experiment, so the rest should be as ordinary as possible. " +
      "Two things worth a sentence each. The data model is generated from the real database, so when the schema changes we regenerate instead of hand-editing. " +
      "And the AI layer sits behind Microsoft's standard interface, so a model swap is a config change, not a rewrite.",
    );
  }

  // 6. Local today, Azure tomorrow -------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "Runs on a laptop today. Azure tomorrow.");
    kicker(s, "Same container image, same code. Only the surroundings change.");
    const colW = 5.9;
    // Local
    s.addShape("roundRect", { x: 0.6, y: 2.15, w: colW, h: 4.0, fill: { color: C.paper2 }, line: { color: C.paper2 }, rectRadius: 0.12 });
    s.addText("Today: local", { x: 0.9, y: 2.35, w: 5, h: 0.45, fontFace: HEAD, fontSize: 22, bold: true, color: C.text, isTextBox: true, margin: 0 });
    s.addText([
      { text: "One command to start, one to reset", options: { bullet: true, breakLine: true } },
      { text: "Real data, zero cloud spend", options: { bullet: true, breakLine: true } },
      { text: "AI uses the built-in stand-in or a model on the developer's GPU", options: { bullet: true, breakLine: true } },
      { text: "Nothing leaves the machine", options: { bullet: true } },
    ], { x: 0.9, y: 2.95, w: 5.3, h: 2.9, fontFace: BODY, fontSize: 15, color: C.text, isTextBox: true, margin: 0, paraSpaceAfter: 10, valign: "top" });
    // Azure
    s.addShape("roundRect", { x: 6.85, y: 2.15, w: colW, h: 4.0, fill: { color: C.ink }, line: { color: C.ink }, rectRadius: 0.12 });
    s.addText("Ready: Azure", { x: 7.15, y: 2.35, w: 5, h: 0.45, fontFace: HEAD, fontSize: 22, bold: true, color: C.paper, isTextBox: true, margin: 0 });
    s.addText([
      { text: "Azure Container Apps, deployed with the Azure Developer CLI", options: { bullet: true, breakLine: true } },
      { text: "Points at the existing Azure SQL database. It never creates or restores one", options: { bullet: true, breakLine: true } },
      { text: "Managed identity everywhere: no passwords or keys in config", options: { bullet: true, breakLine: true } },
      { text: "Entra ID sign-in switched on in front of the app", options: { bullet: true } },
    ], { x: 7.15, y: 2.95, w: 5.3, h: 2.9, fontFace: BODY, fontSize: 15, color: "E3EAF2", isTextBox: true, margin: 0, paraSpaceAfter: 10, valign: "top" });
    s.addText("Infrastructure is code in the repo, and a GitHub Actions workflow deploys on merge.", {
      x: 0.6, y: 6.35, w: 12, h: 0.4, fontFace: BODY, fontSize: 13, color: C.muted, italic: true, isTextBox: true, margin: 0,
    });
    footer(s, n);
    s.addNotes(
      "Left side is where we are; right side is ready to go when there is appetite. Emphasise the middle bullet on the right: in Azure the app would point at the existing database. " +
      "This project never copies production data into a new cloud database. Also worth saying: no secrets in config anywhere in the Azure path, it is all managed identity.",
    );
  }

  // 7. Divider: UI ------------------------------------------------------------------------
  n++; divider(pres, n, "CHAPTER 2", "A quick tour", "The screens people will actually use.", "Kevin");
  pres.slides[pres.slides.length - 1].addNotes("Transition. Two screens for the everyday work, then the two AI features.");

  // 8. Grids ------------------------------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "Grids that do the heavy lifting");
    kicker(s, "Filter, sort and page through 38,000 invoices without a query in sight.");
    framed(s, path.join(SHOTS, "invoices.png"), 0.6, 2.0, 7.7);
    const rows = [
      ["FiFilter", "Server does the work", "Only the rows on screen are fetched. Filters and sorts run in SQL, so it stays quick."],
      ["FiRotateCcw", "Delete is reversible", "Deleting an invoice hides it. Flip \"Show deleted\" and restore it in one click."],
      ["FiColumns", "Pick your columns", "Each user shows the columns they care about. Nothing is lost, just tucked away."],
    ];
    let y = 2.1;
    for (const [icon, head, desc] of rows) {
      await iconRow(s, icon, head, desc, 8.7, y, 4.1, { descH: 1.0, headSize: 15, descSize: 12.5 });
      y += 1.5;
    }
    footer(s, n);
    s.addNotes(
      "This is the Invoices screen. The point to land: it feels like a spreadsheet but the database does the heavy lifting, so it is fast even on the full data set. " +
      "Deleting is a soft delete: rows are marked, never removed, and can be restored. Same pattern for bottlers, payers, sales centers, programs and currencies.",
    );
  }

  // 9. Editing ----------------------------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "Editing without surprises");
    kicker(s, "A tabbed form with the fields grouped the way the business thinks about them.");
    const rows = [
      ["FiList", "Dropdowns that follow the data", "Pick a bottler and the payer list narrows. Pick a payer and the sales centers follow."],
      ["FiCheckCircle", "Guardrails from the database", "Required fields, unique ids and references are enforced. Errors come back in plain English."],
      ["FiClock", "Audit stamps, automatically", "Created and updated timestamps are set for you. Nothing to remember."],
    ];
    let y = 2.1;
    for (const [icon, head, desc] of rows) {
      await iconRow(s, icon, head, desc, 0.6, y, 4.1, { descH: 1.0, headSize: 15, descSize: 12.5 });
      y += 1.5;
    }
    framed(s, path.join(SHOTS, "invoice-dialog.png"), 5.0, 2.0, 7.7);
    footer(s, n);
    s.addNotes(
      "The edit dialog. Four tabs: header, parties and program, submitter, revision and SAP. The dropdowns cascade so you cannot pick a payer that does not belong to the bottler. " +
      "When the database says no, the user sees a sentence, not a SQL error code.",
    );
  }

  // 10. Divider: AI ------------------------------------------------------------------------
  n++; divider(pres, n, "CHAPTER 3", "AI, with guardrails", "Two features. Both useful. Neither can hurt the data.", "Kevin");
  pres.slides[pres.slides.length - 1].addNotes("Transition. Set expectations: this is a first look, running against a stand-in model today, but every safety check is the real thing.");

  // 11. Ask in plain English --------------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "Ask in plain English");
    kicker(s, "Type a question. Get a table. The SQL is there if you want to peek.");
    framed(s, path.join(SHOTS, "ai-query.gif"), 0.6, 2.0, 7.7);
    // flow strip on the right
    const steps = [
      ["FiMessageSquare", "You ask", "\"Top 5 payers by total amount\""],
      ["FiCpu", "The model drafts SQL", "Read-only, using a description of our tables"],
      ["FiShield", "The app checks it", "Anything but a single SELECT is rejected"],
      ["FiGrid", "You get a grid", "Sortable, filterable, capped at 200 rows"],
    ];
    let y = 2.05;
    for (const [icon, head, desc] of steps) {
      await iconRow(s, icon, head, desc, 8.7, y, 4.1, { descH: 0.6, headSize: 15, descSize: 12.5, circle: 0.56 });
      y += 1.12;
    }
    s.addText("Clip plays in slideshow mode.", { x: 8.7, y: 6.55, w: 4.1, h: 0.3, fontFace: BODY, fontSize: 10, color: C.muted, italic: true, isTextBox: true, margin: 0 });
    footer(s, n);
    s.addNotes(
      "Let the clip play once (it is about ten seconds). Narrate the four steps on the right. " +
      "The honest caveat: today the model is a stand-in that understands a dozen kinds of question, which is why the suggestion chips exist. " +
      "The plumbing around it, the prompt, the checks, the execution, is exactly what a real model would go through.",
    );
  }

  // 12. Safety ----------------------------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "What keeps it safe");
    kicker(s, "We assume the model will get it wrong sometimes. The app is built so that does not matter.");
    const rules = [
      ["FiEye", "Read only, full stop", "One SELECT statement. No updates, deletes or admin commands, ever."],
      ["FiShield", "Checked before it runs", "The app inspects the SQL the model wrote and refuses anything outside the rules."],
      ["FiRotateCcw", "Always rolled back", "Every AI query runs in a transaction that is undone at the end, even when it succeeds."],
      ["FiClock", "Hard limits", "Five-second timeout, 200-row cap, ten questions a minute per user."],
      ["FiToggleLeft", "Switchable", "Feature flags hide the AI entirely, or just the document chat, with one setting."],
    ];
    let y = 2.1;
    for (const [icon, head, desc] of rules) {
      await iconRow(s, icon, head, desc, 0.6, y, 7.2, { descH: 0.55, headSize: 15, descSize: 12.5, circle: 0.56 });
      y += 0.92;
    }
    // big stat callouts
    const stats = [["200", "rows max"], ["5 s", "per query"], ["10", "asks per minute"]];
    let x = 8.5;
    for (const [big, small] of stats) {
      s.addShape("roundRect", { x, y: 2.3, w: 1.35, h: 1.7, fill: { color: C.ink }, line: { color: C.ink }, rectRadius: 0.1 });
      s.addText(big, { x, y: 2.45, w: 1.35, h: 0.8, fontFace: HEAD, fontSize: 30, bold: true, color: C.amber, isTextBox: true, margin: 0, align: "center" });
      s.addText(small, { x, y: 3.25, w: 1.35, h: 0.6, fontFace: BODY, fontSize: 11, color: "C9D4E1", isTextBox: true, margin: 0, align: "center" });
      x += 1.5;
    }
    s.addText("Recommended next step: run AI queries under a read-only database login too. Belt and braces.", {
      x: 8.5, y: 4.3, w: 4.3, h: 0.9, fontFace: BODY, fontSize: 12.5, color: C.muted, italic: true, isTextBox: true, margin: 0,
    });
    footer(s, n);
    s.addNotes(
      "This is the slide for the risk-minded. The model writes SQL; the app does not trust it. Single read-only statement, inspected, time-boxed, row-capped, and the transaction is rolled back regardless. " +
      "Plus feature flags to turn it off. Mention the follow-up: a read-only database account for the AI path so the database itself is the last line of defence.",
    );
  }

  // 13. Ask the documents ---------------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "Ask the documents");
    kicker(s, "Questions about a bottler, answered from our own documents, with receipts.");
    const rows = [
      ["FiFileText", "Grounded, not guessed", "The model only sees the documents we hand it and must answer from those."],
      ["FiBookmark", "Every claim is cited", "Each sentence carries a source label. Uncited answers are rejected."],
      ["FiUsers", "One click from a bottler", "The chat button on a bottler row opens with that customer's name already filled in."],
    ];
    let y = 2.1;
    for (const [icon, head, desc] of rows) {
      await iconRow(s, icon, head, desc, 0.6, y, 4.1, { descH: 1.0, headSize: 15, descSize: 12.5 });
      y += 1.5;
    }
    framed(s, path.join(SHOTS, "ask-docs.gif"), 5.0, 2.0, 7.7);
    s.addText("Clip plays in slideshow mode. Documents shown are sample content.", { x: 5.0, y: 6.88, w: 7.7, h: 0.3, fontFace: BODY, fontSize: 10, color: C.muted, italic: true, isTextBox: true, margin: 0, align: "right" });
    footer(s, n);
    s.addNotes(
      "Second AI feature. Think of it as search that talks back. The important design choice: the model can only use what we give it and has to cite it, so the answer is checkable. " +
      "Be upfront that the documents in the demo are samples we wrote; the real version would index the actual customer documents.",
    );
  }

  // 14. Swap the brain --------------------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "Swap the brain, keep the app");
    kicker(s, "The model is a setting. Three options are wired in today.");
    const opts = [
      ["FiBox", "Built-in stand-in", "Default. Works offline, deterministic, needs no keys. Great for demos and tests.", "Today", C.paper2, C.text],
      ["FiHardDrive", "Local model (Ollama)", "A real open model on a developer's GPU. Free-form questions, nothing leaves the machine.", "Ready", C.paper2, C.text],
      ["FiCloud", "Azure OpenAI", "Managed model, Entra ID auth, no keys. What the Azure deployment uses.", "Ready", C.ink, C.paper],
    ];
    let x = 0.6;
    for (const [icon, head, body, tag, fill, color] of opts) {
      s.addShape("roundRect", { x, y: 2.15, w: 3.9, h: 3.5, fill: { color: fill }, line: { color: fill }, rectRadius: 0.1 });
      s.addShape("ellipse", { x: x + 0.3, y: 2.45, w: 0.7, h: 0.7, fill: { color: C.amber }, line: { color: C.amber } });
      s.addImage({ data: await iconPng(icon, C.ink), x: x + 0.3 + 0.154, y: 2.45 + 0.154, w: 0.39, h: 0.39 });
      s.addText(tag, { x: x + 2.6, y: 2.5, w: 1.1, h: 0.32, fontFace: BODY, fontSize: 10.5, bold: true, color: fill === C.ink ? C.amber : C.amberDeep, isTextBox: true, margin: 0, align: "right", charSpacing: 2 });
      s.addText(head, { x: x + 0.3, y: 3.3, w: 3.3, h: 0.4, fontFace: BODY, fontSize: 17, bold: true, color, isTextBox: true, margin: 0 });
      s.addText(body, { x: x + 0.3, y: 3.75, w: 3.3, h: 1.6, fontFace: BODY, fontSize: 13.5, color: fill === C.ink ? "C9D4E1" : C.muted, isTextBox: true, margin: 0, valign: "top" });
      x += 4.12;
    }
    s.addText("Whichever we pick, the prompts, the safety checks and the screens do not change.", {
      x: 0.6, y: 5.95, w: 12, h: 0.45, fontFace: BODY, fontSize: 14, color: C.text, italic: true, isTextBox: true, margin: 0,
    });
    footer(s, n);
    s.addNotes(
      "This is the strategic point. We are not betting on a vendor. Today's demos run on a stand-in; a developer can point it at a local open model; the Azure path uses Azure OpenAI with our identity, no API keys. " +
      "In every case the app code is identical. Note that anything other than the stand-in sends the question, our table names and document text to that model, so the choice is a data-handling decision as much as a technical one.",
    );
  }

  // 15. Divider: Data ---------------------------------------------------------------------
  n++; divider(pres, n, "CHAPTER 4", "The data side", "Where it comes from, how the app uses it, how we keep it safe.", "[Database colleague]");
  pres.slides[pres.slides.length - 1].addNotes("Hand over to your database colleague here. Roughly eight minutes for the next three slides.");

  // 16. Where the data comes from --------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "Where the data comes from");
    kicker(s, "A faithful copy of production, made safe for a laptop.");
    const steps = [
      ["FiArchive", "Production backup", "A standard Azure SQL export file, copied once."],
      ["FiScissors", "Cleaned up", "Cloud-only pieces (identity users, encryption flags) are stripped from a copy. The original is untouched."],
      ["FiDatabase", "Restored locally", "Into SQL Server Express in Docker. About a minute for 744,000 rows."],
      ["FiRefreshCw", "Reset any time", "Throw the container away and it rebuilds itself from the backup."],
    ];
    let x = 0.6;
    for (let i = 0; i < steps.length; i++) {
      const [icon, head, body] = steps[i];
      await card(s, icon, head, body, x, 2.2, 2.85, 3.3);
      if (i < steps.length - 1) arrow(s, x + 2.9, 3.7, 0.3, 0.26, "B8C2CE");
      x += 3.2;
    }
    s.addText("It runs once. If the database already exists, it does nothing. If a restore fails halfway, it cleans up so the next run can try again.", {
      x: 0.6, y: 5.85, w: 12, h: 0.6, fontFace: BODY, fontSize: 13, color: C.muted, italic: true, isTextBox: true, margin: 0,
    });
    footer(s, n);
    s.addNotes(
      "[Colleague] Suggested talking points: the backup is the same export Azure produces; the cleanup only removes things an on-premises SQL Server cannot have, so the schema and data are exactly production's. " +
      "The restore is automatic and idempotent. Reset is a one-liner. Add colour on how long it takes and how often you have refreshed it.",
    );
  }

  // 17. How the app talks to the database ------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "How the app talks to the database");
    kicker(s, "Generated model, short conversations, nothing destructive.");
    const tiles = [
      ["FiGitBranch", "Model generated from the schema", "Nine tables the UI needs, generated straight from the database. Schema changes mean regenerate, not retype."],
      ["FiZap", "Short-lived connections", "Every screen action opens a connection, does its work and closes it. No long-lived sessions hogging the server."],
      ["FiRotateCcw", "Soft delete and audit", "Deletes flip a flag. Created and updated timestamps are set automatically. History is preserved."],
      ["FiShield", "AI reads, never writes", "AI queries run through the same connection, inside a transaction that is always rolled back."],
    ];
    let i = 0;
    for (const [icon, head, body] of tiles) {
      const col = i % 2, row = Math.floor(i / 2);
      await card(s, icon, head, body, 0.6 + col * 6.2, 2.1 + row * 2.35, 5.95, 2.2);
      i++;
    }
    footer(s, n);
    s.addNotes(
      "[Colleague] High level only. Four ideas: the model is generated so it cannot drift from the real schema; connections are short so the app is polite to the server; " +
      "deletes are soft so nothing is truly lost; and the AI path is read-only and rolled back. If asked about scale: filtering, sorting and paging all happen in SQL, so the app only ever pulls one page of rows.",
    );
  }

  // 18. Keeping production data safe -----------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "Keeping production data safe");
    kicker(s, "It is real data, so we treat it like real data.");
    const rows = [
      ["FiHardDrive", "Lives in one place", "The copy exists only on the local Docker volume and the backup file. Both are excluded from source control."],
      ["FiCloudOff", "Never pushed back to the cloud", "Nothing in this project restores the backup to Azure. The Azure deployment points at the existing database."],
      ["FiKey", "No secrets in code", "Local passwords live in an ignored env file. In Azure it is managed identity end to end."],
      ["FiUserCheck", "Next: least privilege", "Give the AI path its own read-only login, and put sign-in in front of the app before anyone but us uses it."],
    ];
    let y = 2.1;
    for (const [icon, head, desc] of rows) {
      await iconRow(s, icon, head, desc, 0.6, y, 7.4, { descH: 0.8, headSize: 15.5, descSize: 13 });
      y += 1.15;
    }
    framed(s, path.join(SHOTS, "bottlers.png"), 8.5, 2.2, 4.3);
    s.addText("Real bottlers, real ids. Handle with care.", { x: 8.5, y: 5.05, w: 4.3, h: 0.35, fontFace: BODY, fontSize: 11.5, color: C.muted, italic: true, isTextBox: true, margin: 0, align: "center" });
    footer(s, n);
    s.addNotes(
      "[Colleague] The reassurance slide. Where the copy lives, what never happens (no restore to the cloud), how secrets are handled, and the two follow-ups we would want before this goes beyond the team: " +
      "a read-only login for the AI path and sign-in on the app.",
    );
  }

  // 19. What's next -------------------------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.paper);
    title(s, "What's next, if you want more");
    kicker(s, "Small steps, each one useful on its own.");
    const steps = [
      ["1", "Real model, real documents", "Switch the AI to Azure OpenAI or a local model and index the actual customer documents."],
      ["2", "Sign-in and least privilege", "Entra ID in front of the app, a read-only database login for the AI path."],
      ["3", "Pilot in Azure", "Deploy to Container Apps against the existing database for a small ops group."],
      ["4", "Grow the screens", "More tables as ops asks for them. Each new screen is a grid, a dialog and an afternoon."],
    ];
    let x = 0.6;
    for (const [num, head, body] of steps) {
      s.addShape("roundRect", { x, y: 2.2, w: 2.95, h: 3.4, fill: { color: C.paper2 }, line: { color: C.paper2 }, rectRadius: 0.1 });
      s.addShape("ellipse", { x: x + 0.3, y: 2.5, w: 0.7, h: 0.7, fill: { color: C.ink }, line: { color: C.ink } });
      s.addText(num, { x: x + 0.3, y: 2.5, w: 0.7, h: 0.7, fontFace: HEAD, fontSize: 20, bold: true, color: C.amber, isTextBox: true, margin: 0, align: "center", valign: "middle" });
      s.addText(head, { x: x + 0.3, y: 3.4, w: 2.4, h: 0.65, fontFace: BODY, fontSize: 16, bold: true, color: C.text, isTextBox: true, margin: 0, valign: "top" });
      s.addText(body, { x: x + 0.3, y: 4.1, w: 2.4, h: 1.4, fontFace: BODY, fontSize: 13, color: C.muted, isTextBox: true, margin: 0, valign: "top" });
      x += 3.1;
    }
    s.addText("None of these require rework. The plumbing for all four is already in the repo.", {
      x: 0.6, y: 5.95, w: 12, h: 0.45, fontFace: BODY, fontSize: 14, color: C.text, italic: true, isTextBox: true, margin: 0,
    });
    footer(s, n);
    s.addNotes(
      "Kevin takes it back. Four options, in the order we would suggest. The ask is not for budget today; it is for a steer on which of these is worth a pilot. " +
      "The numbered order is a real sequence: a real model and sign-in come before anyone outside the team touches it.",
    );
  }

  // 20. Close --------------------------------------------------------------------------------
  {
    const s = pres.addSlide(); n++;
    bg(s, C.ink);
    s.addShape("ellipse", { x: -1.8, y: 3.6, w: 5.6, h: 5.6, fill: { color: C.ink2 }, line: { color: C.ink2 } });
    s.addShape("ellipse", { x: 11.2, y: -0.9, w: 2.4, h: 2.4, fill: { color: C.amber }, line: { color: C.amber } });
    s.addText("Questions?", { x: 0.8, y: 1.6, w: 8, h: 1.2, fontFace: HEAD, fontSize: 48, bold: true, color: C.paper, isTextBox: true, margin: 0 });
    s.addText([
      { text: "A modern, safe screen over the real Invoice Portal data.", options: { bullet: true, breakLine: true } },
      { text: "AI that helps you ask, and cannot change a thing.", options: { bullet: true, breakLine: true } },
      { text: "Runs on a laptop today, ready for Azure when you are.", options: { bullet: true } },
    ], { x: 0.8, y: 3.0, w: 8.5, h: 2.0, fontFace: BODY, fontSize: 18, color: "E3EAF2", isTextBox: true, margin: 0, paraSpaceAfter: 12, valign: "top" });
    s.addText("Kevin Raffay  ·  [Database colleague]", { x: 0.8, y: 5.6, w: 8, h: 0.4, fontFace: BODY, fontSize: 14, color: "8FA3BA", isTextBox: true, margin: 0 });
    footer(s, n, true);
    s.addNotes("Three takeaways, then open the floor. If time is short, the one sentence is: we can look at our data safely and ask it questions, and none of it touches production.");
  }

  await pres.writeFile({ fileName: OUT });
  console.log("wrote", OUT, "slides:", n);
})().catch((e) => { console.error(e); process.exit(1); });
