/** Native, editable PowerPoint slides. No application, database or cloud operations. */
import pptxgen from 'pptxgenjs';
import { readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const data = JSON.parse(readFileSync(path.join(here, 'storyboard.json'), 'utf8'));
const pptx = new pptxgen();
pptx.layout = 'LAYOUT_WIDE';
pptx.author = 'Invoice Portal team';
pptx.subject = 'Executive architecture overview and Azure OpenAI production readiness';
pptx.title = data.title;
pptx.company = 'Invoice Portal';
pptx.lang = 'en-US';
pptx.theme = { headFontFace: 'Aptos Display', bodyFontFace: 'Aptos', lang: 'en-US' };
const C = { navy: '11283F', teal: '007E80', aqua: 'D8F0EE', paper: 'F3F6F9', white: 'FFFFFF', ink: '183249', muted: '53677A', line: 'DCE4EB', amber: '996000' };
const W = 13.333333, H = 7.5;
const bounds = [];
let page;
const clock = s => `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
function record(kind, x, y, w, h, extra = {}) { bounds.push({ slide: page, kind, x, y, w, h, ...extra }); }
function rect(s, x, y, w, h, fill = C.white, line = fill, radius = false) {
  s.addShape(radius ? pptx.ShapeType.roundRect : pptx.ShapeType.rect, { x, y, w, h, radius: 0.15, rectRadius: 0.12, fill: { color: fill }, line: { color: line, width: 0.7 } });
  record('shape', x, y, w, h);
}
function text(s, value, x, y, w, h, size = 24, color = C.ink, bold = false, opts = {}) {
  s.addText(value, { x, y, w, h, fontFace: 'Aptos', fontSize: size, color, bold, margin: 0, breakLine: false, valign: 'mid', paraSpaceAfterPt: 0, ...opts });
  record('text', x, y, w, h, { text: value, fontSize: size });
}
function arrow(s, x1, y1, x2, y2) {
  const x = Math.min(x1, x2), y = Math.min(y1, y2), w = Math.abs(x2 - x1), h = Math.abs(y2 - y1);
  s.addShape(pptx.ShapeType.line, { x, y, w, h, flipV: (y2 < y1), flipH: (x2 < x1), line: { color: C.teal, width: 2.2, beginArrowType: 'none', endArrowType: 'triangle' } });
  record('connector', x, y, w, h);
}
function pill(s, label, x, y, w, fill = C.aqua, color = C.teal) {
  rect(s, x, y, w, 0.35, fill, fill, true);
  text(s, label, x + 0.09, y, w - 0.18, 0.35, 11, color, true);
}
function node(s, title, body, x, y, w, h = 1.18, dark = false) {
  rect(s, x, y, w, h, dark ? C.navy : C.white, dark ? C.navy : C.line, true);
  text(s, title, x + 0.18, y + 0.1, w - 0.36, 0.39, 22, dark ? C.white : C.ink, true);
  if (body) text(s, body, x + 0.18, y + 0.53, w - 0.36, h - 0.57, 16, dark ? 'D5E7EF' : C.muted);
}
const titles = {
  1: 'Invoice Portal Admin', 3: 'One workspace for invoice administration',
  10: 'Controls for AI-assisted reporting', 13: 'Azure OpenAI: from integration to production',
  14: 'Two AI workflows. Two data boundaries.', 15: 'Go live through evidence—not configuration',
  17: 'Invoice workspace', 18: 'AI reporting', 19: 'Answers with visible sources'
};
function base(d, i, elapsed) {
  const s = pptx.addSlide(); page = i + 1;
  s.background = { color: C.paper };
  rect(s, 0, 0, W, 0.085, i >= 12 && i <= 14 ? C.teal : C.navy);
  text(s, d.section.toUpperCase(), 0.6, 0.3, 9.4, 0.3, 12, C.teal, true, { charSpacing: 1.5 });
  pill(s, i < 16 ? `${d.owner === 'Database' ? 'DATABASE' : d.owner === 'Both' ? 'BOTH' : 'PRIMARY'}  /  ${clock(d.seconds)}` : 'UNTIMED APPENDIX', 10.45, 0.3, 2.28);
  const title = titles[page] || d.title;
  text(s, title, 0.6, 0.86, 12.1, 0.93, title.length > 53 ? 29 : 34, C.navy, true);
  rect(s, 0.6, 6.29, 12.1, 0.68, C.aqua, C.aqua, true);
  text(s, d.takeaway, 0.77, 6.34, 11.78, 0.58, 17, C.navy, false);
  text(s, 'INVOICE PORTAL  /  EXECUTIVE BRIEFING', 0.6, 7.15, 7, 0.18, 10, C.muted);
  text(s, i < 16 ? `${clock(elapsed)}–${clock(elapsed + d.seconds)}  •  ${page} / 16` : `REFERENCE  •  ${page - 16} / 4`, 9.15, 7.12, 3.55, 0.24, 11, C.muted, false, { align: 'right' });
  return s;
}
function cards(s, d) {
  const n = d.cards.length, gap = 0.25, width = (12.1 - gap * (n - 1)) / n;
  d.cards.forEach((c, j) => {
    const x = 0.6 + j * (width + gap);
    rect(s, x, 2.08, width, 3.93, C.white, C.line, true);
    pill(s, String(j + 1).padStart(2, '0'), x + 0.25, 2.43, 0.52);
    text(s, c.title, x + 0.22, 2.98, width - 0.44, 0.8, 24, C.navy, true);
    text(s, c.body, x + 0.22, 3.92, width - 0.44, 1.92, 22, C.muted, false, { valign: 'top' });
  });
}
function flow(s, d) {
  if (d.cards.length === 4) {
    d.cards.forEach((c, j) => {
      const x = 0.6 + (j % 2) * 6.25, y = 2.18 + Math.floor(j / 2) * 2.03;
      rect(s, x, y, 5.85, 1.78, C.white, C.line, true);
      rect(s, x, y, 0.06, 1.78, C.teal);
      text(s, c.title, x + 0.22, y + 0.15, 5.41, 0.47, 23, C.navy, true);
      text(s, c.body, x + 0.22, y + 0.77, 5.41, 0.9, 22, C.muted, false, { valign: 'top' });
      if (j % 2 === 0) arrow(s, x + 5.9, y + 0.9, x + 6.2, y + 0.9);
    });
    return;
  }
  const n = d.cards.length, gap = 0.36, width = (12.1 - gap * (n - 1)) / n;
  d.cards.forEach((c, j) => {
    const x = 0.6 + j * (width + gap);
    rect(s, x, 2.18, width, 3.83, C.white, C.line, true);
    rect(s, x, 2.18, width, 0.075, C.teal);
    text(s, c.title, x + 0.19, 2.72, width - 0.38, 0.9, 25, C.navy, true);
    text(s, c.body, x + 0.19, 3.92, width - 0.38, 1.92, 22, C.muted, false, { valign: 'top' });
    if (j < n - 1) arrow(s, x + width + 0.03, 4.15, x + width + gap - 0.03, 4.15);
  });
}
function architecture(s) {
  node(s, 'Browser', 'User interactions', 0.6, 2.5, 2.25);
  node(s, 'Blazor server', '.NET 10 + Radzen', 3.12, 2.5, 2.75, 1.18, true);
  node(s, 'App services', 'CRUD + AI orchestration', 6.16, 2.5, 3.1);
  node(s, 'SQL', 'Existing data', 9.6, 2.5, 3.1);
  arrow(s, 2.86, 3.1, 3.1, 3.1); arrow(s, 5.89, 3.1, 6.14, 3.1); arrow(s, 9.29, 3.1, 9.57, 3.1);
  node(s, 'Model interface', 'Mock / Ollama / Azure OpenAI', 4.1, 4.52, 4.15);
  node(s, 'Retrieval interface', 'Seeded, in-memory documents', 8.57, 4.52, 4.13);
  arrow(s, 7.68, 3.72, 6.17, 4.46); arrow(s, 7.78, 3.72, 10.65, 4.46);
  text(s, 'SignalR circuit', 0.65, 3.94, 2.5, 0.3, 16, C.muted);
  text(s, 'Server-side execution', 3.2, 1.99, 6.3, 0.3, 16, C.teal, true);
}
function domain(s) {
  node(s, 'Bottler', 'Business party', 0.6, 2.13, 3.05);
  node(s, 'Payer', 'Belongs to bottler', 0.6, 3.65, 3.05);
  node(s, 'Sales center', 'Belongs to payer', 0.6, 5.03, 3.05, 1.06);
  node(s, 'Invoice', 'Amount • currency • dates\nStatus • party references', 4.38, 3.38, 4.07, 1.72, true);
  node(s, 'Program', 'Belongs to bottler', 9.28, 2.13, 3.42);
  node(s, 'Reference domains', 'Currency • country\nCompany • brand segment', 9.28, 4.08, 3.42, 1.62);
  arrow(s, 2.12, 3.32, 2.12, 3.6); arrow(s, 2.12, 4.86, 2.12, 4.98);
  arrow(s, 3.7, 4.23, 4.32, 4.23); arrow(s, 9.23, 2.79, 8.5, 3.72); arrow(s, 9.23, 4.77, 8.5, 4.77);
  text(s, 'Simplified domain view—not a full ERD', 4.37, 2.36, 4.45, 0.5, 16, C.teal, true);
}
function target(s, d) {
  pill(s, 'PROPOSED TARGET  •  NOT DEPLOYED', 0.6, 1.98, 4.3);
  node(s, 'Approved users', 'Entra + application roles', 0.6, 2.64, 3.1);
  node(s, 'Blazor app', 'Retain existing interfaces', 4.15, 2.64, 3.45, 1.18, true);
  node(s, 'Azure OpenAI', 'Managed identity', 8.07, 2.64, 4.63);
  arrow(s, 3.73, 3.23, 4.12, 3.23); arrow(s, 7.63, 3.23, 8.04, 3.23);
  node(s, 'SQL access paths', 'CRUD writer ≠ AI reporting reader', 2.4, 4.44, 4.75);
  node(s, 'Approved retrieval', 'Permission-aware sources', 7.63, 4.44, 5.07);
  arrow(s, 5.4, 3.87, 4.77, 4.39); arrow(s, 6.3, 3.87, 10.16, 4.39);
  text(s, 'Built: client + identity wiring    |    Required: access policy    |    Validate: model, region, capacity', 0.6, 5.87, 12.1, 0.25, 15, C.teal, true);
}
function wireframe(s, d, type) {
  pill(s, 'ILLUSTRATIVE  •  SYNTHETIC CONTENT  •  NOT A SCREENSHOT', 0.6, 1.97, 7.65);
  rect(s, 0.6, 2.5, 8.14, 3.48, C.white, C.line, true);
  rect(s, 0.6, 2.5, 8.14, 0.45, C.navy);
  text(s, 'INVOICE PORTAL ADMIN', 0.79, 2.56, 6.8, 0.28, 15, C.white, true);
  if (type === 'ui') {
    text(s, 'Invoices', 0.85, 3.13, 4.3, 0.4, 24, C.ink, true);
    pill(s, 'FILTER  /  SORT  /  COLUMNS', 4.58, 3.16, 3.86);
    const rows = [['Invoice', 'Currency', 'Status'], ['[Synthetic invoice]', '[Code]', '[State]'], ['[Synthetic invoice]', '[Code]', '[State]']];
    rows.forEach((row, r) => {
      rect(s, 0.85, 3.77 + r * 0.5, 7.64, 0.5, r === 0 ? C.aqua : r % 2 ? C.paper : C.white);
      row.forEach((v, c) => text(s, v, 1 + c * 2.55, 3.87 + r * 0.5, 2.35, 0.3, 16, C.ink, r === 0));
    });
    text(s, 'Detail tabs: Header  |  Parties & program  |  Submitter  |  Revision & SAP', 0.85, 5.56, 7.66, 0.26, 13, C.muted);
  } else if (type === 'report') {
    text(s, 'AI Insights', 0.85, 3.1, 7.65, 0.4, 24, C.ink, true);
    rect(s, 0.85, 3.72, 7.64, 0.57, C.paper, C.line);
    text(s, 'Count invoices per currency', 1, 3.83, 7.3, 0.34, 20);
    text(s, 'Generated SQL  ▸   [expand to inspect]', 0.85, 4.49, 7.4, 0.35, 17, C.teal, true);
    rect(s, 0.85, 5, 7.64, 0.72, C.aqua);
    text(s, 'Currency           InvoiceCount           TotalAmount\n[Code]              [Count]                     [Synthetic amount]', 1, 5.07, 7.25, 0.58, 16);
  } else {
    text(s, 'Ask the documents', 0.85, 3.1, 7.65, 0.4, 24, C.ink, true);
    text(s, 'What supplies are listed in this illustrative source?', 0.85, 3.63, 7.55, 0.54, 21);
    rect(s, 0.85, 4.34, 7.64, 0.64, C.aqua);
    text(s, 'The illustrative source lists sample supplies. [S1]', 1, 4.42, 7.27, 0.46, 21);
    text(s, 'S1  /  SYNTHETIC SOURCE\nExcerpt: Sample supplies are listed here.', 0.85, 5.22, 7.6, 0.61, 17, C.muted);
  }
  d.cards.forEach((c, j) => {
    text(s, `${String(j + 1).padStart(2, '0')}  ${c.title}`, 9.03, 2.58 + j * 1.13, 3.67, 0.4, 19, C.teal, true);
    text(s, c.body, 9.03, 3.0 + j * 1.13, 3.67, 0.8, 15, C.muted);
  });
}
let elapsed = 0;
const notes = ['# Invoice Portal — complete speaker notes', '', 'Generated from [storyboard.json](storyboard.json). Edit the storyboard, then rebuild. Wireframes are illustrative; no live run or data approval is implied.', ''];
for (const [i, d] of data.slides.entries()) {
  const s = base(d, i, elapsed);
  if (d.layout === 'title') {
    text(s, 'Architecture, user experience\n& AI integration', 0.63, 2.15, 7.1, 1.52, 37, C.navy, true);
    text(s, 'A 30-minute executive briefing\nPrimary presenter + database colleague', 0.66, 4.17, 6.5, 0.9, 23, C.muted);
    d.cards.forEach((c, j) => node(s, c.title, c.body, 8.01, 2.11 + j * 1.3, 4.69, 1.12, j === 0));
  } else if (d.layout === 'architecture' && i === 1) architecture(s);
  else if (d.layout === 'domain') domain(s);
  else if (d.layout === 'target') target(s, d);
  else if (['ui', 'report', 'document'].includes(d.layout)) wireframe(s, d, d.layout);
  else if (d.layout === 'flow') flow(s, d);
  else cards(s, d);
  const timing = d.seconds ? `${clock(elapsed)}–${clock(elapsed + d.seconds)}` : 'Untimed appendix';
  const sourceText = d.sources.map(p => `- ${p}`).join('\n');
  s.addNotes([`${i + 1}. ${d.title}\nOwner: ${d.owner} | Duration: ${clock(d.seconds)} | Run: ${timing}\n\n${d.notes.join('\n\n')}\n\nSOURCE EVIDENCE\n${sourceText}`]);
  notes.push(`## ${i + 1}. ${d.title}`, '', `**Owner:** ${d.owner} • **Duration:** ${clock(d.seconds)} • **Run:** ${timing}`, '', ...d.notes.flatMap(n => [n, '']), '**Source evidence**', '', ...d.sources.map(p => `- [${p}](../../${p})`), '');
  elapsed += d.seconds;
}
if (elapsed !== 1800 || data.slides.length !== 20) throw new Error('Expected 20 slides and exactly 30 minutes');
await pptx.writeFile({ fileName: path.join(here, 'Invoice-Portal-Executive-Overview.pptx') });
writeFileSync(path.join(here, 'SPEAKER-NOTES.md'), notes.join('\n'));
writeFileSync(path.join(here, 'layout-manifest.json'), JSON.stringify({ width: W, height: H, bounds }, null, 2) + '\n');
console.log(`Generated editable PowerPoint: ${data.slides.length} slides, ${clock(elapsed)}, notes on every slide.`);