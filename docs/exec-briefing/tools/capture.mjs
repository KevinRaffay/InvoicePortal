// Capture stills and animated GIFs of the running Invoice Portal Admin app for the exec deck.
import { mkdirSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const puppeteer = require("puppeteer-core");
const sharp = require("sharp");
const { GIFEncoder, quantize, applyPalette } = require("gifenc");

const here = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.join(here, "..", "media");
mkdirSync(OUT, { recursive: true });
const BASE = process.env.APP_URL || "http://localhost:5098";
const EDGE = "C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe";

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const shot = async (page, name) => {
  await page.screenshot({ path: path.join(OUT, name + ".png") });
  console.log("still:", name);
};

// ---- GIF recording -------------------------------------------------------------------------
const GIF_WIDTH = 960;
class Recorder {
  constructor(page) { this.page = page; this.frames = []; }
  async frame(delayMs = 120) {
    const png = await this.page.screenshot();
    const { data, info } = await sharp(png).resize(GIF_WIDTH).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    this.frames.push({ data, width: info.width, height: info.height, delay: delayMs });
  }
  async hold(ms) { if (this.frames.length) this.frames[this.frames.length - 1].delay += ms; }
  save(name) {
    const gif = GIFEncoder();
    for (const f of this.frames) {
      const palette = quantize(f.data, 256, { format: "rgba4444" });
      const index = applyPalette(f.data, palette, "rgba4444");
      gif.writeFrame(index, f.width, f.height, { palette, delay: f.delay });
    }
    gif.finish();
    const file = path.join(OUT, name + ".gif");
    writeFileSync(file, gif.bytes());
    console.log(`gif: ${name} (${this.frames.length} frames, ${(gif.bytes().length / 1e6).toFixed(1)} MB)`);
  }
}

// Poll a predicate while recording frames, until it is true or the timeout passes.
async function recordUntil(rec, predicate, { every = 200, timeout = 20000 } = {}) {
  const start = Date.now();
  while (Date.now() - start < timeout) {
    await rec.frame(every);
    if (await predicate()) return true;
    await sleep(every);
  }
  return false;
}

const byText = (page, selector, text) =>
  page.evaluateHandle((sel, t) => [...document.querySelectorAll(sel)].find((e) => (e.textContent || "").trim().includes(t)), selector, text);

async function clickText(page, selector, text) {
  const handle = await byText(page, selector, text);
  const el = handle.asElement();
  if (!el) throw new Error(`No ${selector} containing "${text}"`);
  await el.click();
}

// ---- Main ---------------------------------------------------------------------------------
const browser = await puppeteer.launch({ executablePath: EDGE, headless: true, args: ["--window-size=1440,900"] });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 1440, height: 900, deviceScaleFactor: 2 });
  await page.emulateMediaFeatures([{ name: "prefers-color-scheme", value: "light" }]);

  // Home
  await page.goto(BASE + "/", { waitUntil: "load" });
  await page.waitForSelector(".rz-body", { timeout: 30000 });
  await sleep(1500);
  await shot(page, "home");

  // Invoices grid
  await page.goto(BASE + "/invoices", { waitUntil: "load" });
  await page.waitForSelector(".rz-data-grid tbody tr td", { timeout: 30000 });
  await sleep(1200);
  await shot(page, "invoices");

  // Invoice edit dialog
  await page.click('button[title="Edit"]');
  await page.waitForSelector(".rz-dialog", { timeout: 15000 });
  await sleep(1500);
  await shot(page, "invoice-dialog");
  await clickText(page, ".rz-dialog button", "Cancel").catch(() => page.keyboard.press("Escape"));
  await sleep(500);

  // Bottlers
  await page.goto(BASE + "/bottlers", { waitUntil: "load" });
  await page.waitForSelector(".rz-data-grid tbody tr td", { timeout: 30000 });
  await sleep(1200);
  await shot(page, "bottlers");

  // AI Insights: still + GIF (type a question, run it, results appear)
  await page.goto(BASE + "/ai/query", { waitUntil: "load" });
  await page.waitForSelector("textarea", { timeout: 30000 });
  await sleep(1200);
  await shot(page, "ai-query");

  const rec = new Recorder(page);
  await rec.frame(600);
  await page.click("textarea");
  await page.keyboard.down("Control");
  await page.keyboard.press("KeyA");
  await page.keyboard.up("Control");
  await page.keyboard.press("Backspace");
  await rec.frame(200);
  const question = "Top 5 payers by total amount";
  for (const chunk of question.match(/.{1,4}/g)) {
    await page.keyboard.type(chunk, { delay: 30 });
    await rec.frame(110);
  }
  await rec.hold(500);
  await clickText(page, "button", "Run query");
  const gotRows = await recordUntil(rec, () => page.$(".rz-data-grid tbody tr td").then(Boolean), { every: 180 });
  await sleep(600);
  await rec.frame(2500);
  rec.save("ai-query");
  await shot(page, "ai-query-result");
  console.log("ai-query rows appeared:", gotRows);

  // Ask the documents: still + GIF
  await page.goto(BASE + "/bottlers", { waitUntil: "load" });
  await page.waitForSelector(".rz-data-grid tbody tr td", { timeout: 30000 });
  await sleep(800);
  const rec2 = new Recorder(page);
  await rec2.frame(700);
  await clickText(page, "button", "Ask the documents");
  await page.waitForSelector(".rz-dialog input", { timeout: 15000 });
  await sleep(900);
  await shot(page, "ask-docs");
  // Retype the suggested question so the clip shows someone asking, not just a button press.
  const suggested = await page.$eval(".rz-dialog input", (el) => el.value);
  await page.click(".rz-dialog input");
  await page.keyboard.down("Control");
  await page.keyboard.press("KeyA");
  await page.keyboard.up("Control");
  await page.keyboard.press("Backspace");
  await rec2.frame(400);
  for (const chunk of suggested.match(/.{1,6}/g)) {
    await page.keyboard.type(chunk, { delay: 20 });
    await rec2.frame(110);
  }
  await rec2.hold(600);
  await clickText(page, ".rz-dialog button", "Ask");
  const gotAnswer = await recordUntil(
    rec2,
    () => page.evaluate(() => /Sources/.test(document.querySelector(".rz-dialog")?.textContent || "")),
    { every: 180 },
  );
  await sleep(600);
  await rec2.frame(3000);
  rec2.save("ask-docs");
  await shot(page, "ask-docs-answer");
  console.log("ask-docs answer appeared:", gotAnswer);
} finally {
  await browser.close();
}
