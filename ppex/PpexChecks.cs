using ExpandedLib.Checks;

namespace PipesAndPowerExpanded;

/// <summary>The findings of exlib's checks ppex ships knowingly, each with the reason it
/// stands.</summary>
public static class PpexChecks {
  /// <summary>Why a structure's paid stages store its one metal more than once.</summary>
  public const string LastMetal =
    "the structure takes one metal through one stored key: several paid stages store it, and "
    + "the refund pays every stage in the metal paid last";

  /// <summary>Why the pump's and the blower's paid stages store their one wood and one metal more
  /// than once.</summary>
  public const string SharedKeys =
    "the structure takes one wood and one metal through one stored key each: its paid stages "
    + "share them, and the refund pays every stage in the wood and the metal paid last";

  /// <summary>Exempts <paramref name="domain"/>'s known findings; run from the mod system's
  /// <c>Start</c>, as exlib drops every exemption when a world starts loading.</summary>
  /// <param name="domain">ppex's domain, the one whose check runs report the findings.</param>
  public static void Declare(string domain) {
    foreach (
      string block in new[]
      {
        "ppex:boilercornish",
        "ppex:boilerlancashire",
        "ppex:enginecornish",
        "ppex:enginewatt",
      }
    )
      StoredAgain(domain, block, "metal", LastMetal);
    StoredAgain(domain, "ppex:mpfluidpump", "metal", SharedKeys);
  }

  /// <summary>Exempts every <c>StageWildcards</c> rule (g) finding of <paramref name="block"/> that
  /// its paid stages store <paramref name="key"/> again.</summary>
  /// <param name="domain">The domain whose check runs report the findings.</param>
  /// <param name="block">The blocktype's domain-qualified code, e.g.
  /// <c>"ppex:boilercornish"</c>.</param>
  /// <param name="key">The <c>storeWildCard</c> key, <c>"metal"</c> or <c>"wood"</c>.</param>
  /// <param name="reason">Why the finding stands, logged beside it.</param>
  public static void StoredAgain(
    string domain,
    string block,
    string key,
    string reason
  ) =>
    ExlibChecks.Exempt(
      domain,
      "StageWildcards",
      [block, "(g)", $"key {key}"],
      reason
    );
}
