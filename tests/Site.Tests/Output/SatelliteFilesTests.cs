using System.Xml.Linq;
using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Output;

/// <summary>
/// Checks the files nobody visits but every crawler reads: the sitemap, robots.txt,
/// llms.txt, the web manifest and the icons.
/// </summary>
[Trait("Category", "Output")]
public sealed class SatelliteFilesTests
{
	[Fact(DisplayName = "The sitemap lists every indexable page and nothing else")]
	public void Given_the_built_site_When_reading_the_sitemap_Then_it_matches_the_indexable_pages()
	{
		var sitemap = SiteOutput.ReadOrNull("sitemap.xml");

		Assert.NotNull(sitemap);

		var listed = XDocument.Parse(sitemap)
			.Descendants()
			.Where(static element => element.Name.LocalName == "loc")
			.Select(static element => element.Value)
			.OrderBy(static url => url, StringComparer.Ordinal)
			.ToArray();

		var expected = SiteOutput.IndexablePages()
			.Select(static file => SiteOutput.Origin + SiteOutput.UrlOf(file))
			.OrderBy(static url => url, StringComparer.Ordinal)
			.ToArray();

		Assert.Equal(expected, listed);
	}

	[Fact(DisplayName = "robots.txt allows crawling and points at the sitemap")]
	public void Given_the_built_site_When_reading_robots_Then_it_allows_and_points_at_the_sitemap()
	{
		var robots = SiteOutput.ReadOrNull("robots.txt");

		Assert.NotNull(robots);
		Assert.Contains("Sitemap: " + SiteOutput.Origin + "/sitemap.xml", robots, StringComparison.Ordinal);
		Assert.DoesNotContain("Disallow: /", robots, StringComparison.Ordinal);
	}

	[Fact(DisplayName = "llms.txt lists every indexable page, so an answer engine sees the whole site at once")]
	public void Given_the_built_site_When_reading_llms_Then_every_page_is_listed()
	{
		var llms = SiteOutput.ReadOrNull("llms.txt");

		Assert.NotNull(llms);

		var missing = SiteOutput.IndexablePages()
			.Select(static file => SiteOutput.Origin + SiteOutput.UrlOf(file))
			.Where(url => !llms.Contains(url, StringComparison.Ordinal))
			.ToArray();

		Assert.Empty(missing);
	}

	[Fact(DisplayName = "The custom error page and the single stylesheet were both produced")]
	public void Given_the_built_site_When_looking_for_its_essentials_Then_they_are_present()
	{
		Assert.NotNull(SiteOutput.ReadOrNull("404.html"));
		Assert.NotNull(SiteOutput.ReadOrNull("assets/css/site.css"));
	}

	[Fact(DisplayName = "No theme stylesheet leaked into the build, which would prove the default theme is still active")]
	public void Given_the_built_site_When_looking_for_the_default_theme_Then_it_is_absent()
	{
		Assert.Null(SiteOutput.ReadOrNull("assets/css/style.css"));
	}

	[Fact(DisplayName = "The manifest and every icon it names were produced")]
	public void Given_the_built_site_When_reading_the_manifest_Then_its_icons_exist()
	{
		var manifest = SiteOutput.ReadOrNull("site.webmanifest");

		Assert.NotNull(manifest);

		foreach (var icon in new[] { "assets/icons/icon-192.png", "assets/icons/icon-512.png" })
		{
			Assert.Contains("/" + icon, manifest, StringComparison.Ordinal);
			Assert.True(
				File.Exists(Path.Combine(SiteOutput.RequireDirectory(), icon.Replace('/', Path.DirectorySeparatorChar))),
				$"{icon} is named by the manifest but was not built");
		}

		Assert.True(File.Exists(Path.Combine(SiteOutput.RequireDirectory(), "favicon.ico")));
	}
}
