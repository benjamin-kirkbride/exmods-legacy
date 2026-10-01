using System.Collections.Generic;
using ExpandedLib.Helpers;
using ExpandedLib.Industry.Helpers;
using ExpandedLib.Industry.MechanicalPower;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Testing;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using PipesAndPowerExpanded.BlockStructures.Engine.BlockEntities;
using PipesAndPowerExpanded.BlockStructures.Engine.Blocks;
using PipesAndPowerExpanded.BlockStructures.ManualPump.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using BoilerState = PipesAndPowerExpanded.BlockStructures.Boiler.BlockEntityBoiler.BoilerState;

namespace PipesAndPowerExpanded.Tests;

/// <summary>
/// Models the documented starter-setup detail that a Cornish boiler can out-pressure a Watt engine, so
/// its steam main is gated through a <see cref="BlockEntityPressureValve"/>: a sealed steam main feeds
/// the engine's inlet, and a relief valve on the far end bleeds anything above its gate into a drain
/// line. Charge the main hard (boiler over-pressure stand-in) and the valve keeps the line near its
/// gate, so the engine runs in its band instead of climbing toward a burst.
/// </summary>
internal sealed class RegulatedEnginePlant {
  public readonly BlockEntityEngineWatt Engine;
  public readonly BlockEntityPressureValve Valve;

  private readonly Scene _scene;
  private readonly BlockPos _main;
  private readonly BlockPos _drain;

  /// <summary>The engine's placed block, for a test that needs one of its connector faces.</summary>
  public BlockEngineWatt EngineBlock => (BlockEngineWatt)Engine.Block;

  public RegulatedEnginePlant(Scene scene, BlockPos enginePos, float gateAtm) {
    _scene = scene;

    var engineBlock = TestBlocks.Configure(
      new BlockEngineWatt(),
      "ppex:enginewatt-north",
      55,
      ("side", "north")
    );
    Engine = new BlockEntityEngineWatt {
      Pos = enginePos.Copy(),
      Block = engineBlock,
    };
    scene.Machine(enginePos, engineBlock, Engine);
    RccFake.Complete(Engine);

    // Steam main: two steel pipes running from the engine's inlet face toward the valve. The engine's
    // block caps the north end, the valve caps the south end, so the run is sealed and holds pressure.
    BlockFacing inletFace = engineBlock.SteamInletFace; // south for a north engine
    BlockPos m0 = enginePos.AddCopy(inletFace);
    BlockPos m1 = m0.AddCopy(inletFace);
    string mainAxis = EnginePlant.Axis(inletFace);
    EnginePlant.Pipe(scene, m0, mainAxis, 56, material: "steel");
    EnginePlant.Pipe(scene, m1, mainAxis, 57, material: "steel");
    _main = m0;

    // Relief valve south of the main: input face points back north at the main, output face south at
    // the drain. It caps the main's south end (non-air) and bridges the main to the drain network.
    string orient = $"{inletFace.Opposite.Code[0]}{inletFace.Code[0]}";
    var valveBlock = TestBlocks.Configure(
      new BlockPressureValve(),
      $"ppex:pressurevalve-steel-{orient}",
      58,
      ("material", "steel"),
      ("type", "pressurevalve"),
      ("orientation", orient)
    );
    ReflectionHelpers.SetProperty(valveBlock, "Type", "pressurevalve");
    ReflectionHelpers.SetProperty(valveBlock, "Orientation", orient);
    BlockPos vPos = m1.AddCopy(inletFace);
    Valve = new BlockEntityPressureValve {
      Pos = vPos.Copy(),
      Block = valveBlock,
    };
    scene.Machine(vPos, valveBlock, Valve); // place + Initialize (registers the relief tick); not a node
    ReflectionHelpers.SetProperty(
      Valve,
      nameof(Valve.NetworkSystem),
      scene.World.Networks
    );
    SetGate(gateAtm);

    // Drain line on the valve's output face - the relief sink, capped so it reads its own pressure.
    BlockPos d0 = vPos.AddCopy(inletFace);
    EnginePlant.Pipe(scene, d0, mainAxis, 59, material: "steel");
    scene.Block(d0.AddCopy(inletFace), PpexScenes.Cap(60));
    _drain = d0;

    // Fluid-pump sub-machine so the engine has a power demand (it only engages with one).
    BlockPos subPos = engineBlock.SubmachinePos(enginePos);
    var pumpBlock = TestBlocks.Configure(
      new BlockEngineFluidPump(),
      "ppex:enginefluidpump-east",
      61,
      ("side", "east")
    );
    scene.Machine(
      subPos,
      pumpBlock,
      new BlockEntityEngineFluidPump { Pos = subPos.Copy(), Block = pumpBlock }
    );
  }

  /// <summary>Dials the valve's gate to <paramref name="atm"/> (stepping from its 1 atm default).</summary>
  public void SetGate(float atm) {
    // Walk the gate up/down in the valve's real 0.25 atm steps so the clamp logic is exercised.
    int guard = 0;
    while (
      Valve.GatePressure < atm - 0.001f
      && Valve.AdjustGatePressure(true)
      && guard++ < 200
    ) { }
    while (
      Valve.GatePressure > atm + 0.001f
      && Valve.AdjustGatePressure(false)
      && guard++ < 200
    ) { }
  }

  /// <summary>Charges the steam main to <paramref name="atm"/> (the boiler's over-pressure).</summary>
  public RegulatedEnginePlant Charge(float atm) {
    _scene
      .NetworkAt<PipeNetwork>(_main)!
      .TryProduceGas(
        atm * 60f,
        150f,
        "Steam",
        _scene.World.Accessor,
        maxOutputPressure: atm
      );
    return this;
  }

  /// <summary>
  /// Holds the main at <paramref name="atm"/> for <paramref name="seconds"/> ticks - a boiler
  /// continuously over-pressuring the line - re-charging before each tick so the running engine and
  /// the relief valve both act on a fed main (a sealed run would otherwise deplete as the engine draws).
  /// </summary>
  public RegulatedEnginePlant RunCharged(float atm, int seconds) {
    for (int i = 0; i < seconds; i++) {
      Charge(atm);
      _scene.Step(1);
    }
    return this;
  }

  public float MainPressure =>
    _scene.NetworkAt<PipeNetwork>(_main)!.State?.Pressure ?? 0f;
  public float DrainVolume =>
    _scene.NetworkAt<PipeNetwork>(_drain)!.State?.Volume ?? 0f;
  public float InletPressure => Engine.InletPressure;
}

/// <summary>
/// Models the engine-free way to start a water loop (handbook starter setup, step 2): a hand-cranked
/// <see cref="BlockEntityManualFluidPump"/> drawing from a pond intake on its input line and lifting
/// water at a fixed 1 atm into an output main - the line you run up into a boiler before any steam
/// engine exists. The intake is the generator; the pump only moves what stands in the input line.
/// </summary>
internal sealed class ManualPumpPlant {
  public readonly BlockEntityManualFluidPump Pump;
  public readonly BlockEntityFluidIntake Intake;

  private readonly Scene _scene;
  private readonly BlockPos _pond;
  private readonly BlockPos _output;

  public ManualPumpPlant(Scene scene, BlockPos pos) {
    _scene = scene;

    var pumpBlock = TestBlocks.Configure(
      new Block(),
      "ppex:manualfluidpump-north",
      62,
      ("side", "north")
    );
    Pump = new BlockEntityManualFluidPump {
      Pos = pos.Copy(),
      Block = pumpBlock,
    };
    scene.Machine(pos, pumpBlock, Pump); // Initialize registers the crank work tick

    int angle = ExOrientation.AngleFromSide("north");
    BlockFacing inFace = ExOrientation.RotateFacing(BlockFacing.SOUTH, angle);
    BlockFacing outFace = ExOrientation.RotateFacing(BlockFacing.NORTH, angle);

    // Pond intake on the input face (presents a connector back at the pump).
    _pond = pos.AddCopy(inFace);
    var intakeBlock = TestBlocks.Configure(
      new BlockFluidIntake(),
      "ppex:fluidintake",
      63,
      ("orientation", inFace.Opposite.Code[..1])
    );
    ReflectionHelpers.SetProperty(
      intakeBlock,
      "Orientation",
      inFace.Opposite.Code[..1]
    );
    Intake = new BlockEntityFluidIntake {
      Pos = _pond.Copy(),
      Block = intakeBlock,
    };
    scene.Node(_pond, intakeBlock, Intake, "pipe");
    ReflectionHelpers.SetProperty(Intake, nameof(Intake.HasWater), true);
    ReflectionHelpers.SetProperty(
      Intake,
      nameof(Intake.NetworkSystem),
      scene.World.Networks
    );

    // Output main on the delivery face - the line that climbs into the boiler.
    _output = pos.AddCopy(outFace);
    string axis = EnginePlant.Axis(outFace);
    EnginePlant.Pipe(scene, _output, axis, 64);
    scene.Block(_output.AddCopy(outFace), PpexScenes.Cap(65));
  }

  /// <summary>Pre-fills the pond's input line with standing water for the pump to lift.</summary>
  public ManualPumpPlant FillPond(float litres) {
    _scene
      .NetworkAt<PipeNetwork>(_pond)!
      .TryProduceLiquid(litres, 20f, 1f, _scene.World.Accessor);
    return this;
  }

  /// <summary>Cranks the pump for <paramref name="seconds"/> ticks (a player holding right-click).</summary>
  public ManualPumpPlant Crank(int seconds) {
    Pump.OnPumpStart();
    for (int i = 0; i < seconds; i++) {
      Pump.OnPumpStep(); // refresh the watchdog as a held button would
      _scene.Step(1);
    }
    return this;
  }

  /// <summary>The output main.</summary>
  public PipeNetwork Output => _scene.NetworkAt<PipeNetwork>(_output)!;

  public float OutputVolume =>
    _scene.NetworkAt<PipeNetwork>(_output)!.State?.Volume ?? 0f;
  public bool OutputIsWater =>
    _scene.NetworkAt<PipeNetwork>(_output)!.State?.IsLiquid ?? false;
}

/// <summary>
/// Models the steam plant's closed water loop's recovery leg (handbook starter setup, step 5): a
/// <see cref="BlockEntitySteamCondenser"/> takes the engine's spent steam off the north line and
/// condenses it into the water passing W→E, sending the recovered water (condensate + through-flow)
/// on toward the boiler instead of venting it. North = spent-steam line, west = feed water, east =
/// the recovered-water line back to the boiler.
/// </summary>
internal sealed class CondenserPlant {
  public readonly BlockEntitySteamCondenser Condenser;

  private readonly Scene _scene;
  private readonly BlockPos _steam;
  private readonly BlockPos _feed;
  private readonly BlockPos _recovered;

  public CondenserPlant(Scene scene, BlockPos pos) {
    _scene = scene;

    var block = TestBlocks.Configure(
      new PipesAndPowerExpanded.BlockNetworkPipe.Blocks.BlockSteamCondenser(),
      "ppex:steamcondenser-north",
      66,
      ("side", "north")
    );
    Condenser = new BlockEntitySteamCondenser {
      Pos = pos.Copy(),
      Block = block,
    };
    scene.Machine(pos, block, Condenser); // Initialize registers the condense tick

    // North: the spent-steam line.
    _steam = pos.AddCopy(BlockFacing.NORTH);
    EnginePlant.Pipe(scene, _steam, "ns", 67);
    scene.Block(_steam.AddCopy(BlockFacing.NORTH), PpexScenes.Cap(68));

    // West: the feed-water line (the fuller side, so it reads as the inlet).
    _feed = pos.AddCopy(BlockFacing.WEST);
    EnginePlant.Pipe(scene, _feed, "we", 69);
    scene.Block(_feed.AddCopy(BlockFacing.WEST), PpexScenes.Cap(71));

    // East: the recovered-water line back toward the boiler (starts empty -> the outlet).
    _recovered = pos.AddCopy(BlockFacing.EAST);
    EnginePlant.Pipe(scene, _recovered, "we", 72);
    scene.Block(_recovered.AddCopy(BlockFacing.EAST), PpexScenes.Cap(73));
  }

  public CondenserPlant ChargeSteam(float litres) {
    _scene
      .NetworkAt<PipeNetwork>(_steam)!
      .TryProduceGas(
        litres,
        150f,
        "Steam",
        _scene.World.Accessor,
        maxOutputPressure: 20f
      );
    return this;
  }

  public CondenserPlant ChargeFeedWater(float litres) {
    _scene
      .NetworkAt<PipeNetwork>(_feed)!
      .TryProduceLiquid(litres, 40f, 1f, _scene.World.Accessor);
    return this;
  }

  public bool Condensing =>
    (bool)ReflectionHelpers.GetField(Condenser, "_condensing")!;
  public float SteamVolume =>
    _scene.NetworkAt<PipeNetwork>(_steam)!.State?.Volume ?? 0f;
  public float RecoveredVolume =>
    _scene.NetworkAt<PipeNetwork>(_recovered)!.State?.Volume ?? 0f;
  public bool RecoveredIsWater =>
    _scene.NetworkAt<PipeNetwork>(_recovered)!.State?.IsLiquid ?? false;
}

/// <summary>
/// A fired Cornish boiler fed through one machine from a hand-cranked pump: a pond intake, the
/// manual pump, a five-pipe main, a steam condenser or a water relief valve, and a two-pipe line
/// up into the boiler's feed face, every run standing full of water at the start. The boiler's
/// steam port carries a sealed steel pipe charged above anything the boiler reaches in a second,
/// so the steam in the vessel after a tick is what it made in that tick. The pump, the machine
/// between and the boiler are placed, and so tick, in the order the constructor is given.
/// </summary>
internal sealed class FedBoilerPlant {
  /// <summary>The machine between the pump's main and the boiler's line.</summary>
  public enum Between {
    Condenser,
    Valve,
  }

  /// <summary>The machines whose tick order the plant takes.</summary>
  public enum Ticker {
    Pump,
    Between,
    Boiler,
  }

  /// <summary>Water (L) the boiler is primed with before each second, below its intake fill, so
  /// it asks its line for its full intake rate every second.</summary>
  public const float PrimeWater = 200f;

  public readonly Scene Scene = new Scene().Network(
    "pipe",
    s => new PipeNetwork(s)
  );
  public readonly BlockEntityManualFluidPump Pump;
  public readonly BlockEntity Middle;
  public BoilerFixture Boiler { get; private set; } = null!;

  private readonly BlockPos _main = new(-3, 7, 0);
  private readonly BlockPos _line = new(-1, 7, 0);

  /// <param name="between">The machine at (-2, 7, 0); a valve is gated at
  /// <paramref name="gate"/> atm.</param>
  /// <param name="order">The pump, the machine between and the boiler, each once.</param>
  public FedBoilerPlant(
    Between between,
    IReadOnlyList<Ticker> order,
    float gate = 1.5f
  ) {
    var boilerPos = new BlockPos(0, 8, 0);
    var middlePos = new BlockPos(-2, 7, 0);
    var pumpPos = new BlockPos(-7, 7, 1);

    // The boiler's line: up into the feed face under the master cell, west to the machine.
    EnginePlant.Pipe(Scene, boilerPos.DownCopy(), "uw", 80);
    EnginePlant.Pipe(Scene, _line, "we", 81);

    // The pump's main: north off the pump's delivery face, east to the machine.
    for (int x = -6; x <= -3; x++)
      EnginePlant.Pipe(Scene, new BlockPos(x, 7, 0), "we", 100 + x);
    EnginePlant.Pipe(Scene, new BlockPos(-7, 7, 0), "se", 83);

    var pumpBlock = TestBlocks.Configure(
      new Block(),
      "ppex:manualfluidpump-north",
      84,
      ("side", "north")
    );
    Pump = new BlockEntityManualFluidPump {
      Pos = pumpPos.Copy(),
      Block = pumpBlock,
    };

    BlockPos pond = pumpPos.SouthCopy();
    var intakeBlock = TestBlocks.Configure(
      new BlockFluidIntake(),
      "ppex:fluidintake",
      85,
      ("orientation", "n")
    );
    ReflectionHelpers.SetProperty(intakeBlock, "Orientation", "n");
    var intake = new BlockEntityFluidIntake {
      Pos = pond.Copy(),
      Block = intakeBlock,
    };
    Scene.Node(pond, intakeBlock, intake, "pipe");
    ReflectionHelpers.SetProperty(intake, nameof(intake.HasWater), true);
    ReflectionHelpers.SetProperty(
      intake,
      nameof(intake.NetworkSystem),
      Scene.World.Networks
    );

    Block middleBlock;
    if (between == Between.Condenser) {
      middleBlock = TestBlocks.Configure(
        new PipesAndPowerExpanded.BlockNetworkPipe.Blocks.BlockSteamCondenser(),
        "ppex:steamcondenser-north",
        86,
        ("side", "north")
      );
      Middle = new BlockEntitySteamCondenser {
        Pos = middlePos.Copy(),
        Block = middleBlock,
      };
    } else {
      middleBlock = TestBlocks.Configure(
        new BlockPressureValve(),
        "ppex:pipe-pressurevalve-we-iron",
        86,
        ("type", "pressurevalve"),
        ("orientation", "we"),
        ("material", "iron")
      );
      ReflectionHelpers.SetProperty(middleBlock, "Type", "pressurevalve");
      ReflectionHelpers.SetProperty(middleBlock, "Orientation", "we");
      Middle = new BlockEntityPressureValve {
        Pos = middlePos.Copy(),
        Block = middleBlock,
      };
    }

    foreach (Ticker ticker in order)
      switch (ticker) {
        case Ticker.Pump:
          Scene.Machine(pumpPos, pumpBlock, Pump);
          break;
        case Ticker.Between:
          Scene.Machine(middlePos, middleBlock, Middle);
          if (Middle is BlockEntityPressureValve valve) {
            ReflectionHelpers.SetProperty(
              valve,
              nameof(valve.NetworkSystem),
              Scene.World.Networks
            );
            SetGate(gate);
          }
          break;
        case Ticker.Boiler:
          Boiler = new BoilerFixture(Scene, boilerPos, 87, 88);
          break;
      }

    BlockPos steam = Boiler.SteamPipeAttachPos;
    Scene.Block(steam.DownCopy(), PpexScenes.Cap(89));
    EnginePlant.Pipe(Scene, steam, "ud", 90, material: "steel");
    Scene.Block(steam.UpCopy(), PpexScenes.Cap(91));
    Scene.Build();
    Scene
      .NetworkAt<PipeNetwork>(steam)!
      .TryProduceGas(
        60f,
        150f,
        "Steam",
        Scene.World.Accessor,
        maxOutputPressure: 2f
      );
    foreach (BlockPos run in new[] { pond, _main, _line })
      while (
        Scene
          .NetworkAt<PipeNetwork>(run)!
          .TryProduceLiquid(1000f, 20f, 1f, Scene.World.Accessor)
      ) { }
    Pump.OnPumpStart();
  }

  /// <summary>The line from the machine between into the boiler.</summary>
  public PipeNetwork Line => Scene.NetworkAt<PipeNetwork>(_line)!;

  /// <summary>
  /// Runs <paramref name="seconds"/> seconds with the pump cranked, the boiler primed boiling with
  /// <see cref="PrimeWater"/> and no steam before each, and returns the steam (L) it flashed from
  /// its feed in each: what it holds after the tick over what its fire boiled.
  /// </summary>
  public List<float> Run(int seconds) {
    var flashed = new List<float>();
    for (int t = 0; t < seconds; t++) {
      Boiler.Prime(BoilerState.Boiling, water: PrimeWater, steam: 0f);
      Pump.OnPumpStep();
      Scene.Step(1);
      flashed.Add(
        Boiler.SteamVolume - PpexValues.CornishBoilerSteamPerSecond
      );
    }
    return flashed;
  }

  /// <summary>Takes the main's first pipe off the machine between, leaving its west face
  /// capped.</summary>
  public void CutMain() {
    Scene.World.RemoveNode(_main);
    Scene.Block(_main, PpexScenes.Cap(92));
  }

  /// <summary>Dials the valve between to <paramref name="atm"/> in its own steps.</summary>
  public void SetGate(float atm) {
    var valve = (BlockEntityPressureValve)Middle;
    while (valve.GatePressure < atm - 0.001f && valve.AdjustGatePressure(true)) { }
    while (valve.GatePressure > atm + 0.001f && valve.AdjustGatePressure(false)) { }
  }
}
