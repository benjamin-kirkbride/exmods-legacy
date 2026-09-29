using ExpandedLib.Checks;
using PipesAndPowerExpanded;

namespace SteelmakingExpanded;

/// <summary>The findings of exlib's checks smex ships knowingly, each with the reason it
/// stands.</summary>
public static class SmexChecks {
  /// <summary>Exempts <paramref name="domain"/>'s known findings; run from the mod system's
  /// <c>Start</c>, as exlib drops every exemption when a world starts loading.</summary>
  /// <param name="domain">smex's domain, the one whose check runs report the findings.</param>
  public static void Declare(string domain) {
    PpexChecks.StoredAgain(
      domain,
      "smex:converterbessemer",
      "metal",
      PpexChecks.LastMetal
    );
    PpexChecks.StoredAgain(domain, "smex:mpblower", "metal", PpexChecks.SharedKeys);
    PpexChecks.StoredAgain(domain, "smex:mpblower", "wood", PpexChecks.SharedKeys);
  }
}
