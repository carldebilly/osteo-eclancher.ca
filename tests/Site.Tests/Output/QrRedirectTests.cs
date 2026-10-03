using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Output;

/// <summary>
/// The business cards carry a QR code to <see cref="Rules.QrRedirectUrl"/>. Once cards are
/// printed, this page is the only thing that can still be changed, so it must keep sending
/// people to the current booking link, and it must count them on the way through.
/// </summary>
[Trait("Category", "Output")]
public sealed class QrRedirectTests
{
	private static string BookingUrl
		=> DataFile.Dig(DataFile.Load("business.yml"), "booking", "fr")?.ToString()
			?? throw new InvalidOperationException("business.yml has no booking.fr link");

	private static string InlineScripts()
		=> string.Concat(SiteOutput.Parse(SiteOutput.FileFor(Rules.QrRedirectUrl))
			.QuerySelectorAll("script:not([src])")
			.Select(static script => script.TextContent));

	[Fact(DisplayName = "The QR redirect page is built and asks search engines not to index it")]
	public void Given_the_built_site_When_reading_the_qr_redirect_Then_it_exists_and_is_noindex()
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(Rules.QrRedirectUrl));

		var robots = document.QuerySelector("meta[name=robots]")?.GetAttribute("content") ?? "";

		Assert.Contains("noindex", robots, StringComparison.Ordinal);
	}

	[Fact(DisplayName = "The QR redirect sends visitors to the French booking link from business.yml, not a copy of it")]
	public void Given_the_qr_redirect_When_reading_its_script_Then_it_targets_the_current_booking_link()
	{
		var scripts = InlineScripts();

		Assert.Contains("location.replace(", scripts, StringComparison.Ordinal);
		Assert.Contains(BookingUrl, scripts, StringComparison.Ordinal);
	}

	[Fact(DisplayName = "The QR redirect offers a visible booking link for anyone whose browser does not follow the script")]
	public void Given_the_qr_redirect_When_reading_its_content_Then_a_manual_booking_link_is_present()
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(Rules.QrRedirectUrl));

		var manual = document.QuerySelector("main a[data-cta='qr']");

		Assert.NotNull(manual);
		Assert.Equal(BookingUrl, manual.GetAttribute("href"));
	}

	[Fact(DisplayName = "A card scan is reported to analytics before the redirect, with a timeout so a blocked tag cannot strand the visitor")]
	public void Given_the_qr_redirect_When_reading_its_script_Then_the_scan_is_reported_before_leaving()
	{
		var scripts = InlineScripts();

		Assert.Contains("'qr_scan'", scripts, StringComparison.Ordinal);
		Assert.Contains("event_callback", scripts, StringComparison.Ordinal);
		Assert.Contains("setTimeout(", scripts, StringComparison.Ordinal);
	}
}
