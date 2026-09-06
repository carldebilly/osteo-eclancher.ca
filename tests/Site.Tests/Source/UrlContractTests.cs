using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Source;

/// <summary>
/// Locks the published URL set. A URL that changes after launch costs its accumulated
/// ranking and breaks any link pointing at it, so the set is a contract, not a detail.
/// </summary>
public sealed class UrlContractTests
{
	[Fact(DisplayName = "The site publishes exactly the agreed URLs, no more and no fewer")]
	public void Given_the_source_pages_When_collecting_permalinks_Then_they_match_the_agreed_set()
	{
		var expected = Rules.ExpectedUrls.Values
			.SelectMany(static pair => new[] { pair.Fr, pair.En })
			.OrderBy(static url => url, StringComparer.Ordinal)
			.ToArray();

		var actual = SourcePage.All()
			.Where(static page => page.Ref is not null)
			.Select(static page => page.Permalink)
			.OrderBy(static url => url, StringComparer.Ordinal)
			.ToArray();

		Assert.Equal(expected, actual);
	}

	[Fact(DisplayName = "Every page URL ends with a slash, so GitHub Pages never issues a redirect hop")]
	public void Given_the_source_pages_When_reading_permalinks_Then_each_ends_with_a_slash()
	{
		var offenders = SourcePage.All()
			.Where(static page => page.Ref is not null)
			.Where(static page => page.Permalink?.EndsWith('/') != true)
			.Select(static page => $"{page.RelativePath} -> {page.Permalink}")
			.ToArray();

		Assert.Empty(offenders);
	}

	[Fact(DisplayName = "English pages live under /en/ and French pages never do")]
	public void Given_a_page_When_checking_its_permalink_prefix_Then_it_matches_its_language()
	{
		var offenders = new List<string>();

		foreach (var page in SourcePage.All().Where(static page => page.Ref is not null))
		{
			var isEnglishUrl = page.Permalink!.StartsWith("/en/", StringComparison.Ordinal);

			if (isEnglishUrl != (page.Lang == "en"))
			{
				offenders.Add($"{page.RelativePath}: lang={page.Lang} permalink={page.Permalink}");
			}
		}

		Assert.Empty(offenders);
	}

	[Fact(DisplayName = "The custom 404 page keeps its literal filename instead of becoming /404/")]
	public void Given_the_404_page_When_reading_its_permalink_Then_it_is_the_literal_file()
	{
		var notFound = SourcePage.All()
			.SingleOrDefault(static page => page.RelativePath == "docs/404.html");

		Assert.NotNull(notFound);
		Assert.Equal("/404.html", notFound.Permalink);
		Assert.True(notFound.NoIndex, "the 404 page must not be indexed");
		Assert.False(notFound.InSitemap, "the 404 page must stay out of the sitemap");
	}
}
