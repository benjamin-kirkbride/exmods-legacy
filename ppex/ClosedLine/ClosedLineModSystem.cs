using System.Linq;
using ExpandedLib.Helpers;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace PipesAndPowerExpanded.ClosedLine;

/// <summary>
/// Closes ppex and smex in a game where their successors load: what already stands keeps working,
/// and nothing new of either line can be made. Closed, the server removes every recipe of the two
/// lines (<see cref="ClosedLineRecipes"/>), both sides take their collectibles off the creative
/// inventory and the handbook, the client hides their guide pages, their blocks drop no ppex or smex
/// stack and a construction stage that takes a ppex straight pipe also takes the new line's
/// (<see cref="ClosedLinePatches"/>), and each player is told once per world
/// (<see cref="ClosedLineNotice"/>). Open, it does nothing.
/// </summary>
public class ClosedLineModSystem : ModSystem {
  /// <summary>The asset domains the switch closes.</summary>
  public static readonly string[] Domains = ["ppex", "smex"];

  /// <summary>The mod ids any one of which closes the line.</summary>
  public static readonly string[] Successors = ["iiex", "siex"];

  private bool _patched;

  /// <summary>Whether the line is closed in <paramref name="api"/>'s game: iiex or siex is
  /// enabled there. The game's <c>GetMod</c> answers only enabled mods.</summary>
  public static bool IsClosed(ICoreAPI api) =>
    Successors.Any(id => api.ModLoader.GetMod(id) != null);

  /// <summary>Whether <paramref name="code"/> is in the ppex or smex domain; false for
  /// null.</summary>
  public static bool IsOldLine(AssetLocation? code) =>
    code != null && Domains.Contains(code.Domain);

  public override void StartServerSide(ICoreServerAPI api) {
    if (!IsClosed(api))
      return;

    var removed = ClosedLineRecipes.Remove(api);
    int hidden = Hide(api);
    _patched |= ClosedLinePatches.Apply(api);
    ClosedLineNotice.Register(api);
    api.Logger.Notification(
      "[ppex] iiex or siex is enabled, so ppex and smex are closed. Removed {0} recipe(s) ({1}), took {2} collectible(s) off the creative inventory and the handbook, and their blocks drop no ppex or smex stack.",
      removed.Values.Sum(),
      string.Join(", ", removed.Select(r => $"{r.Key} {r.Value}")),
      hidden
    );
  }

  public override void StartClientSide(ICoreClientAPI api) {
    if (!IsClosed(api))
      return;

    Hide(api);
    _patched |= ClosedLinePatches.Apply(api);
    if (api.ModLoader.GetModSystem<ModSystemSurvivalHandbook>() is { } handbook)
      handbook.OnInitCustomPages += pages => pages.RemoveAll(IsGuidePage);
  }

  public override void Dispose() {
    if (_patched)
      ClosedLinePatches.Remove();
    _patched = false;
    base.Dispose();
  }

  /// <summary>Whether <paramref name="page"/> is a ppex or smex guide page: a text page whose title
  /// key is in either domain.</summary>
  public static bool IsGuidePage(GuiHandbookPage page) =>
    page is GuiHandbookTextPage { Title: { } title }
    && IsOldLine(new AssetLocation(title));

  private static int Hide(ICoreAPI api) =>
    ExContentGate.HideFromCreativeAndHandbook(api, obj => IsOldLine(obj.Code));
}
