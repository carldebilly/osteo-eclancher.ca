using System.Text.RegularExpressions;
using Xunit;
using Site.Tests.Support;

namespace Site.Tests.Source;

/// <summary>
/// The contact details and interface strings live in two YAML files because they are repeated
/// in the visible page, the footer and three JSON-LD nodes. Search engines and answer engines
/// merge a practice's listings by matching name, address and phone across sources, so one stale
/// copy is worse than none.
/// </summary>
public sealed class DataFilesTests
{
	private static Dictionary<string, object?> Load(string fileName) => DataFile.Load(fileName);

	private static object? Dig(object? node, params string[] path) => DataFile.Dig(node, path);

	[Fact(DisplayName = "The clinic address is complete and shaped like a real Québec address")]
	public void Given_the_business_data_When_reading_the_address_Then_it_is_complete()
	{
		var business = Load("business.yml");

		Assert.False(string.IsNullOrWhiteSpace(business.GetValueOrDefault("clinic_name")?.ToString()));
		Assert.False(string.IsNullOrWhiteSpace(Dig(business, "address", "streetAddress")?.ToString()));
		Assert.Equal("Verdun", Dig(business, "address", "addressLocality")?.ToString());
		Assert.Equal("QC", Dig(business, "address", "addressRegion")?.ToString());

		var postalCode = Dig(business, "address", "postalCode")?.ToString() ?? "";

		Assert.Matches(new Regex(@"^[A-Z]\d[A-Z] \d[A-Z]\d$"), postalCode);
	}

	[Fact(DisplayName = "The phone number is stored in the dialable form schema.org and mobile browsers expect")]
	public void Given_the_business_data_When_reading_the_phone_Then_it_is_in_e164_form()
	{
		var business = Load("business.yml");

		Assert.Matches(new Regex(@"^\+1\d{10}$"), business.GetValueOrDefault("telephone_e164")?.ToString() ?? "");
		Assert.False(string.IsNullOrWhiteSpace(business.GetValueOrDefault("telephone_display")?.ToString()));
	}

	[Fact(DisplayName = "Both booking links point at this practice's own GOrendezvous widget in the right language")]
	public void Given_the_business_data_When_reading_the_booking_links_Then_each_targets_the_right_widget()
	{
		var business = Load("business.yml");

		var french = Dig(business, "booking", "fr")?.ToString() ?? "";
		var english = Dig(business, "booking", "en")?.ToString() ?? "";

		Assert.Contains("companyId=101639", french, StringComparison.Ordinal);
		Assert.Contains("companyId=101639", english, StringComparison.Ordinal);
		Assert.Contains("/BookingWidget/fr", french, StringComparison.Ordinal);
		Assert.Contains("/BookingWidget/en", english, StringComparison.Ordinal);
	}

	[Fact(DisplayName = "The RITMA membership number is recorded, since it is what lets a client verify the practitioner")]
	public void Given_the_business_data_When_reading_the_credential_Then_the_membership_number_is_present()
	{
		var business = Load("business.yml");

		Assert.Equal("11364", Dig(business, "ritma", "credential", "identifier")?.ToString());
	}

	[Fact(DisplayName = "The French and English interface strings define exactly the same keys")]
	public void Given_the_translation_data_When_comparing_key_trees_Then_they_are_identical()
	{
		var i18n = Load("i18n.yml");

		var french = KeysOf(i18n.GetValueOrDefault("fr"), "");
		var english = KeysOf(i18n.GetValueOrDefault("en"), "");

		var onlyFrench = french.Except(english).OrderBy(static key => key, StringComparer.Ordinal).ToArray();
		var onlyEnglish = english.Except(french).OrderBy(static key => key, StringComparer.Ordinal).ToArray();

		Assert.Empty(onlyFrench);
		Assert.Empty(onlyEnglish);
	}

	private static HashSet<string> KeysOf(object? node, string prefix)
	{
		var keys = new HashSet<string>(StringComparer.Ordinal);

		if (node is Dictionary<object, object?> map)
		{
			foreach (var (key, value) in map)
			{
				var name = prefix + key;
				keys.Add(name);
				keys.UnionWith(KeysOf(value, name + "."));
			}
		}

		return keys;
	}
	[Fact(DisplayName = "The Facebook page is declared once and listed among the profiles search engines merge")]
	public void Given_the_business_data_When_reading_the_facebook_page_Then_it_is_an_https_url_present_in_same_as()
	{
		var business = Load("business.yml");

		var facebookUrl = business.GetValueOrDefault("facebook_url")?.ToString() ?? "";
		var sameAs = (business.GetValueOrDefault("same_as") as List<object?> ?? []).Select(x => x?.ToString());

		Assert.Matches(new Regex(@"^https://www\.facebook\.com/\S+$"), facebookUrl);
		Assert.Contains(facebookUrl, sameAs);
	}

	[Fact(DisplayName = "The analytics measurement id is configured once, in the shape Google issues")]
	public void Given_the_site_config_When_reading_the_analytics_id_Then_it_is_a_ga4_measurement_id()
	{
		var config = Load(Path.Combine("..", "_config.yml"));

		var analyticsId = config.GetValueOrDefault("analytics_id")?.ToString() ?? "";

		Assert.Matches(new Regex(@"^G-[A-Z0-9]{6,12}$"), analyticsId);
		Assert.Equal(Rules.AnalyticsId, analyticsId);
	}

	[Theory(DisplayName = "The privacy page names the analytics tool and drops the earlier claims of collecting nothing and writing no cookie")]
	[InlineData("confidentialite.md", "pas d’outil de mesure d’audience")]
	[InlineData("confidentialite.md", "aucun témoin")]
	[InlineData("confidentialite.md", "ne collecte rien")]
	[InlineData("en/privacy.md", "no analytics tool")]
	[InlineData("en/privacy.md", "no cookie")]
	[InlineData("en/privacy.md", "collects nothing")]
	public void Given_a_privacy_page_When_reading_it_Then_it_names_google_analytics_and_drops_the_old_denial(string relative, string denial)
	{
		var text = File.ReadAllText(Path.Combine(RepoPaths.Docs, relative));

		Assert.Contains("Google Analytics", text);
		Assert.DoesNotContain(denial, text);
	}

	[Theory(DisplayName = "The privacy page names the analytics cookies, their lifetime and how to refuse them, since Law 25 requires visitors be told")]
	[InlineData("confidentialite.md", "13 mois", "gaoptout")]
	[InlineData("en/privacy.md", "13 months", "gaoptout")]
	public void Given_a_privacy_page_When_reading_it_Then_the_analytics_cookies_are_disclosed(string relative, string lifetime, string refusal)
	{
		var text = File.ReadAllText(Path.Combine(RepoPaths.Docs, relative));

		Assert.Contains("_ga", text, StringComparison.Ordinal);
		Assert.Contains(lifetime, text, StringComparison.Ordinal);
		Assert.Contains(refusal, text, StringComparison.Ordinal);
	}

	[Theory(DisplayName = "The title in the header, footer and credential is the job title from business.yml, as printed on the business card")]
	[InlineData("fr")]
	[InlineData("en")]
	public void Given_the_data_files_When_reading_the_practitioner_title_Then_every_copy_matches_the_job_title(string lang)
	{
		var business = Load("business.yml");
		var i18n = Load("i18n.yml");

		var jobTitle = Dig(business, "person", "job_title", lang)?.ToString();
		var role = Dig(i18n, lang, "role")?.ToString();
		var credential = Dig(business, "ritma", "credential", "credentialCategory")?.ToString();

		Assert.False(string.IsNullOrWhiteSpace(jobTitle));
		Assert.Equal(jobTitle, role);
		Assert.Equal(Dig(business, "person", "job_title", "fr")?.ToString(), credential);
	}
}
