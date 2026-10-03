// Photo mask for the site's portrait, painted with the same stiff-brush look as brush.js.
// Plain browser script, like brush.js; Node callers evaluate it with new Function.

// Mask for the portrait in its own proportions: a rounded rectangle whose rim is painted with
// the stiff brush, so the photo keeps room to breathe instead of being boxed in a circle.
// The perimeter is sampled with its outward normal; short strokes follow it at small, varying
// distances from the solid core, fraying the edge. viewBox width is 100, height follows the photo.
function drawBrushRoundRect({ aspect = 924 / 800, cornerRadius = 14 } = {}) {
	let seed = 9090;
	const rand = () => ((seed = (seed * 1664525 + 1013904223) >>> 0) / 4294967296);
	const inset = 1.8, width = 100, height = 100 * aspect;
	const x0 = inset, y0 = inset, x1 = width - inset, y1 = height - inset, r = cornerRadius;

	// Rounded-rectangle outline as [x, y, normalX, normalY], walked clockwise from the top edge.
	const outline = [];
	const straight = (ax, ay, bx, by, nx, ny, n) => {
		for (let i = 0; i < n; i++) outline.push([ax + (bx - ax) * i / n, ay + (by - ay) * i / n, nx, ny]);
	};
	const corner = (cx, cy, from, n) => {
		for (let i = 0; i < n; i++) {
			const a = from + (Math.PI / 2) * i / n;
			outline.push([cx + r * Math.cos(a), cy + r * Math.sin(a), Math.cos(a), Math.sin(a)]);
		}
	};
	straight(x0 + r, y0, x1 - r, y0, 0, -1, 60); corner(x1 - r, y0 + r, -Math.PI / 2, 40);
	straight(x1, y0 + r, x1, y1 - r, 1, 0, 70); corner(x1 - r, y1 - r, 0, 40);
	straight(x1 - r, y1, x0 + r, y1, 0, 1, 60); corner(x0 + r, y1 - r, Math.PI / 2, 40);
	straight(x0, y1 - r, x0, y0 + r, -1, 0, 70); corner(x0 + r, y0 + r, Math.PI, 40);

	const strokes = [];
	for (let i = 0; i < 110; i++) {
		const start = Math.floor(rand() * outline.length);
		const length = 6 + Math.floor(rand() * 26);
		const push = (rand() - 0.3) * 2.6;
		const points = [];
		for (let k = 0; k <= length; k += 2) {
			const [x, y, nx, ny] = outline[(start + k) % outline.length];
			// The distance from the edge drifts along the stroke, like a hand-held brush.
			const d = push + Math.sin(k / length * Math.PI) * (rand() - 0.5) * 0.8;
			points.push(`${(x + nx * d).toFixed(1)},${(y + ny * d).toFixed(1)}`);
		}
		strokes.push(`<polyline points="${points.join(" ")}" stroke-width="${(0.4 + rand() * 1.6).toFixed(1)}" stroke-opacity="${(0.45 + rand() * 0.55).toFixed(1)}"/>`);
	}
	return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${width} ${height.toFixed(1)}" preserveAspectRatio="none">
	<rect x="${x0}" y="${y0}" width="${x1 - x0}" height="${(y1 - y0).toFixed(1)}" rx="${r}" fill="#fff"/>
	<g fill="none" stroke="#fff" stroke-linecap="round" stroke-linejoin="round">${strokes.join("")}</g>
</svg>`;
}
