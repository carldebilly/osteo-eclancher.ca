using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Output;

/// <summary>
/// Keeps the site in the practitioner's own voice.
/// </summary>
/// <remarks>
/// Every page is Julien speaking to a visitor in the first person. Copy written about him in
/// the third person reads as an agency wrote it, which is the opposite of what a page about
/// consent and listening should sound like — and it was the first line of the home page for a
/// while. The byline is the deliberate exception: a signature is a name, not narration.
///
/// Titles, meta descriptions and image alternatives stay in the third person on purpose. They
/// describe the page to a search engine rather than address a reader, so they are not checked
/// here.
/// </remarks>
[Trait("Category", "Output")]
public sealed class VoiceTests
{
	/// <summary>
	/// The sentences of a page: paragraphs and list items inside the main content, with the
	/// byline removed.
	/// </summary>
	/// <remarks>
	/// Headings are excluded on purpose. "Julien Éclancher" as a page title or a section label
	/// is a name, the same way the byline is; it only becomes third-person narration inside a
	/// sentence. Measuring whole-of-main text confuses the two.
	/// </remarks>
	private static string BodyProseOf(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		foreach (var element in document.QuerySelectorAll("script, .byline").ToArray())
		{
			element.Remove();
		}

		var sentences = document
			.QuerySelectorAll("main p, main li, main dd, main blockquote")
			.Select(static element => element.TextContent);

		return string.Join("\n", sentences);
	}

	[Theory(DisplayName = "No page narrates the practitioner in the third person by name")]
	[MemberData(nameof(SiteOutput.IndexablePageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_prose_Then_the_practitioner_is_not_named(string url)
	{
		var prose = BodyProseOf(url);

		// The privacy page names him once, identifying the person the law requires it to name,
		// and does so in the first person ("c'est moi : Julien Éclancher").
		if (url is "/confidentialite/" or "/en/privacy/")
		{
			Assert.True(
				prose.Contains("c’est moi", StringComparison.OrdinalIgnoreCase)
					|| prose.Contains("I am the person responsible", StringComparison.OrdinalIgnoreCase),
				$"{url} names him without claiming the identification in the first person");

			return;
		}

		Assert.DoesNotContain("Julien Éclancher", prose, StringComparison.OrdinalIgnoreCase);
	}

	[Theory(DisplayName = "Every page speaks in the first person somewhere, so no page reads as written about him")]
	[MemberData(nameof(SiteOutput.IndexablePageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_prose_Then_it_speaks_in_the_first_person(string url)
	{
		var prose = BodyProseOf(url);

		var markers = url.StartsWith("/en/", StringComparison.Ordinal)
			? new[] { "I ", "I’", "my ", "me ", "mine" }
			: new[] { "Je ", "je ", "J’", "j’", "mon ", "ma ", "mes ", "moi" };

		Assert.Contains(markers, marker => prose.Contains(marker, StringComparison.Ordinal));
	}

	[Theory(DisplayName = "No page describes him with a third-person pronoun or possessive")]
	[MemberData(nameof(SiteOutput.IndexablePageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_prose_Then_no_third_person_form_describes_him(string url)
	{
		var prose = BodyProseOf(url);

		string[] forms = url.StartsWith("/en/", StringComparison.Ordinal)
			? ["his practice", "he receives", "he practises", "he practices", "he works with"]
			: ["sa pratique", "son approche", "il reçoit", "il pratique", "il accompagne"];

		var found = forms.Where(form => prose.Contains(form, StringComparison.OrdinalIgnoreCase)).ToArray();

		Assert.Empty(found);
	}
}
