using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Source;

/// <summary>
/// The bilingual contract. Each page exists once per language and the two halves know
/// about each other through a shared <c>ref</c>; that lookup is what produces the
/// reciprocal hreflang links and the language switcher, so a missing or duplicated
/// <c>ref</c> silently strips both.
/// </summary>
public sealed class BilingualPairsTests
{
	[Fact(DisplayName = "Every page reference has exactly one French and one English version")]
	public void Given_the_source_pages_When_grouping_by_reference_Then_each_has_one_page_per_language()
	{
		var problems = new List<string>();

		foreach (var group in SourcePage.All()
			.Where(static page => page.Ref is not null)
			.GroupBy(static page => page.Ref!))
		{
			var french = group.Count(static page => page.Lang == "fr");
			var english = group.Count(static page => page.Lang == "en");

			if (french != 1 || english != 1)
			{
				problems.Add($"ref '{group.Key}': {french} fr, {english} en");
			}
		}

		Assert.Empty(problems);
	}

	[Fact(DisplayName = "Known page references are the only ones used, so no orphan page slips in")]
	public void Given_the_source_pages_When_collecting_references_Then_they_match_the_agreed_keys()
	{
		var expected = Rules.ExpectedUrls.Keys.OrderBy(static key => key, StringComparer.Ordinal).ToArray();

		var actual = SourcePage.All()
			.Where(static page => page.Ref is not null)
			.Select(static page => page.Ref!)
			.Distinct()
			.OrderBy(static key => key, StringComparer.Ordinal)
			.ToArray();

		Assert.Equal(expected, actual);
	}

	[Fact(DisplayName = "Twin pages agree on their navigation position, so the menu order survives a language switch")]
	public void Given_a_page_pair_When_comparing_navigation_order_Then_both_sides_agree()
	{
		var problems = new List<string>();

		foreach (var group in SourcePage.All()
			.Where(static page => page.Ref is not null)
			.GroupBy(static page => page.Ref!))
		{
			var orders = group.Select(static page => page.NavOrder).Distinct().ToArray();

			if (orders.Length > 1)
			{
				problems.Add($"ref '{group.Key}': nav_order values {string.Join(", ", orders)}");
			}
		}

		Assert.Empty(problems);
	}

	[Fact(DisplayName = "Pages under en/ inherit their language from the config and never restate it")]
	public void Given_a_page_under_en_When_reading_its_front_matter_Then_it_declares_no_language()
	{
		var offenders = SourcePage.All()
			.Where(static page => page.RelativePath.StartsWith("docs/en/", StringComparison.Ordinal))
			.Where(static page => page.FrontMatter.ContainsKey("lang"))
			.Select(static page => page.RelativePath)
			.ToArray();

		Assert.Empty(offenders);
	}
}
