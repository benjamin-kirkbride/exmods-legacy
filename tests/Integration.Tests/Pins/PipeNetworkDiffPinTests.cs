using ExpandedLib.Testing;
using PipesAndPowerExpanded.BlockNetworkPipe;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using PipesAndPowerExpanded.Tests;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Xunit;

namespace Integration.Tests.Pins;

/// <summary>
/// Pipe network behaviour on the published ppex beside <see cref="PipePinTests"/>: water crosses a
/// run with no throughput cap, and a chimney over an outlet's top connector draws gas as a vent, not a
/// leak. Each scene also records its trace.
/// </summary>
public class PipeNetworkDiffPinTests {
  private const int Length = 10;
  private const int OutletId = 1;
  private const int ChimneyId = 98;

  #region Throughput

  [Fact]
  public void Two_hundred_fifty_litres_of_water_a_second_cross_a_ten_pipe_run() {
    var line = PipeLine.Of(Length);
    var trace = new Trace("pipe-throughput-water-250");
    IBlockAccessor accessor = line.Scene.World.Accessor;
    float[] drawn = new float[20];

    for (int t = 0; t < drawn.Length; t++) {
      line.Net.TryProduceLiquid(250f, 20f, 1f, accessor);
      drawn[t] = line.Net.TryConsumeLiquid(250f, accessor);
      line.Step();
      line.Record(trace, t + 1);
      trace.Line(
        t + 1,
        "drawn=" + Trace.Litres(drawn[t]),
        "flow=" + Trace.Litres(line.Net.State!.FlowRate)
      );
    }
    trace.Save();

    Assert.All(drawn, d => Assert.Equal(250f, d, Trace.VolumeDigits));
    Assert.Equal(249.80f, line.Net.State!.FlowRate, Trace.VolumeDigits);
  }

  #endregion

  #region Chimney

  [Fact]
  public void A_chimney_over_an_outlet_draws_sixteen_litres_a_second() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    var pos = new BlockPos(0, 0, 0);
    var outlet = new BlockEntityPipeOutlet();
    scene.World.Place(pos, UpOutlet(), outlet);
    scene.Block(
      pos.UpCopy(),
      TestBlocks.Configure(new Block(), "game:chimney", ChimneyId)
    );
    scene.World.Initialize(outlet);
    scene.Build();

    PipeNetwork net = scene.NetworkAt<PipeNetwork>(pos)!;
    net.TryProduceGas(
      60f,
      300f,
      "Exhaust",
      scene.World.Accessor,
      maxOutputPressure: 2f
    );
    var trace = new Trace("pipe-chimney-outlet");

    PipeTrace.Runs(trace, 0, scene.World, [pos]);
    for (int t = 1; t <= 3; t++) {
      scene.Step();
      PipeTrace.Runs(trace, t, scene.World, [pos]);
      trace.Line(t, "openings=" + net.State!.OpeningsCount);
      if (t == 1)
        Assert.Equal(44f, net.State.Volume, Trace.VolumeDigits);
    }
    trace.Save();

    Assert.Equal(12f, net.State!.Volume, Trace.VolumeDigits);
    Assert.Equal(0, net.State.OpeningsCount);
  }

  /// <summary>A black-brick outlet whose one connector faces up.</summary>
  private static BlockPipeOutlet UpOutlet() {
    var block = TestBlocks.Configure(
      new BlockPipeOutlet(),
      "ppex:pipe-outlet-black-u",
      OutletId,
      ("type", "outlet"),
      ("brick", "black"),
      ("orientation", "u")
    );
    ReflectionHelpers.SetProperty(block, "Type", "outlet");
    ReflectionHelpers.SetProperty(block, "Orientation", "u");
    return block;
  }

  #endregion
}
