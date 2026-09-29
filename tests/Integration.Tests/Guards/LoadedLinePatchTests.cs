using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Testing;
using Vintagestory.API.Common;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.Guards;

/// <summary>
/// smex's patches applied in <see cref="LoadedLine.World"/>, as a game applies them: the refractory
/// tiers it adds to ppex's pipes and the game's items it patches.
/// </summary>
public class LoadedLinePatchTests(ITestOutputHelper output) {
  // Fails when the mods load through one asset list each: smex's compat patch then finds no ppex
  // pipe to add its tiers to, and no refractory code is registered.
  [Fact]
  public void Smex_patches_its_refractory_tiers_into_ppex() {
    string[] alone =
    [
      .. CodesOf(LoadedLine.Ppex),
    ];
    string[] added =
    [
      .. CodesOf(LoadedLine.World).Except(alone).Order(),
    ];
    output.WriteLine($"{added.Length} codes:");
    foreach (string code in added)
      output.WriteLine("  " + code);

    Assert.Equal(63, added.Length);
    Assert.All(
      added,
      code =>
        Assert.Matches(
          "^ppex:pipe-(outlet|passthrough|passthroughbend)-refractorytier[123]-",
          code
        )
    );
  }

  // Fails when the loader stops mirroring the game's files that smex's patches name: the patch then
  // has no target and crushed-coke is never added.
  [Fact]
  public void Smex_patches_coke_into_vanilla_crushed() {
    Item? coke = LoadedLine.World.World.GetItem(
      new AssetLocation("game:crushed-coke")
    );

    Assert.NotNull(coke);
  }

  private static IEnumerable<string> CodesOf(TestWorld world) =>
    world
      .World.Blocks.Where(b => b?.Code?.Domain == "ppex")
      .Select(b => b.Code.ToString());
}
