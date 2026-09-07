using System.Text.RegularExpressions;

namespace Site.Tests.Support;

/// <summary>
/// The editorial policy for this site, expressed as patterns.
/// </summary>
/// <remarks>
/// Osteopathy is not yet a regulated profession in Québec, so the RITMA code of ethics is
/// what binds the site today: article II.12 forbids making a medical diagnosis, VI.8 forbids
/// promising recovery or remission even in good faith, and II.11 requires every claim about
/// competence and effectiveness to be fair and verifiable. Québec announced in June 2026 that
/// osteopaths will join the Ordre des chiropraticiens, whose advertising rules will be stricter
/// still, so the copy is written to survive that transition unchanged.
///
/// The patterns target the forbidden *claim*, not the vocabulary. "Le diagnostic est posé par
/// votre médecin" is exactly the sentence the code wants; "je pose un diagnostic" is the one it
/// forbids. Matching the bare word would push the copy into vagueness, which serves nobody.
/// </remarks>
internal static class Rules
{
	private const RegexOptions Options =
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

	/// <summary>Claims no RITMA member may publish, with the reason shown when one is found.</summary>
	public static readonly IReadOnlyList<(Regex Pattern, string Why)> ForbiddenClaims =
	[
		(new Regex(@"gu[eé]ri(r|t|e|es|son|ssent)\b", Options),
			"RITMA VI.8 forbids promising recovery, even in good faith"),
		(new Regex(@"r[eé]mission", Options),
			"RITMA VI.8 forbids promising remission"),
		(new Regex(@"\b(je|nous)\s+(pose|posons|[eé]tabli[st]|[eé]tablissons|fais|faisons)\s+(un|le|des)?\s*diagnostic", Options),
			"RITMA II.12 forbids making a medical diagnosis"),
		(new Regex(@"diagnostic\s+ost[eé]opathique", Options),
			"RITMA II.12: there is no osteopathic diagnosis to advertise"),
		(new Regex(@"diagnostiqu(e|er|ons)\s+(la|le|les|votre|vos)", Options),
			"RITMA II.12 forbids making a medical diagnosis"),
		(new Regex(@"(traite|traiter|traitement\s+de)\s+la\s+fibromyalgie", Options),
			"the practice accompanies people; it does not treat the condition"),
		(new Regex(@"soigne(r|z)?\s+(la|votre)\s+(fibromyalgie|anxi[eé]t[eé]|maladie)", Options),
			"RITMA II.11: care of a named condition is a medical claim"),
		(new Regex(@"r[eé]sultats?\s+garanti", Options),
			"RITMA II.11 requires verifiable claims"),
		(new Regex(@"\bgarantie?s?\s+(une|le|la|des|votre)", Options),
			"RITMA II.11 requires verifiable claims"),
		(new Regex(@"sans\s+(aucun\s+)?risque", Options),
			"no manual therapy is risk-free"),
		(new Regex(@"\bmiracle|miraculeu", Options),
			"RITMA I.9 requires integrity and accuracy"),
		(new Regex(@"[eé]limine\s+(la|les|vos)\s+douleur", Options),
			"RITMA II.11 requires verifiable claims"),
		(new Regex(@"\bcures?\b|\bcured\b", Options),
			"English mirror of the recovery-promise rule"),
		(new Regex(@"\bheals?\b|\bhealing\b", Options),
			"English mirror of the recovery-promise rule"),
		(new Regex(@"treats?\s+fibromyalgia|treatment\s+for\s+fibromyalgia", Options),
			"the practice accompanies people; it does not treat the condition"),
		(new Regex(@"\bguaranteed\b", Options),
			"claims must be verifiable"),
		(new Regex(@"\bi\s+diagnose\b|osteopathic\s+diagnosis", Options),
			"RITMA II.12 forbids making a medical diagnosis"),
	];

	/// <summary>
	/// Sentences and headings allowed to contain an otherwise-forbidden phrase, because they
	/// deny the claim rather than make it.
	/// </summary>
	/// <remarks>
	/// "L'ostéopathie n'est pas un traitement de la fibromyalgie" is the most trust-bearing
	/// sentence the site can carry, and it is exactly what RITMA II.11 asks for. It also matches
	/// the claim pattern above, because a regular expression cannot tell a denial from an
	/// assertion.
	///
	/// The exemption is a list of exact strings, not a negative lookbehind on the pattern. A
	/// lookbehind would silently permit every future negation shape, including "je ne peux pas
	/// garantir un traitement de la fibromyalgie, mais…" — a claim wearing a denial. Adding a
	/// sentence here is a deliberate act, and two tests guard the list: every entry must be
	/// interrogative or negative in form, and every entry must actually appear in the site.
	/// </remarks>
	public static readonly IReadOnlyList<string> AllowedDisclaimers =
	[
		"l’ostéopathie n’est pas un traitement de la fibromyalgie",
		// The narrow no-break space before the question mark is French typography, enforced elsewhere.
		"L’ostéopathie est-elle un traitement de la fibromyalgie ?",
		"osteopathy is not a treatment for fibromyalgia",
		"Is osteopathy a treatment for fibromyalgia?",
	];

	/// <summary>Text that must never reach the published site.</summary>
	public static readonly IReadOnlyList<(Regex Pattern, string Why)> ForbiddenPlaceholders =
	[
		(new Regex(@"[àa]\s+confirmer|to\s+confirm", Options),
			"the live site showed \"Verdun — à confirmer\" for months; the address is now known"),
		(new Regex(@"\bTODO\b|\bTBD\b|\bFIXME\b", RegexOptions.CultureInvariant | RegexOptions.Compiled),
			"unfinished copy"),
		(new Regex(@"\blorem\s+ipsum\b", Options),
			"placeholder copy"),
		(new Regex(@"cdn-cgi|__cf_email__", Options),
			"leftover Cloudflare email obfuscation; it produced a broken 404 link on the live site"),
		(new Regex(@"mailto:", Options),
			"the email address was deliberately removed in favour of online booking"),
	];

	/// <summary>Every URL the site publishes, as the agreed contract between the two languages.</summary>
	public static readonly IReadOnlyDictionary<string, (string Fr, string En)> ExpectedUrls =
		new Dictionary<string, (string, string)>
		{
			["home"] = ("/", "/en/"),
			["fibromyalgia"] = ("/maladies-chroniques/fibromyalgie/", "/en/chronic-conditions/fibromyalgia/"),
			["chronic"] = ("/maladies-chroniques/", "/en/chronic-conditions/"),
			["fibromyalgia-resources"] = (
				"/maladies-chroniques/fibromyalgie/ressources-montreal/",
				"/en/chronic-conditions/fibromyalgia/montreal-resources/"),
			["about"] = ("/a-propos/", "/en/about/"),
			["fees"] = ("/tarifs-et-assurances/", "/en/fees-and-insurance/"),
			["privacy"] = ("/confidentialite/", "/en/privacy/"),
		};

	/// <summary>
	/// Hosts the site may link out to. A typo in an external domain produces a link that looks
	/// fine and goes nowhere, and the resource page exists entirely to send readers elsewhere.
	/// </summary>
	/// <remarks>
	/// The Montréal fibromyalgia association is a live example of why this list is checked:
	/// most directories still publish afim.qc.ca for it, and that domain no longer resolves.
	/// This list catches a mistyped or newly added host; it cannot catch a host that dies.
	/// A separate network test, excluded from the required check, does that.
	/// </remarks>
	public static readonly IReadOnlySet<string> AllowedExternalHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		// Booking, the clinic, the association the practitioner belongs to
		"www.gorendezvous.com",
		"www.hakini.ca",
		"www.ritma.ca",
		// The map link in the contact block
		"www.google.com",
		// Condition entities referenced by the structured data
		"fr.wikipedia.org",
		"en.wikipedia.org",
		// Patient organisations and public resources, on the resource pages
		"www.fibromyalgiemontreal.ca",
		"www.sqf.quebec",
		"sqf.quebec",
		"douleurquebec.ca",
		"gerermadouleur.ca",
		"publications.msss.gouv.qc.ca",
		// The professional order osteopaths are being integrated into
		"www.ordredeschiropraticiens.ca",
	};

	/// <summary>Upper bound for the HTML title element, past which search results truncate it.</summary>
	public const int MaxTitleLength = 65;

	/// <summary>Below this, the description wastes the space a search result gives it.</summary>
	public const int MinDescriptionLength = 70;

	/// <summary>Above this, search results truncate it mid-sentence.</summary>
	public const int MaxDescriptionLength = 165;
}
