# IT executive briefing deck

`InvoicePortalAdmin-ExecBriefing.pptx` is a 30-minute, high-level briefing for IT leadership: 20 slides,
speaker notes on every slide, casual tone. Chapters 1 to 3 (architecture, UI, AI) are for the primary presenter;
chapter 4 (slides 15 to 18) is for the database colleague. Replace `[Database colleague]` on slides 1, 2, 15 and 20.

Slides 11 and 13 contain animated GIFs of the AI features. They play in **slideshow mode** only.

`media/` holds the screenshots and GIFs captured from the running app (Mock AI provider, real local data).

## Regenerating

The scripts in `tools/` need Node 18+ and these packages (not part of `docs/package.json`):

```bash
npm install pptxgenjs react-icons react react-dom sharp puppeteer-core gifenc
```

1. Start the app (`docker compose up` or `dotnet run --project InvoicePortal.Admin --launch-profile http`).
2. `APP_URL=http://localhost:5098 node tools/capture.mjs` rewrites `media/` from the live app (uses headless Edge).
3. `node tools/build-deck.js` rebuilds the .pptx.
4. `tools/export-slides.ps1 -Deck InvoicePortalAdmin-ExecBriefing.pptx -OutDir render` renders PNGs with PowerPoint for review.

Note: `docs/presentation/` holds a separate, more formal deck produced independently. They are alternatives, not parts of one set.
