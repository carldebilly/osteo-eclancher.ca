"""Seamless washi paper tile for the website.

The business card's textures are one fixed sheet; a web page scrolls, so the site needs a tile
that repeats without a visible seam. The tile is neutral grey (128 = no change) meant for CSS
`background-blend-mode: soft-light`, so one tile textures any colour, cream or blue gradient alike.
Saved as JPEG: random grain is close to incompressible for PNG, and a mild JPEG loss on noise
is invisible.

Large soft clouds are what give a repeating tile away, so they stay faint here; the grain and
the fibres carry the paper feel.

    python tools/brand/washi_tile.py docs/assets/img/washi-tile.jpg
"""
from __future__ import annotations

import math
import random
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SIZE = 800


def seamless_clouds(rng: np.random.Generator, radius: float) -> np.ndarray:
    # Blur a 3x3 repetition of the same field and keep the centre: features that cross an
    # edge continue on the opposite side, so the tile wraps without a seam.
    field = rng.random((SIZE, SIZE)).astype(np.float32)
    tiled = Image.fromarray((np.tile(field, (3, 3)) * 255).astype(np.uint8))
    blurred = np.asarray(tiled.filter(ImageFilter.GaussianBlur(radius)), dtype=np.float32)
    centre = blurred[SIZE:2 * SIZE, SIZE:2 * SIZE]
    centre -= centre.mean()
    return centre / (np.abs(centre).max() or 1)


def seamless_fibres(seed: int, count: int) -> np.ndarray:
    # Each fibre is drawn at its position and shifted by one tile in every direction, so a
    # strand leaving one edge re-enters from the other.
    rnd = random.Random(seed)
    layer = Image.new("L", (SIZE, SIZE), 0)
    draw = ImageDraw.Draw(layer)
    for _ in range(count):
        x, y = rnd.uniform(0, SIZE), rnd.uniform(0, SIZE)
        heading = rnd.uniform(0, math.tau)
        points = [(x, y)]
        for _ in range(int(rnd.uniform(40, 170) // 4)):
            heading += rnd.uniform(-0.12, 0.12)
            x += 4 * math.cos(heading)
            y += 4 * math.sin(heading)
            points.append((x, y))
        shade = rnd.randint(110, 255)
        for dx in (-SIZE, 0, SIZE):
            for dy in (-SIZE, 0, SIZE):
                draw.line([(px + dx, py + dy) for px, py in points], fill=shade, width=1)
    return np.asarray(layer.filter(ImageFilter.GaussianBlur(0.5)), dtype=np.float32) / 255


def tile(seed: int, mottling: float, grain: float, fibre_lift: float, fibre_count: int) -> Image.Image:
    rng = np.random.default_rng(seed)
    clouds = 0.5 * seamless_clouds(rng, 40) + 0.5 * seamless_clouds(rng, 12)
    fine = rng.normal(0, 1, (SIZE, SIZE)).astype(np.float32)
    value = 128 + mottling * clouds + grain * fine + fibre_lift * seamless_fibres(seed, fibre_count)
    return Image.fromarray(np.clip(value, 0, 255).astype(np.uint8))


if __name__ == "__main__":
    target = sys.argv[1] if len(sys.argv) > 1 else "washi-tile.jpg"
    tile(seed=42, mottling=12, grain=6, fibre_lift=60, fibre_count=110).save(target, quality=80, optimize=True, progressive=True)
    print("tile written to", target)
