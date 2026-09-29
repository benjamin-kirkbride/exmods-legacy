using System;
using System.Collections.Generic;
using ExpandedLib.Industry.Helpers;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Testing;
using NSubstitute;
using PipesAndPowerExpanded.BlockStructures.Engine.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Xunit;

namespace PipesAndPowerExpanded.Tests;

/// <summary>
/// The MP generator's gear-train loop on a client: the gearbox clip at 0.2, played as a Sound so the
/// Effects slider governs it, scaled by <see cref="ExSounds.MachineVolume"/> as <c>.exmod sound</c>
/// changes it, and released when the generator is removed or unloaded.
/// </summary>
public class MpGeneratorSoundTests : IDisposable {
  public void Dispose() => ExSounds.MachineVolume = 1f;

  // Fails when the grind loads a clip other than the gearbox turn, at a volume other than 0.2, as a
  // type other than Sound, or when a running grind is no longer updated and so keeps the volume it
  // loaded with.
  [Fact]
  public void The_grind_is_the_gearbox_clip_and_follows_the_machine_volume() {
    var (generator, loaded, made) = TurningGenerator();
    ExSounds.MachineVolume = 0.5f;

    ReflectionHelpers.Invoke(generator, "UpdateGrindSound", true);
    ExSounds.MachineVolume = 0.2f;
    ReflectionHelpers.Invoke(generator, "UpdateGrindSound", true);

    SoundParams grind = Assert.Single(made);
    Assert.Equal(ExSounds.GearboxTurn, grind.Location);
    Assert.Equal(0.1f, grind.Volume, 3);
    Assert.Equal(EnumSoundType.Sound, grind.SoundType);
    Assert.True(grind.ShouldLoop);
    loaded[0]
      .Received(1)
      .SetVolume(Arg.Is<float>(v => Math.Abs(v - 0.04f) < 1e-4f));
    loaded[0].Received(1).Start();
  }

  // Fails when the named hook leaves the grind loaded.
  [Theory]
  [InlineData("OnBlockRemoved")]
  [InlineData("OnBlockUnloaded")]
  public void Removal_and_unload_release_the_grind(string hook) {
    var (generator, loaded, _) = TurningGenerator();
    ReflectionHelpers.Invoke(generator, "UpdateGrindSound", true);

    ReflectionHelpers.Invoke(generator, hook);

    Assert.Single(loaded).Received(1).Dispose();
  }

  /// <summary>A formed generator whose client API records every sound it loads.</summary>
  private static (
    BlockEntityEngineMpGenerator generator,
    List<ILoadedSound> loaded,
    List<SoundParams> made
  ) TurningGenerator() {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    var plant = new MpGeneratorPlant(scene, new BlockPos(0, 8, 0));
    scene.Build();

    var loaded = new List<ILoadedSound>();
    var made = new List<SoundParams>();
    ICoreClientAPI capi = Substitute.For<ICoreClientAPI>();
    capi.Side.Returns(EnumAppSide.Client);
    IClientWorldAccessor world = Substitute.For<IClientWorldAccessor>();
    capi.World.Returns(world);
    world
      .LoadSound(Arg.Any<SoundParams>())
      .Returns(ci => {
        made.Add(ci.Arg<SoundParams>());
        ILoadedSound sound = Substitute.For<ILoadedSound>();
        bool playing = false;
        sound.IsPlaying.Returns(_ => playing);
        sound.When(s => s.Start()).Do(_ => playing = true);
        sound.When(s => s.Stop()).Do(_ => playing = false);
        loaded.Add(sound);
        return sound;
      });
    ReflectionHelpers.SetField(plant.Generator, "_capi", capi);
    return (plant.Generator, loaded, made);
  }
}
