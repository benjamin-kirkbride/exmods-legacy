using System;
using System.Collections.Generic;
using System.Linq;
using PipesAndPowerExpanded;
using Xunit;

namespace Integration.Tests.Setups;

/// <summary>
/// The starter steam power setup builds as drawn, fires from cold and settles where the drawing
/// says: the boiler never chokes or reaches its limit, the steam main holds the Watt engine's band,
/// the boiler's water holds, and the steam run carries the boiler's 32 L/s. Its recording is the
/// one the wiki plays.
/// </summary>
public class StarterSteamPowerSetupTests {
  #region Fixtures

  private const int RunSeconds = 1200;

  /// <summary>The steam run's flow the drawing prints (L/s).</summary>
  private const double DrawnSteamFlow = 32.0;

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
      "c: the steam main left the engine's band or the engine stopped",
      s =>
        s.SteamPressure < BandLow
        || s.SteamPressure > BandHigh
        || !s.EngineRunning,
      window
    );
    IReadOnlyList<string> unsteady = recording.Unsteady();
    Assert.False(
      unsteady.Contains("boiler.water"),
      "d: the boiler's water was not steady"
    );
    Never(
      plant,
      "d: the boiler's water fell to its floor",
      s => s.Water <= PpexValues.CornishBoilerMinBoilWater,
      window
    );
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
