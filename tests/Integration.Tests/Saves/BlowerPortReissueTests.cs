using System;
using System.IO;
using ExpandedLib;
using ExpandedLib.Industry.MechanicalPower;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Structures;
using ExpandedLib.Testing;
using Integration.Tests.Pins;
using Newtonsoft.Json.Linq;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.Tests;
using SteelmakingExpanded.BlockStructures.BlastFurnace.BlockEntities;
using SteelmakingExpanded.BlockStructures.BlastFurnace.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;
using Xunit;

namespace Integration.Tests.Saves;

/// <summary>
/// A twin-tub blower saved by smex 0.9.8, loaded with its port cell in the game's order on a world
/// running the mechanical-power system. The saved port cell hosts exlib's generic port on its east
/// face, coupling through to the west; the blower re-issues its own port spec on its first server
/// tick, finished or not.
/// </summary>
public class BlowerPortReissueTests {
  private const string Def =
    "smex/assets/smex/blocktypes/blastfurnace/mpblower.json";

  private const float MainAtm = 1.5f;

  [Fact]
  public void A_saved_blower_reissues_its_port_and_loads_the_shaft_with_the_main_pressure() {
    var (world, blower, portCell) = Load();
    BlockMpBlower block = (BlockMpBlower)blower.Block;
    BlockPos main = block
      .BlastOutletWorldPos(blower.Pos)
      .AddCopy(block.OutletFace);
    world.Place(
      main,
      PipeTestWorld.MakePipe(
        orientation: block.OutletFace.Axis == EnumAxis.Z ? "ns" : "we",
        id: 402
      ),
      new BlockEntityPipe()
    );
    world.Place(main.AddCopy(block.OutletFace), PpexScenes.Cap(403));
    world.Initialize(world.GetBlockEntity(main)!);
    world.AddNode(main, "pipe");
    var net = (PipeNetwork)world.NetworkAt(main)!;
    net.TryProduceGas(
      MainAtm * ExlibValues.LitresPerPipe,
      20f,
      "Air",
      world.Accessor,
      maxOutputPressure: MainAtm
    );
    Assert.Equal(MainAtm, net.State!.Pressure, 3);

    world.FireBlockEntityTicks();

    var filler = (BlockEntityStructureFiller)world.GetBlockEntity(portCell)!;
    Assert.Equal(
      "smex.BEBehaviorMpBlowerPort",
      Assert.Single(filler.HostedBehaviors!).Code
    );
    var port = Assert.IsType<BEBehaviorMpBlowerPort>(
      filler.GetBehavior<BEBehaviorMPBase>()
    );

    MechanicalNetwork network = port.Network!;
    network.updateNetwork(5);
    Assert.Equal(
      BlockEntityMpBlower.ShaftLoadAt(MainAtm),
      network.NetworkResistance,
      3
    );

    var fillerBlock = (BlockStructureFiller)world.GetBlock(portCell);
    Assert.True(Couples(fillerBlock, world, portCell, port.PortFacing));
    Assert.False(Couples(fillerBlock, world, portCell, BlockFacing.WEST));
  }

  [Fact]
  public void A_saved_part_built_blower_reissues_its_port_and_does_not_couple_the_west_face() {
    var (world, blower, portCell) = Load(completedStage: 3);
    Assert.False(blower.IsConstructed);

    world.FireBlockEntityTicks();

    var filler = (BlockEntityStructureFiller)world.GetBlockEntity(portCell)!;
    Assert.Equal(
      "smex.BEBehaviorMpBlowerPort",
      Assert.Single(filler.HostedBehaviors!).Code
    );
    var port = Assert.IsType<BEBehaviorMpBlowerPort>(
      filler.GetBehavior<BEBehaviorMPBase>()
    );
    var fillerBlock = (BlockStructureFiller)world.GetBlock(portCell);
    Assert.True(Couples(fillerBlock, world, portCell, port.PortFacing));
    Assert.False(Couples(fillerBlock, world, portCell, BlockFacing.WEST));
  }

  private static bool Couples(
    BlockStructureFiller filler,
    TestWorld world,
    BlockPos pos,
    BlockFacing face
  ) => filler.HasMechPowerConnectorAt(world.World, pos, face
#if GAME_GE_1_22
      , null!
#endif
    );

  /// <summary>
  /// A world wired to the save registry, with a pipe network type and a started
  /// <see cref="MechanicalPowerMod"/>, holding the blower golden <c>smex-mpblower-blowing</c> and
  /// the port-cell golden <c>exlib-structurefiller-mpport</c> moved onto the blower's port cell and
  /// linked to it. Both are created by their saved keys, given their behaviours and fed their trees
  /// before either is initialised, the port cell first. A <paramref name="completedStage"/> replaces
  /// the golden's last completed construction stage (4, the finished blower). Returns the world, the
  /// blower and its port cell.
  /// </summary>
  private static (
    TestWorld world,
    BlockEntityMpBlower blower,
    BlockPos portCell
  ) Load(int? completedStage = null) {
    TestWorld world = SaveRegistry.Instance.Wire(new TestWorld());
    world.RegisterNetwork("pipe", s => new PipeNetwork(s));
    var power = new MechanicalPowerMod();
    world.Mods.Register(power);
    power.Start(world.Api);

    var block = TestBlocks.Configure(
      new BlockMpBlower(),
      "smex:mpblower-east",
      400,
      ("side", "east")
    );
    SaveFixtures.ShippedBehaviors(block, Def);
    block.Attributes = new JsonObject(
      JObject.Parse(
        File.ReadAllText(Path.Combine(SaveGoldens.RepoRoot(), Def))
      )["attributes"]!
    );
    var fillerBlock = TestBlocks.Configure(
      new BlockStructureFiller(),
      "exlib:structurefiller",
      401
    );

    SaveGolden blowerGolden = SaveGoldens.Read("smex-mpblower-blowing");
    ITreeAttribute blowerTree = Tree(blowerGolden);
    if (completedStage is int stage)
      blowerTree.SetInt("currentStage", stage);
    var at = new BlockPos(
      blowerTree.GetInt("posx"),
      blowerTree.GetInt("posy"),
      blowerTree.GetInt("posz")
    );
    BlockPos portCell = block.MpPortWorldPos(at);

    SaveGolden portGolden = SaveGoldens.Read("exlib-structurefiller-mpport");
    ITreeAttribute portTree = Tree(portGolden);
    portTree.SetInt("posx", portCell.X);
    portTree.SetInt("posy", portCell.Y);
    portTree.SetInt("posz", portCell.Z);
    portTree.SetInt("cx", at.X);
    portTree.SetInt("cy", at.Y);
    portTree.SetInt("cz", at.Z);

    BlockEntity filler = Create(world, portGolden.SavedKey, fillerBlock, portTree);
    var blower = (BlockEntityMpBlower)
      Create(world, blowerGolden.SavedKey, block, blowerTree);
    world.Initialize(filler);
    world.Initialize(blower);
    return (world, blower, portCell);
  }

  private static ITreeAttribute Tree(SaveGolden golden) =>
    TreeAttribute.CreateFromBytes(Convert.FromBase64String(golden.TreeBase64));

  private static BlockEntity Create(
    TestWorld world,
    string savedKey,
    Block block,
    ITreeAttribute tree
  ) {
    BlockEntity be = SaveRegistry.Instance.CreateBlockEntity(savedKey);
    be.CreateBehaviors(block, world.World);
    be.FromTreeAttributes(tree, world.World);
    world.Place(be.Pos, block, be);
    return be;
  }
}
