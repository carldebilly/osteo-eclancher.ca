using YamlDotNet.Serialization;

namespace Site.Tests.Support;

/// <summary>
/// Reads the Jekyll data files (<c>docs/_data/*.yml</c>) the same loose way Liquid does,
/// so a test can compare the built output with the value it was rendered from.
/// </summary>
internal static class DataFile
{
	private static readonly IDeserializer Yaml = new DeserializerBuilder().Build();

	/// <summary>Parses a file relative to <c>docs/_data/</c>.</summary>
	public static Dictionary<string, object?> Load(string fileName)
		=> Yaml.Deserialize<Dictionary<string, object?>>(
			File.ReadAllText(Path.Combine(RepoPaths.Data, fileName))) ?? [];

	/// <summary>Walks nested mappings by key, or returns null as soon as one level is missing.</summary>
	public static object? Dig(object? node, params string[] path)
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
}
