// Checks for the business-card build. Run with `npm test` (needs Chrome and Python installed).
import { test } from "node:test";
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { mkdtemp, readFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { fileURLToPath } from "node:url";
import path from "node:path";
import { PDFDocument } from "pdf-lib";

const here = path.dirname(fileURLToPath(import.meta.url));

async function pageSizesInInches(file) {
	const doc = await PDFDocument.load(await readFile(file));
	return doc.getPages().map(page => {
		const { width, height } = page.getMediaBox();
		return [Math.round(width / 72 * 1000) / 1000, Math.round(height / 72 * 1000) / 1000];
	});
}

// Vistaprint rejects or rescales a file whose size does not match the product, and a rescaled
// card shifts text towards the cut. Both PDFs must hold exactly the recto and the verso, at the
// full-bleed size for the printer and at trim size for proofing.
test("When the card is built, then both PDFs have two pages at the exact print sizes", async () => {
	const out = await mkdtemp(path.join(tmpdir(), "carte-"));

	execFileSync("node", [path.join(here, "build.mjs"), out], { stdio: "pipe" });

	assert.deepEqual(await pageSizesInInches(path.join(out, "carte-julien-eclancher-vistaprint.pdf")), [[3.62, 2.12], [3.62, 2.12]]);
	assert.deepEqual(await pageSizesInInches(path.join(out, "carte-julien-eclancher-taille-reelle.pdf")), [[3.5, 2], [3.5, 2]]);
});

// The brush drawings use a seeded generator, so a reprint months later comes out identical to
// the approved proof. A change of drawing must be deliberate, never a side effect of a rerun.
test("When the mark and the name stroke are drawn twice, then the SVG is identical", async () => {
	const source = await readFile(path.join(here, "..", "..", "tools", "brand", "brush.js"), "utf8");
	const { drawMark, drawSwash } = new Function(`${source}; return { drawMark, drawSwash };`)();

	const first = drawMark({ brush: "#FFFFFF" }) + drawSwash();
	const second = drawMark({ brush: "#FFFFFF" }) + drawSwash();

	assert.equal(first, second);
	assert.match(first, /^\s*<svg[\s\S]*<\/svg>\s*<svg[\s\S]*<\/svg>$/);
});
