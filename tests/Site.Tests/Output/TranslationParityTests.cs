using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Output;

/// <summary>
/// Catches structural drift between a page and its translation.
/// </summary>
/// <remarks>
/// Editing one language and forgetting the other is the likeliest defect this site will
/// accumulate: it happened while removing a sentence that pointed readers at sitemap.xml,
/// which left the English page carrying it for a commit.
///
/// What this checks is the shape, not the words: the same sections and the same questions on
/// both sides. A sentence added to one language only still slips through, because translations
/// legitimately differ in length and a word-count rule would fire on every honest edit. The
/// shape is the part where drift costs a reader a whole section.
/// </remarks>
[Trait("Category", "Output")]
public sealed class TranslationParityTests
{
	/// <summary>Each page and its translation, addressed by the French URL.</summary>
	public static TheoryData<string, string> PagePairs()
	{
		var data = new TheoryData<string, string>();

		foreach (var (french, english) in Rules.ExpectedUrls.Values)
		{
			data.Add(french, english);
		}

		return data;
	}

	private static IReadOnlyList<string> HeadingsOf(string url)
		=> [.. SiteOutput.Parse(SiteOutput.FileFor(url))
			.QuerySelectorAll("main h2")
			.Select(static heading => heading.TextContent.Trim())];

	private static int QuestionCountOf(string url)
		=> SiteOutput.Parse(SiteOutput.FileFor(url)).QuerySelectorAll(".faq-item").Length;

	[Theory(DisplayName = "A page and its translation carry the same number of sections")]
	[MemberData(nameof(PagePairs))]
	public void Given_a_page_pair_When_counting_sections_Then_both_sides_agree(string french, string english)
	{
		var left = HeadingsOf(french);
		var right = HeadingsOf(english);

		Assert.True(
			left.Count == right.Count,
			$"{french} has {left.Count} sections, {english} has {right.Count}."
				+ $"{Environment.NewLine}  fr: {string.Join(" | ", left)}"
				+ $"{Environment.NewLine}  en: {string.Join(" | ", right)}");
	}

	[Theory(DisplayName = "A page and its translation ask the same number of questions")]
	[MemberData(nameof(PagePairs))]
	public void Given_a_page_pair_When_counting_questions_Then_both_sides_agree(string french, string english)
	{
		Assert.Equal(QuestionCountOf(french), QuestionCountOf(english));
	}
}
