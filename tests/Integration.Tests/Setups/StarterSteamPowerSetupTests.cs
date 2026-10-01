using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using ExpandedLib.Testing;
using Integration.Tests.Saves;
using PipesAndPowerExpanded;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using PipesAndPowerExpanded.Helpers;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Xunit;
using Xunit.Abstractions;
using AssetLocation = Vintagestory.API.Common.AssetLocation;
using Ticker = Integration.Tests.Setups.StarterSteamPowerPlant.Ticker;

namespace Integration.Tests.Setups;

/// <summary>
/// The starter steam power setup builds as drawn, fires from cold and settles where the drawing
/// says: the boiler never chokes or reaches its limit, the steam main holds the Watt engine's band,
/// the boiler's water holds, and the steam run carries the boiler's 32 L/s. Its recording is the
/// one the wiki plays.
/// </summary>
public class StarterSteamPowerSetupTests(ITestOutputHelper output) {
  #region Fixtures

  private const int RunSeconds = 1200;

  /// <summary>The steam run's flow the drawing prints (L/s).</summary>
  private const double DrawnSteamFlow = 32.0;

  /// <summary>What the steam main's relief valve vents in the drawing (L/s): the boiler's make
  /// over the engine's draw.</summary>
  private const double DrawnSteamVent = 2.0;

  private const float BandLow = 2f;
  private const float BandHigh = 4f;

  /// <summary>
  /// Fails <paramref name="letter"/> at the first second from <paramref name="from"/> that
  /// <paramref name="failed"/> holds, naming the second and its readings.
  /// </summary>
  private static void Never(
    StarterSteamPowerPlant plant,
    string letter,
    Func<StarterSteamPowerPlant.Second, bool> failed,
    int from = 0
  ) {
    int? at = plant.First(failed, from);
    Assert.True(
      at == null,
      $"{letter} at second {at}: {(at == null ? "" : plant.Seconds[at.Value].ToString())}"
    );
  }

  /// <summary>
  /// Fails D13's letters a to d in order: the boiler never chokes; it stays below its limit, no pipe
  /// is lost and the engine never breaks; through the window the engine's inlet holds the 2-4 atm
  /// band and the engine runs; the boiler's water is steady and above its floor.
  /// </summary>
  private static void HoldsAToD(
    StarterSteamPowerPlant plant,
    SetupRecording recording
  ) {
    int window = recording.SteadyFrom;
    Never(plant, "a: the boiler choked", s => s.Choked);
    Never(
      plant,
      "b: the boiler reached its limit, a pipe burst or the engine broke",
      s =>
        s.BoilerPressure >= PpexValues.CornishBoilerMaxOutputPressure
        || s.PipesLost > 0
        || s.EngineBroken
    );
    Never(
      plant,
      "c: the engine's inlet left its band or the engine stopped",
      s =>
        s.InletPressure < BandLow
        || s.InletPressure > BandHigh
        || !s.EngineRunning,
      window
    );
    Assert.False(
      recording.Unsteady().Contains("boiler.water"),
      "d: the boiler's water was not steady"
    );
    Never(
      plant,
      "d: the boiler's water fell to its floor",
      s => s.Water <= PpexValues.CornishBoilerMinBoilWater,
      window
    );
  }

  /// <summary>
  /// The north Cornish boiler's structure at <paramref name="master"/>, each cell with the code
  /// pattern cornish.json gives it. Facing north the structure is turned 180 degrees: an offset
  /// (x, y, z) sits at (-x, y, -z) from the master cell.
  /// </summary>
  private static Dictionary<BlockPos, string> BoilerStructure(BlockPos master) {
    JsonNode structure = JsonNode.Parse(
      File.ReadAllText(
        Path.Combine(
          SaveGoldens.RepoRoot(),
          "ppex",
          "assets",
          "ppex",
          "blocktypes",
          "boiler",
          "cornish.json"
        )
      )
    )!["attributes"]!["multiblockStructure"]!;
    Dictionary<int, string> codes = structure["blockNumbers"]!
      .AsObject()
      .ToDictionary(p => p.Value!.GetValue<int>(), p => p.Key);
    return structure["offsets"]!
      .AsArray()
      .ToDictionary(
        o =>
          master.AddCopy(
            -o!["x"]!.GetValue<int>(),
            o["y"]!.GetValue<int>(),
            -o["z"]!.GetValue<int>()
          ),
        o => codes[o!["w"]!.GetValue<int>()]
      );
  }

  /// <summary>The six orders of the engine, the pump and the water valve, comma-separated.</summary>
  public static TheoryData<string> Orders() {
    var orders = new TheoryData<string>();
    Ticker[] all = [Ticker.Engine, Ticker.Pump, Ticker.WaterValve];
    foreach (Ticker first in all)
      foreach (Ticker second in all.Where(t => t != first))
        orders.Add($"{first},{second},{all.Single(t => t != first && t != second)}");
    return orders;
  }

  #endregion

  #region Building it in game

  // Fails when a cell the plant lays inside the boiler's structure is not the block the structure
  // names there: the feed main run through the brick course under the boiler, or the exhaust outlet
  // in black brick instead of fireclay.
  [Fact]
  public void Every_pipe_laid_inside_the_boilers_structure_is_the_one_it_names() {
    var plant = new StarterSteamPowerPlant();
    Dictionary<BlockPos, string> structure = BoilerStructure(plant.Boiler.Be.Pos);
    Assert.Equal(
      "ppex:pipe-outlet-fire-u",
      structure[plant.Boiler.Block.ExhaustOutletWorldPos(plant.Boiler.Be.Pos)]
    );

    List<string> wrong = plant
      .Placed.Where(p =>
        structure.TryGetValue(p.Pos, out string? wants)
        && !WildcardUtil.Match(new AssetLocation(wants), new AssetLocation(p.Code))
      )
      .Select(p => $"{p.Pos} {p.Code} where the structure wants {structure[p.Pos]}")
      .ToList();

    Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    Assert.Equal(
      new[] { "ppex:pipe-passthrough-fire-ns", "ppex:pipe-passthroughbend-fire-us" },
      plant
        .Placed.Where(p => structure.ContainsKey(p.Pos) && p.Code.Contains("passthrough"))
        .Select(p => p.Code)
        .Distinct()
        .OrderBy(c => c, StringComparer.Ordinal)
    );
  }

  // Fails when a pipe's faces form a shape no shipped pipe has: the steam tee at the corner over the
  // engine with faces d, s and w.
  [Fact]
  public void Every_pipe_the_plant_lays_has_a_shipped_shape() {
    var plant = new StarterSteamPowerPlant();

    List<string> unshipped = [];
    foreach (var (pos, code) in plant.Placed) {
      var pipe = (BlockPipe)plant.Scene.World.GetBlock(pos);
      string faces = Sorted(pipe.Orientation);
      if (!pipe.AllowedOrientations.Values.SelectMany(o => o).Any(o => Sorted(o) == faces))
        unshipped.Add($"{pos} {code}");
    }

    Assert.True(unshipped.Count == 0, string.Join("\n", unshipped));
  }

  private static string Sorted(string faces) =>
    new(faces.OrderBy(c => c).ToArray());

  #endregion

  #region The setup

  // a fails with the chimney left off, b with the steam valve's cell capped, c with a pipe on the
  // steam main open to air, d with BoilerWaterIntakeRate at 1.8, e with BoilStep making 1.1 times
  // its rate, f with the steam valve gated at 3.25 atm.
  [Fact]
  public void The_starter_steam_power_setup_runs_as_drawn() {
    var plant = new StarterSteamPowerPlant();
    SetupRecording recording = plant.Record(RunSeconds);
    plant.Run(RunSeconds);
    HoldsAToD(plant, recording);
    IReadOnlyList<string> unsteady = recording.Unsteady();
    double flow = (double)recording.Steady()["steam.flow"];
    Assert.True(
      Math.Abs(flow - DrawnSteamFlow) <= 0.5,
      $"e: the steam run carried {flow} L/s"
    );
    Assert.True(
      unsteady.Count == 0,
      "f: not steady: " + string.Join(", ", unsteady)
    );
    recording.Save();
  }

  // Fails when the feed is read as the steam run's flow over the expansion factor (the boiler then
  // draws from its first boil, though its water stands above the intake fill until 231 s), or when
  // the boil is read after the tick (a second of draw at the first boil).
  [Fact]
  public void The_boiler_draws_its_feed_only_once_its_water_falls_below_the_intake_fill() {
    var plant = new StarterSteamPowerPlant().Run(300);
    float fill = (float)ReflectionHelpers.GetProperty(plant.Boiler.Be, "MaxWaterIntakeFill")!;

    Never(
      plant,
      "the boiler drew with its water at the intake fill",
      s => s.At > 0 && plant.Seconds[s.At - 1].Water >= fill && Math.Abs(s.Feed) > 0.01f
    );
    int? below = plant.First(s => s.Water < fill);
    Assert.NotNull(below);
    Assert.True(
      plant.Seconds[below.Value + 1].Feed > 0f,
      $"no draw after the water fell below {fill} L at second {below}"
    );
  }

  // a to d fail in every order under their mutations in the drawn order's test above; the chimney
  // left off fails a in all six. e fails in the two orders where the water valve ticks before the
  // pump when the boiler reads its feed main's live pressure (waterNet.State.Pressure in
  // BlockEntityBoiler): the main is then brim-full at the pump's head and the feed flashes steam.
  // It fails in all six when the water valve does not hold the main down (WaterLine.Relieve in
  // BlockEntityPressureValve.OverflowLiquid).
  [Theory]
  [MemberData(nameof(Orders))]
  public void The_setup_holds_in_every_tick_order(string order) {
    var plant = new StarterSteamPowerPlant(
      order: order.Split(',').Select(Enum.Parse<Ticker>).ToList()
    );
    SetupRecording recording = plant.Record(RunSeconds);
    plant.Run(RunSeconds);

    HoldsAToD(plant, recording);
    IReadOnlyDictionary<string, object> steady = recording.Steady();
    output.WriteLine(
      $"{order}: steam.flow {steady["steam.flow"]} l/s,"
        + $" steam-valve.vent {steady["steam-valve.vent"]} l/s,"
        + $" water-valve.vent {steady["water-valve.vent"]} l/s,"
        + $" boiler.feed {steady["boiler.feed"]} l/s, feed.pressure {steady["feed.pressure"]} atm"
    );
    double flow = (double)steady["steam.flow"];
    double vent = (double)steady["steam-valve.vent"];
    Assert.True(
      Math.Abs(flow - DrawnSteamFlow) <= 0.5
        && Math.Abs(vent - DrawnSteamVent) <= 0.5,
      $"e: the steam run carried {flow} L/s and the steam valve vented {vent} L/s"
    );
  }

  // With the water valve gated at 1.5 atm the engine's pump holds the feed main at 2.625 atm, the
  // valve opens and holds it down to 1.5, and the boiler flashes half a litre of steam per litre it
  // draws: 1 L/s on top of its 32, vented by the steam valve. Fails in the orders where a draw
  // falls between the pump and the water valve when the valve opens on the main's live pressure
  // (inState.Pressure in BlockEntityPressureValve.OverflowLiquid), and in all six when the engine's
  // pump records no hold (WaterLine.Hold in BlockEntityEngineFluidPump.DoWork) or the boiler reads
  // the main's live pressure.
  [Theory]
  [MemberData(nameof(Orders))]
  public void A_relief_gated_above_1_atm_holds_the_feed_at_its_gate_in_every_tick_order(
    string order
  ) {
    var plant = new StarterSteamPowerPlant(
      order: order.Split(',').Select(Enum.Parse<Ticker>).ToList(),
      waterGate: 1.5f
    );
    SetupRecording recording = plant.Record(RunSeconds);
    plant.Run(RunSeconds);

    IReadOnlyDictionary<string, object> steady = recording.Steady();
    double flow = (double)steady["steam.flow"];
    double vent = (double)steady["steam-valve.vent"];
    Assert.True(
      Math.Abs(flow - (DrawnSteamFlow + 1.0)) <= 0.5
        && Math.Abs(vent - (DrawnSteamVent + 1.0)) <= 0.5,
      $"{order}: the steam run carried {flow} L/s and the steam valve vented {vent} L/s"
    );
  }

  // Fails when a pump whose engine gives it no power keeps holding its main (the release at the
  // head of BlockEntityEngineFluidPump.DoWork): the main would read the pump's 2.625 atm.
  [Fact]
  public void A_pump_without_power_lets_its_main_go_to_its_fill() {
    var plant = new StarterSteamPowerPlant().Run(300);
    Assert.True(WaterLine.Head(plant.FeedRun) > 1f, "the premise: the pump holds the main");

    ReflectionHelpers.Invoke(plant.Pump, "DoWork", 0f, 1f);

    Assert.True(
      WaterLine.Head(plant.FeedRun) <= 1f,
      $"the main read {WaterLine.Head(plant.FeedRun)} atm"
    );
  }

  // Fails when a pump that has lost its engine keeps holding its main (the
  // OnIdleProductionTick of BlockEntityEngineFluidPump).
  [Fact]
  public void A_pump_without_an_engine_lets_its_main_go_to_its_fill() {
    var plant = new StarterSteamPowerPlant().Run(300);
    Assert.True(WaterLine.Head(plant.FeedRun) > 1f, "the premise: the pump holds the main");

    ReflectionHelpers.Invoke(plant.Pump, "OnIdleProductionTick", 1f);

    Assert.True(
      WaterLine.Head(plant.FeedRun) <= 1f,
      $"the main read {WaterLine.Head(plant.FeedRun)} atm"
    );
  }

  // Fails when a fire whose outlet is open on top draws as it would through a chimney, or when a
  // choked pile is never put out.
  [Fact]
  public void Without_a_chimney_the_fire_chokes_and_is_snuffed() {
    var plant = new StarterSteamPowerPlant(chimney: false).Run(12);

    int? snuffed = plant.First(s => !s.PileBurning);
    Assert.NotNull(snuffed);
    Assert.True(snuffed <= 11, $"the pile burned until second {snuffed}");
    Never(plant, "the fire drew before its snuff", s => !s.Choked && s.At < snuffed);
  }

  // Fails when a steam run with an open end no longer blows the boiler down through it (the
  // BlowDown on a leaking run removed from BlockEntityBoiler.PushSteam): the main leaks below the
  // engine's band while the boiler climbs to its limit.
  [Fact]
  public void An_open_end_on_the_steam_main_blows_the_boiler_down() {
    var plant = new StarterSteamPowerPlant(openEnd: true).Run(RunSeconds);

    Never(
      plant,
      "the steam main reached the engine's engage pressure",
      s => s.SteamPressure >= PpexValues.WattEngineEngagePressure
    );
    Never(plant, "the engine ran", s => s.EngineRunning);
    Never(plant, "a pipe burst", s => s.PipesLost > 0);
    Never(
      plant,
      "the boiler reached its limit",
      s => s.BoilerPressure >= PpexValues.CornishBoilerMaxOutputPressure
    );
  }

  #endregion
}
