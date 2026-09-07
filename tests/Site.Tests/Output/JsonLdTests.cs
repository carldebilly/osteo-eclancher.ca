using System.Text.Json;
using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Output;

/// <summary>
/// Validates the structured data every page emits.
/// </summary>
/// <remarks>
/// The JSON-LD is assembled by Liquid, so a missing value produces a syntax error rather
/// than an empty field, and a page whose structured data does not parse is simply skipped
/// by every consumer without complaint. Since this is what search engines and answer engines
/// read to learn where the practice is and how to book it, a silent skip is expensive.
/// </remarks>
[Trait("Category", "Output")]
public sealed class JsonLdTests
{
	private static IReadOnlyList<JsonDocument> BlocksIn(string url)
		=> [.. SiteOutput.Parse(SiteOutput.FileFor(url))
			.QuerySelectorAll("script[type='application/ld+json']")
			.Select(script => JsonDocument.Parse(script.TextContent))];

	[Theory(DisplayName = "Every structured data block on the page is valid JSON with a context")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_parsing_its_structured_data_Then_every_block_is_valid(string url)
	{
		var blocks = BlocksIn(url);

		Assert.NotEmpty(blocks);
		Assert.All(blocks, block => Assert.True(block.RootElement.TryGetProperty("@context", out _)));
	}

	[Theory(DisplayName = "The practice node carries the address, phone and booking link a listing needs")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_the_business_node_Then_it_is_complete(string url)
	{
		var business = BlocksIn(url)
			.Where(static block => block.RootElement.TryGetProperty("@graph", out _))
			.SelectMany(static block => block.RootElement.GetProperty("@graph").EnumerateArray())
			.Single(static node => node.GetProperty("@id").GetString()!.EndsWith("#business", StringComparison.Ordinal));

		Assert.False(string.IsNullOrWhiteSpace(business.GetProperty("telephone").GetString()));
		Assert.False(string.IsNullOrWhiteSpace(
			business.GetProperty("address").GetProperty("streetAddress").GetString()));

		var bookingUrl = business
			.GetProperty("potentialAction")
			.GetProperty("target")
			.GetProperty("urlTemplate")
			.GetString()!;

		var expectedLanguage = url.StartsWith("/en/", StringComparison.Ordinal) ? "en" : "fr";

		Assert.Contains($"/BookingWidget/{expectedLanguage}", bookingUrl, StringComparison.Ordinal);
	}

	[Fact(DisplayName = "The about page describes the practitioner as a person, which is what an author lookup needs")]
	public void Given_the_about_pages_When_reading_their_structured_data_Then_a_person_is_described()
	{
		foreach (var url in new[] { "/a-propos/", "/en/about/" })
		{
			var person = BlocksIn(url)
				.SingleOrDefault(static block =>
					block.RootElement.TryGetProperty("@type", out var type) && type.GetString() == "Person");

			Assert.True(person is not null, $"{url} has no Person node");
			Assert.Equal("Julien Éclancher", person!.RootElement.GetProperty("name").GetString());
		}
	}

	[Theory(DisplayName = "A page's FAQ markup and its FAQ structured data describe the same questions")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_page_with_a_faq_When_comparing_markup_and_data_Then_the_counts_agree(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));
		var rendered = document.QuerySelectorAll(".faq-item").Length;

		var faqBlock = BlocksIn(url).SingleOrDefault(static block =>
			block.RootElement.TryGetProperty("@type", out var type) && type.GetString() == "FAQPage");

		if (rendered == 0)
		{
			Assert.Null(faqBlock);
			return;
		}

		Assert.True(faqBlock is not null, $"{url} renders {rendered} FAQ entries but emits no FAQPage data");
		Assert.Equal(rendered, faqBlock!.RootElement.GetProperty("mainEntity").GetArrayLength());
	}

	[Theory(DisplayName = "Inner pages emit a breadcrumb trail back to their language's home page")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_an_inner_page_When_reading_its_breadcrumb_Then_it_leads_home(string url)
	{
		if (url is "/" or "/en/" or "/404.html")
		{
			return;
		}

		var breadcrumb = BlocksIn(url).SingleOrDefault(static block =>
			block.RootElement.TryGetProperty("@type", out var type) && type.GetString() == "BreadcrumbList");

		Assert.True(breadcrumb is not null, $"{url} has no breadcrumb");

		var items = breadcrumb!.RootElement.GetProperty("itemListElement").EnumerateArray().ToArray();
		var expectedHome = url.StartsWith("/en/", StringComparison.Ordinal)
			? SiteOutput.Origin + "/en/"
			: SiteOutput.Origin + "/";

		Assert.InRange(items.Length, 2, 3);
		Assert.Equal(expectedHome, items[0].GetProperty("item").GetString());
		Assert.Equal(SiteOutput.Origin + url, items[^1].GetProperty("item").GetString());

		// Positions must run 1..n without a gap, or the trail is ignored.
		for (var index = 0; index < items.Length; index++)
		{
			Assert.Equal(index + 1, items[index].GetProperty("position").GetInt32());
		}

		// A page nested under another must sit below it in the trail, not beside it.
		if (items.Length == 3)
		{
			var parent = items[1].GetProperty("item").GetString()!;

			Assert.StartsWith(parent, SiteOutput.Origin + url, StringComparison.Ordinal);
			Assert.NotEqual(parent, SiteOutput.Origin + url);
		}
	}
}
