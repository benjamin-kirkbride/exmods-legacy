using ExpandedLib.Industry.Molten;
using ExpandedLib.Networks;
using ExpandedLib.Testing;
using SteelmakingExpanded.BlockNetworkMolten.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Xunit;

namespace SteelmakingExpanded.Tests;

/// <summary>
/// smex canals on Industry's "molten" network keep to their own runs: a smex canal butted against
/// another mod's molten node forms two networks whichever is placed first. Red with
/// <c>BlockMoltenCanal.AcceptsNeighbour</c> removed.
/// </summary>
public class CrossLineCanalTests {
  private static readonly BlockPos First = new(0, 0, 0);
  private static readonly BlockPos Second = new(0, 0, 1);

  #region Runs

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void A_smex_canal_and_another_mods_molten_node_form_two_networks_in_either_order(
    bool smexFirst
  ) {
    var w = new TestWorld();
    w.RegisterNetwork("molten", sys => new MoltenNetwork(sys));
    BlockMoltenCanal smex = Canal();
    OtherMoltenNode other = OtherNode();

    Place(w, smexFirst ? smex : other, smexFirst ? other : smex);

    Assert.NotSame(w.NetworkAt(First), w.NetworkAt(Second));
    Assert.Single(w.NetworkAt(First)!.Nodes);
    Assert.Single(w.NetworkAt(Second)!.Nodes);
    Assert.Empty(w.Networks.GetConnectedNeighbors(w.Accessor, First, "molten"));
    Assert.Empty(w.Networks.GetConnectedNeighbors(w.Accessor, Second, "molten"));
  }

  [Fact]
  public void Two_smex_canals_placed_the_same_way_form_one_network() {
    var w = new TestWorld();
    w.RegisterNetwork("molten", sys => new MoltenNetwork(sys));
    BlockMoltenCanal smex = Canal();

    Place(w, smex, smex);

    Assert.Same(w.NetworkAt(First), w.NetworkAt(Second));
    Assert.Equal(2, w.NetworkAt(First)!.Nodes.Count);
  }

  #endregion

  #region Helpers

  private static void Place(TestWorld w, Block first, Block second) {
    w.Place(First, first);
    w.AddNode(First, "molten");
    w.Place(Second, second);
    w.AddNode(Second, "molten");
  }

  private static BlockMoltenCanal Canal() {
    var block = TestBlocks.Configure(
      new BlockMoltenCanal(),
      "smex:moltencanal-straight-ns",
      1,
      ("type", "straight"),
      ("orientation", "ns")
    );
    ReflectionHelpers.SetProperty(block, "Type", "straight");
    ReflectionHelpers.SetProperty(block, "Orientation", "ns");
    return block;
  }

  // A north-south node of the same network type from another domain, accepting every neighbour.
  private static OtherMoltenNode OtherNode() {
    var block = TestBlocks.Configure(
      new OtherMoltenNode(),
      "othermod:canal-straight-ns",
      2,
      ("type", "straight"),
      ("orientation", "ns")
    );
    ReflectionHelpers.SetProperty(block, "Type", "straight");
    ReflectionHelpers.SetProperty(block, "Orientation", "ns");
    return block;
  }

  private sealed class OtherMoltenNode : BlockNetworkNode {
    public override string NetworkType => "molten";
  }

  #endregion
}
