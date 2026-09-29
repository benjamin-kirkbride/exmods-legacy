using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Testing;
using PipesAndPowerExpanded.BlockMigrations;
using Vintagestory.API.Common;
using Xunit;

namespace Integration.Tests.Saves;

/// <summary>
/// <see cref="PipeMigration"/>'s table for smex's refractory-tier gas pipes, over stand-ins for
/// ppex's passthrough in the tier smex's patch adds and in the fire brick: each old code is mapped
/// once, to the tier's block when it is loaded and to the fire brick when it is not.
/// </summary>
public class PipeMigrationTests {
  private const string OldCode = "smex:gaspipe-passthrough-refractorytier1-ns";

  // Fails when the fire fallback ignores a loaded tier: the old code is mapped twice, as exlib's
  // migration then warns on every boot.
  [Fact]
  public void A_loaded_tier_takes_its_old_code_once() {
    List<(AssetLocation Old, AssetLocation New)> remaps = Remaps(
      withTier: true
    );

    Assert.Empty(
      remaps.GroupBy(r => r.Old.ToString()).Where(g => g.Count() > 1)
    );
    Assert.Equal(
      "ppex:pipe-passthrough-refractorytier1-ns",
      remaps.Single(r => r.Old.ToString() == OldCode).New.ToString()
    );
  }

  // Fails when the fire fallback is dropped or kept only for loaded tiers: a world without smex's
  // tiers loses its refractory passthroughs.
  [Fact]
  public void An_unloaded_tier_falls_back_to_the_fire_brick() {
    List<(AssetLocation Old, AssetLocation New)> remaps = Remaps(
      withTier: false
    );

    Assert.Equal(
      "ppex:pipe-passthrough-fire-ns",
      remaps.Single(r => r.Old.ToString() == OldCode).New.ToString()
    );
  }

  private static List<(AssetLocation Old, AssetLocation New)> Remaps(
    bool withTier
  ) {
    using var world = new TestWorld();
    var bricks = new List<string> { "fire" };
    if (withTier)
      bricks.Add("refractorytier1");
    int id = 59000;
    foreach (string brick in bricks)
      world.Register(
        TestBlocks.Configure(
          new Block(),
          $"ppex:pipe-passthrough-{brick}-ns",
          id++,
          ("type", "passthrough"),
          ("brick", brick),
          ("orientation", "ns")
        )
      );
    return [.. new PipeMigration().GetRemaps(world.Api)];
  }
}
