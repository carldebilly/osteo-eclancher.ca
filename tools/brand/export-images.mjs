// Renders the site's bitmap brand images with Chrome, from the same drawings as the business
// card: favicons and app icons, the social share cards, and the PNG logo kit for places that
// cannot show SVG (email signatures, profile pictures).
//
//   node tools/brand/export-images.mjs
//
// Needs Google Chrome; run `npm install` in print/carte-affaires first (playwright-core lives
// there). The share cards read the title and location from docs/_data/i18n.yml, so they cannot
// drift from the site again.
import { readFile, writeFile, mkdtemp, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { createRequire } from "node:module";
import { fileURLToPath, pathToFileURL } from "node:url";
import path from "node:path";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(here, "..", "..");
const docs = path.join(root, "docs");
const { chromium } = createRequire(path.join(root, "print", "carte-affaires", "package.json"))("playwright-core");

const fileUrl = file => pathToFileURL(file).href;
const brushSource = await readFile(path.join(here, "brush.js"), "utf8");
const { drawMark, drawSwash } = new Function(`${brushSource}; return { drawMark, drawSwash };`)();

// Two-level lookup in i18n.yml (`fr:` then `  key: "value"`, or one nesting level deeper).
// The file is simple and regular enough that a full YAML parser would only add a dependency.
const i18n = await readFile(path.join(docs, "_data", "i18n.yml"), "utf8");
function i18nValue(lang, key, parent) {
	const block = i18n.split(/^(?=[a-z]{2}:\s*$)/m).find(b => b.startsWith(`${lang}:`)) ?? "";
	const scope = parent ? (block.split(new RegExp(`^  ${parent}:\\s*$`, "m"))[1] ?? "") : block;
	const indent = parent ? "    " : "  ";
	const match = scope.match(new RegExp(`^${indent}${key}:\\s*"([^"]*)"`, "m"));
	if (!match) throw new Error(`i18n.yml has no ${lang}.${parent ? parent + "." : ""}${key}`);
	return match[1];
}

const colours = {
	blue: { brush: "#5F80A6", dots: "#2E4762", accent: "#C5875A" },
	white: { brush: "#FFFFFF", dots: "#FFFFFF", accent: "#E8A77C" },
	onCrane: { brush: "#FFFFFF", dots: "#23405E", accent: "#B9622F" },
};

const fonts = `<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=EB+Garamond:wght@500;600&family=Hanken+Grotesk:wght@600&display=block">`;
// CSS masks are fetched in CORS mode, which local files fail, and a failed mask hides the whole
// element; inlining the mask as a data URI sidesteps the fetch.
const portraitMask = `data:image/svg+xml;base64,${(await readFile(path.join(docs, "assets", "img", "portrait-mask.svg"))).toString("base64")}`;
const crane = `background-color:#8DB2D0;background-image:url(${fileUrl(path.join(docs, "assets", "img", "washi-tile.jpg"))}),linear-gradient(135deg,#BED8EB,#8DB2D0);background-blend-mode:soft-light,normal`;
// Small squares (icons, profile picture) skip the paper grain: invisible at the sizes they are
// shown, and it makes the PNG several times heavier.
const craneFlat = "background:linear-gradient(135deg,#BED8EB,#8DB2D0)";

const browser = await chromium.launch({ channel: "chrome" });
// Each fragment is written to a file and opened from there: a page injected in memory has no
// origin, and Chrome then refuses to load the local photo and texture it references.
const scratch = await mkdtemp(path.join(tmpdir(), "brand-"));

// Renders one HTML fragment at an exact pixel size and returns the screenshot bytes.
async function render(width, height, body, { transparent = false, type = "png" } = {}) {
	const page = await browser.newPage({ viewport: { width, height } });
	const file = path.join(scratch, "render.html");
	await writeFile(file, `<!doctype html><html><head><meta charset="utf-8">${fonts}<style>
		html,body{margin:0;width:${width}px;height:${height}px;overflow:hidden;background:transparent}
		svg{display:block;width:100%;height:100%}
	</style></head><body>${body}</body></html>`);
	await page.goto(fileUrl(file), { waitUntil: "networkidle" });
	await page.evaluate(() => document.fonts.ready);
	const bytes = await page.screenshot({ omitBackground: transparent, type, ...(type === "jpeg" ? { quality: 86 } : {}) });
	await page.close();
	return bytes;
}

// ICO is a small directory of embedded PNGs: a 6-byte header (reserved, type 1 = icon, count),
// one 16-byte entry per image (width, height, palette, reserved, planes, bits per pixel, byte
// size, offset), then the PNG files back to back. Width and height bytes are 0 for 256.
function ico(pngs) {
	const header = Buffer.alloc(6 + 16 * pngs.length);
	header.writeUInt16LE(0, 0);
	header.writeUInt16LE(1, 2);
	header.writeUInt16LE(pngs.length, 4);
	let offset = header.length;
	pngs.forEach(({ size, bytes }, i) => {
		const entry = 6 + 16 * i;
		header.writeUInt8(size % 256, entry);
		header.writeUInt8(size % 256, entry + 1);
		header.writeUInt16LE(1, entry + 4);
		header.writeUInt16LE(32, entry + 6);
		header.writeUInt32LE(bytes.length, entry + 8);
		header.writeUInt32LE(offset, entry + 12);
		offset += bytes.length;
	});
	return Buffer.concat([header, ...pngs.map(p => p.bytes)]);
}

const write = (relative, bytes) => writeFile(path.join(docs, ...relative.split("/")), bytes);

try {
	// Logo kit: transparent PNGs, the signature lockup and a square profile picture.
	await write("assets/img/enso.png", await render(1024, 1024, drawMark(colours.blue), { transparent: true }));
	await write("assets/img/enso-white.png", await render(1024, 1024, drawMark(colours.white), { transparent: true }));
	await write("assets/img/email-signature.png", await render(600, 160, `
		<div style="display:flex;align-items:center;gap:22px;height:160px;padding:0 12px">
			<div style="width:140px;height:140px;flex:none">${drawMark(colours.blue)}</div>
			<div>
				<div style="font:600 46px/1 'EB Garamond',serif;color:#23405E">Julien Éclancher</div>
				<div style="font:600 15.5px 'Hanken Grotesk',sans-serif;letter-spacing:.2em;text-transform:uppercase;color:#9A4F26;margin-top:12px">${i18nValue("fr", "role")}</div>
			</div>
		</div>`, { transparent: true }));
	const onCrane = (size, scale) => `<div style="width:${size}px;height:${size}px;${craneFlat};display:flex;align-items:center;justify-content:center">
		<div style="width:${scale}%;height:${scale}%">${drawMark(colours.onCrane)}</div></div>`;
	await write("assets/img/profile-picture.png", await render(1024, 1024, onCrane(1024, 78)));

	// App icons: solid Crane-blue ground (iOS turns transparency black), mark inside the
	// central safe zone that launchers keep when they round or circle the icon.
	await write("assets/icons/apple-touch-icon.png", await render(180, 180, onCrane(180, 80)));
	await write("assets/icons/icon-192.png", await render(192, 192, onCrane(192, 80)));
	await write("assets/icons/icon-512.png", await render(512, 512, onCrane(512, 80)));

	// Favicons: at 16 to 48 px the column of dots blurs, so the brush keeps a single terracotta dot.
	const favicon = { ...colours.blue, column: "dot" };
	const favicons = [];
	for (const size of [16, 32, 48]) {
		favicons.push({ size, bytes: await render(size, size, drawMark(favicon), { transparent: true }) });
	}
	await write("assets/icons/favicon-32.png", favicons.find(f => f.size === 32).bytes);
	await write("favicon.ico", ico(favicons));

	// Social share cards, one per language, in the card's identity.
	for (const lang of ["fr", "en"]) {
		const card = `<div style="position:relative;width:1200px;height:630px;${crane}">
			<div style="position:absolute;left:84px;top:0;bottom:0;width:560px;display:flex;flex-direction:column;justify-content:center">
				<div style="font:600 17px 'Hanken Grotesk',sans-serif;letter-spacing:.24em;text-transform:uppercase;color:#9A4F26">${i18nValue(lang, "location", "footer")}</div>
				<div style="font:600 74px/1 'EB Garamond',serif;color:#23405E;margin-top:24px;align-self:flex-start;position:relative">Julien Éclancher
					<div style="position:absolute;left:0;right:-14px;top:calc(100% + 6px);height:16px">${drawSwash()}</div></div>
				<div style="font:600 21px 'Hanken Grotesk',sans-serif;letter-spacing:.2em;text-transform:uppercase;color:#9A4F26;margin-top:44px">${i18nValue(lang, "role")}</div>
				<div style="font:500 30px 'EB Garamond',serif;color:#23405E;margin-top:30px">osteo-eclancher.ca</div>
			</div>
			<div style="position:absolute;right:110px;top:50%;width:340px;transform:translateY(-50%);isolation:isolate">
				<div style="position:absolute;z-index:-1;left:50%;top:50%;width:150%;aspect-ratio:1;transform:translate(-50%,-50%);opacity:.55">${drawMark({ brush: "#FFFFFF", column: "none" })}</div>
				<img src="${fileUrl(path.join(docs, "assets", "img", "julien-800.jpg"))}" style="display:block;width:100%;-webkit-mask:url(${portraitMask}) center/100% 100% no-repeat">
			</div>
		</div>`;
		await write(`assets/og/og-${lang}.jpg`, await render(1200, 630, card, { type: "jpeg" }));
	}
} finally {
	await browser.close();
	await rm(scratch, { recursive: true, force: true });
}
console.log("brand images written to", docs);
