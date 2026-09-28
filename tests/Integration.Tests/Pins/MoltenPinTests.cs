using System;
using System.Linq;
using ExpandedLib.Networks;
using SteelmakingExpanded;
using SteelmakingExpanded.BlockNetworkMolten.Blocks;
using Xunit;

namespace Integration.Tests.Pins;

/// <summary>
/// The molten network on the published smex, as exact numbers: the rate across one connection, how
/// a fed run fills, the minimum-gap floor, a pedestal's fill time and a cell's cooling to solid.
/// Each scene also records its trace.
/// </summary>
public class MoltenPinTests {
  #region Rate and reach

  [Fact]
  public void A_full_start_hands_its_neighbour_a_hundred_units_in_one_tick() {
    var line = new CanalLine("start", "straight");
    var trace = new Trace("molten-rate");
    line.Pour(0, 200);

    line.Record(trace, 0);
    line.Step();
    line.Record(trace, 1);
    trace.Save();

    Assert.Equal(100, line[0].CellAmount);
    Assert.Equal(100, line[1].CellAmount);
  }

  [Fact]
  public void A_fed_ten_cell_run_reaches_its_far_cell_on_the_ninth_tick_and_fills_it() {
    var line = new CanalLine(["start", .. Enumerable.Repeat("straight", 9)]);
    var trace = new Trace("molten-reach-10");
    int firstAtFarCell = 0;

    line.Record(trace, 0);
    for (int t = 1; t <= 20; t++) {
      line.Pour(0, 200);
      line.Step();
      line.Record(trace, t);
      if (firstAtFarCell == 0 && line[9].CellAmount > 0)
        firstAtFarCell = t;
    }
    trace.Save();

    Assert.Equal(9, firstAtFarCell);
    Assert.Equal(100, line[9].CellAmount);
  }

  #endregion

  #region Vertical flow

  [Fact]
  public void No_molten_block_has_a_vertical_connector() {
    var nodes = typeof(SteelmakingExpandedModSystem)
      .Assembly.GetTypes()
      .Where(t => t.IsSubclassOf(typeof(BlockNetworkNode)) && !t.IsAbstract)
      .Select(t => (BlockNetworkNode)Activator.CreateInstance(t)!)
      .Where(n => n.NetworkType == "molten")
      .ToArray();

    Assert.Equal(
      new[]
      {
        nameof(BlockMoltenCanal),
        nameof(BlockMoltenCanalMoldPedestal),
        nameof(BlockMoltenCanalStart),
        nameof(BlockMoltenCanalTap),
      },
      nodes
        .Select(n => n.GetType().Name)
        .OrderBy(n => n, StringComparer.Ordinal)
    );
    foreach (var node in nodes)
      foreach (var (type, orientations) in node.AllowedOrientations)
        Assert.All(
          orientations,
          o =>
            Assert.False(
              o.Contains('u') || o.Contains('d'),
              $"{node.GetType().Name} {type}-{o} has a vertical connector"
            )
        );
  }

  #endregion

  #region Minimum gap

  [Fact]
  public void A_raised_minimum_holds_a_nine_unit_gap_between_canal_cells() {
    WithMinimumFlow(
      10,
      () => {
        var line = new CanalLine("start", "straight");
        var trace = new Trace("molten-minimum-canal");
        line.Pour(0, 9);

        line.Record(trace, 0);
        line.Step();
        line.Record(trace, 1);
        trace.Save();

        Assert.Equal(9, line[0].CellAmount);
        Assert.Equal(0, line[1].CellAmount);
      }
    );
  }

  [Fact]
  public void A_raised_minimum_still_lets_a_pedestal_take_a_nine_unit_remainder() {
    WithMinimumFlow(
      10,
      () => {
        var line = new CanalLine("start", "pedestal");
        var trace = new Trace("molten-minimum-pedestal");
        line.Pour(0, 9);

        line.Record(trace, 0);
        line.Step();
        line.Record(trace, 1);
        trace.Save();

        Assert.Equal(0, line[0].CellAmount);
        Assert.Equal(9, line[1].CellAmount);
      }
    );
  }

  private static void WithMinimumFlow(int units, Action body) {
    int saved = SmexValues.MoltenMinFlowAmount;
    try {
      SmexValues.Edit(c => c.MoltenMinFlowAmount = units);
      body();
    } finally {
      SmexValues.Edit(c => c.MoltenMinFlowAmount = saved);
    }
  }

  #endregion

  #region Pedestal

  [Fact]
  public void A_fed_pedestal_fills_its_mold_on_the_fifth_tick() {
    var line = new CanalLine("start", "straight", "straight", "pedestal");
    line.Pedestal.IsMold = true;
    var trace = new Trace("molten-pedestal-fill");
    int full = 0;

    line.Record(trace, 0);
    for (int t = 1; t <= 10; t++) {
      line.Pour(0, 200);
      line.Step();
      line.Record(trace, t);
      if (
        full == 0
        && line.Pedestal.MoldCurrentUnits >= line.Pedestal.MoldMaxUnits
      )
        full = t;
    }
    trace.Save();

    Assert.Equal(100, line.Pedestal.MoldMaxUnits);
    Assert.Equal(5, full);
  }

  #endregion

  #region Cooling

  // 1.20 and 1.21 decay a stack's temperature only once more than 1/85 hour has passed since its
  // last stamp, and the cell stamps it every tick, so at MoltenClock.HoursPerTick the metal holds
  // its temperature there. 1.22 decays from 1/150 hour on.
  [Fact]
  public void A_canal_cell_of_iron_at_1700_cools_as_the_game_version_decays_it() {
    var line = new CanalLine("straight");
    var trace = new Trace("molten-cooling");
    line.Pour(0, 100);
    int frozen = 0;
    float atSixty = 0f;

    line.Record(trace, 0);
    for (int t = 1; frozen == 0 && t <= 400; t++) {
      line.Step();
      line.Record(trace, t);
      if (t == 60)
        atSixty = line[0].CellTemperature;
      if (line[0].Solidified)
        frozen = t;
    }
    trace.Save();

#if GAME_GE_1_22
    Assert.Equal(1601.0f, atSixty, Trace.TemperatureDigits);
    Assert.Equal(126, frozen);
#else
    Assert.Equal(1700.0f, atSixty, Trace.TemperatureDigits);
    Assert.Equal(0, frozen);
#endif
  }

  #endregion
}
