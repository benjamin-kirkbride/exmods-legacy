using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Networks;
using ExpandedLib.Testing;
using PipesAndPowerExpanded.BlockNetworkPipe;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using PipesAndPowerExpanded.Tests;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Integration.Tests.Pins;

/// <summary>
/// A straight run of real pipes along +Z from the origin, one per entry of its materials, each with
/// its block entity. Every block is placed first and every entity then initialised, which is how the
/// entities join the graph in game. Each end is open to air unless capped; a run built with a
/// chimney ends in a passthrough bend turned up into a chimney block.
/// </summary>
internal sealed class PipeLine {
  private const int CapId = 99;
  private const int ChimneyId = 98;
  private const int PassthroughId = 97;

  public readonly Scene Scene = new Scene().Network(
    "pipe",
    s => new PipeNetwork(s)
  );

  /// <summary>The run's cells, from the origin outward.</summary>
  public readonly List<BlockPos> Cells = [];

  private readonly List<BlockEntity> _entities = [];

  public PipeLine(
    IReadOnlyList<string> materials,
    bool capStart = true,
    bool capEnd = true,
    bool chimney = false
  ) {
    var blocks = new Dictionary<string, BlockPipe>();
    for (int z = 0; z < materials.Count; z++) {
      string material = materials[z];
      if (!blocks.TryGetValue(material, out BlockPipe? block))
        blocks[material] = block = PipeTestWorld.MakePipe(
          material: material,
          id: blocks.Count + 1
        );
      Add(new BlockPos(0, 0, z), block, new BlockEntityPipe());
    }

    if (chimney) {
      var passthrough = TestBlocks.Configure(
        new BlockPipePassthrough(),
        "ppex:pipe-passthroughbend-un",
        PassthroughId,
        ("type", "passthroughbend"),
        ("orientation", "un")
      );
      ReflectionHelpers.SetProperty(passthrough, "Type", "passthroughbend");
      ReflectionHelpers.SetProperty(passthrough, "Orientation", "un");
      var top = new BlockPos(0, 0, materials.Count);
      Add(top, passthrough, new BlockEntityPipePassthrough());
      Scene.Block(
        top.UpCopy(),
        TestBlocks.Configure(new Block(), "game:chimney", ChimneyId)
      );
    }

    if (capStart)
      Scene.Block(new BlockPos(0, 0, -1), PpexScenes.Cap(CapId));
    if (capEnd && !chimney)
      Scene.Block(new BlockPos(0, 0, Cells.Count), PpexScenes.Cap(CapId));

    foreach (BlockEntity be in _entities)
      Scene.World.Initialize(be);
    Scene.Build();
  }

  /// <summary>A run of <paramref name="length"/> pipes of one material.</summary>
  public static PipeLine Of(
    int length,
    string material = "iron",
    bool capStart = true,
    bool capEnd = true,
    bool chimney = false
  ) =>
    new(
      Enumerable.Repeat(material, length).ToArray(),
      capStart,
      capEnd,
      chimney
    );

  private void Add(BlockPos pos, Block block, BlockEntity be) {
    Scene.World.Place(pos, block, be);
    Cells.Add(pos);
    _entities.Add(be);
  }

  /// <summary>The network the first cell belongs to.</summary>
  public PipeNetwork Net => Scene.NetworkAt<PipeNetwork>(Cells[0])!;

  /// <summary>The pipe entity at cell <paramref name="index"/>, as a network node.</summary>
  public IPipeNode Node(int index) =>
    (IPipeNode)Scene.World.GetBlockEntity(Cells[index])!;

  /// <summary>Whether cell <paramref name="index"/> still holds its pipe.</summary>
  public bool Stands(int index) =>
    Scene.World.GetBlock(Cells[index]) is BlockPipe;

  /// <summary>One server second: entity ticks, then every network.</summary>
  public void Step() => Scene.Step(1);

  /// <summary>Records every run the line's cells belong to at <paramref name="tick"/>.</summary>
  public void Record(Trace trace, int tick) =>
    PipeTrace.Runs(trace, tick, Scene.World, Cells);
}

/// <summary>The pipe observables a trace records: per run, its size, volume, pressure, medium and temperature.</summary>
internal static class PipeTrace {
  /// <summary>
  /// Records one line per distinct network among <paramref name="cells"/>, ordered by the lowest
  /// position each holds, so the order does not depend on how the graph enumerates its networks.
  /// </summary>
  public static void Runs(
    Trace trace,
    int tick,
    TestWorld world,
    IEnumerable<BlockPos> cells
  ) {
    var runs = cells
      .Select(world.NetworkAt)
      .OfType<PipeNetwork>()
      .Distinct()
      .Select(net => (net, first: net.Nodes.OrderBy(Key).First()))
      .OrderBy(r => Key(r.first));
    foreach (var (net, first) in runs)
      trace.Line(tick, Run(net, first));
  }

  private static string[] Run(PipeNetwork net, BlockPos first) {
    string at = $"run {first.X},{first.Y},{first.Z} n={net.Nodes.Count}";
    if (net.State is not { } s)
      return [at, "empty"];
    return
    [
      at,
      "vol=" + Trace.Litres(s.Volume),
      "p=" + Trace.Atm(s.Pressure),
      "medium=" + (s.MediumType.Length > 0 ? s.MediumType : "-"),
      "temp=" + Trace.Celsius(s.Temperature),
    ];
  }

  private static (int, int, int) Key(BlockPos p) => (p.X, p.Y, p.Z);
}
