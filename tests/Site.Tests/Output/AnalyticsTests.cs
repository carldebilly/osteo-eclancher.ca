using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Output;

/// <summary>
/// Audience measurement runs without cookies: the consent state is declared denied before the
/// tag is configured, which is what lets the privacy page promise that no identifier is stored.
/// These tests keep that order intact and make sure every booking button can be told apart.
/// </summary>
[Trait("Category", "Output")]
public sealed class AnalyticsTests
{
	private static readonly IReadOnlySet<string> KnownPlacements = new HashSet<string>(StringComparer.Ordinal)
	{
		"nav", "hero", "contact", "band",
	};

	private static string InlineScripts(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		return string.Concat(document.QuerySelectorAll("script:not([src])").Select(static s => s.TextContent));
	}

	[Theory(DisplayName = "Every page loads the analytics tag for the configured measurement id")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_head_Then_the_gtag_loader_matches_the_configured_id(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));
		var expectedSrc = "https://www.googletagmanager.com/gtag/js?id=" + Rules.AnalyticsId;

		var loader = document.QuerySelector("script[src^='https://www.googletagmanager.com/']");

		Assert.NotNull(loader);
		Assert.Equal(expectedSrc, loader.GetAttribute("src"));
	}

	[Theory(DisplayName = "Consent is declared denied before the tag is configured, so no cookie is ever written")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_inline_scripts_Then_consent_default_precedes_config(string url)
	{
		var scripts = InlineScripts(url);

		var consentAt = scripts.IndexOf("gtag('consent', 'default'", StringComparison.Ordinal);
		var configAt = scripts.IndexOf("gtag('config'", StringComparison.Ordinal);

		Assert.True(consentAt >= 0, "The consent default call is missing.");
		Assert.True(configAt > consentAt, "gtag('config') must come after gtag('consent', 'default').");
		Assert.Contains("analytics_storage: 'denied'", scripts);
	}

	[Theory(DisplayName = "Booking clicks are reported as one named event, so the tag can tell which button was used")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_inline_scripts_Then_the_booking_click_listener_is_present(string url)
	{
		var scripts = InlineScripts(url);

		Assert.Contains("'book_click'", scripts);
		Assert.Contains("a[data-cta]", scripts);
	}

	[Theory(DisplayName = "Each booking button names its placement, and no placement repeats on a page")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_booking_buttons_Then_each_has_a_distinct_known_placement(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		var placements = document.QuerySelectorAll("a[href^='https://www.gorendezvous.com/']")
			.Select(static a => a.GetAttribute("data-cta") ?? "")
			.ToArray();

		Assert.NotEmpty(placements);
		Assert.All(placements, p => Assert.Contains(p, KnownPlacements));
		Assert.Equal(placements.Length, placements.Distinct(StringComparer.Ordinal).Count());
	}
}
