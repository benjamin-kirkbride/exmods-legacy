using ExpandedLib.Structures;
using ExpandedLib.Testing;
using SteelmakingExpanded.BlockStructures.SmokeStack.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// A formed smoke stack's chunk saved, unloaded and loaded again through
/// <see cref="TestWorld.Reload"/>, asked at the load for what vanilla's snow catch-up
/// (<c>WeatherSimulationSnowAccum.Event_BeginChunkColLoadChunkThread</c>) asks of the shaft floor:
/// <see cref="Block.AllowSnowCoverage"/>. The catch-up runs on the chunk thread as the column
/// loads, before the main thread initialises the column's block entities.
/// </summary>
public class SmokeStackSnowReloadTests {
  // Stands for the reloaded stack; asks the floor when its tree is read, the last step
  // TestWorld.Reload takes before Initialize.
  private sealed class LoadingStack : BlockEntitySmokeStack {
    internal Block? Brick;
    internal BlockPos? Floor;
    internal bool? MarkedAtLoad;
    internal bool? SnowAtLoad;

    public override void FromTreeAttributes(
      ITreeAttribute tree,
      IWorldAccessor worldForResolving
    ) {
      base.FromTreeAttributes(tree, worldForResolving);
      MarkedAtLoad = NoSnowCells.IsMarked(Floor!);
      SnowAtLoad = Brick!.AllowSnowCoverage(worldForResolving, Floor!);
    }
  }

  // Fails when the stack's unload drops its nosnow marks: at the load the floor is unmarked and
  // takes snow.
  [Fact]
  public void A_reloaded_stack_refuses_snow_on_its_floor_while_its_chunk_loads() {
    var anchor = new BlockPos(9600, 10, 0);
    var (world, rig) = SmokeStackSnowTests.Formed(anchor, "n");
    using var harmony = new HarmonyFixture(
      "legacytest.nosnowreload",
      typeof(NoSnowCells).Assembly
    );
    BlockPos floor = rig.Cell(0, -1, 1);
    var loading = new LoadingStack { Brick = world.GetBlock(floor), Floor = floor };
    world.RegisterBlockEntityFactory(
      world.GetBlock(anchor).EntityClass,
      () => loading
    );
    Assert.True(NoSnowCells.IsMarked(floor));

    BlockEntity? reloaded = world.Reload(anchor);

    Assert.Same(loading, reloaded);
    Assert.True(NoSnowCells.IsMarked(floor));
    Assert.Equal(
      (true, false),
      (loading.MarkedAtLoad!.Value, loading.SnowAtLoad!.Value)
    );
  }
}
