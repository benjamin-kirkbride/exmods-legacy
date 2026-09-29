using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace PipesAndPowerExpanded.ClosedLine;

/// <summary>
/// Takes ppex and smex stacks out of a closed world, server side: out of a player's inventories when
/// they join, out of the block-entity containers of each chunk as it loads, and out of the world as
/// dropped item entities, each as the engine loads or spawns it. The block entities of ppex and smex
/// blocks keep their own inventories.
/// </summary>
/// <remarks>Stacks nested in another stack's attributes, entity inventories and storage that is not
/// a block-entity container or a vanilla tool rack are left as they are.</remarks>
public static class ClosedLineSweep {
  /// <summary>Sweeps each player on join, each chunk once the engine has initialised the block
  /// entities of its newly loaded column, and each item entity on spawn and on load; then sweeps
  /// the chunks and entities <paramref name="api"/>'s world already holds.</summary>
  public static void Register(ICoreServerAPI api) {
    api.Event.PlayerJoin += player => FromPlayer(player);
    api.Event.ChunkDirty += (_, chunk, reason) => {
      if (reason == EnumChunkDirtyReason.NewlyLoaded)
        FromChunk(chunk);
    };
    api.Event.OnEntitySpawn += entity => FromEntity(entity);
    api.Event.OnEntityLoaded += entity => FromEntity(entity);

    foreach (IServerChunk chunk in api.WorldManager.AllLoadedChunks.Values)
      FromChunk(chunk);
    foreach (Entity entity in api.World.LoadedEntities.Values)
      FromEntity(entity);
  }

  /// <summary>Empties every slot holding a ppex or smex stack in each of
  /// <paramref name="player"/>'s inventories but the creative one.</summary>
  /// <returns>How many slots were emptied.</returns>
  public static int FromPlayer(IPlayer player) =>
    player
      .InventoryManager.Inventories.Values.Where(inventory =>
        inventory.ClassName != GlobalConstants.creativeInvClassName
      )
      .Sum(FromInventory);

  /// <summary>Sweeps each block entity of <paramref name="chunk"/> through
  /// <see cref="FromBlockEntity"/>.</summary>
  /// <returns>How many slots were emptied.</returns>
  public static int FromChunk(IWorldChunk chunk) =>
    chunk.BlockEntities.Values.Sum(FromBlockEntity);

  /// <summary>Empties every slot holding a ppex or smex stack in <paramref name="blockEntity"/>'s
  /// container, or in a vanilla tool rack's rack, and marks it dirty; a ground storage left empty is
  /// removed from its cell, as vanilla removes it. The block entity of a ppex or smex block and one
  /// with no container are left as they are.</summary>
  /// <returns>How many slots were emptied.</returns>
  public static int FromBlockEntity(BlockEntity blockEntity) {
    if (ClosedLineModSystem.IsOldLine(blockEntity.Block?.Code))
      return 0;
    IInventory? inventory = blockEntity switch {
      IBlockEntityContainer container => container.Inventory,
      BlockEntityToolrack rack => rack.inventory,
      _ => null,
    };
    int emptied = inventory == null ? 0 : FromInventory(inventory);
    if (emptied == 0)
      return 0;

    blockEntity.MarkDirty(true);
    if (blockEntity is BlockEntityGroundStorage && inventory!.Empty)
      blockEntity.Api.World.BlockAccessor.SetBlock(0, blockEntity.Pos);
    return emptied;
  }

  /// <summary>Kills <paramref name="entity"/> as removed when it is a dropped ppex or smex
  /// stack.</summary>
  /// <returns>Whether it was killed.</returns>
  public static bool FromEntity(Entity entity) {
    if (
      entity is not EntityItem { Itemstack: { } stack }
      || !ClosedLineModSystem.IsOldLine(stack.Collectible?.Code)
    )
      return false;
    entity.Die(EnumDespawnReason.Removed);
    return true;
  }

  /// <summary>Empties and marks dirty every slot of <paramref name="inventory"/> that holds a ppex
  /// or smex stack.</summary>
  /// <returns>How many slots were emptied.</returns>
  public static int FromInventory(IInventory inventory) {
    int emptied = 0;
    foreach (ItemSlot slot in inventory) {
      if (!ClosedLineModSystem.IsOldLine(slot.Itemstack?.Collectible?.Code))
        continue;
      slot.Itemstack = null;
      slot.MarkDirty();
      emptied++;
    }
    return emptied;
  }
}
