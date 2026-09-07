using System.Net;
using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Output;

/// <summary>
/// Actually calls the external hosts the site links to, and fails when one stops answering.
/// </summary>
/// <remarks>
/// Deliberately traited <c>Network</c> and excluded from the required pull-request check. It
/// depends on third parties: sqf.quebec answered 429 while this page was being researched, and
/// a rate limit at someone else's door must not redden an unrelated change. Run it on purpose,
/// on a schedule or before touching the resource pages:
///
/// <code>dotnet test tests/Site.Tests --filter "Category=Network"</code>
///
/// The allowlist in <see cref="Rules.AllowedExternalHosts"/> is the offline half of the same
/// guard: it catches a host that was typed wrong, this catches a host that died.
/// </remarks>
[Trait("Category", "Network")]
public sealed class ExternalReachabilityTests
{
	private static readonly HttpClient Client = CreateClient();

	private static HttpClient CreateClient()
	{
		var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true })
		{
			Timeout = TimeSpan.FromSeconds(20),
		};

		// Several of these sites answer differently to a bare programmatic request.
		client.DefaultRequestHeaders.UserAgent.ParseAdd(
			"Mozilla/5.0 (compatible; osteo-eclancher.ca link check)");

		return client;
	}

	public static TheoryData<string> LinkedUrls()
	{
		var data = new TheoryData<string>();
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var file in SiteOutput.HtmlFiles())
		{
			foreach (var anchor in SiteOutput.Parse(file).QuerySelectorAll("a[href]"))
			{
				var href = anchor.GetAttribute("href")!;

				if (Uri.TryCreate(href, UriKind.Absolute, out var target)
					&& target.Scheme is "http" or "https"
					&& seen.Add(target.GetLeftPart(UriPartial.Path)))
				{
					data.Add(target.GetLeftPart(UriPartial.Path));
				}
			}
		}

		return data;
	}

	[Theory(DisplayName = "Every host the site sends a reader to still answers")]
	[MemberData(nameof(LinkedUrls))]
	public async Task Given_an_outbound_link_When_requesting_it_Then_the_host_answers(string url)
	{
		var response = await Send(HttpMethod.Head, url);

		// A fair number of sites reject HEAD outright; fall back before calling it dead.
		if (response.StatusCode is HttpStatusCode.MethodNotAllowed
			or HttpStatusCode.NotImplemented
			or HttpStatusCode.Forbidden)
		{
			response = await Send(HttpMethod.Get, url);
		}

		Assert.True(
			(int)response.StatusCode < 400 || response.StatusCode == HttpStatusCode.TooManyRequests,
			$"{url} answered {(int)response.StatusCode} {response.StatusCode}");
	}

	private static async Task<HttpResponseMessage> Send(HttpMethod method, string url)
	{
		using var request = new HttpRequestMessage(method, url);

		return await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
	}
}
