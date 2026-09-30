using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExpandedLib.Blocks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using StageIngredient = ExpandedLib.Blocks.ExConstructionIngredient;

namespace PipesAndPowerExpanded.ClosedLine;

/// <summary>
/// The closed line's Harmony patches, under the id <see cref="HarmonyId"/>. A postfix on each
/// distinct <c>GetDrops</c> implementation of the loaded ppex and smex block types, and one on the
/// construction refund, take the ppex and smex stacks out of a ppex or smex block's drops and
/// refund. A postfix on <see cref="CraftingRecipeIngredient.SatisfiesAsIngredient"/> lets a
/// construction stage's ppex straight pipe ingredient take an iiex or siex straight pipe. Each acts only where
/// <see cref="ClosedLineModSystem.IsClosed"/> holds for the world at hand.
/// </summary>
/// <remarks>Stacks of another domain still drop. The refund is
/// <see cref="ExRightClickConstructable.GetConstructionDrops"/> on 1.22 and exlib's
/// <c>ExRightClickConstruction.GetDrops</c> while an <see cref="ExRightClickConstructable"/> is
/// being broken on 1.20 and 1.21. A stage paid with the new line's pipe refunds the ppex pipe its
/// ingredient names, which the refund patch then takes out.</remarks>
public static class ClosedLinePatches {
  /// <summary>The Harmony id every closed-line patch is applied under.</summary>
  public const string HarmonyId = "ppex.closedline";

  private static readonly AccessTools.FieldRef<CollectibleObject, ICoreAPI> ApiOf =
    AccessTools.FieldRefAccess<CollectibleObject, ICoreAPI>("api");

  /// <summary>Patches each target not yet patched under <see cref="HarmonyId"/>: the drops of every
  /// ppex and smex block type <paramref name="api"/>'s world holds, the construction refund and the
  /// ingredient match.</summary>
  /// <remarks>Patches are process-wide, so a server and a client in one process share them.</remarks>
  /// <returns>Whether any method was patched by this call.</returns>
  public static bool Apply(ICoreAPI api) {
    var harmony = new Harmony(HarmonyId);
    bool patched = false;
    foreach (MethodInfo drops in DropsImplementations(api))
      patched |= Patch(harmony, drops, postfix: nameof(DropsPostfix));
#if GAME_GE_1_22
    patched |= Patch(
      harmony,
      AccessTools.Method(
        typeof(ExRightClickConstructable),
        nameof(ExRightClickConstructable.GetConstructionDrops)
      ),
      postfix: nameof(RefundPostfix)
    );
#else
    patched |= Patch(
      harmony,
      AccessTools.Method(
        typeof(ExRightClickConstructable),
        nameof(ExRightClickConstructable.OnBlockBroken)
      ),
      prefix: nameof(BreakingPrefix),
      finalizer: nameof(BreakingFinalizer)
    );
    patched |= Patch(
      harmony,
      AccessTools.Method(
        typeof(ExRightClickConstruction),
        nameof(ExRightClickConstruction.GetDrops)
      ),
      postfix: nameof(LegacyRefundPostfix)
    );
#endif
    patched |= Patch(
      harmony,
      AccessTools.Method(
        typeof(CraftingRecipeIngredient),
        nameof(CraftingRecipeIngredient.SatisfiesAsIngredient),
        [typeof(ItemStack), typeof(bool)]
      ),
      postfix: nameof(PipePostfix)
    );
    return patched;
  }

  /// <summary>Removes every patch applied under <see cref="HarmonyId"/>.</summary>
  public static void Remove() => new Harmony(HarmonyId).UnpatchAll(HarmonyId);

  /// <summary>The distinct <c>GetDrops</c> methods the ppex and smex block types in
  /// <paramref name="api"/>'s world run, each the most derived override of its type.</summary>
  public static IEnumerable<MethodInfo> DropsImplementations(ICoreAPI api) =>
    (api.World.Blocks ?? [])
      .Where(b => ClosedLineModSystem.IsOldLine(b?.Code))
      .Select(b => b.GetType())
      .Distinct()
      .Select(t => t.GetMethod(nameof(Block.GetDrops), DropsSignature)!.DeclaringType!)
      .Distinct()
      .Select(t =>
        t.GetMethod(
          nameof(Block.GetDrops),
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
          DropsSignature
        )!
      );

  private static readonly Type[] DropsSignature = [
    typeof(IWorldAccessor),
    typeof(BlockPos),
    typeof(IPlayer),
    typeof(float),
  ];

  /// <summary><paramref name="stacks"/> less every ppex and smex stack.</summary>
  public static ItemStack[] WithoutOldLine(ItemStack[] stacks) =>
    [.. stacks.Where(s => !ClosedLineModSystem.IsOldLine(s?.Collectible?.Code))];

  /// <summary>Whether <paramref name="code"/> names a ppex straight pipe, its placeholders filled or
  /// not.</summary>
  public static bool IsOldStraightPipe(AssetLocation? code) =>
    code is { Domain: "ppex" } && code.Path.StartsWith("pipe-straight-");

  /// <summary>Whether <paramref name="code"/> is an iiex or siex straight pipe of any tier and
  /// orientation, e.g. <c>iiex:pipe-plated-straight-ns</c>.</summary>
  public static bool IsNewStraightPipe(AssetLocation? code) =>
    code != null
    && ClosedLineModSystem.Successors.Contains(code.Domain)
    && code.Path.StartsWith("pipe-")
    && code.Path.Contains("-straight-");

  private static bool Patch(
    Harmony harmony,
    MethodInfo? target,
    string? prefix = null,
    string? postfix = null,
    string? finalizer = null
  ) {
    if (
      target == null
      || Harmony.GetPatchInfo(target)?.Owners.Contains(HarmonyId) == true
    )
      return false;
    harmony.Patch(
      target,
      prefix: Own(prefix),
      postfix: Own(postfix),
      finalizer: Own(finalizer)
    );
    return true;
  }

  private static HarmonyMethod? Own(string? name) =>
    name == null
      ? null
      : new HarmonyMethod(
        typeof(ClosedLinePatches).GetMethod(
          name,
          BindingFlags.Static | BindingFlags.NonPublic
        )
      );

  private static bool TakesFrom(ExRightClickConstructable behavior) =>
    ClosedLineModSystem.IsOldLine(behavior.Blockentity?.Block?.Code)
    && behavior.Api is { } api
    && ClosedLineModSystem.IsClosed(api);

  private static void DropsPostfix(
    Block __instance,
    object[] __args,
    ref ItemStack[] __result
  ) {
    if (
      __result is { Length: > 0 }
      && ClosedLineModSystem.IsOldLine(__instance.Code)
      && __args[0] is IWorldAccessor { Api: { } api }
      && ClosedLineModSystem.IsClosed(api)
    )
      __result = WithoutOldLine(__result);
  }

#if GAME_GE_1_22
  private static void RefundPostfix(
    ExRightClickConstructable __instance,
    ref ItemStack[] __result
  ) {
    if (__result is { Length: > 0 } && TakesFrom(__instance))
      __result = WithoutOldLine(__result);
  }
#else
  /// <summary>The construction being broken on this thread, whose refund
  /// <see cref="LegacyRefundPostfix"/> filters.</summary>
  [ThreadStatic]
  private static ExRightClickConstructable? _breaking;

  private static void BreakingPrefix(ExRightClickConstructable __instance) =>
    _breaking = __instance;

  private static Exception? BreakingFinalizer(Exception? __exception) {
    _breaking = null;
    return __exception;
  }

  private static void LegacyRefundPostfix(ref ItemStack[] __result) {
    if (__result is { Length: > 0 } && _breaking is { } breaking && TakesFrom(breaking))
      __result = WithoutOldLine(__result);
  }
#endif

  private static void PipePostfix(
    CraftingRecipeIngredient __instance,
    object[] __args,
    ref bool __result
  ) {
    if (
      !__result
      && __instance is StageIngredient
      && IsOldStraightPipe(__instance.Code)
      && __args[0] is ItemStack { Collectible: { } offered }
      && IsNewStraightPipe(offered.Code)
      && ApiOf(offered) is { } api
      && ClosedLineModSystem.IsClosed(api)
    )
      __result = true;
  }
}
