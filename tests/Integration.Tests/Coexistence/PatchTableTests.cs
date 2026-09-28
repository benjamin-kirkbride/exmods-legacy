using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExpandedLib.Industry.Molten;
using ExpandedLib.Testing;
using HarmonyLib;
using NSubstitute;
using NSubstitute.Core;
using PipesAndPowerExpanded;
using SteelmakingExpanded;
using SteelmakingExpanded.BlockNetworkMolten.Blocks;
using SteelmakingExpanded.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Xunit;

namespace Integration.Tests.Coexistence;

/// <summary>
/// Every vanilla method ppex and smex patch, each beside the patches the other line (iiex, siex)
/// puts on the same method, and the one method both lines patch to the same effect: a molten mold
/// placed on a mold rack spills once, with one chat line, when both lines' rack postfixes run. The
/// patches are applied under test-only Harmony ids and unpatched in a <c>finally</c>.
/// </summary>
public class PatchTableTests {
  // {declaring type}.{method} {kind} {patch class}, and the other line's patches on that method.
  private static readonly (string Patch, string OtherLine)[] Table =
  [
    (
      "Block.GetPlacedBlockInfo postfix ChimneyVentInfoPatch",
      "iiex ChimneyVentInfoPatch postfix, on Industry's passthrough and outlet"
    ),
#if GAME_GE_1_22
    (
      "BEBehaviorMPWaterWheel.CheckWater prefix WaterWheelGearedRatioPatch",
      "none"
    ),
    (
      "BEBehaviorMPWaterWheel.CheckWater postfix WaterWheelGearedRatioPatch",
      "none"
    ),
#endif
    ("BlockEntity.OnBlockPlaced postfix ToolMoldEntityPlacedPatch", "none"),
#if GAME_GE_1_21
    ("Item.GetHeldItemInfo postfix IronFeedInfoPatch", "none"),
#else
    ("CollectibleObject.GetHeldItemInfo postfix IronFeedInfoPatch", "none"),
#endif
    ("BlockEntityCoalPile.Initialize postfix CoalPileBurdenPatches", "none"),
    (
      "BlockEntityCoalPile.ToTreeAttributes postfix CoalPileBurdenPatches",
      "none"
    ),
    (
      "BlockEntityCoalPile.FromTreeAttributes postfix CoalPileBurdenPatches",
      "none"
    ),
    (
      "BlockMoldRack.OnBlockInteractStart postfix MoldRackSpillPatch",
      "iiex MoldRackSpillPatch postfix, the same spill"
    ),
    ("CollectibleObject.OnBeforeRender postfix ToolMoldPatches", "none"),
    (
      "BlockToolMold.GetPlacedBlockInteractionHelp postfix ToolMoldPatches",
      "none"
    ),
    ("BlockToolMold.GetDrops postfix ToolMoldPatches", "none"),
    ("BlockToolMold.OnBlockInteractStart prefix ToolMoldPatches", "none"),
  ];

  #region Patch table

  // Red with a patch added to or removed from either mod.
  [Fact]
  public void Every_vanilla_method_ppex_and_smex_patch_is_in_the_table() {
    List<string> patched =
    [
      .. Patches(typeof(PipesAndPowerExpandedModSystem).Assembly),
      .. Patches(typeof(SteelmakingExpandedModSystem).Assembly),
    ];

    Assert.Equal(Table.Select(r => r.Patch).Order(), patched.Order());
  }

  #endregion

  #region Mold rack

  // Red with MoltenMoldSpill.SpillIfMolten leaving the mold's contents in place: the second
  // postfix finds the metal still molten and sends a second chat line.
  [Fact]
  public void A_molten_mold_on_the_rack_spills_once_when_both_lines_spill_it() {
    var world = new TestWorld();
    world.RegisterItem("game:ingot-copper", 1084f);
    Block rackBlock = TestBlocks.Configure(
      new BlockMoldRack(),
      "game:moldrack-north",
      1
    );
    Block moldBlock = TestBlocks.Configure(
      new BlockToolMold(),
      "game:toolmold-burned-ingot",
      2
    );
    world.Register(moldBlock);
    var pos = new BlockPos(0, 0, 0);
    var rack = new BlockEntityMoldRack();
    world.Place(pos, rackBlock, rack);
    world.Initialize(rack);

    var mold = new ItemStack(moldBlock);
    MoltenContents.Write(
      mold,
      MoltenContents.MoldUnitsKey,
      MoltenMetal.CreateStack(world.World, "game:ingot-copper", 1200f)!,
      100
    );
    rack.Inventory[0].Itemstack = mold;
    TestPlayer player = world.Player();
    var selection = new BlockSelection { Position = pos };

    MoldRackSpillPatch.Postfix(world.World, player.Player, selection);
    MoldRackSpillPatch.Postfix(world.World, player.Player, selection);

    ICall chat = Assert.Single(
      player.ServerPlayer!.ReceivedCalls(),
      c => c.GetMethodInfo().Name == "SendIngameError"
    );
    Assert.Equal(MoltenMoldSpill.ErrorCode, chat.GetArguments()[0]);
    Assert.Null(mold.Attributes.GetTreeAttribute("blockEntityAttributes"));
  }

  #endregion

  #region Helpers

  /// <summary>Applies <paramref name="mod"/>'s patches under a test-only Harmony id and returns one
  /// <c>{type}.{method} {kind} {patch class}</c> row per patch method, unpatched after.</summary>
  private static List<string> Patches(Assembly mod) {
    string id = "coexistence." + mod.GetName().Name;
    var harmony = new Harmony(id);
    try {
      harmony.PatchAll(mod);
      return harmony
        .GetPatchedMethods()
        .SelectMany(method => {
          HarmonyLib.Patches info = Harmony.GetPatchInfo(method);
          string target = $"{method.DeclaringType!.Name}.{method.Name}";
          return Rows(target, "prefix", info.Prefixes, id)
            .Concat(Rows(target, "postfix", info.Postfixes, id))
            .Concat(Rows(target, "transpiler", info.Transpilers, id))
            .Concat(Rows(target, "finalizer", info.Finalizers, id));
        })
        .ToList();
    } finally {
      harmony.UnpatchAll(id);
    }
  }

  private static IEnumerable<string> Rows(
    string target,
    string kind,
    IEnumerable<Patch> patches,
    string owner
  ) =>
    patches
      .Where(p => p.owner == owner)
      .Select(p => $"{target} {kind} {p.PatchMethod.DeclaringType!.Name}");

  #endregion
}
