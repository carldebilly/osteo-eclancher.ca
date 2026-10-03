"""Generate the portrait images the site serves.

The portrait is a large file kept out of the repository, so its path is supplied on each run:

    python tools/images/build-images.py --portrait "<path to the high-resolution portrait>"

The logo, the favicons and app icons, and the social share cards are rendered from the brand
drawings instead: see tools/brand/export-images.mjs. The original raster logo is kept in
tools/images/sources/ for reference only.

Outputs are committed, so the site needs no build step for images and GitHub Pages
serves them straight from docs/.
"""

from __future__ import annotations

import argparse
import os
import sys

from PIL import Image


def ensure_dir(path: str) -> None:
    os.makedirs(path, exist_ok=True)


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


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--portrait", required=True, help="High-resolution portrait, at least 800px wide")
    parser.add_argument("--out", default="docs")
    args = parser.parse_args()

    if not os.path.exists(args.portrait):
        print(f"Missing source file: {args.portrait}", file=sys.stderr)
        return 1

    print("Portrait:")
    build_portrait(args.portrait, args.out)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
