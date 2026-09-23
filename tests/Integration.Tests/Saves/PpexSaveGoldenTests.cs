using System;
using ExpandedLib.Testing;
using PipesAndPowerExpanded;
using PipesAndPowerExpanded.BlockNetworkPipe;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using PipesAndPowerExpanded.BlockStructures.Boiler;
using PipesAndPowerExpanded.BlockStructures.Boiler.BlockEntities;
using PipesAndPowerExpanded.BlockStructures.Boiler.Blocks;
using PipesAndPowerExpanded.BlockStructures.Engine;
using PipesAndPowerExpanded.BlockStructures.Engine.BlockEntities;
using PipesAndPowerExpanded.BlockStructures.Engine.Blocks;
using PipesAndPowerExpanded.BlockStructures.ManualPump.BlockEntities;
using PipesAndPowerExpanded.BlockStructures.ManualPump.Blocks;
using PipesAndPowerExpanded.BlockStructures.MpPump.BlockEntities;
using PipesAndPowerExpanded.BlockStructures.MpPump.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Xunit;
using BoilerState = PipesAndPowerExpanded.BlockStructures.Boiler.BlockEntityBoiler.BoilerState;

namespace Integration.Tests.Saves;

/// <summary>
/// Save goldens for every ppex blocktype with a block entity. Each live state is saved, loaded back
/// in the game's order, and must restore what it held; the committed goldens were written by the
/// published build, so a later build that loads them differently fails here.
/// </summary>
public class PpexSaveGoldenTests {
  private static readonly BlockPos At = new(0, 8, 0);

  private const string Blocktypes = "ppex/assets/ppex/blocktypes/";

  #region Pipes

  [Fact]
  public void A_straight_pipe_restores_its_steam() =>
    Charged(
      "ppex-pipe-straight-steam",
      () => Pipe(new BlockPipe(), "ppex:pipe-straight-ns-iron", "straight", "ns"),
      () => new BlockEntityPipe(),
      "ns",
      "Steam",
      45f,
      160f
    );

  [Fact]
  public void A_straight_pipe_restores_its_water() =>
    Charged(
      "ppex-pipe-straight-water",
      () => Pipe(new BlockPipe(), "ppex:pipe-straight-we-steel", "straight", "we", "steel"),
      () => new BlockEntityPipe(),
      "we",
      "Water",
      24f,
      35f,
      check: (_, state) => Assert.Equal(1.2f, state.FeedPressure, 3)
    );

  [Fact]
  public void A_bend_restores_its_steam() =>
    Charged(
      "ppex-pipe-bend-steam",
      () => Pipe(new BlockPipe(), "ppex:pipe-bend-de-iron", "bend", "de"),
      () => new BlockEntityPipe(),
      "de",
      "Steam",
      28f,
      140f
    );

  [Fact]
  public void A_t_junction_restores_its_air() =>
    Charged(
      "ppex-pipe-tjunction-air",
      () => Pipe(new BlockPipe(), "ppex:pipe-tjunction-deu-iron", "tjunction", "deu"),
      () => new BlockEntityPipe(),
      "deu",
      "Air",
      20f,
      20f
    );

  [Fact]
  public void A_cross_junction_restores_its_steam() =>
    Charged(
      "ppex-pipe-xjunction-steam",
      () => Pipe(new BlockPipe(), "ppex:pipe-xjunction-nsud-steel", "xjunction", "nsud", "steel"),
      () => new BlockEntityPipe(),
      "nsud",
      "Steam",
      60f,
      180f
    );

  [Fact]
  public void An_outlet_restores_its_exhaust() =>
    Charged(
      "ppex-pipe-outlet-exhaust",
      () => Brick(new BlockPipeOutlet(), "ppex:pipe-outlet-black-d", "outlet", "d"),
      () => new BlockEntityPipeOutlet(),
      "d",
      "Exhaust",
      12f,
      400f
    );

  [Fact]
  public void A_passthrough_restores_its_exhaust() =>
    Charged(
      "ppex-pipe-passthrough-exhaust",
      () => Brick(new BlockPipePassthrough(), "ppex:pipe-passthrough-black-ns", "passthrough", "ns"),
      () => new BlockEntityPipePassthrough(),
      "ns",
      "Exhaust",
      18f,
      350f
    );

  [Fact]
  public void A_passthrough_bend_restores_its_exhaust() =>
    Charged(
      "ppex-pipe-passthroughbend-exhaust",
      () => Brick(new BlockPipePassthrough(), "ppex:pipe-passthroughbend-black-de", "passthroughbend", "de"),
      () => new BlockEntityPipePassthrough(),
      "de",
      "Exhaust",
      16f,
      300f
    );

  [Fact]
  public void An_open_valve_restores_its_steam() =>
    Charged(
      "ppex-pipe-valve-open-steam",
      () => Pipe(new BlockValve(), "ppex:pipe-valve-ns-iron", "valve", "ns"),
      () => new BlockEntityValve(),
      "ns",
      "Steam",
      30f,
      150f,
      prime: be => ReflectionHelpers.SetField(be, "_open", true),
      check: (be, _) => Assert.True(((BlockEntityValve)be).IsOpen())
    );

  [Fact]
  public void A_shut_valve_comes_back_shut_and_empty() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "ppex-pipe-valve-shut",
        Block = () => Pipe(new BlockValve(), "ppex:pipe-valve-ns-iron", "valve", "ns"),
        Setup = Sealed,
        Live = (world, block) => {
          var be = (BlockEntityValve)
            Node(world, block, new BlockEntityValve(), "ns", open: true);
          Charge(world, "Steam", 30f, 150f);
          ReflectionHelpers.SetField(be, "_open", false);
          return be;
        },
        Check = (be, world) => {
          var valve = Assert.IsType<BlockEntityValve>(be);
          Assert.False(valve.IsOpen());
          Assert.Equal(0f, valve.Pressure);
          Assert.True(
            (world.NetworkAt(At) as PipeNetwork)?.State is null or { Volume: 0f }
          );
        },
      }
    );

  [Fact]
  public void A_pressure_valve_restores_its_gate_and_steam() =>
    Charged(
      "ppex-pipe-pressurevalve-steam",
      () => Pipe(new BlockPressureValve(), "ppex:pipe-pressurevalve-ns-steel", "pressurevalve", "ns", "steel"),
      () => new BlockEntityPressureValve(),
      "ns",
      "Steam",
      40f,
      170f,
      prime: be => ReflectionHelpers.SetField(be, "_gatePressure", 3.5f),
      check: (be, _) =>
        Assert.Equal(3.5f, ((BlockEntityPressureValve)be).GatePressure, 3)
    );

  [Fact]
  public void A_fluid_intake_restores_its_water_flag_but_not_the_pool() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "ppex-pipe-fluidintake-water",
        Block = () =>
          Variants(new BlockFluidIntake(), "ppex:pipe-fluidintake-n", ("type", "fluidintake"), ("orientation", "n")),
        Setup = world => {
          world.RegisterNetwork("pipe", sys => new PipeNetwork(sys));
          world.Place(At.NorthCopy(), Rock());
          FloodBelow(world);
        },
        Live = (world, block) => {
          var be = new BlockEntityFluidIntake();
          Node(world, block, be, "n");
          ReflectionHelpers.SetProperty(be, nameof(be.HasWater), true);
          Charge(world, "Water", 20f, 12f);
          return be;
        },
        Check = (be, world) => {
          var intake = Assert.IsType<BlockEntityFluidIntake>(be);
          Assert.True(intake.HasWater);
          Assert.False(intake.Crowded);
          Assert.Equal("n", intake.Orientation);
          Assert.True(
            ((PipeNetwork)world.NetworkAt(At)!).State is null or { Volume: 0f }
          );
        },
      }
    );

  [Fact]
  public void A_condenser_restores_its_condensing_flag() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "ppex-steamcondenser-condensing",
        Block = () =>
          Variants(new BlockSteamCondenser(), "ppex:steamcondenser-north", ("side", "north")),
        Live = (world, block) => {
          var be = SaveFixtures.Stand(world, At, block, new BlockEntitySteamCondenser());
          ReflectionHelpers.SetField(be, "_condensing", true);
          return be;
        },
        Check = (be, _) =>
          Assert.True(
            (bool)ReflectionHelpers.GetField(
              Assert.IsType<BlockEntitySteamCondenser>(be),
              "_condensing"
            )!
          ),
      }
    );

  #endregion

  #region Boilers

  [Fact]
  public void A_boiling_cornish_boiler_restores_its_water_steam_and_fire() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "ppex-boilercornish-boiling",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockBoilerCornish(), "ppex:boilercornish-north", ("side", "north")),
            Blocktypes + "boiler/cornish.json"
          ),
        Live = (world, block) =>
          Boiler(world, block, new BlockEntityBoilerCornish(), BoilerState.Boiling, 900f, 240f, 30f, burning: true),
        Check = (be, _) =>
          AssertBoiler(
            Assert.IsType<BlockEntityBoilerCornish>(be),
            "boiler/cornish.json",
            BoilerState.Boiling,
            900f,
            240f,
            30f,
            burning: true
          ),
      }
    );

  [Fact]
  public void A_heating_lancashire_boiler_restores_its_water_and_heat() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "ppex-boilerlancashire-heating",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockBoilerLancashire(), "ppex:boilerlancashire-east", ("side", "east")),
            Blocktypes + "boiler/lancashire.json"
          ),
        Live = (world, block) =>
          Boiler(world, block, new BlockEntityBoilerLancashire(), BoilerState.Heating, 1500f, 0f, 12f, burning: true),
        Check = (be, _) =>
          AssertBoiler(
            Assert.IsType<BlockEntityBoilerLancashire>(be),
            "boiler/lancashire.json",
            BoilerState.Heating,
            1500f,
            0f,
            12f,
            burning: true
          ),
      }
    );

  #endregion

  #region Engines and pumps

  [Fact]
  public void A_running_cornish_engine_restores_its_run_state_and_throttle() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "ppex-enginecornish-running",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockEngineCornish(), "ppex:enginecornish-north", ("side", "north")),
            Blocktypes + "engine/cornish.json"
          ),
        Live = (world, block) => {
          var be = Engine(world, block, new BlockEntityEngineCornish());
          ReflectionHelpers.SetField(be, "_throttle", 2);
          return be;
        },
        Check = (be, _) => {
          var engine = Assert.IsType<BlockEntityEngineCornish>(be);
          AssertEngine(engine, "engine/cornish.json");
          Assert.Equal(2, engine.ThrottleIndex);
        },
      }
    );

  [Fact]
  public void A_running_watt_engine_restores_its_run_state() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "ppex-enginewatt-running",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockEngineWatt(), "ppex:enginewatt-west", ("side", "west")),
            Blocktypes + "engine/watt.json"
          ),
        Live = (world, block) => Engine(world, block, new BlockEntityEngineWatt()),
        Check = (be, _) =>
          AssertEngine(Assert.IsType<BlockEntityEngineWatt>(be), "engine/watt.json"),
      }
    );

  [Fact]
  public void An_engine_fluid_pump_restores_its_draw() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "ppex-enginefluidpump-drawing",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockEngineFluidPump(), "ppex:enginefluidpump-east", ("side", "east")),
            Blocktypes + "engine/fluidpump.json"
          ),
        Live = (world, block) => {
          var be = SaveFixtures.Stand(world, At, block, new BlockEntityEngineFluidPump());
          ReflectionHelpers.SetField(be, "_drawingWater", true);
          return be;
        },
        Check = (be, _) =>
          Assert.True(
            (bool)ReflectionHelpers.GetField(
              Assert.IsType<BlockEntityEngineFluidPump>(be),
              "_drawingWater"
            )!
          ),
      }
    );

  [Fact]
  public void An_engine_generator_loads_as_itself() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "ppex-enginempgenerator",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockEngineMPGenerator(), "ppex:enginempgenerator-south", ("side", "south")),
            Blocktypes + "engine/mpgenerator.json"
          ),
        Live = (world, block) =>
          SaveFixtures.Stand(world, At, block, new BlockEntityEngineMpGenerator(), initialize: false),
        Initialize = false,
        Check = (be, _) =>
          Assert.NotNull(
            Assert.IsType<BlockEntityEngineMpGenerator>(be).GetBehavior<BEBehaviorEngineMPGenerator>()
          ),
      }
    );

  [Fact]
  public void A_manual_pump_that_was_pumping_comes_back_idle() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "ppex-manualfluidpump-pumping",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockManualFluidPump(), "ppex:manualfluidpump-north", ("side", "north")),
            Blocktypes + "manualfluidpump.json"
          ),
        Live = (world, block) => {
          var be = SaveFixtures.Stand(world, At, block, new BlockEntityManualFluidPump());
          ReflectionHelpers.SetField(be, "_pumping", true);
          ReflectionHelpers.SetField(be, "_drawingWater", true);
          return be;
        },
        Check = (be, _) => {
          var pump = Assert.IsType<BlockEntityManualFluidPump>(be);
          Assert.False((bool)ReflectionHelpers.GetField(pump, "_pumping")!);
          Assert.False((bool)ReflectionHelpers.GetField(pump, "_drawingWater")!);
        },
      }
    );

  [Fact]
  public void A_running_power_pump_restores_its_speed_draw_and_construction() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "ppex-mpfluidpump-running",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockMpFluidPump(), "ppex:mpfluidpump-north", ("side", "north")),
            Blocktypes + "mpfluidpump.json"
          ),
        Live = (world, block) => {
          var be = SaveFixtures.Stand(world, At, block, new BlockEntityMpFluidPump(), initialize: false);
          SaveFixtures.CompleteConstruction(be, world);
          ReflectionHelpers.SetField(be, "_lastSpeed", 0.8f);
          ReflectionHelpers.SetField(be, "_drawingWater", true);
          return be;
        },
        Initialize = false,
        Check = (be, _) => {
          var pump = Assert.IsType<BlockEntityMpFluidPump>(be);
          Assert.Equal(0.8f, (float)ReflectionHelpers.GetField(pump, "_lastSpeed")!, 3);
          Assert.True((bool)ReflectionHelpers.GetField(pump, "_drawingWater")!);
          Assert.Equal(
            SaveFixtures.ConstructionStages(Blocktypes + "mpfluidpump.json") - 1,
            SaveFixtures.ConstructionStage(pump)
          );
          Assert.NotNull(pump.GetBehavior<BEBehaviorMpPumpDrive>());
        },
      }
    );

  #endregion

  #region Helpers

  /// <summary>
  /// A pipe-network cell charged with <paramref name="litres"/> of <paramref name="medium"/> at
  /// <paramref name="temperature"/> degrees C, sealed on every side; the load must restore the
  /// charge into the cell's network.
  /// </summary>
  private static void Charged(
    string name,
    Func<Block> block,
    Func<BlockEntityPipe> entity,
    string orientation,
    string medium,
    float litres,
    float temperature,
    Action<BlockEntityPipe>? prime = null,
    Action<BlockEntityPipe, PipeNetworkState>? check = null
  ) =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = name,
        Block = block,
        Setup = Sealed,
        Live = (world, b) => {
          BlockEntityPipe be = entity();
          prime?.Invoke(be);
          Node(world, b, be, orientation);
          Charge(world, medium, litres, temperature);
          return be;
        },
        Check = (be, world) => {
          Assert.Equal(entity().GetType(), be.GetType());
          var pipe = (BlockEntityPipe)be;
          Assert.Equal(orientation, pipe.Orientation);
          Assert.Equal(medium, pipe.Medium);
          PipeNetworkState state = ((PipeNetwork)world.NetworkAt(At)!).State!;
          Assert.Equal(medium, state.MediumType);
          Assert.Equal(litres, state.Volume, 3);
          Assert.Equal(temperature, state.Temperature, 3);
          check?.Invoke(pipe, state);
        },
      }
    );

  /// <summary>Places a pipe-network node at <see cref="At"/> with its placed orientation and joins it to the graph.</summary>
  private static BlockEntity Node(
    TestWorld world,
    Block block,
    ExpandedLib.Blocks.Networks.BlockEntityNetworkNode be,
    string orientation,
    bool open = false
  ) {
    if (open)
      ReflectionHelpers.SetField(be, "_open", true);
    be.Orientation = orientation;
    be.PossibleOrientations = [orientation];
    world.Place(At, block, be);
    world.Attach(be);
    ReflectionHelpers.SetProperty(be, nameof(be.NetworkSystem), world.Networks);
    world.AddNode(At, "pipe");
    return be;
  }

  /// <summary>Produces into the network at <see cref="At"/>; water is driven at 1.2 atm feed pressure.</summary>
  private static void Charge(
    TestWorld world,
    string medium,
    float litres,
    float temperature
  ) {
    var net = (PipeNetwork)world.NetworkAt(At)!;
    bool accepted =
      medium == "Water"
        ? net.TryProduceLiquid(litres, temperature, 1.2f, world.Accessor)
        : net.TryProduceGas(litres, temperature, medium, world.Accessor, maxOutputPressure: 3f);
    Assert.True(accepted);
  }

  /// <summary>Registers the pipe network and walls <see cref="At"/> in with rock on all six sides.</summary>
  private static void Sealed(TestWorld world) {
    world.RegisterNetwork("pipe", sys => new PipeNetwork(sys));
    Block rock = Rock();
    foreach (BlockFacing face in BlockFacing.ALLFACES)
      world.Place(At.AddCopy(face), rock);
  }

  private static Block Rock() =>
    TestBlocks.Configure(new Block(), "game:rock-granite", 99);

  /// <summary>Fills the cube below <see cref="At"/> the intake scans with still water.</summary>
  private static void FloodBelow(TestWorld world) {
    var water = TestBlocks.Configure(new Block(), "game:water-still-7", 200);
    water.LiquidCode = "water";
    int depth = PpexValues.FluidIntakeWaterDepth;
    int half = depth / 2;
    for (int dx = -half; dx <= half; dx++)
      for (int dy = -1; dy >= -depth; dy--)
        for (int dz = -half; dz <= half; dz++)
          world.Place(At.AddCopy(dx, dy, dz), water);
  }

  /// <summary>A pipe block primed with its variants, as <c>OnLoaded</c> would prime it.</summary>
  private static Block Pipe(
    BlockPipe block,
    string code,
    string type,
    string orientation,
    string material = "iron"
  ) {
    Variants(block, code, ("type", type), ("orientation", orientation), ("material", material));
    ReflectionHelpers.SetProperty(block, "Type", type);
    ReflectionHelpers.SetProperty(block, "Orientation", orientation);
    return block;
  }

  /// <summary>A brick-lined pipe block (outlet, passthrough) primed with its variants.</summary>
  private static Block Brick(
    BlockPipe block,
    string code,
    string type,
    string orientation
  ) {
    Variants(block, code, ("type", type), ("brick", "black"), ("orientation", orientation));
    ReflectionHelpers.SetProperty(block, "Type", type);
    ReflectionHelpers.SetProperty(block, "Orientation", orientation);
    return block;
  }

  private static Block Variants(
    Block block,
    string code,
    params (string key, string value)[] variants
  ) => TestBlocks.Configure(block, code, 1, variants);

  private static BlockEntity Boiler(
    TestWorld world,
    Block block,
    BlockEntityBoiler be,
    BoilerState state,
    float water,
    float steam,
    float heatingSeconds,
    bool burning
  ) {
    SaveFixtures.Stand(world, At, block, be);
    SaveFixtures.CompleteConstruction(be, world);
    ReflectionHelpers.SetProperty(be, "StructureComplete", true);
    ReflectionHelpers.SetField(be, "_state", state);
    ReflectionHelpers.SetField(be, "_waterVolume", water);
    ReflectionHelpers.SetField(be, "_steamVolume", steam);
    ReflectionHelpers.SetField(be, "_heatingSeconds", heatingSeconds);
    ReflectionHelpers.SetField(be, "_burning", burning);
    return be;
  }

  private static void AssertBoiler(
    BlockEntityBoiler be,
    string asset,
    BoilerState state,
    float water,
    float steam,
    float heatingSeconds,
    bool burning
  ) {
    Assert.Equal(state, (BoilerState)ReflectionHelpers.GetField(be, "_state")!);
    Assert.Equal(water, (float)ReflectionHelpers.GetField(be, "_waterVolume")!, 3);
    Assert.Equal(steam, (float)ReflectionHelpers.GetField(be, "_steamVolume")!, 3);
    Assert.Equal(heatingSeconds, (float)ReflectionHelpers.GetField(be, "_heatingSeconds")!, 3);
    Assert.Equal(burning, (bool)ReflectionHelpers.GetField(be, "_burning")!);
    Assert.False(be.LidOpen);
    Assert.True(be.StructureComplete);
    Assert.True(be.IsConstructed);
    Assert.Equal(
      SaveFixtures.ConstructionStages(Blocktypes + asset) - 1,
      SaveFixtures.ConstructionStage(be)
    );
  }

  private static BlockEntity Engine(TestWorld world, Block block, BlockEntityEngine be) {
    SaveFixtures.Stand(world, At, block, be);
    SaveFixtures.CompleteConstruction(be, world);
    ReflectionHelpers.SetField(be, "_running", true);
    foreach (var (name, value) in new[] {
      (nameof(be.AnimationSpeed), 1.25f),
      (nameof(be.AvailablePower), 8f),
      (nameof(be.InletPressure), 2.4f),
    })
      typeof(BlockEntityEngine).GetProperty(name)!.SetValue(be, value);
    return be;
  }

  private static void AssertEngine(BlockEntityEngine be, string asset) {
    Assert.True(be.IsRunning);
    Assert.False(be.IsBroken);
    Assert.Equal(1.25f, be.AnimationSpeed, 3);
    Assert.Equal(8f, be.AvailablePower, 3);
    Assert.Equal(2.4f, be.InletPressure, 3);
    Assert.True(be.IsConstructed);
    Assert.Equal(
      SaveFixtures.ConstructionStages(Blocktypes + asset) - 1,
      SaveFixtures.ConstructionStage(be)
    );
  }

  #endregion
}
