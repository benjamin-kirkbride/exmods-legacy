using System.IO;
using System.Linq;
using ExpandedLib;
using ExpandedLib.Industry.MechanicalPower;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Structures;
using ExpandedLib.Testing;
using Integration.Tests.Saves;
using Newtonsoft.Json.Linq;
using PipesAndPowerExpanded;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockStructures.Engine.BlockEntities;
using PipesAndPowerExpanded.BlockStructures.Engine.Blocks;
using PipesAndPowerExpanded.Tests;
using SteelmakingExpanded;
using SteelmakingExpanded.BlockStructures.BlastFurnace.BlockEntities;
using SteelmakingExpanded.BlockStructures.BlastFurnace.Blocks;
using SteelmakingExpanded.BlockStructures.CowperStove.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Xunit;
using BoilerState = PipesAndPowerExpanded.BlockStructures.Boiler.BlockEntityBoiler.BoilerState;

namespace Integration.Tests.Pins;

/// <summary>
/// Machines that read the pipe network, on the published ppex and smex, as exact numbers: the
/// cowper's heat gain from exhaust, a boiler driving a Watt engine through a main, and the twin-tub
/// blower's shaft load and port face. Each scene also records its trace.
/// </summary>
public class MachinePinTests {
  #region Cowper stove

  [Fact]
  public void Exhaust_at_900_through_five_pipes_heats_a_cowper_core_to_80_in_a_minute() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    var at = new BlockPos(0, 4, 0);
    var stove = Cowper(scene, at);
    BlockPos[] run = Enumerable
      .Range(1, 5)
      .Select(i => at.AddCopy(0, 0, i))
      .ToArray();
    var pipe = PipeTestWorld.MakePipe(orientation: "ns");
    foreach (BlockPos p in run)
      scene.World.Place(p, pipe, new BlockEntityPipe());
    scene.Block(at.AddCopy(0, 0, 6), PpexScenes.Cap());
    foreach (BlockPos p in run)
      scene.World.Initialize(scene.World.GetBlockEntity(p)!);
    scene.Build();
    var trace = new Trace("machine-cowper-exhaust");
    float afterOne = 0f;

    // The furnace side offers more than the stove draws, so the run stays full at 1 atm and the
    // stove heats from gas that has stood in the pipes.
    for (int t = 1; t <= 60; t++) {
      scene
        .NetworkAt<PipeNetwork>(run[^1])!
        .TryProduceGas(
          2 * SmexValues.CowperIntakeVolume,
          900f,
          "Exhaust",
          scene.World.Accessor
        );
      ReflectionHelpers.Invoke(stove, "OnProductionTick", 1f);
      scene.Step();
      PipeTrace.Runs(trace, t, scene.World, run);
      trace.Line(
        t,
        "cowper",
        "complete=" + Trace.Flag(stove.StructureComplete),
        "core=" + Trace.Celsius(Core(stove)),
        "drops=" + scene.World.Drops.Count
      );
      if (t == 1)
        afterOne = Core(stove);
    }
    trace.Save();

    Assert.Equal(21.1f, afterOne, Trace.TemperatureDigits);
    Assert.Equal(80.1f, Core(stove), Trace.TemperatureDigits);
  }

  private static float Core(BlockEntityCowperStove stove) =>
    (float)ReflectionHelpers.GetField(stove, "_internalTemperature")!;

  /// <summary>
  /// A complete north-facing cowper stove at <paramref name="pos"/>, its connector on the south
  /// face, with the tunables its <c>Initialize</c> would cache primed from the live config.
  /// </summary>
  private static BlockEntityCowperStove Cowper(Scene scene, BlockPos pos) {
    var block = TestBlocks.Configure(
      new Block(),
      "smex:cowperstove-north",
      85,
      ("side", "north")
    );
    var be = new BlockEntityCowperStove();
    scene.World.Place(pos, block, be);
    scene.World.Attach(be);
    ReflectionHelpers.Invoke(be, "UpdateStructureRotation");
    ReflectionHelpers.SetField(
      be,
      "_intakeVolume",
      SmexValues.CowperIntakeVolume
    );
    ReflectionHelpers.SetField(
      be,
      "_factorDefault",
      SmexValues.CowperHeatingSpeedDefault
    );
    ReflectionHelpers.SetField(
      be,
      "_factorOtherCoal",
      SmexValues.CowperHeatingSpeedOtherCoal
    );
    ReflectionHelpers.SetField(
      be,
      "_factorAnthracite",
      SmexValues.CowperHeatingSpeedAnthracite
    );
    ReflectionHelpers.SetField(
      be,
      "_coolingSpeedExhaust",
      SmexValues.CowperCoolingSpeedExhaust
    );
    ReflectionHelpers.SetField(
      be,
      "_coolingSpeedAir",
      SmexValues.CowperCoolingSpeedAir
    );
    ReflectionHelpers.SetField(
      be,
      "_maxTemperature",
      SmexValues.CowperMaxTemperature
    );
    ReflectionHelpers.SetProperty(be, "StructureComplete", true);
    ReflectionHelpers.SetField(be, "_connectorFace", BlockFacing.SOUTH);
    return be;
  }

  #endregion

  #region Boiler and Watt engine

  [Fact]
  public void A_boiling_boiler_runs_a_watt_engine_47_seconds_of_a_minute_through_a_ten_pipe_main() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    var boiler = new BoilerFixture(scene, new BlockPos(0, 8, 0));
    BlockPos attach = boiler.SteamPipeAttachPos;

    // The main leaves the boiler's steam port up through a bend and runs nine pipes north to the
    // engine's inlet; the port filler that caps the bend's down face in game is a cap here.
    BlockPos[] main = Enumerable
      .Range(0, 10)
      .Select(i => attach.AddCopy(0, 0, -i))
      .ToArray();
    var bend = PipeTestWorld.MakePipe(orientation: "dn", id: 50);
    ReflectionHelpers.SetProperty(bend, "Type", "bend");
    var straight = PipeTestWorld.MakePipe(orientation: "ns", id: 51);
    for (int i = 0; i < main.Length; i++)
      scene.World.Place(
        main[i],
        i == 0 ? bend : straight,
        new BlockEntityPipe()
      );
    scene.Block(attach.DownCopy(), PpexScenes.Cap(52));
    foreach (BlockPos p in main)
      scene.World.Initialize(scene.World.GetBlockEntity(p)!);

    var engine = WattWithGenerator(scene, main[^1].AddCopy(0, 0, -1));
    scene.Build();
    boiler.Prime(BoilerState.Boiling, water: 600f, steam: 700f);

    var trace = new Trace("machine-boiler-watt");
    float output = 0f;
    int engaged = 0;
    int running = 0;
    for (int t = 1; t <= 60; t++) {
      scene.Step();
      output += engine.MpPowerBudget;
      if (engine.IsRunning) {
        running++;
        if (engaged == 0)
          engaged = t;
      }
      PipeTrace.Runs(trace, t, scene.World, main);
      trace.Line(
        t,
        "boiler",
        "steam=" + Trace.Litres(boiler.SteamVolume),
        "drops=" + scene.World.Drops.Count
      );
      trace.Line(
        t,
        "engine",
        "constructed=" + Trace.Flag(engine.IsConstructed),
        "broken=" + Trace.Flag(engine.IsBroken),
        "inlet=" + Trace.Atm(engine.InletPressure),
        "power=" + Trace.Power(engine.AvailablePower),
        "shaft=" + Trace.Power(engine.MpPowerBudget)
      );
    }
    trace.Save();

    Assert.Equal(11, engaged);
    Assert.Equal(47, running);
    Assert.Equal(28.2f, output, Trace.PowerDigits);
  }

  /// <summary>
  /// A constructed north-facing Watt engine at <paramref name="pos"/>, its inlet on the south face,
  /// driving an MP generator in its sub-machine cell.
  /// </summary>
  private static BlockEntityEngineWatt WattWithGenerator(
    Scene scene,
    BlockPos pos
  ) {
    var block = TestBlocks.Configure(
      new BlockEngineWatt(),
      "ppex:enginewatt-north",
      53,
      ("side", "north")
    );
    var engine = new BlockEntityEngineWatt();
    scene.Machine(pos, block, engine);
    RccFake.Complete(engine);

    BlockPos sub = block.SubmachinePos(pos);
    var genBlock = TestBlocks.Configure(
      new BlockEngineMPGenerator(),
      "ppex:enginempgenerator-east",
      54,
      ("side", "east")
    );
    scene.Machine(sub, genBlock, new BlockEntityEngineMpGenerator());
    return engine;
  }

  #endregion

  #region Twin-tub blower

  private const string BlowerDef =
    "smex/assets/smex/blocktypes/blastfurnace/mpblower.json";

  private static readonly BlockPos BlowerAt = new(0, 16, 0);

  [Fact]
  public void The_blower_port_carries_the_shaft_load_of_the_main_pressure() {
    var (scene, blower, port, main) = Blower();
    var trace = new Trace("machine-blower-load");
    var net = scene.NetworkAt<PipeNetwork>(main)!;
    float[] loads = new float[3];

    float[] atm = [0f, 1.5f, 2f];
    for (int t = 0; t < atm.Length; t++) {
      float want =
        atm[t] * ExlibValues.LitresPerPipe - (net.State?.Volume ?? 0f);
      if (want > 0f)
        net.TryProduceGas(
          want,
          20f,
          "Air",
          scene.World.Accessor,
          maxOutputPressure: atm[t]
        );
      scene.Step();
      loads[t] = port.GetResistance();
      PipeTrace.Runs(trace, t + 1, scene.World, [main]);
      trace.Line(
        t + 1,
        "blower",
        "constructed=" + Trace.Flag(blower.IsConstructed),
        "stage=" + SaveFixtures.ConstructionStage(blower),
        "load=" + Trace.Power(loads[t]),
        "output=" + Trace.Litres(blower.OutputPerSecond),
        "drops=" + scene.World.Drops.Count
      );
    }
    trace.Save();

    Assert.Equal(0.05f, loads[0], Trace.PowerDigits);
    Assert.Equal(0.125f, loads[1], Trace.PowerDigits);
    Assert.Equal(0.15f, loads[2], Trace.PowerDigits);
    Assert.Equal(
      BlockEntityMpBlower.ShaftLoadAt(1.5f),
      loads[1],
      Trace.PowerDigits
    );
  }

  [Fact]
  public void An_axle_on_the_port_cells_west_face_does_not_couple() {
    var (scene, _, port, _) = Blower();
    BlockPos portCell = port.Blockentity.Pos;
    var filler = (BlockStructureFiller)scene.World.GetBlock(portCell);

    Assert.Equal(BlockFacing.EAST, port.PortFacing);
    Assert.True(Couples(filler, scene, portCell, BlockFacing.EAST));
    Assert.False(Couples(filler, scene, portCell, BlockFacing.WEST));
  }

  private static bool Couples(
    BlockStructureFiller filler,
    Scene scene,
    BlockPos pos,
    BlockFacing face
  ) => filler.HasMechPowerConnectorAt(scene.World.World, pos, face
#if GAME_GE_1_22
      , null!
#endif
    );

  /// <summary>
  /// A constructed south-facing twin-tub blower from the shipped def: its footprint frame is the
  /// def's own, so the port declared on the east face couples east. The port cell holds a filler
  /// hosting the declared port behaviour, not initialised, since that would join it to a live power
  /// network. The outlet cell holds a filler marked as a pipe port on the outlet face, as placement
  /// marks it, and the blast main is one sealed pipe past it. Returns the scene, the blower, its
  /// port and the main's cell.
  /// </summary>
  private static (
    Scene scene,
    BlockEntityMpBlower blower,
    BEBehaviorMPFillerPort port,
    BlockPos main
  ) Blower() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    SaveRegistry.Instance.Wire(scene.World);

    var block = TestBlocks.Configure(
      new BlockMpBlower(),
      "smex:mpblower-south",
      400,
      ("side", "south")
    );
    SaveFixtures.ShippedBehaviors(block, BlowerDef);
    block.Attributes = new JsonObject(
      JObject.Parse(
        File.ReadAllText(Path.Combine(SaveGoldens.RepoRoot(), BlowerDef))
      )["attributes"]!
    );

    FillerCell cell = StructureFillers
      .FootprintCells(block, BlowerAt, block.StructureAngle)
      .Single(c => c.Behaviors != null);
    FillerBehavior hosted = Assert.Single(cell.Behaviors!);
    var fillerBe = new BlockEntityStructureFiller {
      Principal = BlowerAt.Copy(),
      AllowAttach = cell.AllowAttach,
      HostedBehaviors = cell.Behaviors,
    };
    scene.World.Place(
      cell.Pos,
      TestBlocks.Configure(
        new BlockStructureFiller(),
        "exlib:structurefiller",
        401
      ),
      fillerBe
    );
    scene.World.Attach(fillerBe);
    scene.World.Place(
      block.BlastOutletWorldPos(BlowerAt),
      scene.World.GetBlock(cell.Pos),
      new BlockEntityStructureFiller {
        Principal = BlowerAt.Copy(),
        AllowAttach = true,
        PortFace = block.OutletFace.Code[..1],
        PortNetworkType = "pipe",
      }
    );
    var port = new BEBehaviorMPFillerPort(fillerBe);
    port.ConfigureFromFiller(BlowerAt, hosted.ConnectorFace, hosted.Properties);
    fillerBe.Behaviors.Add(port);

    var blower = new BlockEntityMpBlower();
    SaveFixtures.Stand(scene.World, BlowerAt, block, blower);
    SaveFixtures.CompleteConstruction(blower, scene.World);

    BlockPos main = block
      .BlastOutletWorldPos(BlowerAt)
      .AddCopy(block.OutletFace);
    var pipe = PipeTestWorld.MakePipe(
      orientation: block.OutletFace.Axis == EnumAxis.Z ? "ns" : "we",
      id: 402
    );
    scene.World.Place(main, pipe, new BlockEntityPipe());
    scene.Block(main.AddCopy(block.OutletFace), PpexScenes.Cap(403));
    scene.World.Initialize(scene.World.GetBlockEntity(main)!);
    scene.Build();
    return (scene, blower, port, main);
  }

  #endregion
}
