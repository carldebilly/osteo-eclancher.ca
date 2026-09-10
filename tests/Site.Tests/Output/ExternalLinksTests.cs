using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Output;

/// <summary>
/// Checks the links that leave the site.
/// </summary>
/// <remarks>
/// <see cref="LinksAndAssetsTests"/> only follows hrefs starting with a slash, so until now
/// nothing guarded the outbound links — and the resource pages exist entirely to send readers
/// to organisations that can help them. A mistyped domain there is worse than a broken internal
/// link: it fails someone who is looking for a support line.
/// </remarks>
[Trait("Category", "Output")]
public sealed class ExternalLinksTests
{
	private static IReadOnlyList<(string Page, Uri Target)> OutboundLinks()
	{
		var links = new List<(string, Uri)>();

		foreach (var file in SiteOutput.HtmlFiles())
		{
			var page = SiteOutput.UrlOf(file);

			foreach (var element in SiteOutput.Parse(file).QuerySelectorAll("a[href], script[src]"))
			{
				var href = element.GetAttribute("href") ?? element.GetAttribute("src")!;

				if (Uri.TryCreate(href, UriKind.Absolute, out var target)
					&& target.Scheme is "http" or "https")
				{
					links.Add((page, target));
				}
			}
		}

		return links;
	}

	[Fact(DisplayName = "Every outbound link points at a host the site has deliberately allowed")]
	public void Given_the_built_site_When_reading_outbound_links_Then_each_host_is_allowed()
	{
		var offenders = OutboundLinks()
			.Where(static link => !Rules.AllowedExternalHosts.Contains(link.Target.Host))
			.Select(static link => $"{link.Page} -> {link.Target.Host}")
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		Assert.Empty(offenders);
	}

	[Fact(DisplayName = "Every outbound link uses HTTPS, so no reader is downgraded on the way out")]
	public void Given_the_built_site_When_reading_outbound_links_Then_each_uses_https()
	{
		var offenders = OutboundLinks()
			.Where(static link => link.Target.Scheme != "https")
			.Select(static link => $"{link.Page} -> {link.Target}")
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		Assert.Empty(offenders);
	}

	[Fact(DisplayName = "A link that opens a new tab also drops the opener reference")]
	public void Given_a_link_opening_a_new_tab_When_reading_its_rel_Then_the_opener_is_dropped()
	{
		var offenders = new List<string>();

		foreach (var file in SiteOutput.HtmlFiles())
		{
			foreach (var anchor in SiteOutput.Parse(file).QuerySelectorAll("a[target=_blank]"))
			{
				var rel = anchor.GetAttribute("rel") ?? "";

				if (!rel.Contains("noopener", StringComparison.OrdinalIgnoreCase))
				{
					offenders.Add($"{SiteOutput.UrlOf(file)} -> {anchor.GetAttribute("href")}");
				}
			}
		}

		Assert.Empty(offenders);
	}

	[Fact(DisplayName = "The dead domain most directories still publish for the Montréal association is absent")]
	public void Given_the_built_site_When_searching_for_the_dead_association_domain_Then_it_is_absent()
	{
		var offenders = SiteOutput.HtmlFiles()
			.Where(static file => File.ReadAllText(file).Contains("afim.qc.ca", StringComparison.OrdinalIgnoreCase))
			.Select(SiteOutput.UrlOf)
			.ToArray();

		Assert.Empty(offenders);
	}
}
