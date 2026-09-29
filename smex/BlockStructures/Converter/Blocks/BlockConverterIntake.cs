using System.Linq;
using ExpandedLib.Networks;
using ExpandedLib.Helpers;
using ExpandedLib.Industry.MechanicalPower;
using ExpandedLib.Registries;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SteelmakingExpanded.BlockStructures.Converter.Blocks;

/// <summary>
/// The converter's blast intake: a fixed structure port (not a network node) that
/// exposes a single pipe connector on the face it is turned to face. A pipe run
/// docks against that connector and the <see cref="BlockEntities.BlockEntityConverterControl"/>
/// reads/consumes blast from the network on the other side of it. Horizontally
/// orientable so it can be aligned with the control block.
/// </summary>
[BlockRegister]
public partial class BlockConverterIntake
  : Block,
    INetworkConnector,
    IWrenchOrientable {
  public string NetworkType => "pipe";

  /// <summary>
  /// The single horizontal face that carries the pipe connector, derived from the
  /// block's <c>side</c> variant (north → north, rotated for the other sides).
  /// </summary>
  public BlockFacing ConnectorFace =>
    ExOrientation.RotateFacing(
      BlockFacing.NORTH,
      ExOrientation.AngleFromSide(Variant["side"])
    );

  public bool HasConnectorAt(BlockFacing face) => face == ConnectorFace;

  public override bool CanAttachBlockAt(
    IBlockAccessor world,
    Block block,
    BlockPos pos,
    BlockFacing blockFace,
    Cuboidi attachmentArea
  ) => HasConnectorAt(blockFace) || SideSolid[blockFace.Index];

  /// <summary>Turns the intake in place a quarter turn per step of <paramref name="dir"/> (see
  /// <see cref="SideWrench.Turn"/>).</summary>
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
