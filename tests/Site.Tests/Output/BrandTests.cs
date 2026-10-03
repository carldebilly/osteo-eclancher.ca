using System.Text.RegularExpressions;
using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Output;

/// <summary>
/// The site carries the identity approved on the business card: the vector ensō, the Crane-blue
/// washi paper, the white brush stroke under key words. These tests keep its pieces in place and
/// keep the brush drawings, which are generated vectors, from silently growing heavy.
/// </summary>
[Trait("Category", "Output")]
public sealed partial class BrandTests
{
	[GeneratedRegex(@"url\(\s*['""]?(?<path>[^'"")]+)['""]?\s*\)")]
	private static partial Regex CssUrl();

	[Theory(DisplayName = "Every page shows the vector ensō in the header, not the old 200 px raster")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_header_logo_Then_it_is_the_vector_enso(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		var logo = document.QuerySelector("header.site .brand img");

		Assert.NotNull(logo);
		Assert.Equal("/assets/img/enso.svg", logo.GetAttribute("src"));
	}

	[Theory(DisplayName = "Each home page draws the brush stroke under the words its front matter names")]
	[InlineData("/", "votre corps")]
	[InlineData("/en/", "your body")]
	public void Given_a_home_page_When_reading_the_hero_title_Then_the_stroke_underlines_the_named_words(string url, string words)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		var stroked = document.QuerySelectorAll(".hero h1 .brush-under").ToArray();

		Assert.Single(stroked);
		Assert.Equal(words, stroked[0].TextContent);
	}

	[Fact(DisplayName = "Every file the stylesheet points at exists in the build, so no background silently goes missing")]
	public void Given_the_built_stylesheet_When_resolving_its_urls_Then_each_file_exists()
	{
		var css = SiteOutput.ReadOrNull("assets/css/site.css");
		Assert.NotNull(css);

		var missing = CssUrl().Matches(css)
			.Select(static match => match.Groups["path"].Value)
			.Where(static path => path.StartsWith('/'))
			.Where(static path => SiteOutput.ReadOrNull(path.TrimStart('/')) is null)
			.ToArray();

		Assert.Empty(missing);
	}

	[Theory(DisplayName = "Brand assets stay within their weight budget, since the logo and textures load on every page")]
	[InlineData("assets/img/enso.svg", 40)]
	[InlineData("assets/img/enso-halo.svg", 40)]
	[InlineData("assets/img/brush-under.svg", 30)]
	[InlineData("assets/img/portrait-mask.svg", 30)]
	[InlineData("assets/img/washi-tile.jpg", 200)]
	public void Given_a_brand_asset_When_measuring_it_Then_it_stays_under_budget(string relativePath, int budgetKilobytes)
	{
		var path = Path.Combine(SiteOutput.RequireDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));

		Assert.True(File.Exists(path), $"{relativePath} was not built");
		Assert.InRange(new FileInfo(path).Length, 1, budgetKilobytes * 1024);
	}
}
