# osteo-eclancher.ca

Bilingual site for Julien Éclancher, osteopathy intern in Verdun, Montréal.

Jekyll sources live in `docs/` and are built by GitHub Pages itself on every push to `master`
("deploy from a branch", source `master:/docs`). There is no deploy workflow to maintain.

## Working on it

Preview locally, with the same container GitHub runs, so a build that works here works there:

```powershell
./tools/preview.ps1 -Serve      # http://localhost:8080/
```

Check everything:

```powershell
dotnet test tests/Site.Tests
```

The suite has two halves. `Category!=Output` reads the sources and needs nothing built: it
locks the URL set, pairs each page with its translation, bounds titles and descriptions, and
fails on copy that would breach the RITMA code of ethics. `Category=Output` reads `_site/` and
checks what visitors actually get: reciprocal hreflang, canonicals, structured data, internal
links, the sitemap and typography. Both run on every pull request.

## Where things are

| Path | What |
|---|---|
| `docs/_data/business.yml` | Address, phone, booking links, RITMA number. Single source for the page, the footer and the JSON-LD. |
| `docs/_data/i18n.yml` | Interface strings. The `fr` and `en` key trees must match. |
| `docs/*.md`, `docs/en/*.md` | Page content. Each pair shares a `ref`, which is what produces hreflang and the language switcher. |
| `docs/_includes/` | Head, header, footer, booking button, contact block, FAQ, JSON-LD. |
| `tools/images/build-images.py` | Regenerates every image, icon and social card from `tools/images/sources/`. |

## Adding a page

Add the French file and its English twin with the same `ref`, give both a `permalink`,
`title`, `description` and matching `nav_order`, then extend `Rules.ExpectedUrls` in
`tests/Site.Tests/Support/Rules.cs`. The tests will tell you what is missing.
