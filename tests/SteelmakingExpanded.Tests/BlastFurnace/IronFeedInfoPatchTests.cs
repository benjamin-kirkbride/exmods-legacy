using System.Reflection;
using System.Text;
using ExpandedLib.Testing;
using HarmonyLib;
using SteelmakingExpanded.Patches;
using Vintagestory.API.Common;
using Xunit;

namespace SteelmakingExpanded.Tests;

/// <summary>
/// The held-item patch that states an iron feed's blast furnace yield, applied under a test-only
/// Harmony id and unpatched in a <c>finally</c>.
/// </summary>
public class IronFeedInfoPatchTests {
  private const string HarmonyId = "smextest.ironfeedinfo";

  // Fails on 1.20 when the patch targets Item.GetHeldItemInfo, which only 1.21 and later declare:
  // Harmony then finds no target and throws. Fails on every series when the postfix appends nothing.
  [Fact]
  public void The_patch_finds_its_target_and_a_held_iron_feed_states_its_furnace_yield() {
    var harmony = new Harmony(HarmonyId);
    try {
      harmony.CreateClassProcessor(typeof(IronFeedInfoPatch)).Patch();
      MethodBase target = Assert.Single(harmony.GetPatchedMethods());
      Assert.Equal(nameof(CollectibleObject.GetHeldItemInfo), target.Name);

      var world = new TestWorld();
      Item ore = world.RegisterItem("game:crushed-iron");
      var info = new StringBuilder();
      ore.GetHeldItemInfo(
        new DummySlot(new ItemStack(ore)),
        info,
        world.World,
        false
      );

      Assert.Contains("smex:itemdesc-blastfurnace-yield", info.ToString());
    } finally {
      harmony.UnpatchAll(HarmonyId);
    }
  }
}
