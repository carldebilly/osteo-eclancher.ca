using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Output;

/// <summary>
/// Keeps the rendered text typographically consistent.
/// </summary>
/// <remarks>
/// Page bodies go through kramdown, which turns a straight apostrophe into a typographic one.
/// Front-matter strings go through Liquid, which does not. Left alone, the same paragraph ends
/// up mixing both shapes, and on a site whose whole argument is care and attention that is the
/// first thing a reader's eye catches without being able to name it. French spacing before
/// question marks is checked for the same reason, and because a breaking space there lets a
/// question mark wrap onto its own line.
/// </remarks>
[Trait("Category", "Output")]
public sealed class TypographyTests
{
	private const char StraightApostrophe = '\'';
	private const char TypographicApostrophe = '’';

	/// <summary>Visible text only: scripts and attributes legitimately use straight quotes.</summary>
	private static string VisibleTextOf(string url)
	{
		var document = SiteOutput.Parse(SiteOutput.FileFor(url));

		foreach (var script in document.QuerySelectorAll("script").ToArray())
		{
			script.Remove();
		}

		return document.Body?.TextContent ?? "";
	}

	[Theory(DisplayName = "Visible text uses the typographic apostrophe everywhere, never the straight one")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_visible_text_Then_apostrophes_are_consistent(string url)
	{
		var text = VisibleTextOf(url);
		var straight = text.Count(static character => character == StraightApostrophe);

		Assert.True(
			straight == 0,
			$"{url} shows {straight} straight apostrophes alongside {text.Count(static c => c == TypographicApostrophe)} typographic ones");
	}

	[Theory(DisplayName = "French pages put an unbreakable space before a question or exclamation mark")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_french_page_When_reading_its_punctuation_Then_spacing_is_unbreakable(string url)
	{
		if (url.StartsWith("/en/", StringComparison.Ordinal))
		{
			return;
		}

		var text = VisibleTextOf(url);
		var offenders = new List<string>();

		for (var index = 1; index < text.Length; index++)
		{
			if (text[index] is not ('?' or '!'))
			{
				continue;
			}

			if (text[index - 1] == ' ')
			{
				offenders.Add(text[Math.Max(0, index - 40)..Math.Min(text.Length, index + 1)].Trim());
			}
		}

		Assert.Empty(offenders);
	}

	[Theory(DisplayName = "No page shows a raw HTML entity that failed to decode")]
	[MemberData(nameof(SiteOutput.AllPageUrls), MemberType = typeof(SiteOutput))]
	public void Given_a_built_page_When_reading_its_visible_text_Then_no_entity_leaked(string url)
	{
		var text = VisibleTextOf(url);

		Assert.DoesNotContain("&nbsp;", text, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("&amp;", text, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("&#", text, StringComparison.Ordinal);
	}
}
