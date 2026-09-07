using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Output;

/// <summary>
/// Checks what the built pages actually declare about themselves and about each other.
/// </summary>
/// <remarks>
/// Reciprocity is the part worth testing. Google ignores an hreflang annotation that the
/// other page does not confirm, and a one-way link produces no error anywhere: the English
/// pages simply never surface. The source tests cannot catch it, because the failure lives
/// in the Liquid lookup rather than in the front matter.
/// </remarks>
[Trait("Category", "Output")]
public sealed class HeadTests
{
	[Theory(DisplayName = "Each page declares French, English and x-default alternates as absolute URLs")]
	[MemberData(nameof(SiteOutput.IndexablePageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_alternates_Then_all_three_are_declared(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		var alternates = document
			.QuerySelectorAll("link[rel=alternate][hreflang]")
			.ToDictionary(
				link => link.GetAttribute("hreflang")!,
				link => link.GetAttribute("href")!);

		Assert.Equal(["en-CA", "fr-CA", "x-default"], alternates.Keys.OrderBy(static key => key, StringComparer.Ordinal));
		Assert.All(alternates.Values, value => Assert.StartsWith(SiteOutput.Origin, value, StringComparison.Ordinal));
		Assert.Equal(alternates["fr-CA"], alternates["x-default"]);
	}

	[Theory(DisplayName = "Each page lists itself among its own alternates")]
	[MemberData(nameof(SiteOutput.IndexablePageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_alternates_Then_it_includes_itself(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		var alternates = document
			.QuerySelectorAll("link[rel=alternate][hreflang]")
			.Select(static link => link.GetAttribute("href")!)
			.ToArray();

		Assert.Contains(SiteOutput.Origin + url, alternates);
	}

	[Theory(DisplayName = "The page each alternate points to exists and points back, so the pairing is reciprocal")]
	[MemberData(nameof(SiteOutput.IndexablePageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_following_its_twin_Then_the_twin_points_back(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		var twinUrl = document
			.QuerySelectorAll("link[rel=alternate][hreflang]")
			.Select(static link => link.GetAttribute("href")![SiteOutput.Origin.Length..])
			.First(candidate => candidate != url);

		var twinFile = SiteOutput.IndexablePages().SingleOrDefault(file => SiteOutput.UrlOf(file) == twinUrl);

		Assert.True(twinFile is not null, $"{url} points at {twinUrl}, which was not built");

		var twinAlternates = SiteOutput.Parse(twinFile!)
			.QuerySelectorAll("link[rel=alternate][hreflang]")
			.Select(static link => link.GetAttribute("href")!)
			.ToArray();

		Assert.Contains(SiteOutput.Origin + url, twinAlternates);
	}

	[Theory(DisplayName = "Each page's canonical URL is its own address, never another page's")]
	[MemberData(nameof(SiteOutput.IndexablePageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_canonical_Then_it_is_its_own_address(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));
		var canonicals = document.QuerySelectorAll("link[rel=canonical]").ToArray();

		Assert.Single(canonicals);
		Assert.Equal(SiteOutput.Origin + url, canonicals[0].GetAttribute("href"));
	}

	[Theory(DisplayName = "The document language and the Open Graph locale agree with the URL")]
	[MemberData(nameof(SiteOutput.IndexablePageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_language_Then_it_matches_its_url(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));
		var expectedLang = url.StartsWith("/en/", StringComparison.Ordinal) ? "en" : "fr";

		Assert.Equal(expectedLang, document.DocumentElement.GetAttribute("lang"));
		Assert.Equal(
			expectedLang == "en" ? "en_CA" : "fr_CA",
			document.QuerySelector("meta[property='og:locale']")?.GetAttribute("content"));
	}

	[Theory(DisplayName = "Each page carries the title, description and social card a search result needs")]
	[MemberData(nameof(SiteOutput.IndexablePageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_metadata_Then_nothing_essential_is_missing(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		Assert.False(string.IsNullOrWhiteSpace(document.Title));

		var description = document.QuerySelector("meta[name=description]")?.GetAttribute("content");
		Assert.False(string.IsNullOrWhiteSpace(description), $"{url} has no meta description");

		var image = document.QuerySelector("meta[property='og:image']")?.GetAttribute("content");
		Assert.False(string.IsNullOrWhiteSpace(image), $"{url} has no Open Graph image");
	}

	[Fact(DisplayName = "The 404 page asks not to be indexed and offers a way out in both languages")]
	public void Given_the_404_page_When_reading_it_Then_it_is_noindex_and_bilingual()
	{
		var document = SiteOutput.Parse(Path.Combine(SiteOutput.RequireDirectory(), "404.html"));

		var robots = document.QuerySelector("meta[name=robots]")?.GetAttribute("content") ?? "";
		Assert.Contains("noindex", robots, StringComparison.Ordinal);

		var links = document.QuerySelectorAll("main a[href]").Select(static a => a.GetAttribute("href")).ToArray();
		Assert.Contains("/", links);
		Assert.Contains("/en/", links);
	}
}
