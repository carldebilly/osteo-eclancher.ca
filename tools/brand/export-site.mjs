// Writes the brand assets the website uses into docs/assets/img/, from the same generators as
// the business card. Run after changing brush.js, portrait-mask.js or washi_tile.py:
//
//   node tools/brand/export-site.mjs
import { readFile, writeFile } from "node:fs/promises";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import path from "node:path";

const here = path.dirname(fileURLToPath(import.meta.url));
const target = path.join(here, "..", "..", "docs", "assets", "img");

// brush.js and portrait-mask.js are plain browser scripts (card.html loads them with <script>),
// so they are evaluated here rather than imported.
const source = await readFile(path.join(here, "brush.js"), "utf8") + await readFile(path.join(here, "portrait-mask.js"), "utf8");
const { drawMark, drawSwash, drawBrushRoundRect } = new Function(`${source}; return { drawMark, drawSwash, drawBrushRoundRect };`)();

const assets = {
	// Header logo: the site's blue brush on cream.
	"enso.svg": drawMark({ brush: "#5F80A6", dots: "#2E4762", accent: "#C5875A", detail: "web" }),
	// Halo behind the portrait: the white brush alone, without the dots.
	"enso-halo.svg": drawMark({ brush: "#FFFFFF", detail: "web", column: "none" }),
	"brush-under.svg": drawSwash({ detail: "web" }),
	"portrait-mask.svg": drawBrushRoundRect(),
};
for (const [name, svg] of Object.entries(assets)) {
	await writeFile(path.join(target, name), svg.trim() + "\n");
}

execFileSync("python", [path.join(here, "washi_tile.py"), path.join(target, "washi-tile.jpg")], { stdio: "inherit" });
console.log("brand assets written to", target);
