using System.Collections.Generic;
using ExpandedLib.Industry.Helpers;
using ExpandedLib.Testing;
using NSubstitute;
using PipesAndPowerExpanded.Tests;
using SteelmakingExpanded;
using SteelmakingExpanded.BlockStructures.BlastFurnace;
using SteelmakingExpanded.BlockStructures.BlastFurnace.BlockEntities;
using SteelmakingExpanded.BlockStructures.Converter.BlockEntities;
using SteelmakingExpanded.BlockStructures.CowperStove.BlockEntities;
using SteelmakingExpanded.BlockStructures.SmokeStack.BlockEntities;
using SteelmakingExpanded.Tests;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Xunit;
using BoilerState = PipesAndPowerExpanded.BlockStructures.Boiler.BlockEntityBoiler.BoilerState;

namespace Integration.Tests;

/// <summary>
/// The fire ambience of the blast furnace, the converter, the cowper stove, the smoke stack and the
/// boiler on a client: each machine loads its loop once, starts it when the state synced from the
/// server says it burns and stops it when it goes out, however often it is relit. A machine's
/// client copy here is fed the tree its server copy writes, as the game syncs a block entity.
/// </summary>
public class FireAmbienceSoundTests {
  #region Blast furnace

  // Fails when the furnace loads a new fire loop on each relight instead of restarting its one
  // loop, or when the loop no longer follows the synced state in or out.
  [Fact]
  public void A_blast_furnace_put_out_and_relit_ten_times_keeps_one_fire_loop() {
    var rig = new BlastFurnaceRig();
    var client = new RecordingClient();
    BlockEntityBlastFurnace furnace = ClientFurnace(rig, client);

    for (int i = 0; i < 10; i++) {
      rig.RelightHearth().FeedBlast().Tick();
      Assert.Equal(BlastFurnaceState.Firing, rig.State);
      Sync(rig.Furnace, furnace, rig.World);
      Assert.True(Assert.Single(client.Loaded).IsPlaying);

      // The tuyere runs hold their blast for about fifteen ticks after the blowers stop, and the
      // disruption grace runs from then.
      rig.CutBlast().Tick((int)SmexValues.BfDisruptionGraceSeconds * 2);
      Assert.Equal(BlastFurnaceState.Idle, rig.State);
      Sync(rig.Furnace, furnace, rig.World);
      Assert.False(client.Loaded[0].IsPlaying);
    }

    SoundParams fire = Assert.Single(client.Made);
    Assert.Equal(ExSounds.Fire, fire.Location);
    Assert.Equal(0.6f, fire.Volume, 3);
    Assert.Equal(32f, fire.Range);
    Assert.True(fire.ShouldLoop);
    client.Loaded[0].Received(10).Start();
    client.Loaded[0].Received(10).Stop();
  }

  // Fails when the named hook leaves the furnace's fire loaded.
  [Theory]
  [InlineData("OnBlockRemoved")]
  [InlineData("OnBlockUnloaded")]
  public void Removal_and_unload_release_the_furnace_fire(string hook) {
    var rig = new BlastFurnaceRig().FeedBlast();
    var client = new RecordingClient();
    BlockEntityBlastFurnace furnace = ClientFurnace(rig, client);
    rig.Tick();
    Sync(rig.Furnace, furnace, rig.World);

    ReflectionHelpers.Invoke(furnace, hook);

    Assert.Single(client.Loaded).Received(1).Dispose();
  }

  #endregion

  #region Converter, cowper stove, smoke stack

  // Fails when a refining tick no longer marks the vessel as blowing, when a later tick that does
  // not refine leaves the mark standing, when the mark is not synced, or when either blow loop is
  // loaded again for the second heat.
  [Fact]
  public void A_converter_blows_with_one_embers_loop_and_one_fire_loop() {
    var rig = new ConverterRig();
    ReflectionHelpers.SetProperty(rig.Control, "StructureComplete", true);
    var client = new RecordingClient();
    var control = new BlockEntityConverterControl {
      Pos = rig.Control.Pos.Copy(),
      Block = rig.Control.Block,
      Api = client.Api,
    };

    for (int heat = 0; heat < 2; heat++) {
      rig.PourIronToInput(50).Fill().ChargeBlast(3f).Refine();
      Sync(rig.Control, control, rig.World);
      Assert.All(client.Loaded, s => Assert.True(s.IsPlaying));

      // The rig's vessel is not built, so the production tick stops short of refining.
      ReflectionHelpers.Invoke(rig.Control, "OnProductionTick", 1f);
      Sync(rig.Control, control, rig.World);
      Assert.All(client.Loaded, s => Assert.False(s.IsPlaying));
    }

    Assert.Equal(2, client.Made.Count);
    Assert.Equal(ExSounds.Embers, client.Made[0].Location);
    Assert.Equal(0.5f, client.Made[0].Volume, 3);
    Assert.Equal(ExSounds.Fire, client.Made[1].Location);
    // Asked for at 1.5; SoundParams caps a volume at 1.
    Assert.Equal(1f, client.Made[1].Volume, 3);
    Assert.All(client.Made, p => Assert.Equal(24f, p.Range));
    foreach (ILoadedSound sound in client.Loaded) {
      sound.Received(2).Start();
      sound.Received(2).Stop();
    }
  }

  // Fails when soaking up exhaust no longer marks the stove, when a tick without exhaust leaves the
  // mark standing, when the mark is not synced, or when the roar is loaded again for the second charge.
  [Fact]
  public void A_cowper_stove_roars_with_one_loop_while_it_soaks_up_exhaust() {
    var rig = new CowperRig();
    var client = new RecordingClient();
    var stove = new BlockEntityCowperStove {
      Pos = rig.Stove.Pos.Copy(),
      Block = rig.Stove.Block,
      Api = client.Api,
    };

    for (int charge = 0; charge < 2; charge++) {
      rig.ChargeFromExhaust(1200f);
      Sync(rig.Stove, stove, rig.World);
      Assert.True(Assert.Single(client.Loaded).IsPlaying);

      rig.CutExhaust();
      Sync(rig.Stove, stove, rig.World);
      Assert.False(client.Loaded[0].IsPlaying);
    }

    SoundParams roar = Assert.Single(client.Made);
    Assert.Equal(ExSounds.Fire, roar.Location);
    Assert.Equal(0.4f, roar.Volume, 3);
    Assert.Equal(24f, roar.Range);
    client.Loaded[0].Received(2).Start();
    client.Loaded[0].Received(2).Stop();
  }

  // Fails when a vessel that stops blowing on a tick that leaves its status as it was is not
  // marked for sync.
  [Fact]
  public void A_converter_that_stops_blowing_is_marked_for_sync() {
    var rig = new ConverterRig();
    ReflectionHelpers.SetField(
      rig.Control,
      "_status",
      Lang.Get("smex:bessemer-status-notbuilt")
    );
    ReflectionHelpers.SetField(rig.Control, "_blowing", true);
    rig.World.Accessor.ClearReceivedCalls();

    ReflectionHelpers.Invoke(rig.Control, "OnProductionTick", 1f);

    Assert.False((bool)ReflectionHelpers.GetField(rig.Control, "_blowing")!);
    rig.World.Accessor.Received(1).MarkBlockEntityDirty(rig.Control.Pos);
  }

  // Fails when a stove that stops soaking on a tick that leaves its status as it was is not marked
  // for sync.
  [Fact]
  public void A_cowper_stove_that_stops_soaking_is_marked_for_sync() {
    var rig = new CowperRig();
    ReflectionHelpers.SetField(
      rig.Stove,
      "_lastStatus",
      Lang.Get("smex:cowperstove-status-idle")
    );
    ReflectionHelpers.SetField(rig.Stove, "_soaking", true);
    rig.World.Accessor.ClearReceivedCalls();

    rig.CutExhaust();

    Assert.False((bool)ReflectionHelpers.GetField(rig.Stove, "_soaking")!);
    rig.World.Accessor.Received(1).MarkBlockEntityDirty(rig.Stove.Pos);
  }

  // Fails when the draught no longer follows the synced draw, or is loaded again when the stack
  // draws a second time.
  [Fact]
  public void A_smoke_stack_vents_with_one_loop_while_it_draws() {
    var (world, net) = PipeTestWorld.Run(4, capEnds: true);
    BlockEntitySmokeStack server = DrawingStack(world);
    var client = new RecordingClient();
    var stack = new BlockEntitySmokeStack {
      Pos = server.Pos.Copy(),
      Block = server.Block,
      Api = client.Api,
    };

    for (int draw = 0; draw < 2; draw++) {
      net.TryProduceGas(
        200f,
        400f,
        "Exhaust",
        world.Accessor,
        maxOutputPressure: 10f
      );
      ReflectionHelpers.Invoke(server, "OnProductionTick", 1f);
      Sync(server, stack, world);
      Assert.True(Assert.Single(client.Loaded).IsPlaying);

      net.TryConsumeGas(float.MaxValue, world.Accessor);
      ReflectionHelpers.Invoke(server, "OnProductionTick", 1f);
      Sync(server, stack, world);
      Assert.False(client.Loaded[0].IsPlaying);
    }

    SoundParams draught = Assert.Single(client.Made);
    Assert.Equal(ExSounds.Fire, draught.Location);
    Assert.Equal(0.3f, draught.Volume, 3);
    Assert.Equal(32f, draught.Range);
    client.Loaded[0].Received(2).Start();
    client.Loaded[0].Received(2).Stop();
  }

  #endregion

  #region Loading

  // A chunk's tree is read before its block entity has an API, so a machine that loads burning
  // starts its loop from Initialize.

  // Fails when the furnace's Initialize no longer starts the fire of a furnace that loads lit.
  [Fact]
  public void A_blast_furnace_loaded_lit_starts_its_fire_when_initialized() {
    var rig = new BlastFurnaceRig().FeedBlast();
    rig.Tick();
    var client = new RecordingClient();
    var furnace = new BlockEntityBlastFurnace {
      Pos = rig.Furnace.Pos.Copy(),
      Block = rig.Furnace.Block,
    };

    Load(rig.Furnace, furnace, rig.World, client);

    Assert.Equal(ExSounds.Fire, Assert.Single(client.Made).Location);
    Assert.True(Assert.Single(client.Loaded).IsPlaying);
  }

  // Fails when the control's Initialize no longer starts the blow of a vessel that loads blowing.
  [Fact]
  public void A_converter_loaded_blowing_starts_its_blow_when_initialized() {
    var rig = new ConverterRig();
    ReflectionHelpers.SetProperty(rig.Control, "StructureComplete", true);
    rig.PourIronToInput(50).Fill().ChargeBlast(3f).Refine();
    var client = new RecordingClient();
    var control = new BlockEntityConverterControl {
      Pos = rig.Control.Pos.Copy(),
      Block = rig.Control.Block,
    };

    Load(rig.Control, control, rig.World, client);

    Assert.Equal(2, client.Made.Count);
    Assert.All(client.Loaded, s => Assert.True(s.IsPlaying));
  }

  // Fails when the stove's Initialize no longer starts the roar of a stove that loads soaking.
  [Fact]
  public void A_cowper_stove_loaded_soaking_starts_its_roar_when_initialized() {
    var rig = new CowperRig().ChargeFromExhaust(1200f);
    var client = new RecordingClient();
    var stove = new BlockEntityCowperStove {
      Pos = rig.Stove.Pos.Copy(),
      Block = rig.Stove.Block,
    };

    Load(rig.Stove, stove, rig.World, client);

    Assert.Equal(ExSounds.Fire, Assert.Single(client.Made).Location);
    Assert.True(Assert.Single(client.Loaded).IsPlaying);
  }

  // Fails when the stack's Initialize no longer starts the draught of a stack that loads drawing.
  [Fact]
  public void A_smoke_stack_loaded_drawing_starts_its_draught_when_initialized() {
    var (world, net) = PipeTestWorld.Run(4, capEnds: true);
    net.TryProduceGas(
      200f,
      400f,
      "Exhaust",
      world.Accessor,
      maxOutputPressure: 10f
    );
    BlockEntitySmokeStack server = DrawingStack(world);
    ReflectionHelpers.Invoke(server, "OnProductionTick", 1f);
    var client = new RecordingClient();
    var stack = new BlockEntitySmokeStack {
      Pos = server.Pos.Copy(),
      Block = server.Block,
    };

    Load(server, stack, world, client);

    Assert.Equal(ExSounds.Fire, Assert.Single(client.Made).Location);
    Assert.True(Assert.Single(client.Loaded).IsPlaying);
  }

  #endregion

  #region Boiler

  // Fails when the boiler's client tick no longer follows the boiling state, or loads the hum
  // again when the water boils a second time.
  [Fact]
  public void A_boiler_hums_with_one_loop_while_it_boils() {
    var rig = new BoilerRig();
    var client = new RecordingClient();
    rig.Be.Api = client.Api;

    for (int boil = 0; boil < 2; boil++) {
      rig.SetState(BoilerState.Boiling);
      ReflectionHelpers.Invoke(rig.Be, "OnClientTick", 0.25f);
      Assert.True(Assert.Single(client.Loaded).IsPlaying);

      rig.SetState(BoilerState.Idle);
      ReflectionHelpers.Invoke(rig.Be, "OnClientTick", 0.25f);
      Assert.False(client.Loaded[0].IsPlaying);
    }

    SoundParams hum = Assert.Single(client.Made);
    Assert.Equal(ExSounds.Lava, hum.Location);
    Assert.Equal(0.4f, hum.Volume, 3);
    Assert.Equal(16f, hum.Range);
    client.Loaded[0].Received(2).Start();
    client.Loaded[0].Received(2).Stop();
  }

  #endregion

  /// <summary>A client copy of the rig's furnace, at the same place and facing.</summary>
  private static BlockEntityBlastFurnace ClientFurnace(
    BlastFurnaceRig rig,
    RecordingClient client
  ) =>
    new() {
      Pos = rig.Furnace.Pos.Copy(),
      Block = rig.Furnace.Block,
      BaseAngleRad = rig.Furnace.BaseAngleRad,
      Api = client.Api,
    };

  /// <summary>A complete stack at the origin of <paramref name="world"/>, on its pipe network.</summary>
  private static BlockEntitySmokeStack DrawingStack(TestWorld world) {
    var stack = new BlockEntitySmokeStack {
      Pos = new BlockPos(0, 0, 0),
      Block = TestBlocks.Configure(
        new Block(),
        "smex:smokestack-north",
        70,
        ("orientation", "north")
      ),
    };
    world.Attach(stack);
    ReflectionHelpers.SetField(stack, "_system", world.Networks);
    ReflectionHelpers.SetProperty(stack, "StructureComplete", true);
    return stack;
  }

  /// <summary>Loads <paramref name="client"/> from the tree <paramref name="server"/> writes, in
  /// the game's order: the tree is read before the entity is initialised on the client
  /// API.</summary>
  private static void Load(
    BlockEntity server,
    BlockEntity client,
    TestWorld world,
    RecordingClient api
  ) {
    Sync(server, client, world);
    Assert.Empty(api.Made);
    client.Initialize(api.Api);
  }

  /// <summary>Feeds <paramref name="client"/> the tree <paramref name="server"/> writes.</summary>
  private static void Sync(
    BlockEntity server,
    BlockEntity client,
    TestWorld world
  ) {
    var tree = new TreeAttribute();
    server.ToTreeAttributes(tree);
    client.FromTreeAttributes(tree, world.World);
  }

  /// <summary>A client API that records every sound it loads; each loaded sound tracks whether it
  /// is playing.</summary>
  private sealed class RecordingClient {
    public readonly ICoreClientAPI Api = Substitute.For<ICoreClientAPI>();
    public readonly List<ILoadedSound> Loaded = [];
    public readonly List<SoundParams> Made = [];

    public RecordingClient() {
      Api.Side.Returns(EnumAppSide.Client);
      IClientWorldAccessor world = Substitute.For<IClientWorldAccessor>();
      Api.World.Returns(world);
      world
        .LoadSound(Arg.Any<SoundParams>())
        .Returns(ci => {
          Made.Add(ci.Arg<SoundParams>());
          ILoadedSound sound = Substitute.For<ILoadedSound>();
          bool playing = false;
          sound.IsPlaying.Returns(_ => playing);
          sound.When(s => s.Start()).Do(_ => playing = true);
          sound.When(s => s.Stop()).Do(_ => playing = false);
          Loaded.Add(sound);
          return sound;
        });
    }
  }
}
