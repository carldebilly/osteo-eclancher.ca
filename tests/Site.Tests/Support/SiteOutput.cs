using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Xunit;

namespace Site.Tests.Support;

/// <summary>
/// The built site. Produced by <c>tools/preview.ps1</c> locally and by
/// <c>actions/jekyll-build-pages</c> in CI, which is the same container GitHub Pages
/// itself runs, so what these tests read is what visitors get.
/// </summary>
internal static class SiteOutput
{
	private static readonly HtmlParser Parser = new();

	/// <summary>Origin the site is served from; every canonical and alternate URL starts with it.</summary>
	public const string Origin = "https://osteo-eclancher.ca";

	/// <summary>Absolute path of the built site, or null when no build is present.</summary>
	public static string? Directory
	{
		get
		{
			var fromEnvironment = Environment.GetEnvironmentVariable("SITE_OUTPUT_DIR");

			if (!string.IsNullOrWhiteSpace(fromEnvironment) && System.IO.Directory.Exists(fromEnvironment))
			{
				return fromEnvironment;
			}

			var conventional = Path.Combine(RepoPaths.Root, "_site");

			return System.IO.Directory.Exists(conventional) ? conventional : null;
		}
	}

	/// <summary>Site root, or a clear failure when the site has not been built.</summary>
	public static string RequireDirectory()
		=> Directory ?? throw new InvalidOperationException(
			"No built site found. Run tools/preview.ps1 first, or set SITE_OUTPUT_DIR to a build output.");

	/// <summary>Every generated HTML page, absolute paths.</summary>
	public static IReadOnlyList<string> HtmlFiles()
		=> [.. System.IO.Directory
			.EnumerateFiles(RequireDirectory(), "*.html", SearchOption.AllDirectories)
			.OrderBy(static path => path, StringComparer.Ordinal)];

	/// <summary>Every generated page except the 404, which has no twin and is not indexed.</summary>
	public static IReadOnlyList<string> IndexablePages()
		=> [.. HtmlFiles().Where(static path => Path.GetFileName(path) != "404.html")];

	/// <summary>The site URL a generated file is served at, derived from its path.</summary>
	public static string UrlOf(string htmlFile)
	{
		var relative = Path.GetRelativePath(RequireDirectory(), htmlFile).Replace('\\', '/');

		return relative == "index.html"
			? "/"
			: relative.EndsWith("/index.html", StringComparison.Ordinal)
				? "/" + relative[..^"index.html".Length]
				: "/" + relative;
	}

	/// <summary>Parses a generated page into a DOM.</summary>
	public static IHtmlDocument Parse(string htmlFile) => Parser.ParseDocument(File.ReadAllText(htmlFile));

	/// <summary>Reads a generated file at a site-root-relative path, or null when absent.</summary>
	public static string? ReadOrNull(string relativePath)
	{
		var path = Path.Combine(RequireDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));

		return File.Exists(path) ? File.ReadAllText(path) : null;
	}

	/// <summary>Every generated page, as theory data addressed by site URL.</summary>
	public static TheoryData<string> AllPageUrls() => UrlsOf(HtmlFiles());

	/// <summary>Every generated page except the 404, as theory data addressed by site URL.</summary>
	public static TheoryData<string> IndexablePageUrls() => UrlsOf(IndexablePages());

	private static TheoryData<string> UrlsOf(IReadOnlyList<string> files)
	{
		var data = new TheoryData<string>();

		foreach (var file in files)
		{
			data.Add(UrlOf(file));
		}

		return data;
	}

	/// <summary>The generated file serving a given site URL.</summary>
	public static string FileFor(string url) => HtmlFiles().Single(file => UrlOf(file) == url);
}
