using System;
using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Testing;
using PipesAndPowerExpanded;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using PipesAndPowerExpanded.BlockStructures.Engine.BlockEntities;
using PipesAndPowerExpanded.BlockStructures.Engine.Blocks;
using PipesAndPowerExpanded.Tests;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using BlockPipePassthrough = PipesAndPowerExpanded.BlockNetworkPipe.Blocks.BlockPipePassthrough;
using BoilerState = PipesAndPowerExpanded.BlockStructures.Boiler.BlockEntityBoiler.BoilerState;

namespace Integration.Tests.Setups;

/// <summary>
/// The starter steam power setup as the wiki draws it, every machine under its shipped code: a fired
/// Cornish boiler with a chimney on its exhaust, its steam main to a Watt engine turning a fluid pump,
/// a 3.5 atm relief valve on the steam main, a pond intake under the pump, and the feed main from the
/// pump and the engine's condensate back into the boiler with a 0.5 atm relief valve. Machines are
/// built in tick order, the engine, the pump and the water valve last in the order the constructor
/// is given. Each second is recorded under the drawing's ids and kept as a <see cref="Second"/>.
/// </summary>
internal sealed class StarterSteamPowerPlant {
  /// <summary>The file name of the setup's recording.</summary>
  public const string Setup = "starter-steam-power";

  /// <summary>Gate of the steam main's relief valve (atm).</summary>
  public const float SteamGate = 3.5f;

  /// <summary>Gate of the feed main's relief valve (atm).</summary>
  public const float WaterGate = 0.5f;

  /// <summary>Water poured into the boiler before it is fired (L), a bucket pour's limit.</summary>
  public const float PrimeWater = 500f;

  /// <summary>One second of the run, read after its tick.</summary>
  public readonly record struct Second(
    int At,
    bool Choked,
    bool PileBurning,
    float BoilerPressure,
    float Water,
    int PipesLost,
    bool EngineBroken,
    bool EngineRunning,
    float SteamPressure,
    float InletPressure,
    float Feed
  );

  /// <summary>The machines whose tick order the plant takes as a constructor argument.</summary>
  public enum Ticker {
    Engine,
    Pump,
    WaterValve,
  }

  /// <summary>The tick order D11 builds: engine, pump, then the water valve.</summary>
  public static readonly IReadOnlyList<Ticker> DrawnOrder = [
    Ticker.Engine,
    Ticker.Pump,
    Ticker.WaterValve,
  ];

  public readonly Scene Scene = new Scene().Network(
    "pipe",
    s => new PipeNetwork(s)
  );
  public readonly BoilerFixture Boiler;
  public readonly BlockEntityEngineWatt Engine;
  public readonly BlockEntityEngineFluidPump Pump;
  public readonly BlockEntityFluidIntake Intake;
  public readonly BlockEntityPressureValve? SteamValve;
  public readonly BlockEntityPressureValve WaterValve;

  /// <summary>Every second run so far, from 0.</summary>
  public readonly List<Second> Seconds = [];

  private readonly BlockPos _exhaust;
  private readonly BlockPos _steam;
  private readonly BlockPos _feed;
  private readonly BlockPos _pond;
  private readonly List<BlockPos> _pipes = [];
  private readonly List<(BlockPos Pos, string Code)> _placed = [];
  private int _nextId = 200;
  private SetupRecording? _recording;
  private float _lastWater;

  /// <summary>Water (L) the boiler drew from its feed main in the last second: the change in its
  /// water plus what it boiled.</summary>
  private float _feedDraw;

  /// <param name="chimney">A chimney on the exhaust outlet; without one the outlet is open on top.</param>
  /// <param name="steamValve">The steam main's relief valve; without one its cell is capped.</param>
  /// <param name="openEnd">A pipe on the steam main open to air on its north face.</param>
  /// <param name="order">
  /// The order the engine, the pump and the water valve are placed in, which is their tick order;
  /// each named once. Null is <see cref="DrawnOrder"/>.
  /// </param>
  /// <param name="waterGate">Gate of the feed main's relief valve (atm).</param>
  /// <exception cref="ArgumentException"><paramref name="order"/> does not name each of the three once.</exception>
  public StarterSteamPowerPlant(
    bool chimney = true,
    bool steamValve = true,
    bool openEnd = false,
    IReadOnlyList<Ticker>? order = null,
    float waterGate = WaterGate
  ) {
    order ??= DrawnOrder;
    if (order.Count != 3 || order.Distinct().Count() != 3)
      throw new ArgumentException(
        "The order names the engine, the pump and the water valve once each.",
        nameof(order)
      );
    Scene.World.BreakRunsBlockHooks = false;

    Boiler = new BoilerFixture(Scene, new BlockPos(0, 8, 0));

    _exhaust = Boiler.Block.ExhaustOutletWorldPos(Boiler.Be.Pos);
    var outlet = new BlockEntityPipeOutlet();
    BlockPipeOutlet outletBlock = PpexScenes.UpOutlet(NextId());
    Scene.World.Place(_exhaust, outletBlock, outlet);
    Scene.World.Initialize(outlet);
    _placed.Add((_exhaust, outletBlock.Code.ToString()));
    if (chimney)
      Scene.Block(_exhaust.UpCopy(), PpexScenes.Chimney(NextId()));

    // Steam main: up off the boiler's port, east, down to the engine's inlet on the ground. The
    // port filler under the first pipe is a cap here.
    _steam = Boiler.SteamPipeAttachPos;
    BlockPos tee = _steam.AddCopy(1, 0, 0);
    BlockPos corner = _steam.AddCopy(2, 0, 0);
    BlockPos foot = new(corner.X, Boiler.Be.Pos.Y, corner.Z);
    BlockPos inlet = foot.EastCopy();
    Scene.Block(_steam.DownCopy(), PpexScenes.Cap(NextId()));
    var steamMain = new Main()
      .Open(_steam, BlockFacing.DOWN)
      .Lay(_steam, corner, foot, inlet)
      .Open(tee, BlockFacing.SOUTH)
      .Open(inlet, BlockFacing.NORTH);
    if (openEnd)
      steamMain.Open(tee, BlockFacing.NORTH);
    Place(steamMain);
    BlockPos steamValvePos = tee.SouthCopy();
    if (steamValve) {
      SteamValve = NewValve(steamValvePos, "ns");
      Install(SteamValve, SteamGate);
    } else
      Scene.Block(steamValvePos, PpexScenes.Cap(NextId()));

    BlockPos enginePos = inlet.NorthCopy();
    var engineBlock = TestBlocks.Configure(
      new BlockEngineWatt(),
      "ppex:enginewatt-north",
      NextId(),
      ("side", "north")
    );
    Engine = new BlockEntityEngineWatt {
      Pos = enginePos.Copy(),
      Block = engineBlock,
    };

    BlockPos pumpPos = engineBlock.SubmachinePos(enginePos);
    var pumpBlock = TestBlocks.Configure(
      new BlockEngineFluidPump(),
      "ppex:enginefluidpump-east",
      NextId(),
      ("side", "east")
    );
    Pump = new BlockEntityEngineFluidPump {
      Pos = pumpPos.Copy(),
      Block = pumpBlock,
    };

    // Pond run: a bend under the pump, the intake beside it on the pond, facing the bend.
    _pond = pumpPos.DownCopy();
    Place(new Main().Open(_pond, BlockFacing.UP).Open(_pond, BlockFacing.EAST));
    BlockPos intakePos = _pond.EastCopy();
    var intakeBlock = TestBlocks.Configure(
      new BlockFluidIntake(),
      "ppex:pipe-fluidintake-w",
      NextId(),
      ("type", "fluidintake"),
      ("orientation", "w")
    );
    ReflectionHelpers.SetProperty(intakeBlock, "Orientation", "w");
    Intake = new BlockEntityFluidIntake {
      Pos = intakePos.Copy(),
      Block = intakeBlock,
    };
    Scene.Node(intakePos, intakeBlock, Intake, "pipe");
    ReflectionHelpers.SetProperty(Intake, nameof(Intake.HasWater), true);
    ReflectionHelpers.SetProperty(
      Intake,
      nameof(Intake.NetworkSystem),
      Scene.World.Networks
    );

    // Feed main: from the engine's condensate outlet past the pump's left face, down a course,
    // south along the boiler's east side and round behind the firebox, then north through the
    // structure's two fireclay passthroughs to its bend under the master cell, the feed face. The
    // water valve hangs off it to the north.
    BlockFacing left = ExpandedLib.Helpers.ExOrientation.RotateFacing(
      BlockFacing.WEST,
      ExpandedLib.Helpers.ExOrientation.AngleFromSide("east")
    );
    BlockPos condensate = enginePos.AddCopy(engineBlock.WaterOutletFace);
    BlockPos pumpOut = pumpPos.AddCopy(left);
    BlockPos waterTee = pumpOut.WestCopy();
    _feed = Boiler.Be.Pos.DownCopy();
    BlockPos behind = _feed.AddCopy(0, 0, 3);
    Place(
      new Main()
        .Lay(
          condensate,
          new BlockPos(condensate.X, condensate.Y, pumpOut.Z),
          pumpOut,
          new BlockPos(_feed.X + 1, pumpOut.Y, pumpOut.Z),
          new BlockPos(_feed.X + 1, _feed.Y, pumpOut.Z),
          new BlockPos(_feed.X + 2, _feed.Y, pumpOut.Z),
          new BlockPos(_feed.X + 2, _feed.Y, behind.Z),
          behind,
          _feed
        )
        .Open(condensate, engineBlock.WaterOutletFace.Opposite)
        .Open(pumpOut, left.Opposite)
        .Open(waterTee, BlockFacing.NORTH)
        .Open(_feed, BlockFacing.UP),
      _feed,
      _feed.SouthCopy(),
      _feed.SouthCopy(2)
    );
    WaterValve = NewValve(waterTee.NorthCopy(), "sn");

    foreach (Ticker ticker in order)
      switch (ticker) {
        case Ticker.Engine:
          Scene.Machine(enginePos, engineBlock, Engine);
          RccFake.Complete(Engine);
          break;
        case Ticker.Pump:
          Scene.Machine(pumpPos, pumpBlock, Pump);
          break;
        case Ticker.WaterValve:
          Install(WaterValve, waterGate);
          break;
      }
  }

  /// <summary>
  /// Every pipe and pipe fitting the plant laid, with its placed code: the steam, pond and feed mains
  /// and the exhaust outlet. The valves, the intake and the cap under the steam main are not listed.
  /// </summary>
  public IReadOnlyList<(BlockPos Pos, string Code)> Placed => _placed;

  /// <summary>
  /// Records the plant's runs and machines under the drawing's ids, for <paramref name="seconds"/>.
  /// </summary>
  public SetupRecording Record(int seconds) {
    _recording = new SetupRecording(Setup, Scene, seconds);
    _recording.Run("steam", _steam);
    _recording.Run("exhaust", _exhaust);
    _recording.Run("feed", _feed);
    _recording.Run("pond", _pond);

    _recording
      .Machine("boiler", "ppex:boilercornish-north")
      .State(BoilerWord)
      .Value("water", "L", () => Water)
      .Value("pressure", "atm", () => Boiler.Be.InternalPressure)
      .Value("feed", "l/s", () => _feedDraw)
      .Constant("max", "atm", PpexValues.CornishBoilerMaxOutputPressure)
      .Constant("boostAbove", "atm", 1f);
    _recording
      .Machine("chimney", PpexScenes.ChimneyCode)
      .State(() => Draws ? "drawing" : "idle");
    _recording
      .Machine("engine", "ppex:enginewatt-north")
      .State(() =>
        Engine.IsBroken ? "broken"
        : Engine.IsRunning ? "running"
        : "idle"
      )
      .Value("pressure", "atm", () => Engine.InletPressure)
      .Constant("engage", "atm", PpexValues.WattEngineEngagePressure)
      .Constant("break", "atm", PpexValues.WattEngineBreakPressure)
      .Constant("steam", "l/s", PpexValues.WattEngineSteamRate)
      .Constant("water", "l/s", PpexValues.WattEngineWaterRate)
      .Constant("efficiency", "", PpexValues.SteamEngineEfficiency);
    _recording
      .Machine("pump", "ppex:enginefluidpump-east")
      .State(() => PumpDraws ? "drawing" : "idle");
    _recording
      .Machine("intake", "ppex:pipe-fluidintake-w")
      .State(() => PumpDraws ? "drawing" : "idle");
    if (SteamValve != null)
      RecordValve("steam-valve", SteamValve);
    RecordValve("water-valve", WaterValve);
    return _recording;
  }

  /// <summary>
  /// Primes the boiler idle with <see cref="PrimeWater"/> and no steam, its pile burning, then runs
  /// <paramref name="seconds"/> seconds, sampling the recording after each when there is one.
  /// </summary>
  public StarterSteamPowerPlant Run(int seconds) {
    Scene.Build();
    Boiler.Prime(BoilerState.Idle, water: PrimeWater, steam: 0f);
    _lastWater = Water;
    for (int t = 0; t < seconds; t++) {
      float boiled = Boiled();
      Scene.Step(1);
      float water = Water;
      _feedDraw = water - _lastWater + boiled;
      _lastWater = water;
      _recording?.Sample(t);
      Seconds.Add(
        new Second(
          t,
          Choked,
          Pile?.IsBurning == true,
          Boiler.Be.InternalPressure,
          Water,
          _pipes.Count(p => Scene.World.GetBlock(p).Id == 0),
          Engine.IsBroken,
          Engine.IsRunning,
          SteamRun?.State?.Pressure ?? 0f,
          Engine.InletPressure,
          _feedDraw
        )
      );
    }
    return this;
  }

  /// <summary>The first second <paramref name="failed"/> holds, or null when none does.</summary>
  public int? First(Func<Second, bool> failed, int from = 0) =>
    Seconds.Where(s => s.At >= from && failed(s)).Select(s => (int?)s.At).FirstOrDefault();

  private PipeNetwork? SteamRun => Scene.NetworkAt<PipeNetwork>(_steam);

  /// <summary>The feed main, from the pump and the engine's condensate outlet into the boiler.</summary>
  public PipeNetwork FeedRun => Scene.NetworkAt<PipeNetwork>(_feed)!;

  private BlockEntityCoalPile? Pile =>
    Scene.EntityAt<BlockEntityCoalPile>(Boiler.Block.FuelWorldPos(Boiler.Be.Pos));

  private bool Choked => (bool)ReflectionHelpers.GetField(Boiler.Be, "_choked")!;

  private float Water => (float)ReflectionHelpers.GetField(Boiler.Be, "_waterVolume")!;

  /// <summary>
  /// Water (L) the boiler turns to steam in the coming second, read before its tick: its full rate
  /// while boiling with water at or above its floor and pressure below its limit, else none.
  /// </summary>
  private float Boiled() =>
    (BoilerState)ReflectionHelpers.GetField(Boiler.Be, "_state")! == BoilerState.Boiling
    && Water >= PpexValues.CornishBoilerMinBoilWater
    && Boiler.Be.InternalPressure < PpexValues.CornishBoilerMaxOutputPressure
      ? PpexValues.CornishBoilerSteamPerSecond / PpexValues.SteamExpansionFactor
      : 0f;

  private bool PumpDraws =>
    (bool)ReflectionHelpers.GetField(Pump, "_drawingWater")!;

  private bool Draws =>
    Pile?.IsBurning == true
    && Scene.NetworkAt<PipeNetwork>(_exhaust)?.HasDraught(Scene.World.Accessor) == true;

  private string BoilerWord() {
    if (Choked)
      return "choked";
    if ((bool)ReflectionHelpers.GetField(Boiler.Be, "_steamLeaking")!)
      return "blowing";
    var state = (BoilerState)ReflectionHelpers.GetField(Boiler.Be, "_state")!;
    return state.ToString().ToLowerInvariant();
  }

  private void RecordValve(string id, BlockEntityPressureValve valve) {
    _recording!
      .Machine(id, valve.Block.Code.ToString())
      .State(() => Vent(valve) > 0f ? "venting" : "shut")
      .Value("vent", "l/s", () => Vent(valve))
      .Constant("gate", "atm", valve.GatePressure);
  }

  private static float Vent(BlockEntityPressureValve valve) =>
    (float)ReflectionHelpers.GetField(valve, "_lastVentVolume")!;

  private int NextId() => _nextId++;

  /// <summary>An iron pressure-relief valve for <paramref name="pos"/>, input face first in
  /// <paramref name="orientation"/>, its output open to air; placed by <see cref="Install"/>.</summary>
  private BlockEntityPressureValve NewValve(BlockPos pos, string orientation) {
    var block = TestBlocks.Configure(
      new BlockPressureValve(),
      $"ppex:pipe-pressurevalve-{orientation}-iron",
      NextId(),
      ("type", "pressurevalve"),
      ("orientation", orientation),
      ("material", "iron")
    );
    ReflectionHelpers.SetProperty(block, "Type", "pressurevalve");
    ReflectionHelpers.SetProperty(block, "Orientation", orientation);
    return new BlockEntityPressureValve { Pos = pos.Copy(), Block = block };
  }

  /// <summary>Places <paramref name="valve"/>, which starts its tick, and dials it to
  /// <paramref name="gate"/> atm in the valve's own steps.</summary>
  private void Install(BlockEntityPressureValve valve, float gate) {
    Scene.Machine(valve.Pos, valve.Block, valve);
    ReflectionHelpers.SetProperty(
      valve,
      nameof(valve.NetworkSystem),
      Scene.World.Networks
    );
    while (valve.GatePressure < gate - 0.001f && valve.AdjustGatePressure(true)) { }
    while (valve.GatePressure > gate + 0.001f && valve.AdjustGatePressure(false)) { }
  }

  /// <summary>
  /// Lays <paramref name="main"/> in iron pipe, except the cells in <paramref name="passthroughs"/>,
  /// which are fireclay passthroughs (a bend where their two faces are not opposite).
  /// </summary>
  private void Place(Main main, params BlockPos[] passthroughs) {
    foreach (var (pos, faces) in main.Cells) {
      string orientation = new(
        "udnsew".Where(c => faces.Contains(c)).ToArray()
      );
      if (passthroughs.Contains(pos))
        Passthrough(pos, orientation);
      else {
        EnginePlant.Pipe(Scene, pos, orientation, NextId());
        _placed.Add((pos, Scene.World.GetBlock(pos).Code.ToString()));
      }
      _pipes.Add(pos);
    }
  }

  private void Passthrough(BlockPos pos, string orientation) {
    string type = orientation is "ns" or "we" or "ud" ? "passthrough" : "passthroughbend";
    string code = $"ppex:pipe-{type}-fire-{orientation}";
    var block = TestBlocks.Configure(
      new BlockPipePassthrough(),
      code,
      NextId(),
      ("type", type),
      ("brick", "fire"),
      ("orientation", orientation)
    );
    ReflectionHelpers.SetProperty(block, "Type", type);
    ReflectionHelpers.SetProperty(block, "Orientation", orientation);
    Scene.Node(
      pos,
      block,
      new BlockEntityPipePassthrough { Pos = pos.Copy(), Block = block },
      "pipe"
    );
    _placed.Add((pos, code));
  }
  /// <summary>A pipe run under construction: each cell and the faces it connects on.</summary>
  private sealed class Main {
    public readonly Dictionary<BlockPos, HashSet<char>> Cells = [];

    /// <summary>Lays pipe along the axis-aligned legs between consecutive corners.</summary>
    public Main Lay(params BlockPos[] corners) {
      for (int i = 1; i < corners.Length; i++) {
        BlockPos at = corners[i - 1].Copy();
        BlockPos to = corners[i];
        while (!at.Equals(to)) {
          BlockFacing step = Toward(at, to);
          Open(at, step);
          at = at.AddCopy(step);
          Open(at, step.Opposite);
        }
      }
      return this;
    }

    /// <summary>Adds a connector on <paramref name="face"/> of the cell at <paramref name="pos"/>.</summary>
    public Main Open(BlockPos pos, BlockFacing face) {
      if (!Cells.TryGetValue(pos, out HashSet<char>? faces))
        Cells[pos.Copy()] = faces = [];
      faces.Add(face.Code[0]);
      return this;
    }

    private static BlockFacing Toward(BlockPos at, BlockPos to) =>
      to.X != at.X ? (to.X > at.X ? BlockFacing.EAST : BlockFacing.WEST)
      : to.Y != at.Y ? (to.Y > at.Y ? BlockFacing.UP : BlockFacing.DOWN)
      : to.Z > at.Z ? BlockFacing.SOUTH
      : BlockFacing.NORTH;
  }
}
