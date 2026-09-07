"""Generate every image the site serves.

The logo lives in tools/images/sources/. The portrait is a large file kept out of the
repository, so its path is supplied on each run:

    python tools/images/build-images.py --portrait "<path to the high-resolution portrait>"

Outputs are committed, so the site needs no build step for images and GitHub Pages
serves them straight from docs/.
"""

from __future__ import annotations

import argparse
import os
import sys
import urllib.request

from PIL import Image, ImageDraw, ImageFont, ImageOps

CREAM = "#F5F0E8"
BLUE = "#3B5A7D"
TERRA = "#C5875A"
MUTED = "#6E675C"

# Google Fonts ships the variable EB Garamond used by the site; the Open Graph card is the
# only place we render type into a bitmap, so it is fetched on demand rather than vendored.
GARAMOND_URL = (
    "https://raw.githubusercontent.com/google/fonts/main/ofl/ebgaramond/EBGaramond%5Bwght%5D.ttf"
)

OG_LINES = {
    "fr": ("Julien Éclancher", "Ostéopathe I.O. · Verdun, Montréal"),
    "en": ("Julien Éclancher", "Osteopath I.O. · Verdun, Montréal"),
}


def ensure_dir(path: str) -> None:
    os.makedirs(path, exist_ok=True)


def load_serif(size: int, cache_dir: str) -> ImageFont.FreeTypeFont:
    """EB Garamond if it can be fetched or is cached, otherwise the closest Windows serif."""
    cached = os.path.join(cache_dir, "EBGaramond.ttf")

    if not os.path.exists(cached):
        try:
            ensure_dir(cache_dir)
            urllib.request.urlretrieve(GARAMOND_URL, cached)
        except Exception as error:  # noqa: BLE001 - any network problem falls back to a local font
            print(f"  EB Garamond unavailable ({error}); falling back to a system serif")

    if os.path.exists(cached):
        font = ImageFont.truetype(cached, size)
        try:
            font.set_variation_by_axes([500])  # medium, matching the site headings
        except Exception:  # noqa: BLE001 - static builds have no variation axes
            pass
        return font

    for candidate in ("georgia.ttf", "constan.ttf", "times.ttf"):
        path = os.path.join(os.environ.get("WINDIR", r"C:\Windows"), "Fonts", candidate)
        if os.path.exists(path):
            return ImageFont.truetype(path, size)

    return ImageFont.load_default()


def build_portrait(source: str, out_root: str) -> None:
    image = Image.open(source).convert("RGB")
    ratio = image.height / image.width
    target = os.path.join(out_root, "assets", "img")
    ensure_dir(target)

    for width in (400, 800):
        height = round(width * ratio)
        resized = image.resize((width, height), Image.LANCZOS)
        resized.save(os.path.join(target, f"julien-{width}.webp"), "WEBP", quality=82, method=6)
        print(f"  julien-{width}.webp  {width}x{height}")

        if width == 800:
            # JPEG rather than PNG for the fallback: a photo as PNG would be about a megabyte.
            # This file doubles as the Open Graph source and the schema.org image.
            resized.save(
                os.path.join(target, "julien-800.jpg"),
                "JPEG",
                quality=85,
                optimize=True,
                progressive=True,
            )
            print(f"  julien-800.jpg   {width}x{height}")


def build_logo(source: str, out_root: str) -> None:
    logo = Image.open(source).convert("RGBA")
    target = os.path.join(out_root, "assets", "img")
    ensure_dir(target)

    # Rendered at 42 CSS pixels, so 84 covers a 2x display without shipping the 200px original.
    doubled = logo.resize((84, 84), Image.LANCZOS)
    doubled.save(os.path.join(target, "logo-84.webp"), "WEBP", lossless=True, method=6)
    doubled.save(os.path.join(target, "logo-84.png"), "PNG", optimize=True)
    print("  logo-84.webp / logo-84.png  84x84")


def build_icons(source: str, out_root: str) -> None:
    logo = Image.open(source).convert("RGBA")
    icons = os.path.join(out_root, "assets", "icons")
    ensure_dir(icons)

    for size in (32, 192, 512):
        name = "favicon-32.png" if size == 32 else f"icon-{size}.png"
        logo.resize((size, size), Image.LANCZOS).save(os.path.join(icons, name), "PNG", optimize=True)
        print(f"  {name}  {size}x{size}")

    # iOS flattens transparency to black, so the touch icon gets the site background baked in.
    touch = Image.new("RGB", (180, 180), CREAM)
    scaled = logo.resize((180, 180), Image.LANCZOS)
    touch.paste(scaled, (0, 0), scaled)
    touch.save(os.path.join(icons, "apple-touch-icon.png"), "PNG", optimize=True)
    print("  apple-touch-icon.png  180x180")

    logo.resize((48, 48), Image.LANCZOS).save(
        os.path.join(out_root, "favicon.ico"), format="ICO", sizes=[(16, 16), (32, 32), (48, 48)]
    )
    print("  favicon.ico  16/32/48")


def build_open_graph(portrait_source: str, out_root: str, cache_dir: str) -> None:
    target = os.path.join(out_root, "assets", "og")
    ensure_dir(target)

    portrait = Image.open(portrait_source).convert("RGB")
    photo = ImageOps.fit(portrait, (470, 630), Image.LANCZOS, centering=(0.5, 0.25))

    name_font = load_serif(74, cache_dir)
    role_font = load_serif(34, cache_dir)
    domain_font = load_serif(28, cache_dir)

    for lang, (name, role) in OG_LINES.items():
        card = Image.new("RGB", (1200, 630), CREAM)
        card.paste(photo, (730, 0))

        draw = ImageDraw.Draw(card)
        draw.text((80, 210), name, font=name_font, fill=BLUE)
        draw.text((80, 320), role, font=role_font, fill=TERRA)
        draw.text((80, 400), "osteo-eclancher.ca", font=domain_font, fill=MUTED)
        draw.line([(80, 190), (200, 190)], fill=TERRA, width=2)

        path = os.path.join(target, f"og-{lang}.jpg")
        card.save(path, "JPEG", quality=88, optimize=True, progressive=True)
        print(f"  og-{lang}.jpg  1200x630")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--portrait", required=True, help="High-resolution portrait, at least 800px wide")
    parser.add_argument("--logo", default=os.path.join("tools", "images", "sources", "logo-mark.png"))
    parser.add_argument("--out", default="docs")
    parser.add_argument("--cache", default=os.path.join("tools", "images", ".cache"))
    args = parser.parse_args()

    for path in (args.portrait, args.logo):
        if not os.path.exists(path):
            print(f"Missing source file: {path}", file=sys.stderr)
            return 1

    print("Portrait:")
    build_portrait(args.portrait, args.out)
    print("Logo:")
    build_logo(args.logo, args.out)
    print("Icons:")
    build_icons(args.logo, args.out)
    print("Open Graph cards:")
    build_open_graph(args.portrait, args.out, args.cache)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
