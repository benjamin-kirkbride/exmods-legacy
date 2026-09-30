using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Blocks;
using ExpandedLib.Helpers;
using ExpandedLib.Industry.Molten;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Networks;
using ExpandedLib.Testing;
using Integration.Tests.Guards;
using NSubstitute;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using SteelmakingExpanded.BlockNetworkMolten.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Xunit;

namespace Integration.Tests.OrientedParts;

/// <summary>
/// The pipe and molten network blocks turn under the vanilla wrench: every loaded network block
/// with an <c>orientation</c> group declares <c>ExOrientable</c> in network mode with the scheme its
/// states make up, and a lone pipe and a lone canal step to their next orientation with their block
/// entity kept. Their drops and middle-click stay the fallback-orientation stack the network node
/// answers.
/// </summary>
public class NetworkWrenchTests {
  private static readonly BlockPos Centre = new(4, 10, 4);

  #region Declaration

  // Fails when the ExOrientable declaration is removed from any one of the 25 blocktypes.
  [Fact]
  public void Every_network_block_with_an_orientation_declares_the_scheme_its_states_make_up() {
    var groups = LoadedLine
      .Blocks.Where(b => b is BlockNetworkNode && b.Variant.ContainsKey("orientation"))
      .GroupBy(b => b.CodeWithVariant("orientation", "*").ToString())
      .ToList();
    Assert.NotEmpty(groups);

    foreach (var group in groups) {
      HashSet<string> states = [.. group.Select(b => b.Variant["orientation"])];
      ExOrientationScheme[] schemes =
      [
        .. ExOrientations.All.Where(s => s.Tokens.ToHashSet().SetEquals(states)),
      ];
      Assert.True(schemes.Length == 1, $"{group.Key}: {schemes.Length} schemes match its states");

      foreach (Block block in group) {
        var orientable = block.GetBehavior<BlockBehaviorExOrientable>();
        Assert.True(orientable != null, $"{block.Code} declares no ExOrientable");
        Assert.True(orientable!.IsNetworkOriented, $"{block.Code} is not in network mode");
        Assert.Equal(schemes[0].Name, orientable.Scheme.Name);
        Assert.Null(orientable.UnresolvedScheme);
      }
    }
  }

  #endregion

  #region Turning

  // Fails when the ExOrientable declaration is removed from pipes/straight.json.
  [Fact]
  public void A_wrench_turns_a_lone_pipe_to_its_next_orientation_and_keeps_its_entity() {
    var world = StandingWorld("pipe", "ppex:pipe-straight-*");
    var pipe = new BlockEntityPipe();
    world.Place(Centre, Primed(OrientedPartsScene.Loaded("ppex:pipe-straight-ns-iron")), pipe);
    world.Attach(pipe);

    Wrench(world, 1);

    Assert.Equal("we", world.GetBlock(Centre).Variant["orientation"]);
    Assert.Same(pipe, world.GetBlockEntity(Centre));
  }

  // Fails when the ExOrientable declaration is removed from molten/canalbrick/straight.json.
  [Fact]
  public void A_wrench_turns_a_lone_brick_canal_to_its_next_orientation_and_keeps_its_entity() {
    var world = StandingWorld("molten", "smex:moltencanal-straight-*");
    var canal = new BlockEntityMoltenCanal();
    world.Place(Centre, Primed(OrientedPartsScene.Loaded("smex:moltencanal-straight-fire-ns")), canal);
    world.Attach(canal);

    Wrench(world, 1);

    Assert.Equal("we", world.GetBlock(Centre).Variant["orientation"]);
    Assert.Same(canal, world.GetBlockEntity(Centre));
  }

  #endregion

  #region Drops

  // The orientation of the stack a network block's drop and middle-click answer, by blocktype,
  // whichever variant stands: the first state its class definitions list for the type.
  private static readonly Dictionary<string, string> DropOrientation = new() {
    ["ppex:pipe-straight"] = "ns",
    ["ppex:pipe-bend"] = "nw",
    ["ppex:pipe-tjunction"] = "uns",
    ["ppex:pipe-xjunction"] = "nswe",
    ["ppex:pipe-valve"] = "ns",
    ["ppex:pipe-pressurevalve"] = "ns",
    ["ppex:pipe-outlet"] = "s",
    ["ppex:pipe-passthrough"] = "ns",
    ["ppex:pipe-passthroughbend"] = "nw",
    ["ppex:pipe-fluidintake"] = "s",
    ["smex:moltencanal-straight"] = "ns",
    ["smex:moltencanal-bend"] = "nw",
    ["smex:moltencanal-tjunction"] = "nes",
    ["smex:moltencanal-xjunction"] = "nswe",
    ["smex:moltencanal-start"] = "s",
    ["smex:moltencanal-moldpedestal"] = "n",
    ["smex:moltencanal-tap"] = "n",
    ["smex:blastfurnace-tuyere"] = "s",
    ["smex:smokestack-intake"] = "n",
  };

  // Fails when a network block's drop or middle-click answers a different variant than the one
  // above, as when a class answers the variant that stands.
  [Fact]
  public void Network_blocks_drop_and_pick_the_stack_they_did_before_they_declared_a_scheme() {
    IWorldAccessor world = LoadedLine.World.World;
    var seen = new HashSet<string>();
    var wrong = new List<string>();

    foreach (
      Block block in LoadedLine.Blocks.Where(b =>
        b is BlockNetworkNode && b.Variant.ContainsKey("orientation")
      )
    ) {
      Primed(block);
      string key = BlocktypeKey(block);
      seen.Add(key);
      Assert.True(DropOrientation.TryGetValue(key, out string? expected), $"{key} has no expectation");

      ItemStack[] drops = block.GetDrops(world, Centre, null);
      Assert.Single(drops);
      ItemStack pick = block.OnPickBlock(world, Centre);
      string want = block.CodeWithVariant("orientation", expected!).ToString();
      if (drops[0].Block.Code.ToString() != want || pick.Block.Code.ToString() != want)
        wrong.Add($"{block.Code}: drops {drops[0].Block.Code}, picks {pick.Block.Code}, expected {want}");
    }

    Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    Assert.Equal(DropOrientation.Keys.Order(), seen.Order());
  }

  #endregion

  #region Helpers

  /// <summary><paramref name="block"/> with the network type its load reads off its variants.</summary>
  private static Block Primed(Block block) {
    ReflectionHelpers.SetProperty(block, "Type", block.Variant["type"]);
    return block;
  }

  private static string BlocktypeKey(Block block) =>
    block.Code.Domain
    + ":"
    + block.Code.Path.Split('-')[0]
    + "-"
    + block.Variant["type"];

  /// <summary>A fresh world running <paramref name="network"/>, holding every loaded block that
  /// matches <paramref name="wildcard"/>.</summary>
  private static TestWorld StandingWorld(string network, string wildcard) {
    var world = new TestWorld();
    if (network == "pipe")
      world.RegisterNetwork("pipe", sys => new PipeNetwork(sys));
    else
      world.RegisterNetwork("molten", sys => new MoltenNetwork(sys));
    foreach (Block block in OrientedPartsScene.Matching(new AssetLocation(wildcard)))
      world.Register(Primed(OrientedPartsScene.Loaded(block.Code.ToString())));
    return world;
  }

  /// <summary>Turns the wrench <paramref name="dir"/> steps through the block now at the centre.</summary>
  private static void Wrench(TestWorld world, int dir) {
    var holder = Substitute.For<EntityAgent>();
    holder.World = world.World;
    ((BlockNetworkNode)world.GetBlock(Centre)).Rotate(
      holder,
      new BlockSelection { Position = Centre.Copy(), Face = BlockFacing.UP },
      dir
    );
  }

  #endregion
}
