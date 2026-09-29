using System.Collections.Generic;
using ExpandedLib.Testing;
using NSubstitute;
using SteelmakingExpanded.BlockStructures.BlastFurnace.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Xunit;

namespace Integration.Tests.ClosedLine;

/// <summary>
/// The closed line's sweep in <see cref="ClosedLineWorld.Closed"/>, driven through the engine events
/// it listens to: ppex and smex stacks leave a joining player's inventories, the containers of a
/// loading chunk and dropped items, and a smex block entity keeps its own inventory.
/// </summary>
[Collection(ClosedLineWorld.Collection)]
public class ClosedLineSweepTests {
  private static ClosedLineWorld Closed => ClosedLineWorld.Closed;

  // Fails when the join sweep empties nothing, empties a vanilla stack or the creative inventory
  // too, or does not mark an emptied slot dirty.
  [Fact]
  public void A_player_keeps_only_the_ingot_after_the_join_sweep() {
    TestPlayer joiner = Closed.World.Player("sweeper");
    IServerPlayer player = Assert.IsAssignableFrom<IServerPlayer>(joiner.ServerPlayer);
    var inventories = new Dictionary<string, IInventory> {
      ["hotbar"] = joiner.Hotbar,
    };
    foreach (
      string name in new[] {
        GlobalConstants.backpackInvClassName,
        GlobalConstants.craftingInvClassName,
        GlobalConstants.mousecursorInvClassName,
        GlobalConstants.characterInvClassName,
        GlobalConstants.creativeInvClassName,
      }
    )
      inventories[name] = TestInventory.Of(Closed.World, 4, $"{name}-sweeper");
    joiner.Hotbar[0].Itemstack = new ItemStack(Closed.Pipe);
    joiner.Hotbar[1].Itemstack = new ItemStack(Closed.Ingot);
    inventories[GlobalConstants.backpackInvClassName][0].Itemstack = new ItemStack(Closed.Mold);
    inventories[GlobalConstants.backpackInvClassName][1].Itemstack = new ItemStack(Closed.Ingot);
    inventories[GlobalConstants.craftingInvClassName][0].Itemstack = new ItemStack(Closed.Pipe);
    inventories[GlobalConstants.mousecursorInvClassName][0].Itemstack = new ItemStack(Closed.Mold);
    inventories[GlobalConstants.characterInvClassName][0].Itemstack = new ItemStack(Closed.Pipe);
    inventories[GlobalConstants.creativeInvClassName][0].Itemstack = new ItemStack(Closed.Pipe);
    player.InventoryManager.Inventories.Returns(inventories);
    var modified = new List<int>();
    ((InventoryBase)inventories[GlobalConstants.backpackInvClassName]).SlotModified +=
      modified.Add;

    Closed.World.Api.Event.PlayerJoin += Raise.Event<PlayerDelegate>(player);

    var kept = new List<string>();
    foreach ((string name, IInventory inventory) in inventories)
      if (name != GlobalConstants.creativeInvClassName)
        kept.AddRange(ClosedLineWorld.Codes(inventory));
    Assert.Equal(["game:ingot-iron", "game:ingot-iron"], kept);
    Assert.Equal(
      ["ppex:pipe-straight-ns-iron"],
      ClosedLineWorld.Codes(inventories[GlobalConstants.creativeInvClassName])
    );
    Assert.Equal([0], modified);
  }

  // Fails when the chunk sweep empties nothing, sweeps a chunk that did not newly load, skips the
  // tool rack, leaves an emptied ground storage in its cell, or marks dirty a container it emptied
  // nothing from, or not one it did.
  [Fact]
  public void A_vanilla_chest_keeps_only_the_ingot_after_its_chunk_loads() {
    var world = new TestWorld();
    var chest = new BlockEntityGenericTypedContainer();
    world.Place(new BlockPos(0, 1, 0), StandIn("game:chest-east", 1), chest);
    world.Initialize(chest);
    chest.Inventory[0].Itemstack = new ItemStack(Closed.Pipe);
    chest.Inventory[1].Itemstack = new ItemStack(Closed.Mold);
    chest.Inventory[2].Itemstack = new ItemStack(Closed.Ingot);
    var rack = new BlockEntityToolrack {
      Api = world.Api,
      inventory = TestInventory.Of(world, 4, "toolrack-1"),
    };
    world.Place(new BlockPos(1, 1, 0), StandIn("game:toolrack-east", 2), rack);
    rack.inventory[0].Itemstack = new ItemStack(Closed.Mold);
    rack.inventory[1].Itemstack = new ItemStack(Closed.Ingot);
    var ground = new BlockEntityGroundStorage { Api = world.Api };
    var groundPos = new BlockPos(2, 1, 0);
    world.Place(groundPos, StandIn("game:groundstorage", 3), ground);
    ground.Inventory[0].Itemstack = new ItemStack(Closed.Pipe);
    var mold = new BlockEntityToolMold { Api = world.Api };
    world.Place(new BlockPos(3, 1, 0), StandIn("game:toolmold-burned-anvil", 4), mold);
    var moldRack = new BlockEntityMoldRack { Api = world.Api };
    world.Place(new BlockPos(4, 1, 0), StandIn("game:moldrack-normal", 5), moldRack);
    moldRack.Inventory[0].Itemstack = new ItemStack(Closed.Ingot);
    var blockEntities = new Dictionary<BlockPos, BlockEntity> {
      [chest.Pos] = chest,
      [rack.Pos] = rack,
      [groundPos] = ground,
      [mold.Pos] = mold,
      [moldRack.Pos] = moldRack,
    };
    IWorldChunk chunk = Substitute.For<IWorldChunk>();
    chunk.BlockEntities.Returns(blockEntities);

    Load(chunk, EnumChunkDirtyReason.MarkedDirty);
    Assert.Equal(3, ClosedLineWorld.Codes(chest.Inventory).Count);
    Load(chunk, EnumChunkDirtyReason.NewlyLoaded);

    Assert.Equal(["game:ingot-iron"], ClosedLineWorld.Codes(chest.Inventory));
    Assert.Equal(["game:ingot-iron"], ClosedLineWorld.Codes(rack.inventory));
    Assert.Same(world.Air, world.GetBlock(groundPos));
    Assert.Null(world.GetBlockEntity(groundPos));
    world.Accessor.Received().MarkBlockEntityDirty(chest.Pos);
    world.Accessor.DidNotReceive().MarkBlockEntityDirty(moldRack.Pos);
    Assert.Equal(["game:ingot-iron"], ClosedLineWorld.Codes(moldRack.Inventory));
  }

  // Fails when the sweep does not skip the block entities of ppex and smex blocks: the hopper
  // loses its pipe and mold.
  [Fact]
  public void A_smex_hopper_keeps_its_own_inventory() {
    var hopper = new BlockEntityHopperReinforced {
      Block = Closed.World.World.GetBlock(new AssetLocation("smex:hopperreinforced")),
      Pos = new BlockPos(0, 1, 0),
    };
    Assert.NotNull(hopper.Block);
    hopper.Inventory[0].Itemstack = new ItemStack(Closed.Pipe);
    hopper.Inventory[1].Itemstack = new ItemStack(Closed.Mold);
    hopper.Inventory[2].Itemstack = new ItemStack(Closed.Ingot);
    IWorldChunk chunk = Substitute.For<IWorldChunk>();
    chunk.BlockEntities.Returns(
      new Dictionary<BlockPos, BlockEntity> { [hopper.Pos] = hopper }
    );

    Load(chunk, EnumChunkDirtyReason.NewlyLoaded);

    Assert.Equal(
      ["ppex:pipe-straight-ns-iron", Closed.Mold.Code.ToString(), "game:ingot-iron"],
      ClosedLineWorld.Codes(hopper.Inventory)
    );
  }

  // Fails when the spawn or the load sweep kills nothing, or kills every dropped item.
  [Fact]
  public void A_dropped_ppex_pipe_is_removed_on_spawn_and_a_smex_mold_on_load() {
    var pipe = new EntityItem { Itemstack = new ItemStack(Closed.Pipe) };
    var ingot = new EntityItem { Itemstack = new ItemStack(Closed.Ingot) };
    var mold = new EntityItem { Itemstack = new ItemStack(Closed.Mold) };

    Closed.World.Api.Event.OnEntitySpawn += Raise.Event<EntityDelegate>(pipe);
    Closed.World.Api.Event.OnEntitySpawn += Raise.Event<EntityDelegate>(ingot);
    Closed.World.Api.Event.OnEntityLoaded += Raise.Event<EntityDelegate>(mold);

    Assert.False(pipe.Alive);
    Assert.Equal(EnumDespawnReason.Removed, pipe.DespawnReason?.Reason);
    Assert.False(mold.Alive);
    Assert.Equal(EnumDespawnReason.Removed, mold.DespawnReason?.Reason);
    Assert.True(ingot.Alive);
  }

  // Fails when the start does not sweep what the world already holds.
  [Fact]
  public void The_chunks_and_items_loaded_before_the_start_are_swept() {
    Assert.Equal(["game:ingot-iron"], ClosedLineWorld.Codes(Closed.StartRack.Inventory));
    Assert.False(Closed.StartDrop.Alive);
  }

  private static void Load(IWorldChunk chunk, EnumChunkDirtyReason reason) =>
    Closed.World.Api.Event.ChunkDirty += Raise.Event<ChunkDirtyDelegate>(
      new Vec3i(0, 0, 0),
      chunk,
      reason
    );

  private static Block StandIn(string code, int id) =>
    TestBlocks.Configure(new Block(), code, id);
}
