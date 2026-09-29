using ExpandedLib.Testing;
using NSubstitute;
using SteelmakingExpanded.BlockStructures.BlastFurnace.BlockEntities;
using SteelmakingExpanded.BlockStructures.Converter.BlockEntities;
using SteelmakingExpanded.BlockStructures.CowperStove.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Xunit;

namespace Integration.Tests.OrientedParts;

/// <summary>
/// The four fixed parts nothing else can turn (the blast furnace tap, the cowper heat sink, the
/// converter's intake and transmission) turn under the vanilla wrench: a turn exchanges the
/// <c>side</c> variant in place, so a part turned out of its layout stops its structure, turning it
/// back restores it, and the part's block entity keeps its state through both.
/// </summary>
public class WrenchTurnTests {
  #region Out of the layout and back

  // Fails when SideWrench.Turn places the turned block with SetBlock, which re-creates the tap's
  // block entity closed; and when the tap's Rotate does nothing.
  [Fact]
  public void The_iron_tap_turns_out_and_back_and_keeps_pouring() {
    OrientedPartsScene scene = TurnedPartTests.BlastFurnace();
    var tap = new BlockEntityBlastFurnaceTap();
    scene.Stand(scene.Cell(2, -2, 2), tap);
    tap.TogglePouring();

    scene.Wrench(2, -2, 2, 1).MonitorTick();
    Assert.Equal("south", scene.World.GetBlock(scene.Cell(2, -2, 2)).Variant["side"]);
    Assert.False(scene.Machine.StructureComplete);
    Assert.False(scene.Producing);

    scene.Wrench(2, -2, 2, -1).MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
    Assert.Same(tap, scene.World.GetBlockEntity(scene.Cell(2, -2, 2)));
    Assert.Equal("west", tap.Block.Variant["side"]);
    Assert.True(tap.IsPouring);
  }

  // Fails when SideWrench.Turn places the turned block with SetBlock, which re-creates the heat
  // sink's block entity cold; and when the heat sink's Rotate does nothing.
  [Fact]
  public void A_heat_sink_turns_out_and_back_and_keeps_its_heat() {
    OrientedPartsScene scene = TurnedPartTests.Cowper();
    var sink = new BlockEntityHeatSink();
    scene.Stand(scene.Cell(0, 2, 1), sink);
    sink.Temperature = 900f;

    scene.Wrench(0, 2, 1, 1).MonitorTick();
    Assert.False(scene.Machine.StructureComplete);
    Assert.False(scene.Producing);

    scene.Wrench(0, 2, 1, -1).MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
    Assert.Same(sink, scene.World.GetBlockEntity(scene.Cell(0, 2, 1)));
    Assert.Equal(900f, sink.Temperature);
  }

  // Fails when the intake's Rotate does nothing.
  [Fact]
  public void The_intake_turns_out_and_back() {
    OrientedPartsScene scene = TurnedPartTests.Converter();

    scene.Wrench(0, 0, 4, 1).MonitorTick();
    Assert.Equal(
      "smex:converter-intake-south",
      scene.World.GetBlock(scene.Cell(0, 0, 4)).Code.ToString()
    );
    Assert.False(scene.Machine.StructureComplete);
    Assert.False(scene.Producing);

    scene.Wrench(0, 0, 4, -1).MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
  }

  // Fails when SideWrench.Turn places the turned block with SetBlock (a new block entity stands in
  // the cell); when the transmission's OnExchanged or Recouple leaves the network alone, so the
  // turned transmission stays on the axle's network; and when Recouple leaves without discovering,
  // so the turned-back transmission is on none.
  [Fact]
  public void The_transmission_turns_out_and_back_and_re_couples_its_axle() {
    OrientedPartsScene scene = TurnedPartTests.Converter();
    BlockPos at = scene.Cell(0, -1, 0);
    var transmission = new BlockEntityConverterTransmission();
    scene.Stand(at, transmission);
    // A second transmission facing back stands in for the axle line on the connector face.
    var axle = new BlockEntityConverterTransmission();
    scene.Stand(at.EastCopy(), axle, "smex:convertertransmission-east");
    var mp = transmission.GetBehavior<BEBehaviorMPConverterTransmission>()!;
    var line = axle.GetBehavior<BEBehaviorMPConverterTransmission>()!.Network!;
    Assert.Same(line, mp.Network);

    scene.Wrench(0, -1, 0, 1).MonitorTick();
    Assert.False(scene.Machine.StructureComplete);
    Assert.Equal(BlockFacing.NORTH, mp.OutFacingForNetworkDiscovery);
    Assert.NotNull(mp.Network);
    Assert.NotSame(line, mp.Network);
    Assert.DoesNotContain(line.nodes.Values, n => n == mp);

    scene.Wrench(0, -1, 0, -1).MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
    Assert.Same(transmission, scene.World.GetBlockEntity(at));
    Assert.Equal(BlockFacing.EAST, mp.OutFacingForNetworkDiscovery);
    Assert.Same(line, mp.Network);
  }

  #endregion

  #region The turn

  // Fails when SideWrench.Turn drops the sign of dir or turns the other way round.
  [Theory]
  [InlineData(1, "south")]
  [InlineData(-1, "north")]
  [InlineData(2, "east")]
  [InlineData(4, "west")]
  [InlineData(-5, "north")]
  public void The_wrench_turns_a_quarter_per_step(int dir, string side) {
    OrientedPartsScene scene = TurnedPartTests.BlastFurnace();

    scene.Wrench(2, -2, 2, dir);

    Assert.Equal(side, scene.World.GetBlock(scene.Cell(2, -2, 2)).Variant["side"]);
  }

  // Fails when SideWrench.Turn exchanges without checking the turned block exists.
  [Fact]
  public void A_turn_to_a_facing_the_world_lacks_leaves_the_part() {
    OrientedPartsScene scene = TurnedPartTests.BlastFurnace();

    scene.Wrench(2, -2, 2, 1, registerTurns: false).MonitorTick();

    Assert.Equal(
      "smex:blastfurnacetap-tier1-west",
      scene.World.GetBlock(scene.Cell(2, -2, 2)).Code.ToString()
    );
    Assert.True(scene.Producing, scene.MissingReport);
  }

  #endregion

  #region Look-at help

  // Fails when a part's GetPlacedBlockInteractionHelp does not append SideWrench.Help.
  [Theory]
  [InlineData("smex:blastfurnacetap-tier1-west")]
  [InlineData("smex:cowperstoveheatsink-tier1-north")]
  [InlineData("smex:converter-intake-west")]
  [InlineData("smex:convertertransmission-west")]
  public void The_look_at_help_offers_the_wrench_turn(string code) {
    var world = new TestWorld();
    world.RegisterItem("game:wrench-iron");
    var pos = new BlockPos(0, 10, 0);
    Block part = OrientedPartsScene.Loaded(code);
    world.Place(pos, part);

    WorldInteraction[] help = part.GetPlacedBlockInteractionHelp(
      world.World,
      new BlockSelection { Position = pos, Face = BlockFacing.UP },
      Substitute.For<IPlayer>()
    );

    WorldInteraction turn = Assert.Single(
      help,
      h => h.ActionLangCode == "smex:blockhelp-rotate"
    );
    Assert.Equal(EnumMouseButton.Right, turn.MouseButton);
    Assert.Equal(
      "game:wrench-iron",
      Assert.Single(turn.Itemstacks).Collectible.Code.ToString()
    );
  }

  #endregion
}
