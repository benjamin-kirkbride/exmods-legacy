using System;
using ExpandedLib.Industry.Molten;
using Vintagestory.API.Common;

namespace SteelmakingExpanded.BlockNetworkMolten;

/// <summary>
/// smex's metal-bit recovery for solidified metal chipped or broken out of a canal cell, the molten
/// barrel or the bessemer charge. It reads smex's own solid-drop rule and slag, never exlib's metal
/// registry, so another mod's metal definitions do not change what smex drops.
/// </summary>
public static class MoltenRecovery {
  /// <summary>Maps a molten metal item to its solid drop ("game:ingot-iron" to "game:metalbit-iron");
  /// non-ingot items drop as themselves.</summary>
  internal static AssetLocation SolidDropLocation(AssetLocation metalItemLoc) {
    if (metalItemLoc.Path.StartsWith("ingot-"))
      return new AssetLocation(
        metalItemLoc.Domain,
        "metalbit-" + metalItemLoc.Path[6..]
      );
    return metalItemLoc;
  }

  /// <summary>
  /// The metal-bit recovery stack for <paramref name="units"/> of the metal <paramref name="metalCode"/>
  /// at <paramref name="temperature"/> deg C, <paramref name="unitsPerBit"/> units per bit (at least one
  /// bit), mapped to the solid drop by <see cref="SolidDropLocation"/>.
  /// <para>
  /// <paramref name="unitsPerBit"/> defaults to <see cref="SmexValues.MoltenUnitsPerBit"/>, which
  /// also prices scrap going back into the converter. A rate that differs from the remelt's opens a
  /// duplication loop, so callers should leave it alone.
  /// </para>
  /// </summary>
  /// <returns>The drop; <c>smex:slag</c> in the same count when the solid item does not resolve and
  /// <paramref name="slagFallback"/> is set; otherwise <c>null</c>.</returns>
  public static ItemStack? BuildRecovery(
    IWorldAccessor world,
    AssetLocation metalCode,
    float temperature,
    int units,
    int? unitsPerBit = null,
    bool slagFallback = false
  ) {
    int perBit = Math.Max(1, unitsPerBit ?? SmexValues.MoltenUnitsPerBit);
    int count = Math.Max(1, units / perBit);
    AssetLocation loc = SolidDropLocation(metalCode);
    Item? item = world.GetItem(loc);
    if (item == null) {
      if (!slagFallback)
        return null;
      Item? slag = world.GetItem(new AssetLocation("smex:slag"));
      return slag != null ? new ItemStack(slag, count) : null;
    }
    var drop = new ItemStack(item, count);
    MoltenMetal.SetTemperature(world, drop, temperature);
    return drop;
  }
}
