using System.Collections.Generic;
using System.Linq;
using PipesAndPowerExpanded.ClosedLine;
using Vintagestory.API.Common;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.ClosedLine;

/// <summary>
/// The open line in <see cref="ClosedLineWorld.Open"/>, where neither iiex nor siex is enabled:
/// the recipes, the creative inventory and the drops stay as they were, with the closed line's
/// patches applied.
/// </summary>
public class OpenLineTests(ITestOutputHelper output) {
  private static ClosedLineWorld Open => ClosedLineWorld.Open;

  // Fails when the switch is forced closed: the ppex and smex recipes are removed.
  [Fact]
  public void Every_recipe_stays() {
    Dictionary<string, (int All, int OldLine)> after = Open.CountRecipes();
    foreach (string registry in ClosedLineRecipes.Registries)
      output.WriteLine(
        $"{registry}: {Open.RecipesBefore[registry].All} before, {after[registry].All} after"
      );

    Assert.Equal(Open.RecipesBefore, after);
    Assert.True(after["grid"].OldLine > 0);
  }

  // Fails when the switch is forced closed: the ppex and smex collectibles lose their tabs.
  [Fact]
  public void Every_creative_tab_stays() {
    Assert.NotEmpty(Open.InCreativeBefore);
    Assert.All(
      Open.InCreativeBefore,
      code => {
        CollectibleObject c = Open.OldLine.First(o => o.Code.ToString() == code);
        Assert.True(
          c.CreativeInventoryTabs is { Length: > 0 }
            || c.CreativeInventoryStacks is { Length: > 0 },
          code
        );
      }
    );
  }

  // Fails when the switch is forced closed, or the drop postfix ignores it: the ppex and smex
  // stacks leave the drops.
  [Fact]
  public void Every_block_drops_what_it_dropped_before() {
    List<Block> blocks = [.. Open.OldLine.OfType<Block>()];
    var baseline = new Dictionary<string, HashSet<string>>();
    foreach (Block block in blocks) {
      Open.Ready(block);
      if (Open.DropsOf(block).Codes is { } codes)
        baseline[block.Code.ToString()] = codes;
    }
    ClosedLinePatches.Apply(Open.World.Api);

    var changed = new List<string>();
    foreach (Block block in blocks)
      if (
        baseline.TryGetValue(block.Code.ToString(), out HashSet<string>? before)
        && Open.DropsOf(block).Codes is var after
        && (after == null || !before.SetEquals(after))
      )
        changed.Add(block.Code.ToString());
    output.WriteLine($"{baseline.Count} blocks dropped, {changed.Count} changed");

    Assert.Empty(changed);
    Assert.Contains(
      baseline.Values.SelectMany(c => c),
      c => ClosedLineModSystem.IsOldLine(new AssetLocation(c))
    );
  }
}
