using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Testing;
using NSubstitute;
using PipesAndPowerExpanded.ClosedLine;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.ClosedLine;

/// <summary>
/// The open line in <see cref="ClosedLineWorld.Open"/>, where neither iiex nor siex is enabled:
/// the recipes, the creative inventory and the drops stay as they were, with the closed line's
/// patches applied, and no stack is swept.
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

  // Fails when the switch is forced closed: the sweep takes the ppex and smex stacks.
  [Fact]
  public void Nothing_is_swept() {
    TestPlayer joiner = Open.World.Player("sweeper");
    IServerPlayer player = Assert.IsAssignableFrom<IServerPlayer>(joiner.ServerPlayer);
    joiner.Hotbar[0].Itemstack = new ItemStack(Open.Pipe);
    player.InventoryManager.Inventories.Returns(
      new Dictionary<string, IInventory> { ["hotbar"] = joiner.Hotbar }
    );
    var rack = new BlockEntityMoldRack {
      Block = new Block { Code = new AssetLocation("game:moldrack-normal") },
      Pos = new BlockPos(0, 1, 0),
    };
    rack.Inventory[0].Itemstack = new ItemStack(Open.Mold);
    IWorldChunk chunk = Substitute.For<IWorldChunk>();
    chunk.BlockEntities.Returns(new Dictionary<BlockPos, BlockEntity> { [rack.Pos] = rack });
    var drop = new EntityItem { Itemstack = new ItemStack(Open.Pipe) };

    Open.World.Api.Event.PlayerJoin += Raise.Event<PlayerDelegate>(player);
    Open.World.Api.Event.ChunkDirty += Raise.Event<ChunkDirtyDelegate>(
      new Vec3i(0, 0, 0),
      chunk,
      EnumChunkDirtyReason.NewlyLoaded
    );
    Open.World.Api.Event.OnEntitySpawn += Raise.Event<EntityDelegate>(drop);

    Assert.Equal(["ppex:pipe-straight-ns-iron"], ClosedLineWorld.Codes(joiner.Hotbar));
    Assert.Equal([Open.Mold.Code.ToString()], ClosedLineWorld.Codes(rack.Inventory));
    Assert.True(drop.Alive);
    Assert.Equal(
      [Open.Mold.Code.ToString(), "game:ingot-iron"],
      ClosedLineWorld.Codes(Open.StartRack.Inventory)
    );
    Assert.True(Open.StartDrop.Alive);
  }
}
