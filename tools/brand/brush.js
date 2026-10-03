// Brush drawings for the card and the site: the ensō mark and the stroke under the name.
// Plain browser script (no module syntax) so card.html can load it with a <script> tag;
// Node callers evaluate it with new Function.
// The only raster logo is 200 px, which prints blurry; these are vectors that scale to any size.
//
// Both are painted by one stiff-brush model. A stroke is many thin "bristles" that follow the
// same path, each at its own offset across the stroke's width. Three things make it read as
// paint rather than a vector shape:
// - paint lines: narrow channels of bare paper that open up along the stroke, start early and
//   widen as the brush dries, so the stroke splits into bundles of bristles;
// - dry skips: bristles break off briefly, more and more often towards the end;
// - the lift: bristles run out one by one and fade, so the tail dissolves instead of stopping.
// Every drawing uses a seeded PRNG, so each render is identical.

function seededRandom(seed) {
	return () => ((seed = (seed * 1664525 + 1013904223) >>> 0) / 4294967296);
}

function smoothStep(a, b, t) {
	const k = Math.min(1, Math.max(0, (t - a) / (b - a)));
	return k * k * (3 - 2 * k);
}

// Paints one brush stroke and returns its SVG polylines (the colour comes from the parent <g>).
// path(t) gives the centre line as { x, y, nx, ny }, the normal pointing to offset +0.5.
// envelope(t) is the full width at t; endOf(offset) is where each bristle runs out of ink.
function paintStroke({ rand, path, envelope, bristles, steps, startOf, endOf, skipChance, gapLength, channels, opacity, width, strokeFade }) {
	const polylines = [];
	for (let i = 0; i < bristles; i++) {
		// -0.5 and +0.5 are the two edges of the stroke.
		const offset = i / (bristles - 1) - 0.5 + (rand() - 0.5) * 0.008;
		const start = startOf(offset), end = endOf(offset);
		const lineWidth = width[0] + rand() * (width[1] - width[0]);
		const lineOpacity = opacity[0] + rand() * (opacity[1] - opacity[0]);
		// Each bristle wanders a little across the stroke, so bundles drift instead of running as
		// perfect parallel lines.
		const phase = rand() * Math.PI * 2, freq = 2 + rand() * 4, drift = 0.003 + rand() * 0.006;
		// A bristle fades and thins over the last stretch of its own run. Opacity is per polyline,
		// so the bristle is drawn as short chunks that each carry the fade at their midpoint.
		const ownFade = t => Math.min(1, Math.max(0.2, (end - t) / 0.06));

		let points = [], chunkStart = start, gapUntil = -1;
		const flush = (t, keepLast) => {
			if (points.length > 1) {
				const mid = (chunkStart + t) / 2;
				const fade = ownFade(mid) * strokeFade(mid);
				polylines.push(`<polyline points="${points.join(" ")}" stroke-width="${(lineWidth * (0.5 + 0.5 * fade)).toFixed(2)}" stroke-opacity="${(lineOpacity * fade).toFixed(2)}"/>`);
			}
			points = keepLast && points.length ? [points[points.length - 1]] : [];
			chunkStart = t;
		};
		for (let step = 0; step <= steps; step++) {
			const t = start + (end - start) * step / steps;
			const wandered = offset + drift * Math.sin(t * Math.PI * 2 * freq + phase);
			// Inside an open paint line the paper shows through: no ink here.
			if (channels.some(c => t > c.from && Math.abs(wandered - c.offset) < c.halfWidth(t))) {
				flush(t, false);
				continue;
			}
			if (t < gapUntil) continue;
			if (rand() < skipChance(t)) {
				flush(t, false);
				gapUntil = t + gapLength(t);
				continue;
			}
			const p = path(t), w = envelope(t);
			points.push(`${(p.x + p.nx * wandered * w).toFixed(2)},${(p.y + p.ny * wandered * w).toFixed(2)}`);
			if (points.length >= 7) flush(t, true);
		}
		flush(end, false);
	}
	return polylines;
}

// Paint lines: channels that open at a random point along the stroke, widen quickly to a
// visible line, then keep widening slowly as the brush dries.
function paintLines(rand, { count, from, to, maxHalfWidth }) {
	return Array.from({ length: count }, () => {
		const opensAt = from + rand() * (to - from);
		const peak = maxHalfWidth * (0.4 + rand() * 0.6);
		return {
			offset: -0.42 + rand() * 0.84,
			from: opensAt,
			halfWidth: t => 0.006 + peak * (0.6 * smoothStep(opensAt, opensAt + 0.12, t) + 0.4 * smoothStep(opensAt + 0.12, 1, t)),
		};
	});
}

// The ensō with the column of dots from the original logo. viewBox 0 0 100 100.
function drawMark({ brush = "#5F80A6", dots = "#3B5A7D", accent = "#D9946A" } = {}) {
	const rand = seededRandom(1142);
	const cx = 50, cy = 50, radius = 34.5;
	// Angles are clockwise from 12 o'clock. The brush lands at the upper right and travels
	// counter-clockwise for 334 degrees, so the dry tail stops just short of the landing.
	const startAngle = 50, sweep = -334;
	// A slow wobble keeps the circle hand-drawn rather than compass-drawn.
	const wobble = t => 0.45 * Math.sin(t * Math.PI * 2 * 2.3 + 0.8) + 0.35 * Math.sin(t * Math.PI * 2 * 5.1);
	const path = t => {
		const a = (startAngle + sweep * t) * Math.PI / 180, r = radius + wobble(t);
		return { x: cx + r * Math.sin(a), y: cy - r * Math.cos(a), nx: Math.sin(a), ny: -Math.cos(a) };
	};
	// The loaded brush lands wide, settles to an even body, then lifts to about half its width.
	const envelope = t => 11.5
		* (1 + 0.28 * (1 - smoothStep(0, 0.14, t)))
		* (1 - 0.5 * smoothStep(0.62, 1, t))
		* (1 + 0.05 * Math.sin(t * Math.PI * 2 * 3.7));

	const strokes = paintStroke({
		rand, path, envelope, bristles: 70, steps: 340,
		startOf: offset => 0.012 + rand() * 0.006 + offset * offset * 0.03,
		// Inner bristles (negative offset) run dry first; the outer edge carries the stroke round.
		endOf: offset => Math.min(1, 0.6 + (offset + 0.5) * 0.32 + rand() * 0.18),
		skipChance: t => 0.002 + 0.018 * smoothStep(0.35, 0.7, t) + 0.04 * smoothStep(0.7, 1, t),
		gapLength: t => 0.008 + rand() * (0.01 + 0.05 * smoothStep(0.4, 1, t)),
		channels: paintLines(rand, { count: 14, from: 0.12, to: 0.65, maxHalfWidth: 0.026 }),
		opacity: [0.88, 1], width: [0.45, 0.85],
		strokeFade: t => 1 - 0.65 * smoothStep(0.86, 1, t),
	});

	// The landing (the "boule"): ink pools where the brush first presses, a rounded blot a little
	// wider than the stroke, stretched along the direction of travel, its rim slightly irregular
	// so it does not look stamped.
	const landing = path(0.02), blotRadius = envelope(0) * 0.56;
	const blot = Array.from({ length: 48 }, (_, k) => {
		const a = k / 48 * Math.PI * 2;
		const r = blotRadius * (1 + 0.05 * Math.sin(a * 3 + 1) + (rand() - 0.5) * 0.06);
		const along = Math.cos(a) * r * 1.15, across = Math.sin(a) * r;
		// Tangent of the circle at the landing, perpendicular to the normal.
		const tx = landing.ny, ty = -landing.nx;
		return `${(landing.x + tx * along + landing.nx * across).toFixed(2)},${(landing.y + ty * along + landing.ny * across).toFixed(2)}`;
	}).join(" ");

	// A few drops flicked off as the brush lifts, scattered just past the tail.
	const drops = Array.from({ length: 9 }, () => {
		const p = path(0.96 + rand() * 0.06), spread = (rand() - 0.4) * 7;
		return `<ellipse cx="${(p.x + p.nx * spread).toFixed(2)}" cy="${(p.y + p.ny * spread).toFixed(2)}" rx="${(0.25 + rand() * 0.55).toFixed(2)}" ry="${(0.2 + rand() * 0.35).toFixed(2)}" fill="${brush}" fill-opacity="${(0.6 + rand() * 0.4).toFixed(2)}"/>`;
	}).join("");

	const x = 50;
	return `
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" role="img" aria-label="Logo">
	<polygon points="${blot}" fill="${brush}"/>
	<g fill="none" stroke="${brush}" stroke-linecap="round" stroke-linejoin="round">${strokes.join("")}</g>
	${drops}
	<circle cx="${x}" cy="25" r="2.7" fill="${accent}"/>
	<circle cx="${x}" cy="35" r="0.95" fill="${dots}"/>
	<circle cx="${x}" cy="41.3" r="1.35" fill="${dots}"/>
	<circle cx="${x}" cy="48.6" r="2.05" fill="${dots}"/>
	<line x1="${x}" y1="54.5" x2="${x}" y2="64" stroke="${dots}" stroke-width="0.55" stroke-linecap="round"/>
	<circle cx="${x}" cy="69.8" r="1.9" fill="none" stroke="${dots}" stroke-width="0.6"/>
	<line x1="${x}" y1="75.2" x2="${x}" y2="95" stroke="${accent}" stroke-width="0.55" stroke-linecap="round"/>
</svg>`;
}

// The stroke under the practitioner's name: a thin horizontal line of the same brush, landing
// on the left and lifting dry on the right, with paint lines along its length. viewBox 0 0 200 12.
function drawSwash({ brush = "#FFFFFF" } = {}) {
	const rand = seededRandom(4242);
	// A gentle upward bow with a slight rise; the normal points down (offset +0.5 is the lower edge).
	const path = t => ({ x: 3 + 194 * t, y: 6.4 - 1.2 * Math.sin(Math.PI * t) - 0.8 * t, nx: 0, ny: 1 });
	const envelope = t => 7 * (1 + 0.15 * (1 - smoothStep(0, 0.08, t))) * (1 - 0.45 * smoothStep(0.55, 1, t));

	const strokes = paintStroke({
		rand, path, envelope, bristles: 38, steps: 260,
		startOf: offset => rand() * 0.01 + offset * offset * 0.02,
		// The upper edge runs dry first, the way the ensō's inner edge does.
		endOf: offset => Math.min(1, 0.62 + (offset + 0.5) * 0.28 + rand() * 0.12),
		skipChance: t => 0.003 + 0.015 * smoothStep(0.3, 0.7, t) + 0.04 * smoothStep(0.7, 1, t),
		gapLength: t => 0.008 + rand() * (0.01 + 0.04 * smoothStep(0.4, 1, t)),
		channels: paintLines(rand, { count: 9, from: 0.02, to: 0.45, maxHalfWidth: 0.06 }),
		opacity: [0.8, 1], width: [0.35, 0.7],
		strokeFade: t => 1 - 0.6 * smoothStep(0.85, 1, t),
	});

	return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 12" preserveAspectRatio="none" aria-hidden="true">
	<g fill="none" stroke="${brush}" stroke-linecap="round" stroke-linejoin="round">${strokes.join("")}</g>
</svg>`;
}
