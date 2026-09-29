using ExpandedLib.Checks;
using PipesAndPowerExpanded;
using PipesAndPowerExpanded.ClosedLine;
using Vintagestory.API.Common;

namespace SteelmakingExpanded;

/// <summary>What smex's machines make that no recipe names, and the findings of exlib's checks
/// smex ships knowingly, each with the reason it stands.</summary>
public static class SmexChecks {
  /// <summary>Declares what <paramref name="domain"/>'s machines make and exempts its known
  /// findings; run from the mod system's <c>Start</c>, as exlib drops both when a world starts
  /// loading.</summary>
  /// <param name="api">The api of the starting side. The slag block is exempt only while the line
  /// is open (<see cref="ClosedLineModSystem.IsClosed"/>): the closed line takes it off the
  /// creative inventory, and an exemption matching no finding is itself reported.</param>
  /// <param name="domain">smex's domain, the one whose check runs report the findings.</param>
  public static void Declare(ICoreAPI api, string domain) {
    ExlibChecks.Produces(
      domain,
      "smex:solidifiediron",
      "the blast furnace, put out with molten iron in its hearth"
    );
    if (!ClosedLineModSystem.IsClosed(api))
      ExlibChecks.Exempt(
        domain,
        "Obtainability",
        "smex:slag",
        "the slag block has no in-world producer"
      );

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
