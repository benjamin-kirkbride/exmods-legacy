using ExpandedLib.Helpers;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Structures;
using ExpandedLib.Testing;
using Integration.Tests.Guards;
using SteelmakingExpanded.BlockStructures.SmokeStack.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// The smoke stack intake's shaft stays clear of weather snow: the shipped intake, formed in every
/// orientation, marks the refractory floor under its shaft and the air cell above that floor in
/// <see cref="NoSnowCells"/>, and exlib's snow postfixes refuse snow there.
/// </summary>
public class SmokeStackSnowTests {
  /// <summary>The shipped intake facing <paramref name="orientation"/>, formed at
  /// <paramref name="anchor"/> in a fresh world whose pipe network it joins.</summary>
  internal static (TestWorld World, StructureRig Rig) Formed(
    BlockPos anchor,
    string orientation
  ) {
    var world = new TestWorld();
    world.RegisterNetwork("pipe", sys => new PipeNetwork(sys));
    Block intake = LoadedLine.World.World.GetBlock(
      new AssetLocation("smex:smokestack-intake-tier1-" + orientation)
    )!;
    var stack = new BlockEntitySmokeStack();
    world.Place(anchor, intake, stack);
    world.Attach(stack);
    StructureRig rig = StructureRig
      .Around(world, stack, ExOrientation.AngleFromSide(orientation))
      .Complete();
    return (world, rig);
  }

  // Fails when the nosnow role is removed from smokestack/intake.json.
  [Theory]
  [InlineData("n", 0)]
  [InlineData("e", 1)]
  [InlineData("s", 2)]
  [InlineData("w", 3)]
  public void A_formed_stack_keeps_snow_off_its_shaft_floor(
    string orientation,
    int site
  ) {
    var (world, rig) = Formed(
      new BlockPos(9000 + 100 * site, 10, 0),
      orientation
    );
    // Taken after the loaded line, whose load patches and releases exlib under its own id.
    using var harmony = new HarmonyFixture(
      "legacytest.nosnow",
      typeof(NoSnowCells).Assembly
    );
    BlockPos floor = rig.Cell(0, -1, 1);
    BlockPos settle = rig.Cell(0, 0, 1);
    Block brick = world.GetBlock(floor);
    var deeper = new Block();
    var layer = new Block { snowCovered1 = deeper };
    layer.notSnowCovered = layer;

    Assert.True(NoSnowCells.IsMarked(floor));
    Assert.True(NoSnowCells.IsMarked(settle));
    Assert.False(NoSnowCells.IsMarked(rig.Cell(0, 1, 1)));
    Assert.False(brick.AllowSnowCoverage(world.World, floor));
    Assert.True(brick.AllowSnowCoverage(world.World, rig.Cell(1, -1, 1)));
    Assert.Same(layer, layer.GetSnowCoveredVariant(settle, 2));
    Assert.Same(deeper, layer.GetSnowCoveredVariant(rig.Cell(0, 1, 1), 2));
  }
}
