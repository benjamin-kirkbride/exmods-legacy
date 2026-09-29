using ExpandedLib.Registries;
using Vintagestory.API.Common;

namespace SteelmakingExpanded.BlockStructures.Converter.BlockEntities;

/// <summary>Block entity for the bessemer transmission; carries the mechanical power that drives the converter (see <see cref="BEBehaviorMPConverterTransmission"/>).</summary>
[BlockEntityRegister]
public class BlockEntityConverterTransmission : BlockEntity {
  /// <summary>Takes the exchanged block and re-couples the axle on its connector face (see
  /// <see cref="BEBehaviorMPConverterTransmission.Recouple"/>).</summary>
  public override void OnExchanged(Block block) {
    base.OnExchanged(block);
    GetBehavior<BEBehaviorMPConverterTransmission>()?.Recouple();
  }
}
