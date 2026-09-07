using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Source;

/// <summary>
/// Guards the two strings a searcher actually reads before deciding to click. They are
/// easy to leave empty and impossible to notice missing, since a page looks fine without them.
/// </summary>
public sealed class SeoMetadataTests
{
	public static TheoryData<string> AllPages()
	{
		var data = new TheoryData<string>();

		foreach (var page in SourcePage.All())
		{
			data.Add(page.RelativePath);
		}

		return data;
	}

	private static SourcePage Page(string relativePath)
		=> SourcePage.All().Single(page => page.RelativePath == relativePath);

	[Theory(DisplayName = "Each page carries a title short enough to survive a search result")]
	[MemberData(nameof(AllPages))]
	public void Given_a_page_When_measuring_its_title_Then_it_fits_a_search_result(string relativePath)
	{
		var page = Page(relativePath);

		Assert.False(string.IsNullOrWhiteSpace(page.SeoTitle), $"{relativePath} has no title");
		Assert.True(
			page.SeoTitle!.Length <= Rules.MaxTitleLength,
			$"{relativePath}: title is {page.SeoTitle.Length} characters, over {Rules.MaxTitleLength}: {page.SeoTitle}");
	}

	[Theory(DisplayName = "Each page carries a description long enough to be useful and short enough to survive")]
	[MemberData(nameof(AllPages))]
	public void Given_a_page_When_measuring_its_description_Then_it_fits_a_search_snippet(string relativePath)
	{
		var page = Page(relativePath);

		Assert.False(string.IsNullOrWhiteSpace(page.Description), $"{relativePath} has no description");

		var length = page.Description!.Length;

		Assert.True(
			length >= Rules.MinDescriptionLength && length <= Rules.MaxDescriptionLength,
			$"{relativePath}: description is {length} characters, outside {Rules.MinDescriptionLength}-{Rules.MaxDescriptionLength}: {page.Description}");
	}

	[Theory(DisplayName = "A page kept out of the index is also kept out of the sitemap")]
	[MemberData(nameof(AllPages))]
	public void Given_a_noindex_page_When_checking_the_sitemap_flag_Then_it_is_excluded(string relativePath)
	{
		var page = Page(relativePath);

		if (page.NoIndex)
		{
			Assert.False(page.InSitemap, $"{relativePath} is noindex but still listed in the sitemap");
		}
	}

	[Theory(DisplayName = "Every FAQ entry has both a question and an answer")]
	[MemberData(nameof(AllPages))]
	public void Given_a_page_with_a_faq_When_reading_its_entries_Then_none_is_half_written(string relativePath)
	{
		var page = Page(relativePath);

		foreach (var (question, answer) in page.Faq)
		{
			Assert.False(string.IsNullOrWhiteSpace(question), $"{relativePath} has a FAQ entry with no question");
			Assert.False(string.IsNullOrWhiteSpace(answer), $"{relativePath}: FAQ entry '{question}' has no answer");
		}
	}
}
