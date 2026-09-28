using System.Linq;
using Xunit;

namespace Integration.Tests.Pins;

/// <summary>
/// The pipe network's limits on the published ppex, as exact numbers: burst ratings, capacity,
/// throughput, leaks, chimney draw, the burst timer and the temperature of standing steam. Each
/// scene also records its trace.
/// </summary>
public class PipePinTests {
  private const int Length = 10;

  /// <summary>Ten steel pipes with one iron pipe at index 5.</summary>
  private static string[] Materials(string run) =>
    run switch {
      "mixed" => Enumerable
        .Range(0, Length)
        .Select(i => i == 5 ? "iron" : "steel")
        .ToArray(),
      _ => Enumerable.Repeat(run, Length).ToArray(),
    };

  #region Rating and capacity

  [Theory]
  [InlineData("iron", 5f, 1500f)]
  [InlineData("steel", 10f, 3000f)]
  [InlineData("mixed", 5f, 1500f)]
  public void A_sealed_run_charges_to_its_weakest_burst_rating(
    string run,
    float atm,
    float litres
  ) {
    var line = new PipeLine(Materials(run));
    var trace = new Trace($"pipe-charge-{run}");

    line.Net.TryProduceGas(
      10000f,
      150f,
      "Steam",
      line.Scene.World.Accessor,
      maxOutputPressure: 100f
    );
    line.Record(trace, 0);
    for (int t = 1; t <= 5; t++) {
      line.Step();
      line.Record(trace, t);
    }
    trace.Save();

    Assert.Equal(atm, line.Net.State!.Pressure, Trace.PressureDigits);
    Assert.Equal(litres, line.Net.State.Volume, Trace.VolumeDigits);
  }

  [Theory]
  [InlineData("iron")]
  [InlineData("steel")]
  [InlineData("mixed")]
  public void A_run_held_at_its_rating_loses_one_weakest_pipe_on_the_thirtieth_second(
    string run
  ) {
    string[] materials = Materials(run);
    var line = new PipeLine(materials);
    var trace = new Trace($"pipe-burst-{run}");
    line.Net.TryProduceGas(
      10000f,
      150f,
      "Steam",
      line.Scene.World.Accessor,
      maxOutputPressure: 100f
    );

    line.Record(trace, 0);
    for (int t = 1; t < 30; t++) {
      line.Step();
      line.Record(trace, t);
    }
    Assert.All(Enumerable.Range(0, Length), i => Assert.True(line.Stands(i)));

    line.Step();
    int[] gone = Enumerable
      .Range(0, Length)
      .Where(i => !line.Stands(i))
      .ToArray();
    // Which of several equal pipes fails is drawn at random, so the trace keeps only the count.
    trace.Line(30, $"burst={gone.Length}");
    trace.Save();

    int lost = Assert.Single(gone);
    Assert.Equal(run == "steel" ? "steel" : "iron", materials[lost]);
    if (run == "mixed")
      Assert.Equal(5, lost);
  }

  #endregion

  #region Throughput

  [Fact]
  public void Five_hundred_litres_a_second_cross_a_ten_pipe_run() {
    var line = PipeLine.Of(Length);
    var trace = new Trace("pipe-throughput-500");
    float[] drawn = new float[20];

    for (int t = 0; t < drawn.Length; t++) {
      line.Node(0).TryProduce(500f, 150f, "Steam", maxOutputPressure: 5f);
      drawn[t] = line.Node(Length - 1).TryConsume(500f);
      line.Step();
      line.Record(trace, t + 1);
      trace.Line(t + 1, "drawn=" + Trace.Litres(drawn[t]));
    }
    trace.Save();

    Assert.All(drawn, d => Assert.Equal(500f, d, Trace.VolumeDigits));
    Assert.Equal(499.60f, line.Net.State!.FlowRate, Trace.VolumeDigits);
  }

  #endregion

  #region Leaks

  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  public void An_open_run_leaks_eight_litres_of_gas_a_second_whatever_its_open_ends(
    int openEnds
  ) {
    var line = PipeLine.Of(Length, capStart: openEnds < 2, capEnd: false);
    var trace = new Trace($"pipe-leak-gas-{openEnds}");
    line.Net.TryProduceGas(200f, 150f, "Steam", line.Scene.World.Accessor);
    float volumeAfterOne = 0f;
    float tempAfterOne = 0f;

    line.Record(trace, 0);
    for (int t = 1; t <= 10; t++) {
      line.Step();
      line.Record(trace, t);
      if (t == 1) {
        volumeAfterOne = line.Net.State!.Volume;
        tempAfterOne = line.Net.State.Temperature;
      }
    }
    trace.Save();

    Assert.Equal(192f, volumeAfterOne, Trace.VolumeDigits);
    Assert.Equal(145f, tempAfterOne, Trace.TemperatureDigits);
    Assert.Equal(120f, line.Net.State!.Volume, Trace.VolumeDigits);
    Assert.Equal(100f, line.Net.State.Temperature, Trace.TemperatureDigits);
  }

  [Fact]
  public void An_open_run_leaks_ten_litres_of_water_a_second() {
    var line = PipeLine.Of(Length, capEnd: false);
    var trace = new Trace("pipe-leak-water");
    line.Net.TryProduceLiquid(200f, 20f, 1f, line.Scene.World.Accessor);
    float afterOne = 0f;

    line.Record(trace, 0);
    for (int t = 1; t <= 10; t++) {
      line.Step();
      line.Record(trace, t);
      if (t == 1)
        afterOne = line.Net.State!.Volume;
    }
    trace.Save();

    Assert.Equal(190f, afterOne, Trace.VolumeDigits);
    Assert.Equal(100f, line.Net.State!.Volume, Trace.VolumeDigits);
  }

  #endregion

  #region Chimney

  [Fact]
  public void A_chimney_over_a_passthrough_draws_sixteen_litres_a_second() {
    var line = PipeLine.Of(Length, chimney: true);
    var trace = new Trace("pipe-chimney");
    line.Net.TryProduceGas(300f, 300f, "Exhaust", line.Scene.World.Accessor);
    float afterOne = 0f;

    line.Record(trace, 0);
    for (int t = 1; t <= 10; t++) {
      line.Step();
      line.Record(trace, t);
      if (t == 1)
        afterOne = line.Net.State!.Volume;
    }
    trace.Save();

    Assert.Equal(284f, afterOne, Trace.VolumeDigits);
    Assert.Equal(140f, line.Net.State!.Volume, Trace.VolumeDigits);
  }

  #endregion

  #region Standing steam

  [Fact]
  public void Steam_standing_a_minute_in_a_ten_pipe_run_keeps_its_temperature() {
    var line = PipeLine.Of(Length);
    var trace = new Trace("pipe-standing-steam");
    line.Net.TryProduceGas(300f, 150f, "Steam", line.Scene.World.Accessor);

    line.Record(trace, 0);
    for (int t = 1; t <= 60; t++) {
      line.Step();
      line.Record(trace, t);
    }
    trace.Save();

    Assert.Equal(150f, line.Net.State!.Temperature, Trace.TemperatureDigits);
    Assert.Equal(300f, line.Net.State.Volume, Trace.VolumeDigits);
  }

  #endregion
}
