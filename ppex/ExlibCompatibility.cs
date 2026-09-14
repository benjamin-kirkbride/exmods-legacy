using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PipesAndPowerExpanded.Tests")]

namespace PipesAndPowerExpanded;

/// <summary>
/// exlib 0.8 moved the namespaces this mod binds to, and the game's dependency check accepts it
/// because a modinfo dependency is a floor. The version is compared by its first two numbers, so a
/// prerelease suffix does not matter.
/// </summary>
internal static class ExlibCompatibility {
  internal const string Message =
    "Pipes and Power Expanded 0.6 needs exlib 0.7.x, but exlib {0} is installed. "
    + "Downgrade exlib to 0.7.2, or replace this mod with Iron Industry Expanded.";

  internal static bool IsTooNew(string version) {
    string[] parts = version.Split('.');
    if (parts.Length < 2 || !int.TryParse(parts[0], out int major) || !int.TryParse(parts[1], out int minor))
      return false;
    return major > 0 || minor >= 8;
  }
}
