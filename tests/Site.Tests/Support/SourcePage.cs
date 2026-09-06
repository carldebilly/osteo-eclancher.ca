using YamlDotNet.Serialization;

namespace Site.Tests.Support;

/// <summary>
/// One Jekyll source page: its front matter (with the site's path-based defaults applied
/// the same way <c>_config.yml</c> applies them) and its body.
/// </summary>
internal sealed class SourcePage
{
	private static readonly IDeserializer Yaml = new DeserializerBuilder().Build();

	private SourcePage(string path, IReadOnlyDictionary<string, object?> frontMatter, string body)
	{
		FullPath = path;
		FrontMatter = frontMatter;
		Body = body;
	}

	/// <summary>Absolute path of the source file.</summary>
	public string FullPath { get; }

	/// <summary>Path relative to the repository root, forward slashes.</summary>
	public string RelativePath => RepoPaths.Relative(FullPath);

	/// <summary>Parsed front matter. Missing keys are simply absent.</summary>
	public IReadOnlyDictionary<string, object?> FrontMatter { get; }

	/// <summary>Everything after the closing front-matter fence.</summary>
	public string Body { get; }

	/// <summary>
	/// Language of the page. Mirrors the front-matter defaults in <c>_config.yml</c>:
	/// <c>en</c> for anything under <c>en/</c>, <c>fr</c> otherwise, unless the page says so explicitly.
	/// </summary>
	public string Lang
		=> String("lang")
			?? (RelativePath.StartsWith("docs/en/", StringComparison.Ordinal) ? "en" : "fr");

	/// <summary>Key shared by the two language versions of the same page.</summary>
	public string? Ref => String("ref");

	/// <summary>Heading shown as the H1.</summary>
	public string? Title => String("title");

	/// <summary>Value used for the HTML title element; falls back to <see cref="Title"/>.</summary>
	public string? SeoTitle => String("seo_title") ?? Title;

	/// <summary>Meta description.</summary>
	public string? Description => String("description");

	/// <summary>Explicit output URL.</summary>
	public string? Permalink => String("permalink");

	/// <summary>Position in the header navigation; absent means the page is not in the nav.</summary>
	public int? NavOrder => int.TryParse(String("nav_order"), out var order) ? order : null;

	/// <summary>True when the page asks search engines not to index it.</summary>
	public bool NoIndex => String("noindex") is "true" or "True";

	/// <summary>False when the page opts out of the sitemap.</summary>
	public bool InSitemap => String("sitemap") is not ("false" or "False");

	/// <summary>Question and answer pairs rendered as the FAQ block.</summary>
	public IReadOnlyList<(string Question, string Answer)> Faq
	{
		get
		{
			if (FrontMatter.GetValueOrDefault("faq") is not List<object?> items)
			{
				return [];
			}

			var pairs = new List<(string, string)>(items.Count);

			foreach (var item in items)
			{
				if (item is Dictionary<object, object?> map)
				{
					pairs.Add((
						map.GetValueOrDefault("q")?.ToString() ?? "",
						map.GetValueOrDefault("a")?.ToString() ?? ""));
				}
			}

			return pairs;
		}
	}

	private string? String(string key) => FrontMatter.GetValueOrDefault(key)?.ToString();

	/// <summary>
	/// Every content page under <c>docs/</c>. Layouts, includes and data files are excluded:
	/// they are asserted separately.
	/// </summary>
	public static IReadOnlyList<SourcePage> All() => LazyAll.Value;

	private static readonly Lazy<IReadOnlyList<SourcePage>> LazyAll = new(LoadAll);

	private static IReadOnlyList<SourcePage> LoadAll()
	{
		var pages = new List<SourcePage>();

		foreach (var file in Directory.EnumerateFiles(RepoPaths.Docs, "*", SearchOption.AllDirectories))
		{
			var relative = RepoPaths.Relative(file);

			if (relative.Contains("/_layouts/", StringComparison.Ordinal)
				|| relative.Contains("/_includes/", StringComparison.Ordinal)
				|| relative.Contains("/_data/", StringComparison.Ordinal)
				|| relative.Contains("/assets/", StringComparison.Ordinal))
			{
				continue;
			}

			var extension = Path.GetExtension(file);

			if (extension is not (".html" or ".md"))
			{
				continue;
			}

			if (TryParse(file, out var page))
			{
				pages.Add(page);
			}
		}

		return pages;
	}

	private static bool TryParse(string file, out SourcePage page)
	{
		page = null!;
		var text = File.ReadAllText(file);
		var normalized = text.Replace("\r\n", "\n");

		if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
		{
			return false;
		}

		var end = normalized.IndexOf("\n---", 3, StringComparison.Ordinal);

		if (end < 0)
		{
			return false;
		}

		var yaml = normalized[4..end];
		var body = normalized[(end + 4)..].TrimStart('\n');

		var parsed = Yaml.Deserialize<Dictionary<string, object?>>(yaml) ?? [];

		page = new SourcePage(file, parsed, body);

		return true;
	}
}
