using ExpandedLib.Helpers;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SteelmakingExpanded.BlockStructures;

/// <summary>
/// Wrench turns for the fixed structure parts whose facing is their <c>side</c> variant. A turn
/// exchanges the block in place for its variant a quarter turn over, so the cell keeps its block
/// entity.
/// </summary>
public static class SideWrench {
  /// <summary>
  /// Exchanges the block at <paramref name="blockSel"/> for the variant of <paramref name="block"/>
  /// turned <paramref name="dir"/> quarter turns: positive turns counter-clockwise seen from above
  /// (east to north), negative clockwise. The vanilla wrench passes +1 on attack and -1 on use.
  /// </summary>
  /// <param name="block">The part standing at <paramref name="blockSel"/>; its code carries a
  /// horizontal <c>side</c> variant.</param>
  /// <param name="byEntity">The wrench holder; its world is the one exchanged in.</param>
  /// <param name="blockSel">The part's cell.</param>
  /// <param name="dir">Quarter turns, any sign.</param>
  /// <returns>The block now standing in the cell, or null when the world holds no block of the
  /// turned code and the cell is left as it stands.</returns>
  public static Block? Turn(
    Block block,
    EntityAgent byEntity,
    BlockSelection blockSel,
    int dir
  ) {
    BlockFacing side = BlockFacing.FromCode(block.Variant["side"]);
    BlockFacing turned = BlockFacing.HORIZONTALS_ANGLEORDER[
      GameMath.Mod(side.HorizontalAngleIndex + dir, 4)
    ];
    Block? next = byEntity.World.GetBlock(
      block.CodeWithVariant("side", turned.Code)
    );
    if (next == null)
      return null;

    byEntity.World.BlockAccessor.ExchangeBlock(next.BlockId, blockSel.Position);
    return next;
  }

  /// <summary>The placed-block help line for a wrench turn: the right mouse button with any
  /// registered wrench.</summary>
  public static WorldInteraction Help(IWorldAccessor world) =>
    new() {
      ActionLangCode = "smex:blockhelp-rotate",
      MouseButton = EnumMouseButton.Right,
      Itemstacks = ExItems.WrenchStacks(world),
    };
}
