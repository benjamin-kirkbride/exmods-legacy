using System;
using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Testing;
using PipesAndPowerExpanded.BlockNetworkPipe;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using PipesAndPowerExpanded.Tests;
using SteelmakingExpanded.BlockNetworkMolten;
using SteelmakingExpanded.BlockNetworkMolten.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Xunit;

namespace Integration.Tests.Pins;

/// <summary>
/// The grid a seeded sequence works on, and the reload it can draw: every block entity saved to
/// bytes, loaded into a fresh world by its type, placed, then initialised in position order.
/// </summary>
internal static class SequenceGrid {
  /// <summary>Cells per side of the square grid at y 0.</summary>
  public const int Size = 4;

  /// <summary>Operations one sequence runs, each followed by one server second.</summary>
  public const int Steps = 60;

  public static IEnumerable<BlockPos> Cells() =>
    from x in Enumerable.Range(0, Size)
    from z in Enumerable.Range(0, Size)
    select new BlockPos(x, 0, z);

  public static (int, int, int) Key(BlockPos p) => (p.X, p.Y, p.Z);

  /// <summary>
  /// Moves every block of the grid from <paramref name="old"/> into <paramref name="fresh"/> the way
  /// a chunk reload does, and returns <paramref name="fresh"/>.
  /// </summary>
  public static TestWorld Reload(TestWorld old, TestWorld fresh) {
    var loaded = new List<BlockEntity>();
    foreach (BlockPos pos in Cells()) {
      Block block = old.GetBlock(pos);
      if (block.BlockId == 0)
        continue;
      if (old.GetBlockEntity(pos) is not { } be) {
        fresh.Place(pos, block);
        continue;
      }
      var tree = new TreeAttribute();
      be.ToTreeAttributes(tree);
      var copy = (BlockEntity)Activator.CreateInstance(be.GetType())!;
      copy.FromTreeAttributes(
        TreeAttribute.CreateFromBytes(tree.ToBytes()),
        fresh.World
      );
      fresh.Place(pos, block, copy);
      loaded.Add(copy);
    }
    foreach (BlockEntity be in loaded)
      fresh.Initialize(be);
    return fresh;
  }

  public static void Remove(TestWorld world, BlockPos pos) {
    world.GetBlockEntity(pos)?.OnBlockRemoved();
    world.Accessor.SetBlock(0, pos);
  }
}

/// <summary>
/// A seeded run of pipe operations over <see cref="SequenceGrid"/>: place a straight, a bend or a
/// cap, fill a run with a gas or water, draw from it, break a cell, reload the grid, or idle. Gas
/// is produced at 4 atm or less, under the iron pipes' 5 atm rating, so no pipe bursts and the
/// trace does not depend on which of several pipes the network would pick.
/// </summary>
internal sealed class PipeSequence {
  private static readonly string[] Kinds =
  [
    "straight-ns",
    "straight-we",
    "bend-nw",
    "bend-se",
    "bend-en",
    "bend-ws",
    "cap",
  ];

  private static readonly string[] Media = ["Steam", "Air", "Exhaust", "Water"];

  private readonly Random _rand;
  private readonly Dictionary<string, Block> _blocks = [];
  private readonly List<BlockPos> _pipes = [];
  private TestWorld _world = NewWorld();

  public PipeSequence(int seed) {
    _rand = new Random(seed);
    for (int i = 0; i < Kinds.Length; i++)
      _blocks[Kinds[i]] =
        Kinds[i] == "cap" ? PpexScenes.Cap(99) : Pipe(Kinds[i], i + 1);
  }

  /// <summary>Runs the sequence and returns its trace, named with the seed.</summary>
  public Trace Run(string name) {
    var trace = new Trace(name);
    for (int step = 1; step <= SequenceGrid.Steps; step++) {
      trace.Line(step, "op", Apply());
      _world.Tick(1);
      PipeTrace.Runs(trace, step, _world, _pipes.OrderBy(SequenceGrid.Key));
      Assert.All(
        _pipes,
        p => Assert.IsAssignableFrom<BlockPipe>(_world.GetBlock(p))
      );
    }
    return trace;
  }

  private string Apply() {
    int roll = _rand.Next(100);
    return roll switch {
      < 35 => Place(),
      < 55 => Fill(),
      < 70 => Draw(),
      < 80 => Break(),
      < 85 => Reload(),
      _ => "idle",
    };
  }

  private string Place() {
    var empty = SequenceGrid
      .Cells()
      .Where(p => _world.GetBlock(p).BlockId == 0)
      .ToList();
    if (empty.Count == 0)
      return "idle";
    BlockPos pos = empty[_rand.Next(empty.Count)];
    string kind = Kinds[_rand.Next(Kinds.Length)];
    if (kind == "cap")
      _world.Place(pos, _blocks[kind]);
    else {
      var be = new BlockEntityPipe();
      _world.Place(pos, _blocks[kind], be);
      _world.Initialize(be);
      _pipes.Add(pos);
    }
    return $"place {kind} {At(pos)}";
  }

  private string Fill() {
    if (Pick() is not { } pos)
      return "idle";
    string medium = Media[_rand.Next(Media.Length)];
    int litres = 10 + _rand.Next(190);
    int temp = 20 + _rand.Next(200);
    int atm = 1 + _rand.Next(4);
    if (medium == "Water")
      Net(pos).TryProduceLiquid(litres, temp, atm, _world.Accessor);
    else
      Node(pos).TryProduce(litres, temp, medium, maxOutputPressure: atm);
    return $"fill {medium} {litres}L {temp}C {atm}atm {At(pos)}";
  }

  private string Draw() {
    if (Pick() is not { } pos)
      return "idle";
    int litres = 10 + _rand.Next(190);
    if (Net(pos).State is { IsLiquid: true })
      Net(pos).TryConsumeLiquid(litres, _world.Accessor);
    else
      Node(pos).TryConsume(litres);
    return $"draw {litres}L {At(pos)}";
  }

  private string Break() {
    var cells = SequenceGrid
      .Cells()
      .Where(p => _world.GetBlock(p).BlockId != 0)
      .ToList();
    if (cells.Count == 0)
      return "idle";
    BlockPos pos = cells[_rand.Next(cells.Count)];
    SequenceGrid.Remove(_world, pos);
    _pipes.Remove(pos);
    return $"break {At(pos)}";
  }

  private string Reload() {
    _world = SequenceGrid.Reload(_world, NewWorld());
    return "reload";
  }

  private BlockPos? Pick() =>
    _pipes.Count == 0
      ? null
      : _pipes.OrderBy(SequenceGrid.Key).ElementAt(_rand.Next(_pipes.Count));

  private PipeNetwork Net(BlockPos pos) => (PipeNetwork)_world.NetworkAt(pos)!;

  private IPipeNode Node(BlockPos pos) =>
    (IPipeNode)_world.GetBlockEntity(pos)!;

  private static TestWorld NewWorld() =>
    new TestWorld().RegisterNetwork("pipe", s => new PipeNetwork(s));

  private static BlockPipe Pipe(string kind, int id) {
    string[] parts = kind.Split('-');
    var pipe = PipeTestWorld.MakePipe(orientation: parts[1], id: id);
    ReflectionHelpers.SetProperty(pipe, "Type", parts[0]);
    return pipe;
  }

  private static string At(BlockPos p) => $"{p.X},{p.Y},{p.Z}";
}

/// <summary>
/// A seeded run of canal operations over <see cref="SequenceGrid"/>: place a straight, a bend or a
/// start, pour iron or copper into a cell, drain one, break one, reload the grid, or idle. The
/// calendar advances <see cref="MoltenClock.HoursPerTick"/> each second. After every step the metal
/// standing in the grid equals what was poured less what was drained and what left with a broken
/// cell.
/// </summary>
internal sealed class CanalSequence {
  private static readonly string[] Kinds =
  [
    "straight-ns",
    "straight-we",
    "bend-nw",
    "bend-se",
    "bend-en",
    "bend-ws",
    "start-n",
    "start-s",
    "start-e",
    "start-w",
  ];

  private readonly Random _rand;
  private readonly List<BlockPos> _cells = [];
  private TestWorld _world;
  private int _tick;
  private int _ledger;

  public CanalSequence(int seed) {
    _rand = new Random(seed);
    _world = NewWorld(0);
  }

  /// <summary>Runs the sequence and returns its trace, named with the seed.</summary>
  public Trace Run(string name) {
    var trace = new Trace(name);
    for (int step = 1; step <= SequenceGrid.Steps; step++) {
      trace.Line(step, "op", Apply());
      _tick++;
      MoltenClock.SetTick(_world, _tick);
      _world.Tick(1);
      MoltenTrace.Cells(trace, step, Cells());
      Assert.Equal(_ledger, Cells().Sum(c => c.CellAmount));
    }
    return trace;
  }

  private IEnumerable<BlockEntityMoltenCanal> Cells() =>
    _cells.Select(p => (BlockEntityMoltenCanal)_world.GetBlockEntity(p)!);

  private string Apply() {
    int roll = _rand.Next(100);
    return roll switch {
      < 35 => Place(),
      < 60 => Pour(),
      < 72 => Drain(),
      < 82 => Break(),
      < 87 => Reload(),
      _ => "idle",
    };
  }

  private string Place() {
    var empty = SequenceGrid
      .Cells()
      .Where(p => _world.GetBlock(p).BlockId == 0)
      .ToList();
    if (empty.Count == 0)
      return "idle";
    BlockPos pos = empty[_rand.Next(empty.Count)];
    int kind = _rand.Next(Kinds.Length);
    var (block, be) = MoltenClock.Cell(Kinds[kind], kind + 1);
    _world.Place(pos, block, be);
    _world.Initialize(be);
    _cells.Add(pos);
    return $"place {Kinds[kind]} {At(pos)}";
  }

  private string Pour() {
    if (Pick() is not { } pos)
      return "idle";
    bool iron = _rand.Next(2) == 0;
    int units = 10 + _rand.Next(140);
    int temp = iron ? 1550 + _rand.Next(200) : 1150 + _rand.Next(150);
    string metal = iron ? MoltenClock.Iron : MoltenClock.Copper;
    int took = MoltenClock.Pour(_world, Cell(pos), metal, units, temp);
    _ledger += took;
    return $"pour {metal} {units} {temp}C took={took} {At(pos)}";
  }

  private string Drain() {
    if (Pick() is not { } pos)
      return "idle";
    int units = 10 + _rand.Next(90);
    int drained = Cell(pos).DrainMetal(units);
    _ledger -= drained;
    return $"drain {units} got={drained} {At(pos)}";
  }

  private string Break() {
    if (Pick() is not { } pos)
      return "idle";
    _ledger -= Cell(pos).CellAmount;
    SequenceGrid.Remove(_world, pos);
    _cells.Remove(pos);
    return $"break {At(pos)}";
  }

  private string Reload() {
    _world = SequenceGrid.Reload(_world, NewWorld(_tick));
    return "reload";
  }

  private BlockPos? Pick() =>
    _cells.Count == 0
      ? null
      : _cells.OrderBy(SequenceGrid.Key).ElementAt(_rand.Next(_cells.Count));

  private BlockEntityMoltenCanal Cell(BlockPos pos) =>
    (BlockEntityMoltenCanal)_world.GetBlockEntity(pos)!;

  private static TestWorld NewWorld(int tick) {
    var world = new TestWorld().RegisterNetwork(
      "molten",
      s => new MoltenNetwork(s)
    );
    MoltenClock.RegisterMetals(world);
    MoltenClock.SetTick(world, tick);
    return world;
  }

  private static string At(BlockPos p) => $"{p.X},{p.Y},{p.Z}";
}

/// <summary>
/// Seeded pipe and canal sequences on the published ppex and smex. Each seed runs twice and must
/// give the same trace both times; the trace is saved with the seed in its name.
/// </summary>
public class SequenceTraceTests {
  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  public void A_seeded_pipe_sequence_records_the_same_trace_twice(int seed) {
    string name = $"sequence-pipe-seed{seed}";
    Trace first = new PipeSequence(seed).Run(name);
    Trace second = new PipeSequence(seed).Run(name);

    Assert.Equal(first.Text, second.Text);
    first.Save();
  }

  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  public void A_seeded_canal_sequence_records_the_same_trace_twice(int seed) {
    string name = $"sequence-canal-seed{seed}";
    Trace first = new CanalSequence(seed).Run(name);
    Trace second = new CanalSequence(seed).Run(name);

    Assert.Equal(first.Text, second.Text);
    first.Save();
  }
}
