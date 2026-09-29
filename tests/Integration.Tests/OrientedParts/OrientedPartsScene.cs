using System.Collections.Generic;
using ExpandedLib.Machines;
using ExpandedLib.Networks;
using ExpandedLib.Structures;
using ExpandedLib.Testing;
using Integration.Tests.Guards;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Integration.Tests.OrientedParts;

/// <summary>
/// A shipped structure formed in a fresh world around its real core block, with the parts under
/// test standing as the loaded blocks of ppex and smex and every other cell raised by
/// <see cref="StructureRig"/>. Turns a part in place and lets the machine's monitor decide.
/// </summary>
internal sealed class OrientedPartsScene {
  private readonly StructureRig _rig;

  private OrientedPartsScene(StructureRig rig, BlockEntityMultiblockStructure machine) {
    _rig = rig;
    Machine = machine;
  }

  /// <summary>The structure's block entity.</summary>
  public BlockEntityMultiblockStructure Machine { get; }

  /// <summary>The world the structure stands in.</summary>
  public TestWorld World => _rig.World;

  /// <summary>The rig's report of the unsatisfied cells, for assertion messages.</summary>
  public string MissingReport => _rig.MissingReport;

  /// <summary>Whether the structure is complete and its production process may run.</summary>
  public bool Producing =>
    Machine.StructureComplete && ProductionReadiness.IsReady(Machine);

  /// <summary>
  /// Forms the structure whose core is <paramref name="coreCode"/> at <paramref name="anchor"/>,
  /// turned to <paramref name="layoutAngle"/> degrees (the angle the machine hands
  /// <c>InitForUse</c>), with each of <paramref name="parts"/> standing at its layout offset as the
  /// loaded block of that code. Throws when the structure does not complete.
  /// </summary>
  public static OrientedPartsScene Form(
    BlockPos anchor,
    string coreCode,
    BlockEntityMultiblockStructure machine,
    int layoutAngle,
    params (int X, int Y, int Z, string Code)[] parts
  ) {
    var world = new TestWorld();
    Block core = Loaded(coreCode);
    world.Place(anchor, core, machine);
    machine.CreateBehaviors(core, LoadedLine.World.World);
    world.Attach(machine);

    StructureRig rig = StructureRig.Around(world, machine, layoutAngle);
    foreach (var (x, y, z, code) in parts)
      rig.Occupy(rig.Cell(x, y, z), Loaded(code));
    rig.Complete();
    return new OrientedPartsScene(rig, machine);
  }

  /// <summary>Exchanges the block at layout offset (<paramref name="x"/>, <paramref name="y"/>,
  /// <paramref name="z"/>) for the loaded block <paramref name="code"/>, keeping the cell's block
  /// entity, as a wrench turn or a node re-picking its orientation does.</summary>
  public OrientedPartsScene Turn(int x, int y, int z, string code) {
    Block turned = Loaded(code);
    _rig.World.Register(turned);
    _rig.World.Accessor.ExchangeBlock(turned.BlockId, _rig.Cell(x, y, z));
    return this;
  }

  /// <summary>Runs one completion monitor interval of the five structures (3 s).</summary>
  public OrientedPartsScene MonitorTick() {
    _rig.World.AdvanceBlockEntityTime(3000);
    return this;
  }

  // Vanilla blocks are not in the loaded line; each gets one stand-in, clear of the rig's ids.
  private static readonly Dictionary<string, Block> Vanilla = new();

  /// <summary>The loaded block of <paramref name="code"/>, a network node primed with the
  /// orientation its load would read off its variants. A <c>game</c> code gets a plain stand-in.</summary>
  public static Block Loaded(string code) {
    if (code.StartsWith("game:")) {
      lock (Vanilla) {
        if (!Vanilla.TryGetValue(code, out Block? standIn))
          Vanilla[code] = standIn = TestBlocks.Configure(
            new Block(),
            code,
            29000 + Vanilla.Count
          );
        return standIn;
      }
    }
    Block block = LoadedLine.World.World.GetBlock(new AssetLocation(code))!;
    if (block is BlockNetworkNode && block.Variant["orientation"] is string token)
      ReflectionHelpers.SetProperty(block, "Orientation", token);
    return block;
  }

  /// <summary>Every loaded block matching <paramref name="wildcard"/>.</summary>
  public static IEnumerable<Block> Matching(AssetLocation wildcard) {
    foreach (Block block in LoadedLine.World.World.Blocks)
      if (
        block?.Code != null
        && Vintagestory.API.Util.WildcardUtil.Match(wildcard, block.Code)
      )
        yield return block;
  }
}
