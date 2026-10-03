// Renders card.html into the files Vistaprint accepts: a two-page vector PDF (recto, verso)
// with bleed, the same at trim size for proofing, 600 dpi PNGs as a fallback, and review sheets.
//
//   node build.mjs [outDir]       (default: ./out)
//
// The door code printed on the verso is not in the public repository: it is read from
// card.local.json (git-ignored), falling back to card.example.json.
import { writeFile, readFile, mkdir, access } from "node:fs/promises";
import { execFileSync } from "node:child_process";
import { fileURLToPath, pathToFileURL } from "node:url";
import path from "node:path";
import QRCode from "qrcode";
import { chromium } from "playwright-core";
import { PDFDocument } from "pdf-lib";

const here = path.dirname(fileURLToPath(import.meta.url));
const generated = path.join(here, "generated");
const out = path.resolve(process.argv[2] ?? path.join(here, "out"));
await mkdir(generated, { recursive: true });
await mkdir(out, { recursive: true });

const exists = file => access(file).then(() => true, () => false);

// Paper textures are slow to generate and never change, so they are made once.
if (!(await exists(path.join(generated, "crane-washi.png")))) {
	execFileSync("python", [path.join(here, "textures.py"), generated], { stdio: "inherit" });
}

const localConfig = path.join(here, "card.local.json");
const config = JSON.parse(await readFile(await exists(localConfig) ? localConfig : path.join(here, "card.example.json"), "utf8"));

// Short, stable URL: the site redirects /rdv/ to the booking page, so printed cards survive a
// change of booking link. The address must never move.
const qrSvg = await QRCode.toString("https://osteo-eclancher.ca/rdv/", {
	type: "svg", errorCorrectionLevel: "M", margin: 0, color: { dark: "#2E4762", light: "#0000" },
});
await writeFile(path.join(generated, "card-data.js"), `const CARD_DATA = ${JSON.stringify({ qrSvg, doorCode: config.doorCode })};\n`);

const pageUrl = pathToFileURL(path.join(here, "card.html")).href;
const browser = await chromium.launch({ channel: "chrome" });

async function open(query, scale = 1) {
	const page = await browser.newPage({ deviceScaleFactor: scale, viewport: { width: 1000, height: 700 } });
	await page.goto(pageUrl + query);
	await page.evaluate(() => document.fonts.ready);
	return page;
}

// Chrome rounds page sizes to whole CSS pixels (3.62 in = 347.52 px), so the PDF comes out a few
// thousandths of an inch too big. Content is anchored top-left, so resetting the page box to the
// exact size only shaves that excess off the right and bottom bleed.
async function exactPdf(query, file, widthIn, heightIn) {
	const page = await open(query);
	const bytes = await page.pdf({ width: `${widthIn}in`, height: `${heightIn}in`, printBackground: true, pageRanges: "1-2" });
	await page.close();
	const doc = await PDFDocument.load(bytes);
	for (const pdfPage of doc.getPages()) {
		const top = pdfPage.getMediaBox().height;
		const box = [0, top - heightIn * 72, widthIn * 72, heightIn * 72];
		pdfPage.setMediaBox(...box);
		pdfPage.setCropBox(...box);
	}
	await writeFile(file, await doc.save());
}

try {
	// For Vistaprint: with the 0.06 in bleed on every side.
	await exactPdf("", path.join(out, "carte-julien-eclancher-vistaprint.pdf"), 3.62, 2.12);
	// Actual card size, as it comes out of the cutter: for proofreading or printing at home at 100 %.
	await exactPdf("?trim", path.join(out, "carte-julien-eclancher-taille-reelle.pdf"), 3.5, 2);

	// 600 dpi: CSS pixels are 96 per inch.
	const raster = await open("", 600 / 96);
	const faces = await raster.locator(".page").all();
	await faces[0].screenshot({ path: path.join(out, "recto-600dpi.png") });
	await faces[1].screenshot({ path: path.join(out, "verso-600dpi.png") });
	await raster.close();

	for (const [query, file] of [["?preview&guides", "apercu-reperes.png"], ["?preview", "apercu.png"]]) {
		const review = await open(query, 3);
		await review.locator("body").screenshot({ path: path.join(out, file) });
		await review.close();
	}
} finally {
	await browser.close();
}
console.log("written to", out);
