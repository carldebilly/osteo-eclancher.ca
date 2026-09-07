namespace Site.Tests.Support;

/// <summary>
/// Locates the repository on disk from the test assembly's location, so the tests
/// run identically from the IDE, <c>dotnet test</c> and CI.
/// </summary>
internal static class RepoPaths
{
	private static readonly Lazy<string> LazyRoot = new(FindRoot);

	/// <summary>Absolute path of the repository root.</summary>
	public static string Root => LazyRoot.Value;

	/// <summary>Absolute path of the Jekyll source folder published by GitHub Pages.</summary>
	public static string Docs => Path.Combine(Root, "docs");

	/// <summary>Absolute path of the Jekyll data folder.</summary>
	public static string Data => Path.Combine(Docs, "_data");

	private static string FindRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);

		while (dir is not null)
		{
			if (File.Exists(Path.Combine(dir.FullName, "docs", "_config.yml")))
			{
				return dir.FullName;
			}

			dir = dir.Parent;
		}

		throw new InvalidOperationException(
			$"Could not find the repository root (a folder containing docs/_config.yml) above {AppContext.BaseDirectory}.");
	}

	/// <summary>Path relative to the repository root, using forward slashes, for readable assertion messages.</summary>
	public static string Relative(string absolutePath)
		=> Path.GetRelativePath(Root, absolutePath).Replace('\\', '/');
}
