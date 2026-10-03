# Business card

Source of the printed business card, rendered to the files Vistaprint accepts.

The card is an HTML page (`card.html`) printed to PDF by Chrome, so the type stays vector and the
fonts are the site's own. The brush drawings (the ensō and the stroke under the name) come from
`tools/brand/brush.js`, shared with the website. The washi paper textures come from `textures.py`.

## Build

Needs Node 22, Python 3 with `numpy` and `Pillow`, and Google Chrome.

```powershell
npm install
npm run build        # writes out/
npm test             # checks the PDF sizes and that the drawings are reproducible
```

The first build generates the paper textures into `generated/` (about ten seconds); later builds reuse them.

## The door code

The verso prints the clinic's door code. This repository is public, so the code is not committed:
copy `card.example.json` to `card.local.json` (git-ignored) and put the real code there. Without it,
the card prints the placeholder `0000`.

## Output

| File | Use |
|---|---|
| `carte-julien-eclancher-vistaprint.pdf` | **Upload this one.** Recto then verso, 3.62 × 2.12 in including the 0.06 in bleed. |
| `carte-julien-eclancher-taille-reelle.pdf` | The card as cut, 3.5 × 2 in. Print at 100 % to proofread at real size. |
| `recto-600dpi.png`, `verso-600dpi.png` | Fallback if a PDF upload fails. |
| `apercu.png`, `apercu-reperes.png` | Review sheets; the second shows the cut line (red) and the safety margin (green). |

Text stays at least 0.18 in inside the cut, well within Vistaprint's safety area. Choose an
uncoated (matte) stock: the verso is written on with a pen.

## Things that must not change

- The QR code encodes `https://osteo-eclancher.ca/rdv/`. Cards in circulation depend on it, and
  the site guards that address with a test.
- The drawings are seeded, so a reprint matches the approved proof. A change to `brush.js`
  changes the card; rebuild and compare `apercu.png` before ordering.
