using ExpandedLib.Industry.Pipes;
using ExpandedLib.Testing;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Xunit;
using BlockPipe = PipesAndPowerExpanded.BlockNetworkPipe.Blocks.BlockPipe;
using BlockPipeOutlet = PipesAndPowerExpanded.BlockNetworkPipe.Blocks.BlockPipeOutlet;
using BlockPipePassthrough = PipesAndPowerExpanded.BlockNetworkPipe.Blocks.BlockPipePassthrough;
using IndustryPipe = ExpandedLib.Industry.Pipes.BlockPipe;
using IndustryPassthrough = ExpandedLib.Industry.Pipes.BlockPipePassthrough;

namespace PipesAndPowerExpanded.Tests;

/// <summary>
/// ppex pipes on Industry's "pipe" network keep to their own runs: a ppex pipe butted against a
/// flanged Industry pipe forms two runs whichever is placed first (red with ppex's
/// <c>JointFamily</c> override removed), and ppex's passthrough and outlet are not Industry's
/// passthrough type, which iiex's chimney patch matches (red with ppex's passthrough deriving from
/// Industry's).
/// </summary>
public class CrossLineRunTests {
  private static readonly BlockPos First = new(0, 0, 0);
  private static readonly BlockPos Second = new(0, 0, 1);

  #region Types

  [Fact]
  public void The_ppex_passthrough_and_outlet_are_not_Industrys_passthrough() {
    Assert.False(
      typeof(IndustryPassthrough).IsAssignableFrom(typeof(BlockPipePassthrough))
    );
    Assert.False(
      typeof(IndustryPassthrough).IsAssignableFrom(typeof(BlockPipeOutlet))
    );
  }

  #endregion

  #region Runs

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void A_ppex_pipe_and_a_flanged_Industry_pipe_form_two_runs_in_either_order(
    bool ppexFirst
  ) {
    var w = new TestWorld();
    w.RegisterNetwork("pipe", sys => new PipeNetwork(sys));
    BlockPipe ppex = PipeTestWorld.MakePipe();
    IndustryPipe industry = FlangedIndustryPipe();
    Assert.Equal(IndustryPipe.FlangedJoint, industry.JointFamily);

    Place(w, ppexFirst ? ppex : industry, ppexFirst ? industry : ppex);

    Assert.NotSame(w.NetworkAt(First), w.NetworkAt(Second));
    Assert.Single(w.NetworkAt(First)!.Nodes);
    Assert.Single(w.NetworkAt(Second)!.Nodes);
    Assert.Empty(w.Networks.GetConnectedNeighbors(w.Accessor, First, "pipe"));
    Assert.Empty(w.Networks.GetConnectedNeighbors(w.Accessor, Second, "pipe"));
  }

  [Fact]
  public void Two_ppex_pipes_placed_the_same_way_form_one_run() {
    var w = new TestWorld();
    w.RegisterNetwork("pipe", sys => new PipeNetwork(sys));
    BlockPipe ppex = PipeTestWorld.MakePipe();

    Place(w, ppex, ppex);

    Assert.Same(w.NetworkAt(First), w.NetworkAt(Second));
    Assert.Equal(2, w.NetworkAt(First)!.Nodes.Count);
  }

  #endregion

  #region Helpers

  private static void Place(TestWorld w, Block first, Block second) {
    w.Place(First, first);
    w.AddNode(First, "pipe");
    w.Place(Second, second);
    w.AddNode(Second, "pipe");
  }

  // A tierless north-south Industry segment, which presents the flanged joint.
  private static IndustryPipe FlangedIndustryPipe() {
    var pipe = TestBlocks.Configure(
      new IndustryPipe(),
      "game:pipe-straight-ns",
      2,
      ("type", "straight"),
      ("orientation", "ns")
    );
    ReflectionHelpers.SetProperty(pipe, "Type", "straight");
    ReflectionHelpers.SetProperty(pipe, "Orientation", "ns");
    return pipe;
  }

  #endregion
}
