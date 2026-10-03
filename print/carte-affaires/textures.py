"""Paper textures for the card backgrounds, at print resolution.

A flat tint looked lifeless, so the blue and the sand get the feel of washi paper: a soft
cloudy mottling, a fine grain, and a scattering of long pale fibres. Generated as 600 dpi
images at the full-bleed size, so the PDF embeds exactly these pixels instead of letting the
browser rasterise an SVG filter at whatever resolution it picks. Seeded, so every run matches.
"""
from __future__ import annotations

import math
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

DPI = 600
WIDTH, HEIGHT = round(3.62 * DPI), round(2.12 * DPI)


def blurred_noise(rng: np.random.Generator, radius: float) -> np.ndarray:
    """Random field smoothed to a given feature size, normalised to [-1, 1]."""
    # Blurring a small field and scaling it up is far cheaper than blurring at full size,
    # and gives the same soft clouds.
    scale = max(1, int(radius // 4))
    small = rng.random((HEIGHT // scale + 2, WIDTH // scale + 2)).astype(np.float32)
    image = Image.fromarray((small * 255).astype(np.uint8))
    image = image.filter(ImageFilter.GaussianBlur(radius / scale))
    image = image.resize((WIDTH, HEIGHT), Image.BICUBIC)
    field = np.asarray(image, dtype=np.float32)
    field -= field.mean()
    return field / (np.abs(field).max() or 1)


def fibres(seed: int, count: int) -> np.ndarray:
    """Long, thin, gently curved strands, the visible fibres of handmade paper. Values in [0, 1]."""
    rnd = random.Random(seed)
    layer = Image.new("L", (WIDTH, HEIGHT), 0)
    draw = ImageDraw.Draw(layer)
    for _ in range(count):
        x, y = rnd.uniform(0, WIDTH), rnd.uniform(0, HEIGHT)
        heading = rnd.uniform(0, math.tau)
        length = rnd.uniform(60, 260)
        points = [(x, y)]
        for _ in range(int(length // 6)):
            heading += rnd.uniform(-0.12, 0.12)
            x += 6 * math.cos(heading)
            y += 6 * math.sin(heading)
            points.append((x, y))
        draw.line(points, fill=rnd.randint(90, 255), width=rnd.choice((1, 1, 2)))
    return np.asarray(layer.filter(ImageFilter.GaussianBlur(0.6)), dtype=np.float32) / 255


def diagonal_gradient(light: tuple[int, int, int], deep: tuple[int, int, int]) -> np.ndarray:
    """Light at the top-left corner easing into deep at the bottom-right, as an RGB float array."""
    ys, xs = np.mgrid[0:HEIGHT, 0:WIDTH].astype(np.float32)
    # Project each pixel on the corner-to-corner diagonal, measured in inches so the card's
    # aspect ratio does not skew the angle, then ease it so neither end looks banded.
    t = (xs / DPI * 3.62 + ys / DPI * 2.12) / (3.62 ** 2 + 2.12 ** 2)
    t = t * t * (3 - 2 * t)
    light_rgb, deep_rgb = np.array(light, np.float32), np.array(deep, np.float32)
    return light_rgb + (deep_rgb - light_rgb) * t[..., None]


def washi(base: tuple[int, int, int] | np.ndarray, seed: int, mottling: float, grain: float, fibre_lift: float, fibre_count: int) -> Image.Image:
    """Paper texture over a flat colour or over a per-pixel colour array (a gradient)."""
    rng = np.random.default_rng(seed)
    clouds = 0.65 * blurred_noise(rng, 180) + 0.35 * blurred_noise(rng, 45)
    fine = rng.normal(0, 1, (HEIGHT, WIDTH)).astype(np.float32)
    strands = fibres(seed, fibre_count)

    # Lightness offset in 0-255 units, applied equally to R, G and B so the hue never drifts.
    # A negative fibre_lift draws darker strands, which is what shows on a light ground.
    offset = mottling * clouds + grain * fine + fibre_lift * strands
    rgb = np.clip(np.asarray(base, dtype=np.float32) + offset[..., None], 0, 255)
    return Image.fromarray(rgb.astype(np.uint8))


if __name__ == "__main__":
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).parent / "generated"
    out.mkdir(parents=True, exist_ok=True)
    # Crane blue, from a pale sky behind the ensō to a deeper powder blue under the text corner.
    crane = diagonal_gradient(light=(190, 216, 235), deep=(141, 178, 208))
    washi(crane, seed=42, mottling=15, grain=3.5, fibre_lift=34, fibre_count=280).save(out / "crane-washi.png", dpi=(DPI, DPI))
    washi((242, 233, 219), seed=7, mottling=8, grain=2.4, fibre_lift=-14, fibre_count=170).save(out / "sand-washi.png", dpi=(DPI, DPI))
    print("textures written to", out)
