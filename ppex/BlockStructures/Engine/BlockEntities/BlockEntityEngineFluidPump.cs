using System;
using ExpandedLib;
using ExpandedLib.Helpers;
using ExpandedLib.Industry.Helpers;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Registries;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace PipesAndPowerExpanded.BlockStructures.Engine.BlockEntities;

/// <summary>
/// Engine sub-machine: a water pump. The pump is not the source - the fluid intake is the
/// generator. While powered it makes an intake on the bottom (source) network produce water and
/// transfers the same volume into the left (water-line) network at a pressure proportional to the
/// engine's inlet steam. With no intake it still runs but moves nothing.
/// </summary>
[BlockEntityRegister]
public class BlockEntityEngineFluidPump : BlockEntityEngineSubmachine {
  /// <summary>True while the pump has an active intake on its source line and is moving water;
  /// synced to clients to drive the water-drawing loop sound.</summary>
  private bool _drawingWater;

  private readonly ExSoundLoop _waterSound = new(ExSounds.Watering, 0.6f);

  protected override string? OutputInfo(float power) =>
    Lang.Get(
      _drawingWater
        ? "ppex:enginefluidpump-info-pumping"
        : "ppex:enginefluidpump-info-nointake",
      ExMeasure.FlowRate(PpexValues.PumpWaterPerSecond * power),
      ExMeasure.Pressure(
        (Engine?.InletPressure ?? 0f) * PpexValues.SteamEngineEfficiency
      )
    );

  protected override void DoWork(float power, float dt) {
    WaterLine.Hold(this, null, 0f);
    if (power <= 0f) {
      SetDrawing(false);
      return;
    }

    var ba = Api.World.BlockAccessor;
    PipeNetwork? bottomNet = ConnectedNetwork(BlockFacing.DOWN);
    PipeNetwork? leftNet = ConnectedNetwork(LeftFace);

    BlockEntityFluidIntake? intake = FindIntake(bottomNet);
    SetDrawing(intake != null);
    if (intake == null)
      return;

    float pressure =
      (Engine?.InletPressure ?? 0f) * PpexValues.SteamEngineEfficiency;
    float amount = PpexValues.PumpWaterPerSecond * power * dt;

    float move = Math.Min(amount, OutputFreeCapacity(leftNet));
    float drawn = bottomNet?.TryConsumeLiquid(move, ba) ?? 0f;
    if (drawn > 0f)
      leftNet?.TryProduceLiquid(drawn, 20f, pressure, ba);
    WaterLine.Hold(this, leftNet, pressure);

    intake.ProduceWater(amount, 20f, ba);
  }

  protected override void OnIdleProductionTick(float dt) =>
    WaterLine.Hold(this, null, 0f);

  /// <summary>Updates the synced water-drawing flag, syncing to clients only on change.</summary>
  private void SetDrawing(bool drawing) {
    if (drawing == _drawingWater)
      return;
    _drawingWater = drawing;
    MarkDirty();
  }

  /// <summary>The first fluid intake on <paramref name="net"/> that can currently draw water, or <c>null</c>.</summary>
  private BlockEntityFluidIntake? FindIntake(PipeNetwork? net) {
    if (net == null)
      return null;
    var ba = Api.World.BlockAccessor;
    foreach (var p in net.Nodes) {
      if (
        ba.GetBlockEntity(p) is BlockEntityFluidIntake intake
        && intake.CanIntake
      )
        return intake;
    }
    return null;
  }

  /// <summary>Litres of water the output network can still accept.</summary>
  private static float OutputFreeCapacity(PipeNetwork? net) =>
    net == null
      ? 0f
      : net.Nodes.Count * ExlibValues.LitresPerPipe - (net.State?.Volume ?? 0f);

  /// <summary>
  /// Runs a watering trickle loop while the pump is actually drawing water, on top of the
  /// shared piston-stroke sounds - the same loop the manual fluid pump uses.
  /// </summary>
  protected override void OnClientStateTick(float dt) =>
    _waterSound.Update(Api, Pos, _drawingWater);

  private void DisposeSounds() => _waterSound.Dispose();

  public override void ToTreeAttributes(ITreeAttribute tree) {
    base.ToTreeAttributes(tree);
    tree.SetBool("drawingWater", _drawingWater);
  }

  public override void FromTreeAttributes(
    ITreeAttribute tree,
    IWorldAccessor worldForResolving
  ) {
    base.FromTreeAttributes(tree, worldForResolving);
    _drawingWater = tree.GetBool("drawingWater");
  }

  public override void OnBlockRemoved() {
    DisposeSounds();
    base.OnBlockRemoved();
  }

  public override void OnBlockUnloaded() {
    DisposeSounds();
    base.OnBlockUnloaded();
  }
}
