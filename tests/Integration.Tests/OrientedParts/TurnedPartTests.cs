using System.Text;
using ExpandedLib.Testing;
using Integration.Tests.Saves;
using NSubstitute;
using PipesAndPowerExpanded.BlockStructures.Boiler;
using PipesAndPowerExpanded.BlockStructures.Boiler.BlockEntities;
using SteelmakingExpanded.BlockStructures.BlastFurnace.BlockEntities;
using SteelmakingExpanded.BlockStructures.Converter.BlockEntities;
using SteelmakingExpanded.BlockStructures.CowperStove.BlockEntities;
using Vintagestory.API.MathTools;
using Xunit;

namespace Integration.Tests.OrientedParts;

/// <summary>
/// A part turned in place stops its structure: each shipped layout pins the facing of its fixed
/// parts (<c>multiblockFacings</c>) and the connector faces of its network nodes
/// (<c>multiblockConnectors</c>), so a complete structure with one part turned goes incomplete on
/// the next monitor tick and its production stops, and turning the part back restores both. Each
/// structure is formed turned away from its authored frame, so the checks are read rotated.
/// </summary>
public class TurnedPartTests {
  #region Boilers

  /// <summary>A boiler formed at side <paramref name="side"/>; its layout turns by the side's
  /// angle plus 180.</summary>
  private static OrientedPartsScene Boiler(
    string blocktype,
    BlockEntityBoiler be,
    string side,
    int layoutAngle,
    string door,
    string run,
    string bend,
    int site
  ) =>
    OrientedPartsScene.Form(
      new BlockPos(20000 + 100 * site, 10, 0),
      $"ppex:{blocktype}-{side}",
      be,
      layoutAngle,
      (0, 0, -2, $"game:cokeovendoor-closed-{door}"),
      (0, -1, -2, $"ppex:pipe-passthrough-fire-{run}"),
      (0, -1, -1, $"ppex:pipe-passthrough-fire-{run}"),
      (0, -1, 0, $"ppex:pipe-passthroughbend-fire-{bend}")
    );

  private static OrientedPartsScene Cornish() =>
    Boiler("boilercornish", new BlockEntityBoilerCornish(), "east", 90, "east", "we", "uw", 0);

  private static OrientedPartsScene Lancashire() =>
    Boiler("boilerlancashire", new BlockEntityBoilerLancashire(), "west", 270, "west", "we", "ue", 1);

  // Fails when a boiler's coke oven door legend is back to game:cokeovendoor* with its facing entry gone.
  [Theory]
  [InlineData("cornish")]
  [InlineData("lancashire")]
  public void A_turned_boiler_door_stops_the_boiler(string boiler) {
    bool cornish = boiler == "cornish";
    OrientedPartsScene scene = cornish ? Cornish() : Lancashire();
    string plateOutside = cornish ? "east" : "west";
    Assert.True(scene.Producing);

    scene.Turn(0, 0, -2, "game:cokeovendoor-closed-north").MonitorTick();
    Assert.False(scene.Machine.StructureComplete);
    Assert.False(scene.Producing);

    // The state stays a wildcard: an open door with its plate outside still completes.
    scene.Turn(0, 0, -2, $"game:cokeovendoor-opened-{plateOutside}").MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
  }

  // Fails when the passthroughs' marks are removed from the boiler's multiblockConnectors.
  [Theory]
  [InlineData("cornish")]
  [InlineData("lancashire")]
  public void A_re_oriented_boiler_passthrough_stops_the_boiler(string boiler) {
    OrientedPartsScene scene = boiler == "cornish" ? Cornish() : Lancashire();

    scene.Turn(0, -1, -1, "ppex:pipe-passthrough-fire-ns").MonitorTick();
    Assert.False(scene.Machine.StructureComplete);
    Assert.False(scene.Producing);

    scene.Turn(0, -1, -1, "ppex:pipe-passthrough-fire-we").MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
  }

  // Fails when the boiler's look-at line counts with vanilla's InCompleteBlockCount, which passes a
  // re-oriented node and reads the turned layout's door against its unturned facing (1 here).
  [Fact]
  public void The_boiler_look_at_line_counts_a_turned_part() {
    var be = new BlockEntityBoilerCornish();
    OrientedPartsScene scene = Boiler(
      "boilercornish", be, "east", 90, "east", "we", "uw", 2
    );
    SaveFixtures.CompleteConstruction(be, scene.World);

    // The harness echoes lang keys; this one key renders its count so the line can be read.
    TestLang
      .Service.Get("ppex:structure-incomplete-count", Arg.Any<object[]>())
      .Returns(ci => "incomplete, missing " + ci.ArgAt<object[]>(1)[0]);

    scene
      .Turn(0, -1, -2, "ppex:pipe-passthrough-fire-ns")
      .Turn(0, -1, -1, "ppex:pipe-passthrough-fire-ns")
      .MonitorTick();
    var info = new StringBuilder();
    be.GetBlockInfo(null!, info);

    Assert.Contains("incomplete, missing 2", info.ToString());
  }

  #endregion

  #region Blast furnace

  internal static OrientedPartsScene BlastFurnace() =>
    OrientedPartsScene.Form(
      new BlockPos(21000, 10, 0),
      "smex:blastfurnacedoor-tier1",
      new BlockEntityBlastFurnace(),
      0,
      (2, -2, 2, "smex:blastfurnacetap-tier1-west"),
      (-2, -1, 2, "smex:blastfurnacetap-tier1-east"),
      (0, -2, 1, "smex:blastfurnace-tuyere-tier1-n"),
      (0, -2, 3, "smex:blastfurnace-tuyere-tier1-s")
    );

  // Fails when the east-wall tap's legend is back to smex:blastfurnacetap* with its facing entry gone.
  [Fact]
  public void A_turned_iron_tap_stops_the_furnace() {
    OrientedPartsScene scene = BlastFurnace();
    Assert.True(scene.Machine.StructureComplete, scene.MissingReport);

    scene.Turn(2, -2, 2, "smex:blastfurnacetap-tier1-north").MonitorTick();
    Assert.False(scene.Machine.StructureComplete);
    Assert.False(scene.Producing);

    scene.Turn(2, -2, 2, "smex:blastfurnacetap-tier1-west").MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
  }

  // Fails when the two taps share one block number again, so either facing satisfies both walls.
  [Fact]
  public void The_slag_tap_facing_the_iron_taps_way_stops_the_furnace() {
    OrientedPartsScene scene = BlastFurnace();

    scene.Turn(-2, -1, 2, "smex:blastfurnacetap-tier1-west").MonitorTick();
    Assert.False(scene.Machine.StructureComplete);

    scene.Turn(-2, -1, 2, "smex:blastfurnacetap-tier1-east").MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
  }

  // Fails when the front tuyere's mark is removed from door.json's multiblockConnectors.
  [Fact]
  public void A_re_oriented_tuyere_stops_the_furnace() {
    OrientedPartsScene scene = BlastFurnace();

    scene.Turn(0, -2, 1, "smex:blastfurnace-tuyere-tier1-s").MonitorTick();
    Assert.False(scene.Machine.StructureComplete);
    Assert.False(scene.Producing);

    scene.Turn(0, -2, 1, "smex:blastfurnace-tuyere-tier1-n").MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
  }

  #endregion

  #region Bessemer converter

  // At side west the control turns its layout to 270: authored south reads west, e reads s.
  internal static OrientedPartsScene Converter() =>
    OrientedPartsScene.Form(
      new BlockPos(22000, 10, 0),
      "smex:convertercontrol-west",
      new BlockEntityConverterControl(),
      270,
      (0, -1, 0, "smex:convertertransmission-west"),
      (0, 0, 4, "smex:converter-intake-west"),
      (1, 1, 2, "smex:moltencanal-tap-s"),
      (2, 1, 2, "smex:moltencanal-straight-fire-ns"),
      (1, -2, 2, "smex:moltencanal-start-fire-s"),
      (2, -2, 2, "smex:moltencanal-straight-fire-ns")
    );

  // Fails when the transmission's legend is back to smex:convertertransmission* with its facing entry gone.
  [Fact]
  public void A_turned_transmission_stops_the_converter() {
    OrientedPartsScene scene = Converter();
    Assert.True(scene.Producing, scene.MissingReport);

    scene.Turn(0, -1, 0, "smex:convertertransmission-north").MonitorTick();
    Assert.False(scene.Machine.StructureComplete);
    Assert.False(scene.Producing);

    scene.Turn(0, -1, 0, "smex:convertertransmission-west").MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
  }

  // Fails when the input straight's marks are removed from control.json's multiblockConnectors.
  [Fact]
  public void A_re_oriented_input_canal_stops_the_converter() {
    OrientedPartsScene scene = Converter();

    scene.Turn(2, 1, 2, "smex:moltencanal-straight-fire-we").MonitorTick();
    Assert.False(scene.Machine.StructureComplete);
    Assert.False(scene.Producing);

    scene.Turn(2, 1, 2, "smex:moltencanal-straight-fire-ns").MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
  }

  #endregion

  #region Cowper stove

  // At side north the intake turns its layout to 180: authored south reads north, east reads west.
  internal static OrientedPartsScene Cowper() =>
    OrientedPartsScene.Form(
      new BlockPos(23000, 10, 0),
      "smex:cowperstove-intake-tier1-north",
      new BlockEntityCowperStove(),
      180,
      (0, 0, 1, "smex:cowperstoveheatsink-tier1-north"),
      (0, 1, 1, "smex:cowperstoveheatsink-tier1-north"),
      (0, 2, 1, "smex:cowperstoveheatsink-tier1-north"),
      (0, 3, 1, "smex:cowperstoveheatsink-tier1-north"),
      (-1, 0, 1, "game:cokeovendoor-closed-west"),
      (0, 1, 0, "ppex:pipe-outlet-fire-s"),
      (0, 0, 2, "ppex:pipe-outlet-fire-n"),
      (0, 1, 2, "ppex:pipe-passthrough-fire-ns")
    );

  // Fails when the heat sinks' legend is back to smex:cowperstoveheatsink* with its facing entry gone.
  [Fact]
  public void A_turned_heat_sink_stops_the_stove() {
    OrientedPartsScene scene = Cowper();
    Assert.True(scene.Producing, scene.MissingReport);

    scene.Turn(0, 2, 1, "smex:cowperstoveheatsink-tier1-east").MonitorTick();
    Assert.False(scene.Machine.StructureComplete);
    Assert.False(scene.Producing);

    scene.Turn(0, 2, 1, "smex:cowperstoveheatsink-tier1-north").MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
  }

  // Fails when the cowper's coke oven door legend is back to game:cokeovendoor* with its facing entry gone.
  [Fact]
  public void A_turned_stove_door_stops_the_stove() {
    OrientedPartsScene scene = Cowper();

    scene.Turn(-1, 0, 1, "game:cokeovendoor-closed-east").MonitorTick();
    Assert.False(scene.Machine.StructureComplete);

    scene.Turn(-1, 0, 1, "game:cokeovendoor-closed-west").MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
  }

  // Fails when the hot-blast outlet's mark is removed from intake.json's multiblockConnectors.
  [Fact]
  public void A_re_oriented_hot_blast_outlet_stops_the_stove() {
    OrientedPartsScene scene = Cowper();

    scene.Turn(0, 1, 0, "ppex:pipe-outlet-fire-n").MonitorTick();
    Assert.False(scene.Machine.StructureComplete);
    Assert.False(scene.Producing);

    scene.Turn(0, 1, 0, "ppex:pipe-outlet-fire-s").MonitorTick();
    Assert.True(scene.Producing, scene.MissingReport);
  }

  #endregion
}
