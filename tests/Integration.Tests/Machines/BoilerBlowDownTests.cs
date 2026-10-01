using System.Linq;
using ExpandedLib;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Testing;
using Vintagestory.API.MathTools;
using Xunit;
using BoilerState = PipesAndPowerExpanded.BlockStructures.Boiler.BlockEntityBoiler.BoilerState;

namespace PipesAndPowerExpanded.Tests;

/// <summary>
/// A firing Cornish boiler whose steam has a way out blows down to about 1 atm instead of sitting
/// at its 5 atm limit: an unpiped neck, a run with an open end, and every boiler sharing such a
/// run. A sealed run still drives it to the limit and the burst.
/// </summary>
public class BoilerBlowDownTests {
  #region Fixtures

  private const int RunLength = 10;

  /// <summary>Water that leaves 200 L of headspace in the 800 L vessel and lasts 225 s of
  /// boiling above the 150 L floor.</summary>
  private const float Water = 600f;

  /// <summary>Pressure just under the Cornish's 5 atm limit.</summary>
  private const float NearLimitAtm = 4.75f;

  /// <summary>Steam at <see cref="NearLimitAtm"/> in that headspace.</summary>
  private const float NearLimit = 200f * NearLimitAtm;

  /// <summary>
  /// Boiler A, optionally boiler B, and a straight run of <see cref="RunLength"/> + 1 pipes from
  /// A's steam outlet to B's. With <c>openEnd</c>, the pipe on A's outlet is also open to air on
  /// its west face. A burst clears the boiler's cell without running the game's break hooks.
  /// </summary>
  private sealed class Plant {
    public readonly Scene Scene = new Scene().Network(
      "pipe",
      s => new PipeNetwork(s)
    );
    public readonly BoilerFixture A;
    public readonly BoilerFixture? B;

    public Plant(bool openEnd, bool twoBoilers) {
      Scene.World.BreakRunsBlockHooks = false;
      A = new BoilerFixture(Scene, new BlockPos(0, 8, 0), 10, 11);
      if (twoBoilers)
        B = new BoilerFixture(Scene, new BlockPos(RunLength, 8, 0), 12, 13);
      BlockPos attach = A.SteamPipeAttachPos;
      BlockPos far = attach.AddCopy(RunLength, 0, 0);
      EnginePlant.Pipe(Scene, attach, openEnd ? "dwe" : "de", 50);
      for (int x = 1; x < RunLength; x++)
        EnginePlant.Pipe(Scene, attach.AddCopy(x, 0, 0), "we", 60 + x);
      EnginePlant.Pipe(Scene, far, "dw", 52);
      // The boilers' steam-port fillers seal the down faces in game.
      Scene.Block(attach.DownCopy(), PpexScenes.Cap(51));
      Scene.Block(far.DownCopy(), PpexScenes.Cap(51));
      Scene.Build();
    }

    public BoilerFixture[] Boilers => B == null ? [A] : [A, B];

    /// <summary>Charges the run to <paramref name="atm"/> before its first tick finds an open
    /// end.</summary>
    public void Charge(float atm) {
      var run = Scene.NetworkAt<PipeNetwork>(A.SteamPipeAttachPos)!;
      run.TryProduceGas(
        run.Nodes.Count * ExlibValues.LitresPerPipe * atm,
        150f,
        "Steam",
        Scene.World.Accessor,
        maxOutputPressure: atm
      );
    }

    public bool Burst(BoilerFixture boiler) =>
      Scene.World.GetBlock(boiler.Be.Pos).Id == 0;

    /// <summary>Steps <paramref name="seconds"/>, stopping at the first burst.</summary>
    public void Run(int seconds) {
      for (int i = 0; i < seconds && !Boilers.Any(Burst); i++)
        Scene.Step();
    }
  }

  #endregion

  #region Open outlets

  // Fails when the open neck vents only BoilerSteamLeakRate, flat.
  [Fact]
  public void An_open_neck_blows_a_firing_boiler_down_to_about_one_atmosphere() {
    var rig = new BoilerRig()
      .SetState(BoilerState.Boiling)
      .SetWater(Water)
      .SetSteam(NearLimit);

    rig.Tick(times: 60);

    Assert.InRange(rig.Be.InternalPressure, 0.5f, 1.2f);
  }

  // Fails when a leaking run leaves the boiler's blow-down to the run's own leak.
  [Fact]
  public void Two_boilers_on_one_leaking_run_both_blow_down_from_cold() {
    var plant = new Plant(openEnd: true, twoBoilers: true);
    plant.A.Prime(BoilerState.Boiling, Water, steam: 0f);
    plant.B!.Prime(BoilerState.Boiling, Water, steam: 0f);

    plant.Run(150);

    foreach (BoilerFixture boiler in plant.Boilers) {
      Assert.False(plant.Burst(boiler), "a boiler on an open run burst");
      Assert.InRange(boiler.Be.InternalPressure, 0.5f, 1.2f);
    }
  }

  // Fails when a leaking run leaves the boiler's blow-down to the run's own leak.
  [Fact]
  public void A_charged_main_that_springs_a_leak_blows_its_boiler_down() {
    var plant = new Plant(openEnd: true, twoBoilers: false);
    plant.Charge(NearLimitAtm);
    plant.A.Prime(BoilerState.Boiling, Water, NearLimit);

    plant.Run(120);

    Assert.False(plant.Burst(plant.A), "the boiler on an open main burst");
    Assert.InRange(plant.A.Be.InternalPressure, 0.5f, 1.2f);
  }

  #endregion

  #region Sealed run

  // Fails when every piped boiler blows down, open run or not.
  [Fact]
  public void A_firing_boiler_on_a_sealed_run_bursts() {
    var plant = new Plant(openEnd: false, twoBoilers: false);
    plant.A.Prime(BoilerState.Boiling, Water, NearLimit);

    plant.Run(150);

    Assert.True(plant.Burst(plant.A), "a sealed boiler held its limit");
  }

  #endregion
}
