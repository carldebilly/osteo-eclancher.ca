using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Output;

/// <summary>
/// Follows every internal link and resource reference in the built site.
/// </summary>
/// <remarks>
/// The previous version of this site shipped a contact link pointing at a Cloudflare
/// email-obfuscation script that no longer existed, so the only address on the page led to
/// a 404 for months. Nothing on the page looked wrong. This test exists so that class of
/// failure cannot ship again.
/// </remarks>
[Trait("Category", "Output")]
public sealed class LinksAndAssetsTests
{
	private static bool Resolves(string reference)
	{
		var path = reference.Split('#')[0].Split('?')[0];

		if (path.Length == 0)
		{
			return true;
		}

		var relative = path.TrimStart('/');

		if (path.EndsWith('/'))
		{
			relative += "index.html";
		}

		var full = Path.Combine(SiteOutput.RequireDirectory(), relative.Replace('/', Path.DirectorySeparatorChar));

		return File.Exists(full);
	}

	[Theory(DisplayName = "Every internal link on a page reaches a file that was actually built")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_following_its_internal_links_Then_each_resolves(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		var broken = document
			.QuerySelectorAll("a[href]")
			.Select(static a => a.GetAttribute("href")!)
			.Where(static href => href.StartsWith('/'))
			.Distinct(StringComparer.Ordinal)
			.Where(static href => !Resolves(href))
			.ToArray();

		Assert.Empty(broken);
	}

	[Theory(DisplayName = "Every stylesheet, script, icon and image a page requests was actually built")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_following_its_resources_Then_each_resolves(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));
		var references = new List<string>();

		references.AddRange(document.QuerySelectorAll("link[href]")
			.Where(static link => link.GetAttribute("rel") is not ("alternate" or "canonical"))
			.Select(static link => link.GetAttribute("href")!));

		references.AddRange(document.QuerySelectorAll("img[src]").Select(static img => img.GetAttribute("src")!));
		references.AddRange(document.QuerySelectorAll("script[src]").Select(static s => s.GetAttribute("src")!));

		references.AddRange(document.QuerySelectorAll("source[srcset]")
			.SelectMany(static source => source.GetAttribute("srcset")!.Split(','))
			.Select(static entry => entry.Trim().Split(' ')[0]));

		var broken = references
			.Where(static reference => reference.StartsWith('/'))
			.Distinct(StringComparer.Ordinal)
			.Where(static reference => !Resolves(reference))
			.ToArray();

		Assert.Empty(broken);
	}

	[Theory(DisplayName = "Every image carries alternative text, even when it is deliberately empty")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_images_Then_each_declares_alt_text(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		var missing = document
			.QuerySelectorAll("img")
			.Where(static img => !img.HasAttribute("alt"))
			.Select(static img => img.GetAttribute("src") ?? "(no src)")
			.ToArray();

		Assert.Empty(missing);
	}

	[Theory(DisplayName = "Every booking link opens safely and in the language of the page it sits on")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_booking_links_Then_each_matches_the_page_language(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));
		var expectedLanguage = url.StartsWith("/en/", StringComparison.Ordinal) ? "en" : "fr";

		var bookingLinks = document
			.QuerySelectorAll("a[href*='gorendezvous.com']")
			.ToArray();

		Assert.NotEmpty(bookingLinks);

		foreach (var link in bookingLinks)
		{
			var href = link.GetAttribute("href")!;

			Assert.Contains($"/BookingWidget/{expectedLanguage}", href, StringComparison.Ordinal);
			Assert.Equal("_blank", link.GetAttribute("target"));
			Assert.Contains("noopener", link.GetAttribute("rel") ?? "", StringComparison.Ordinal);
		}
	}

	[Theory(DisplayName = "No page still contains unrendered template syntax or the removed email markup")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_scanning_its_html_Then_no_template_residue_remains(string url)
	{
		var html = File.ReadAllText(SiteOutput.FileFor(url));

		Assert.DoesNotContain("{{", html, StringComparison.Ordinal);
		Assert.DoesNotContain("{%", html, StringComparison.Ordinal);
		Assert.DoesNotContain("cdn-cgi", html, StringComparison.Ordinal);
		Assert.DoesNotContain("mailto:", html, StringComparison.Ordinal);
	}
	[Theory(DisplayName = "Every page footer links to the Facebook page so visitors and crawlers can connect the two")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_footer_Then_it_links_to_the_facebook_page(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		var link = document.QuerySelector("footer a[href^='https://www.facebook.com/']");

		Assert.NotNull(link);
		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Contains("noopener", link.GetAttribute("rel") ?? "");
	}

}
