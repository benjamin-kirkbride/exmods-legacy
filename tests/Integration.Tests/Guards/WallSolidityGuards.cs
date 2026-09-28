using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Testing;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.Guards;

/// <summary>
/// A block with full-cube collision can be built into a wall, and vanilla's room check reads a
/// face as closed only when it is side-solid. Every loaded ppex and smex block with one unit
/// collision box is side-solid on every face, or its blocktype is a finding; the rule is siex's
/// <c>WallBlockSolidityGuards</c>, read here from the blocks <see cref="LoadedLine"/> loaded.
/// </summary>
public class WallSolidityGuards(ITestOutputHelper output) {
  /// <summary>Finding, and why it stands.</summary>
  private static readonly Dictionary<string, string> Allowed = new();

  /// <summary>Finding, and the defect it records.</summary>
  private static readonly Dictionary<string, string> KnownFindings = new[] {
    "ppex:boilercornish",
    "ppex:boilerlancashire",
    "ppex:enginecornish",
    "ppex:enginefluidpump",
    "ppex:enginempgenerator",
    "ppex:enginewatt",
    "ppex:manualfluidpump",
    "ppex:mpfluidpump",
    "ppex:pipe",
    "smex:blastfurnacetap",
    "smex:converter",
    "smex:convertercontrol",
    "smex:convertertransmission",
    "smex:cowperstoveheatsink",
    "smex:engineairblower",
    "smex:hopperbell",
    "smex:hopperreinforced",
    "smex:moltenbarrel",
    "smex:moltencanal",
    "smex:mpblower",
    "smex:smokestack",
  }.ToDictionary(
    b => $"{b}: full-cube collision, not side-solid",
    _ =>
      "F-24: the default collision box, or one unit box, with \"sidesolid\": { \"all\": false }"
  );

  // Fails when a full-cube blocktype declares a face not side-solid, e.g. the cowper stove
  // intake's "sidesolid" turned from { "all": true } to { "all": false }.
  [Fact]
  public void Every_full_cube_block_is_side_solid() {
    List<Block> blocks = [.. LoadedLine.Blocks];
    var open = blocks
      .Where(b => IsFullCube(b) && OpenFaces(b).Any())
      .GroupBy(LoadedLine.Blocktype)
      .OrderBy(g => g.Key, System.StringComparer.Ordinal)
      .ToList();
    output.WriteLine(
      $"{blocks.Count} blocks, {blocks.Count(IsFullCube)} full-cube, {open.Count} finding(s)"
    );
    foreach (var blocktype in open)
      output.WriteLine(
        $"  {blocktype.Key}: {blocktype.Count()} variant(s), first "
          + $"{blocktype.First().Code} open on "
          + string.Join(", ", OpenFaces(blocktype.First()))
      );

    Assert.True(blocks.Count > 0, "neither mod loaded a block");
    FindingLists.Assert(
      open.Select(g => $"{g.Key}: full-cube collision, not side-solid"),
      Allowed,
      KnownFindings
    );
  }

  // Fails when a partial box, a box list or no collision is counted as a full cube.
  [Fact]
  public void Only_one_unit_collision_box_is_a_full_cube() {
    Block Boxed(params Cuboidf[]? boxes) => new() { CollisionBoxes = boxes };

    Assert.True(IsFullCube(Boxed(new Cuboidf(0, 0, 0, 1, 1, 1))));
    Assert.False(IsFullCube(Boxed(new Cuboidf(0, 0, 0, 1, 0.5f, 1))));
    Assert.False(
      IsFullCube(
        Boxed(new Cuboidf(0, 0, 0, 1, 1, 1), new Cuboidf(0, 0, 0, 1, 1, 1))
      )
    );
    Assert.False(IsFullCube(Boxed(null)));
  }

  private static bool IsFullCube(Block block) =>
    block.CollisionBoxes is [var box]
    && box.X1 == 0
    && box.Y1 == 0
    && box.Z1 == 0
    && box.X2 == 1
    && box.Y2 == 1
    && box.Z2 == 1;

  private static IEnumerable<string> OpenFaces(Block block) =>
    BlockFacing.ALLFACES.Where(f => !block.SideSolid[f.Index]).Select(f => f.Code);
}
