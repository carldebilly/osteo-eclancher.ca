using Site.Tests.Support;
using Xunit;

namespace Site.Tests.Source;

/// <summary>
/// Reads every source file a visitor's browser could end up rendering and fails on copy
/// that would breach the RITMA code of ethics, or on placeholder text.
/// </summary>
/// <remarks>
/// This is the test that matters most on this site. A wording slip here is not a bug a user
/// reports: it is a complaint to the association, and it would be found by whoever is looking
/// for grounds to file one. Running it on the data files and templates as well as the pages
/// matters because the contact block, the footer and the JSON-LD all carry prose too.
/// </remarks>
public sealed class DeontologyLintTests
{
	public static TheoryData<string> AllTextFiles()
	{
		var data = new TheoryData<string>();

		foreach (var file in Directory.EnumerateFiles(RepoPaths.Docs, "*", SearchOption.AllDirectories))
		{
			var relative = RepoPaths.Relative(file);
			var extension = Path.GetExtension(file);

			if (extension is ".html" or ".md" or ".yml" or ".txt" or ".webmanifest")
			{
				data.Add(relative);
			}
		}

		return data;
	}

	/// <summary>
	/// Removes the allowed disclaimers before scanning, so a sentence that denies a claim is not
	/// read as making it. Everything else in the file is still scanned.
	/// </summary>
	private static string WithoutAllowedDisclaimers(string text)
	{
		foreach (var disclaimer in Rules.AllowedDisclaimers)
		{
			text = text.Replace(disclaimer, "", StringComparison.OrdinalIgnoreCase);
		}

		return text;
	}

	[Theory(DisplayName = "No source file promises recovery, claims a diagnosis or guarantees a result")]
	[MemberData(nameof(AllTextFiles))]
	public void Given_a_source_file_When_scanning_for_forbidden_claims_Then_none_is_present(string relativePath)
	{
		var text = WithoutAllowedDisclaimers(File.ReadAllText(Path.Combine(RepoPaths.Root, relativePath)));
		var found = new List<string>();

		foreach (var (pattern, why) in Rules.ForbiddenClaims)
		{
			foreach (var match in pattern.Matches(text).Cast<System.Text.RegularExpressions.Match>())
			{
				found.Add($"'{match.Value}' at offset {match.Index} ({why})");
			}
		}

		Assert.Empty(found);
	}

	[Theory(DisplayName = "No source file still carries placeholder text or the old broken email markup")]
	[MemberData(nameof(AllTextFiles))]
	public void Given_a_source_file_When_scanning_for_placeholders_Then_none_is_present(string relativePath)
	{
		var text = File.ReadAllText(Path.Combine(RepoPaths.Root, relativePath));
		var found = new List<string>();

		foreach (var (pattern, why) in Rules.ForbiddenPlaceholders)
		{
			foreach (var match in pattern.Matches(text).Cast<System.Text.RegularExpressions.Match>())
			{
				found.Add($"'{match.Value}' at offset {match.Index} ({why})");
			}
		}

		Assert.Empty(found);
	}

	[Fact(DisplayName = "Both health pages say in writing that osteopathy adds to medical care rather than replacing it")]
	public void Given_the_health_pages_When_reading_their_body_Then_each_defers_to_medical_care()
	{
		var problems = new List<string>();

		foreach (var page in SourcePage.All()
			.Where(static page => page.Ref is "fibromyalgia" or "chronic"))
		{
			var text = page.Body;

			var defers = page.Lang == "fr"
				? text.Contains("ne remplace pas", StringComparison.OrdinalIgnoreCase)
				: text.Contains("does not replace", StringComparison.OrdinalIgnoreCase);

			if (!defers)
			{
				problems.Add(page.RelativePath);
			}
		}

		Assert.Empty(problems);
	}

	[Fact(DisplayName = "Every page mentioning insurance also states that the intern title is not reimbursed by every insurer")]
	public void Given_a_page_mentioning_insurance_When_reading_it_Then_the_intern_caveat_is_present()
	{
		var problems = new List<string>();

		foreach (var page in SourcePage.All().Where(static page => page.Ref is "fees"))
		{
			var text = page.Body;

			var caveat = page.Lang == "fr"
				? text.Contains("interne en ostéopathie", StringComparison.OrdinalIgnoreCase)
					&& text.Contains("assureur", StringComparison.OrdinalIgnoreCase)
				: text.Contains("intern", StringComparison.OrdinalIgnoreCase)
					&& text.Contains("insurer", StringComparison.OrdinalIgnoreCase);

			if (!caveat)
			{
				problems.Add(page.RelativePath);
			}
		}

		Assert.Empty(problems);
	}

	[Fact(DisplayName = "Every allowed disclaimer denies or asks, so the exemption cannot smuggle in a claim")]
	public void Given_the_allowed_disclaimers_When_reading_their_form_Then_each_denies_or_asks()
	{
		var offenders = new List<string>();

		foreach (var disclaimer in Rules.AllowedDisclaimers)
		{
			var asks = disclaimer.TrimEnd().EndsWith('?');
			var denies = disclaimer.Contains("n’est pas", StringComparison.OrdinalIgnoreCase)
				|| disclaimer.Contains("is not", StringComparison.OrdinalIgnoreCase);

			if (!asks && !denies)
			{
				offenders.Add(disclaimer);
			}
		}

		Assert.Empty(offenders);
	}

	[Fact(DisplayName = "Every allowed disclaimer is actually used, so no dead exemption accumulates")]
	public void Given_the_allowed_disclaimers_When_searching_the_site_Then_each_appears_somewhere()
	{
		var corpus = string.Join(
			"\n",
			Directory
				.EnumerateFiles(RepoPaths.Docs, "*", SearchOption.AllDirectories)
				.Where(static file => Path.GetExtension(file) is ".html" or ".md" or ".yml" or ".txt")
				.Select(File.ReadAllText));

		var unused = Rules.AllowedDisclaimers
			.Where(disclaimer => !corpus.Contains(disclaimer, StringComparison.OrdinalIgnoreCase))
			.ToArray();

		Assert.Empty(unused);
	}
}
