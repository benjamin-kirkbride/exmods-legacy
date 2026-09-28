using System.Linq;
using ExpandedLib.Industry.Molten;
using SteelmakingExpanded.BlockNetworkMolten.BlockEntities;
using Xunit;

namespace SteelmakingExpanded.Tests;

/// <summary>
/// smex's canal cells on Industry's molten network return smex's flow rules, read live from
/// <see cref="SmexValues"/>, with no exchange through the up and down faces; the start seeds the
/// flow and the tap and pedestal take sub-minimum remainders. Red with <c>HorizontalOnly</c> false,
/// with the rules read once instead of live, with <c>IsFlowSource</c> false for the start, and with
/// <c>AcceptsSubMinimumFlow</c> false for the tap.
/// </summary>
public class MoltenFlowRulesTests {
  [Fact]
  public void A_canal_cell_returns_smexs_rules_read_live_and_horizontal_only() {
    IMoltenCell cell = new BlockEntityMoltenCanal();
    int rate = SmexValues.MoltenFlowRate;
    int gap = SmexValues.MoltenMinFlowAmount;

    Assert.Equal(new MoltenFlowRules(rate, gap, true, true), cell.FlowRules);
    try {
      SmexValues.Edit(c => {
        c.MoltenFlowRate = 37;
        c.MoltenMinFlowAmount = 4;
      });

      Assert.Equal(new MoltenFlowRules(37, 4, true, true), cell.FlowRules);
    } finally {
      SmexValues.Edit(c => {
        c.MoltenFlowRate = rate;
        c.MoltenMinFlowAmount = gap;
      });
    }
  }

  [Fact]
  public void Only_the_start_seeds_the_flow_and_only_the_tap_and_pedestal_drain() {
    IMoltenCell canal = new BlockEntityMoltenCanal();
    IMoltenCell start = new BlockEntityMoltenCanalStart();
    IMoltenCell tap = new BlockEntityMoltenCanalTap();
    IMoltenCell pedestal = new BlockEntityMoltenCanalMoldPedestal();

    Assert.Equal(
      new[] { false, true, false, false },
      new[] { canal, start, tap, pedestal }.Select(c => c.IsFlowSource)
    );
    Assert.Equal(
      new[] { false, false, true, true },
      new[] { canal, start, tap, pedestal }.Select(c =>
        c.AcceptsSubMinimumFlow
      )
    );
  }
}
