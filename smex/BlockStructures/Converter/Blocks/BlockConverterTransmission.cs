using System.Linq;
using ExpandedLib.Registries;
using SteelmakingExpanded.BlockStructures.Converter.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace SteelmakingExpanded.BlockStructures.Converter.Blocks;

/// <summary>
/// Mechanical-power intake for the converter. Couples an axle on the south face
/// in its natural (north) orientation; the connector follows the "side" variant.
/// </summary>
[BlockRegister]
public partial class BlockConverterTransmission
  : Block,
    IMechanicalPowerBlock,
    IWrenchOrientable {
  private BlockFacing ConnectorFace =>
    Variant["side"] switch {
      "north" => BlockFacing.SOUTH,
      "east" => BlockFacing.WEST,
      "south" => BlockFacing.NORTH,
      "west" => BlockFacing.EAST,
      _ => BlockFacing.SOUTH,
    };

  /// <summary>Accepts an axle only on the connector face derived from the block's orientation.</summary>
  public bool HasMechPowerConnectorAt(
    IWorldAccessor world,
    BlockPos pos,
    BlockFacing face
#if GAME_GE_1_22
    ,
    BlockMPBase forBlock
#endif
  ) => face == ConnectorFace;

  public void DidConnectAt(
    IWorldAccessor world,
    BlockPos pos,
    BlockFacing face
  ) { }

  /// <summary>Returns the mechanical-power network driving the converter, via the transmission's MP behavior.</summary>
  public MechanicalNetwork? GetNetwork(IWorldAccessor world, BlockPos pos) =>
    world
      .BlockAccessor.GetBlockEntity(pos)
      ?.GetBehavior<BEBehaviorMPConverterTransmission>()
      ?.Network;

  public override void OnNeighbourBlockChange(
    IWorldAccessor world,
    BlockPos pos,
    BlockPos neighbour
  ) {
    base.OnNeighbourBlockChange(world, pos, neighbour);
  }

  /// <summary>Turns the transmission a quarter turn per step of <paramref name="dir"/> (see
  /// <see cref="SideWrench.Turn"/>), keeping its block entity, which re-couples its axle on the
  /// new connector face (<see cref="BlockEntityConverterTransmission.OnExchanged"/>).</summary>
  public void Rotate(
    EntityAgent byEntity,
    BlockSelection blockSel,
    int dir
  ) => SideWrench.Turn(this, byEntity, blockSel, dir);

  /// <summary>Appends the wrench turn to the placed-block help.</summary>
  public override WorldInteraction[] GetPlacedBlockInteractionHelp(
    IWorldAccessor world,
    BlockSelection selection,
    IPlayer forPlayer
  ) =>
    (base.GetPlacedBlockInteractionHelp(world, selection, forPlayer) ?? [])
      .Append(SideWrench.Help(world))
      .ToArray();
}
