using ExpandedLib.Industry.MechanicalPower;
using ExpandedLib.Registries;
using Vintagestory.API.Common;

namespace SteelmakingExpanded.BlockStructures.BlastFurnace.BlockEntities;

/// <summary>
/// The twin-tub blower's mechanical-power port: the footprint port whose load the blower sets from
/// its own tick, since the bellows draw in proportion to the pressure they raise in the main.
/// </summary>
[BlockEntityBehaviorRegister]
public class BEBehaviorMpBlowerPort(BlockEntity blockentity)
  : BEBehaviorMPFillerPort(blockentity) {
  private float? _liveLoad;

  /// <summary>
  /// Overrides the declared resistance with what the bellows currently draw. Null or a negative
  /// value falls back to the declared figure. <see cref="GetResistance"/> is read by the network
  /// solver every tick and must not go looking for the blower's state itself.
  /// </summary>
  public void SetLoad(float? load) =>
    _liveLoad = load is { } l && l >= 0f ? l : null;

  /// <summary>The live load when the blower has set one, else the declared resistance.</summary>
  public override float GetResistance() => _liveLoad ?? base.GetResistance();
}
