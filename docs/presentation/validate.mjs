import { readFileSync, existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import assert from 'node:assert/strict';
import JSZip from 'jszip';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../..');
const data = JSON.parse(readFileSync(path.join(here, 'storyboard.json'), 'utf8'));
const manifest = JSON.parse(readFileSync(path.join(here, 'layout-manifest.json'), 'utf8'));
const expected = [30,90,60,60,60,300,120,120,120,120,90,30,90,120,90,300,0,0,0,0];
assert.deepEqual(data.slides.map(s => s.seconds), expected);
assert.equal(data.slides.reduce((a, s) => a + s.seconds, 0), 1800);
assert.equal(data.slides.slice(12,15).reduce((a, s) => a + s.seconds, 0), 300);
assert.equal(data.slides.slice(6,10).reduce((a, s) => a + s.seconds, 0), 480);
assert(data.slides.slice(6,10).every(s => s.owner === 'Database'));
assert(data.slides.slice(12,15).every(s => s.owner === 'Primary'));
for (const [i, s] of data.slides.entries()) {
  assert(s.notes.join('').length > 100, `Missing notes on slide ${i + 1}`);
  assert(s.sources.length > 0);
  for (const p of s.sources) assert(existsSync(path.join(root, p)), `Missing source: ${p}`);
}
for (const o of manifest.bounds) {
  assert(o.x >= 0 && o.y >= 0 && o.w >= 0 && o.h >= 0, `Negative geometry on slide ${o.slide}`);
  assert(o.x + o.w <= manifest.width + 0.001 && o.y + o.h <= manifest.height + 0.001, `Out-of-slide ${o.kind} on slide ${o.slide}: ${o.text || ''}`);
}
const zip = await JSZip.loadAsync(readFileSync(path.join(here, 'Invoice-Portal-Executive-Overview.pptx')));
const slides = Object.keys(zip.files).filter(p => /^ppt\/slides\/slide\d+\.xml$/.test(p));
const notes = Object.keys(zip.files).filter(p => /^ppt\/notesSlides\/notesSlide\d+\.xml$/.test(p));
assert.equal(slides.length, 20); assert.equal(notes.length, 20);
const presentation = await zip.file('ppt/presentation.xml').async('string');
assert(/cx="12192000" cy="6858000"/.test(presentation), 'Expected 16:9 layout');
for (let n = 1; n <= 20; n++) {
  const xml = await zip.file(`ppt/slides/slide${n}.xml`).async('string');
  const note = await zip.file(`ppt/notesSlides/notesSlide${n}.xml`).async('string');
  assert(xml.includes('<a:t>'), `Slide ${n} has no editable text`);
  assert(note.includes('SOURCE EVIDENCE'), `Slide ${n} lacks source notes`);
  assert(note.includes(`Owner: ${data.slides[n - 1].owner}`));
  if ([3,17,18,19].includes(n)) assert(xml.includes('ILLUSTRATIVE'), `Missing wireframe label on ${n}`);
}
console.log('PASS: 20 slides; 16:9; 30 minutes; database 8 minutes; production 5 minutes; owner labels; source paths; editable text; 20 notes; shape bounds; illustrative labels.');
console.log('Visual and PowerPoint text-overflow verification: run render:presentation. Human rehearsal and data approval remain separate.');