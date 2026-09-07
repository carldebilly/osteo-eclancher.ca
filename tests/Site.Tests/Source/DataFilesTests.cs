using System.Text.RegularExpressions;
using Xunit;
using YamlDotNet.Serialization;
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
	private static readonly IDeserializer Yaml = new DeserializerBuilder().Build();

	private static Dictionary<string, object?> Load(string fileName)
		=> Yaml.Deserialize<Dictionary<string, object?>>(
			File.ReadAllText(Path.Combine(RepoPaths.Data, fileName))) ?? [];

	private static object? Dig(object? node, params string[] path)
	{
		foreach (var key in path)
		{
			node = node switch
			{
				Dictionary<string, object?> typed => typed.GetValueOrDefault(key),
				Dictionary<object, object?> loose => loose.GetValueOrDefault(key),
				_ => null,
			};
		}

		return node;
	}

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
}
