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
links, the sitemap and typography. Both run on every pull request. A third category, `Network`, actually calls the external
hosts the resource pages link to; it is excluded from the pull-request check because a rate
limit at someone else's door must not block a change. Run it on purpose with
`dotnet test tests/Site.Tests --filter "Category=Network"`.

## Content conventions

Two decisions worth knowing before editing the pages.

**Facts repeat across pages on purpose.** A visitor reads one page, not the site, so the
insurance caveat and the clinic address appear wherever someone needs them rather than once
with links. This is the client's call, and it is why a normalisation review of the content
pages will find repetition and should leave it.

**Every external claim is checked against the organisation's own pages, not a search result.**
The resource pages link out and describe what each organisation does; anything not verified at
the source gets removed rather than hedged. A phone number nobody confirmed fails the person
the page exists for. `dotnet test --filter "Category=Network"` checks the links still answer;
it does not check that the destinations still say what we claim.

## Analytics

Google Analytics 4 runs in consent-denied mode (`_includes/analytics.html`): no cookie, no stored identifier, page views and booking clicks only. The measurement id lives in `_config.yml` (`analytics_id`); remove the key to build without the tag. The `gtag('consent', 'default', …)` call must stay before `gtag('config', …)` — a test enforces the order, because the standard snippet Google hands out omits it and would start writing cookies the privacy page says do not exist.

Every booking button is rendered by `_includes/cta-booking.html` and must pass a `placement` (`nav`, `hero`, `contact`, `band`). Clicks are reported as one `book_click` event carrying that placement, so the buttons can be compared in GA4. Adding a new button means adding its placement to `AnalyticsTests.KnownPlacements` too.

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
