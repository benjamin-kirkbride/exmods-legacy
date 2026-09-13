// Shims for API members introduced in 1.21 (so present on 1.21 and later, missing on 1.20 only).
// Guarded by !GAME_GE_1_21 (game version < 1.21, i.e. only 1.20) so they neither shadow the real
// members on 1.21+ nor compile where those members already exist. See the GAME_GE_* convention in
// src/Directory.Build.props.
#if !GAME_GE_1_21
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace ExpandedLib.Legacy;

public static class LegacyApi120
{
  /// <summary>1.21 added <c>BlockDropItemStack.ToRandomItemstackForPlayer</c> (player-aware drop
  /// rate). 1.20 has only <c>GetNextItemStack</c>; fall back to it - 1.20 had no player luck
  /// modifier to honour anyway.</summary>
  public static ItemStack? ToRandomItemstackForPlayer(
    this BlockDropItemStack drop,
    IPlayer byPlayer,
    IWorldAccessor world,
    float dropQuantityMultiplier = 1f
  ) => drop.GetNextItemStack(dropQuantityMultiplier);

  extension(BlockEntityToolMold mold)
  {
    /// <summary>1.21 added <c>BlockEntityToolMold.MeshAngle</c>; 1.20 tool molds do not rotate.</summary>
    public float MeshAngle => 0f;
  }
}
#endif
