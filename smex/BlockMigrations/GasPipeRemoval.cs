using System.Collections.Generic;
using ExpandedLib.Migrations;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace SteelmakingExpanded.BlockMigrations;

/// <summary>
/// Purges the gas pipe blocks of released smex versions that no later version maps: the brickless
/// <c>gaspipe-heated-*</c> of 0.8.0 and the <c>gaspipe-passthroughbend-*</c> of 0.8.5. A save made
/// on those versions loses them on load.
/// </summary>
public class GasPipeRemoval : IBlockRemoval {
  private static readonly string[] HeatedFacings = ["ew", "ns", "sn", "we"];

  private static readonly string[] BendFacings =
    ["de", "dn", "ds", "dw", "en", "nw", "se", "ue", "un", "us", "uw", "ws"];

  public string Name => "Unmapped gas pipes";

  public IEnumerable<AssetLocation> GetRemovals(ICoreServerAPI api) {
    foreach (string facing in HeatedFacings)
      yield return new AssetLocation("smex", "gaspipe-heated-" + facing);
    foreach (string facing in BendFacings)
      yield return new AssetLocation("smex", "gaspipe-passthroughbend-" + facing);
  }
}
