using System;
using System.Collections.Generic;
using System.Linq;
using ExpandedLib;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Testing;
using PipesAndPowerExpanded.BlockStructures.Engine;
using PipesAndPowerExpanded.Helpers;
using Vintagestory.API.MathTools;
using Xunit;

namespace PipesAndPowerExpanded.Tests;

/// <summary>
/// Whole-process scenarios for the steam plant's support systems (handbook starter setup): a
/// pressure-relief valve gating an over-pressured boiler main so a Watt engine runs in band, and a
/// hand-cranked manual pump lifting pond water into a boiler line before any engine exists. Like the
/// other plant scenarios these lay the real machines + pipe lines into one <see cref="Scene"/> and
/// advance them together.
/// </summary>
public class SteamSupplyScenarioTests {
  #region Condensate outlet

  /// <summary>
  /// A running engine's condensate has to reach a line plumbed onto its water-outlet face. Nothing
  /// else in the fixtures wires that face, so this is the only cover on the outlet half of the water
  /// loop - the half a player closes back into the boiler.
  /// </summary>
  [Fact]
  public void A_running_engine_sends_its_condensate_into_a_plumbed_outlet() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    var enginePos = new BlockPos(0, 8, 0);
    var plant = new RegulatedEnginePlant(scene, enginePos, gateAtm: 2.5f);

    // A sealed one-cell condensate line on the engine's water-outlet face.
    var outletFace = plant.EngineBlock.WaterOutletFace;
    BlockPos outlet = enginePos.AddCopy(outletFace);
    EnginePlant.Pipe(scene, outlet, EnginePlant.Axis(outletFace), 90);
    scene.Block(outlet.AddCopy(outletFace), PpexScenes.Cap(91));
    scene.Build();

    plant.RunCharged(3f, 4);

    Assert.True(plant.Engine.IsRunning, "the engine should be driven");
    Assert.True(
      scene.NetworkAt<PipeNetwork>(outlet)!.State?.Volume > 0f,
      "the condensate should have gone into the connected line"
    );
  }

  /// <summary>
  /// A closed water loop holds its main brim-full, which is the normal state of a working plant, not
  /// a fault: the outlet must back up quietly there. Only a face with nothing plumbed onto it, or one
  /// plumbed into a run carrying gas, sprays where the player can see it.
  /// </summary>
  [Fact]
  public void Only_an_outlet_with_nowhere_to_send_water_sprays() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    var line = new BlockPos(0, 8, 0);
    EnginePlant.Pipe(scene, line, "we", 92);
    scene.Block(line.WestCopy(), PpexScenes.Cap(93));
    scene.Block(line.EastCopy(), PpexScenes.Cap(94));
    scene.Build();

    var net = scene.NetworkAt<PipeNetwork>(line)!;

    Assert.True(
      BlockEntityEngine.OutletSpills(null),
      "an unplumbed outlet should spray"
    );

    // Brim-full of water: the line refuses more, but a closed loop always reads this way.
    net.TryProduceLiquid(
      ExlibValues.LitresPerPipe * 4f,
      90f,
      0f,
      scene.World.Accessor
    );
    Assert.False(
      net.TryProduceLiquid(1f, 90f, 0f, scene.World.Accessor),
      "the premise: a brim-full line takes no more water"
    );
    Assert.False(
      BlockEntityEngine.OutletSpills(net),
      "a backed-up water line should not spray"
    );

    // The same line carrying gas can never take water - that is a plumbing mistake worth showing.
    net.TryConsumeLiquid(net.State!.Volume, scene.World.Accessor);
    net.TryProduceGas(60f, 150f, "Steam", scene.World.Accessor);
    Assert.True(
      BlockEntityEngine.OutletSpills(net),
      "an outlet plumbed into a gas run should spray"
    );
  }

  #endregion

  #region Pressure-valve regulation (boiler main → relief valve → engine in band)

  [Fact]
  public void A_relief_valve_bleeds_an_over_pressured_main_so_the_engine_runs_in_band() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    var plant = new RegulatedEnginePlant(
      scene,
      new BlockPos(0, 8, 0),
      gateAtm: 2.5f
    );
    scene.Build();

    // The boiler holds the main at 5 atm; the relief valve bleeds the excess above the 2.5 atm gate
    // into the drain each tick, so the gated line never runs away from the engine.
    plant.RunCharged(5f, 4);

    Assert.True(
      plant.DrainVolume > 0f,
      "the valve should have bled overflow into the drain"
    );
    Assert.True(
      plant.MainPressure < 5f,
      "the relieved main should sit below the boiler's charge"
    );
    Assert.False(plant.Engine.IsBroken, "the engine should not have burst");
    Assert.True(
      plant.Engine.IsRunning,
      "the engine should be driven by the gated main"
    );
  }

  [Fact]
  public void With_the_gate_above_the_charge_the_valve_never_opens() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    // Gate raised to the steel rating: 5 atm of charge is below it, so nothing is relieved.
    var plant = new RegulatedEnginePlant(
      scene,
      new BlockPos(0, 8, 0),
      gateAtm: 10f
    );
    scene.Build();

    plant.RunCharged(5f, 4);

    Assert.Equal(0f, plant.DrainVolume, 2); // valve stayed shut
    Assert.True(plant.MainPressure > 4f, "the unrelieved main stays high");
  }

  #endregion

  #region Manual pump (engine-free water start)

  [Fact]
  public void Cranking_the_manual_pump_lifts_pond_water_into_the_output_main() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    var plant = new ManualPumpPlant(scene, new BlockPos(0, 8, 0));
    scene.Build();

    plant.FillPond(30f); // standing water on the input line
    plant.Crank(3); // player holds right-click for three ticks

    Assert.True(plant.OutputIsWater, "the output main should carry water");
    Assert.True(
      plant.OutputVolume > 0f,
      "cranking should lift water into the output main"
    );
  }

  [Fact]
  public void An_uncranked_pump_moves_no_water() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    var plant = new ManualPumpPlant(scene, new BlockPos(0, 8, 0));
    scene.Build();

    plant.FillPond(30f);
    scene.Step(3); // never cranked

    Assert.Equal(0f, plant.OutputVolume, 3);
  }

  // Fails when the pump records no hold (WaterLine.Hold in BlockEntityManualFluidPump.DoWork): the
  // brim-full main would read its fill, 1 atm, not the 1.5 atm head the pump is set to.
  [Fact]
  public void A_crank_that_fills_its_main_holds_it_at_the_pumps_head() {
    float head = PpexValues.ManualPumpDeliveryPressure;
    try {
      PpexValues.Edit(c => c.ManualPumpDeliveryPressure = 1.5f);
      var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
      var plant = new ManualPumpPlant(scene, new BlockPos(0, 8, 0));
      scene.Build();

      plant.FillPond(30f).Crank(20);

      Assert.Equal(1.5f, WaterLine.Head(plant.Output), 3);
    } finally {
      PpexValues.Edit(c => c.ManualPumpDeliveryPressure = head);
    }
  }

  // Fails when a released crank keeps its hold (the release in
  // BlockEntityManualFluidPump.StopPumping).
  [Fact]
  public void A_released_crank_lets_its_main_go_to_its_fill() {
    float head = PpexValues.ManualPumpDeliveryPressure;
    try {
      PpexValues.Edit(c => c.ManualPumpDeliveryPressure = 1.5f);
      var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
      var plant = new ManualPumpPlant(scene, new BlockPos(0, 8, 0));
      scene.Build();
      plant.FillPond(30f).Crank(20);

      plant.Pump.OnPumpStop();

      Assert.Equal(1f, WaterLine.Head(plant.Output), 3);
    } finally {
      PpexValues.Edit(c => c.ManualPumpDeliveryPressure = head);
    }
  }

  #endregion

  #region Condenser (closed water loop's recovery leg)

  [Fact]
  public void Spent_steam_is_condensed_and_recovered_into_the_water_line() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    var plant = new CondenserPlant(scene, new BlockPos(0, 8, 0));
    scene.Build();

    plant.ChargeSteam(300f).ChargeFeedWater(40f);
    float steamBefore = plant.SteamVolume;
    scene.Step(4);

    Assert.True(
      plant.Condensing,
      "the condenser should report condensing with steam on the line"
    );
    Assert.True(
      plant.SteamVolume < steamBefore,
      "spent steam should be drawn off the line"
    );
    Assert.True(
      plant.RecoveredIsWater,
      "the recovered line should carry water"
    );
    Assert.True(
      plant.RecoveredVolume > 0f,
      "recovered water should reach the line back to the boiler"
    );
  }

  [Fact]
  public void Without_steam_the_condenser_condenses_nothing() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    var plant = new CondenserPlant(scene, new BlockPos(0, 8, 0));
    scene.Build();

    plant.ChargeFeedWater(40f); // feed water but no steam to condense
    scene.Step(4);

    Assert.False(plant.Condensing);
  }

  #endregion

  #region A boiler fed through a condenser or a relief valve

  /// <summary>Every order of the pump, the machine between and the boiler, as
  /// <c>"Pump,Between,Boiler"</c>.</summary>
  public static TheoryData<string> FedOrders() {
    var orders = new TheoryData<string>();
    FedBoilerPlant.Ticker[] all = Enum.GetValues<FedBoilerPlant.Ticker>();
    foreach (var first in all)
      foreach (var second in all.Where(t => t != first))
        orders.Add($"{first},{second},{all.Single(t => t != first && t != second)}");
    return orders;
  }

  /// <summary>
  /// Runs <paramref name="test"/> with the manual pump delivering 20 L/s at 2 atm, enough to keep
  /// its main and the line past the machine between brim-full against the boiler's 10 L/s intake.
  /// </summary>
  private static void WithStrongPump(Action test) {
    float head = PpexValues.ManualPumpDeliveryPressure;
    float rate = PpexValues.ManualPumpWaterPerSecond;
    try {
      PpexValues.Edit(c => {
        c.ManualPumpDeliveryPressure = 2f;
        c.ManualPumpWaterPerSecond = 20f;
      });
      test();
    } finally {
      PpexValues.Edit(c => {
        c.ManualPumpDeliveryPressure = head;
        c.ManualPumpWaterPerSecond = rate;
      });
    }
  }

  /// <summary>The steam (L) a boiler flashes in a second drawing its full intake at
  /// <paramref name="atm"/>.</summary>
  private static float Flash(float atm) =>
    PpexValues.BoilerWaterIntakeRate
    * (atm - 1f)
    * PpexValues.WaterPressureSteamBoost;

  private static List<FedBoilerPlant.Ticker> Parse(string order) =>
    order.Split(',').Select(Enum.Parse<FedBoilerPlant.Ticker>).ToList();

  // The pump holds its main at 2 atm and the condenser holds its outlet at that, so the boiler
  // flashes 10 L of steam a second. Fails in all six when the condenser records no hold on its
  // outlet (WaterLine.Hold in BlockEntitySteamCondenser.Process).
  [Theory]
  [MemberData(nameof(FedOrders))]
  public void A_boiler_fed_through_a_condenser_flashes_at_the_pumps_head_in_every_order(
    string order
  ) =>
    WithStrongPump(() => {
      var plant = new FedBoilerPlant(FedBoilerPlant.Between.Condenser, Parse(order));

      List<float> flashed = plant.Run(12);

      foreach (float steam in flashed.Skip(4))
        Assert.Equal(Flash(2f), steam, 2);
    });

  // Fails when a condenser taken off its main keeps holding its outlet (the release at the top of
  // BlockEntitySteamCondenser.OnTick): the line would still read the pump's 2 atm.
  [Fact]
  public void A_condenser_taken_off_its_main_lets_its_outlet_go_to_its_fill() =>
    WithStrongPump(() => {
      var plant = new FedBoilerPlant(
        FedBoilerPlant.Between.Condenser,
        [FedBoilerPlant.Ticker.Pump, FedBoilerPlant.Ticker.Between, FedBoilerPlant.Ticker.Boiler]
      );
      plant.Run(8);
      Assert.Equal(2f, WaterLine.Head(plant.Line), 3);

      plant.CutMain();
      plant.Run(1);

      Assert.True(
        WaterLine.Head(plant.Line) <= 1f,
        $"the line read {WaterLine.Head(plant.Line)} atm"
      );
    });

  // The pump holds its main at 2 atm, over the valve's 1.5 atm gate, and the valve holds its
  // output at the 2 atm it spills at, so the boiler flashes 10 L of steam a second. Fails in all
  // six when the valve records no hold on its output (WaterLine.Hold in
  // BlockEntityPressureValve.OverflowLiquid).
  [Theory]
  [MemberData(nameof(FedOrders))]
  public void A_boiler_fed_from_a_relief_valves_output_flashes_at_its_mains_head_in_every_order(
    string order
  ) =>
    WithStrongPump(() => {
      var plant = new FedBoilerPlant(FedBoilerPlant.Between.Valve, Parse(order));

      List<float> flashed = plant.Run(12);

      foreach (float steam in flashed.Skip(4))
        Assert.Equal(Flash(2f), steam, 2);
    });

  // Fails when a valve turned up past its main keeps holding its output (the release at the top of
  // BlockEntityPressureValve.OnTick): the line would still read the main's 2 atm.
  [Fact]
  public void A_relief_valve_turned_up_past_its_main_lets_its_output_go_to_its_fill() =>
    WithStrongPump(() => {
      var plant = new FedBoilerPlant(
        FedBoilerPlant.Between.Valve,
        [FedBoilerPlant.Ticker.Pump, FedBoilerPlant.Ticker.Between, FedBoilerPlant.Ticker.Boiler]
      );
      plant.Run(8);
      Assert.Equal(2f, WaterLine.Head(plant.Line), 3);

      plant.SetGate(2.5f);
      plant.Run(1);

      Assert.True(
        WaterLine.Head(plant.Line) <= 1f,
        $"the line read {WaterLine.Head(plant.Line)} atm"
      );
    });

  #endregion
}
