using ExpandedLib.Industry.Pipes;
using ExpandedLib.Testing;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Xunit;
using BoilerState = PipesAndPowerExpanded.BlockStructures.Boiler.BlockEntityBoiler.BoilerState;

namespace PipesAndPowerExpanded.Tests;

/// <summary>
/// A boiler's fire draws only through a chimney or a stack on its exhaust run. Open pipe ends
/// carry exhaust away without giving draught, so a fire with nothing else chokes.
/// </summary>
public class BoilerDraughtTests {
  #region Fixtures

  private const int OutletId = 96;
  private const int ChimneyId = 97;
  private const int JunctionId = 95;

  /// <summary>A fired Cornish boiler with the fireclay outlet on its exhaust cell, open on top.</summary>
  private sealed class Plant {
    public readonly Scene Scene = new Scene().Network(
      "pipe",
      s => new PipeNetwork(s)
    );
    public readonly BoilerFixture Boiler;
    public readonly BlockPos Outlet;

    public Plant() {
      Boiler = new BoilerFixture(Scene, new BlockPos(0, 8, 0));
      Outlet = Boiler.Block.ExhaustOutletWorldPos(Boiler.Be.Pos);
      var outlet = new BlockEntityPipeOutlet();
      Scene.World.Place(Outlet, PpexScenes.UpOutlet(OutletId), outlet);
      Scene.World.Initialize(outlet);
    }

    /// <summary>A chimney standing on the outlet.</summary>
    public Plant WithChimney() {
      Scene.Block(Outlet.UpCopy(), PpexScenes.Chimney(ChimneyId));
      return this;
    }

    /// <summary>A pipe on the outlet open to air on its five other faces.</summary>
    public Plant WithOpenJunction() {
      EnginePlant.Pipe(Scene, Outlet.UpCopy(), "udnsew", JunctionId);
      return this;
    }

    public void Fire(int seconds) {
      Scene.Build();
      Boiler.Prime(BoilerState.Boiling, water: 600f, steam: 0f);
      Scene.Step(seconds);
    }

    public bool Choked =>
      (bool)ReflectionHelpers.GetField(Boiler.Be, "_choked")!;

    public bool Burning =>
      (bool)ReflectionHelpers.GetField(Boiler.Be, "_burning")!;

    public bool PileBurning =>
      Scene
        .EntityAt<BlockEntityCoalPile>(
          Boiler.Block.FuelWorldPos(Boiler.Be.Pos)
        )!
        .IsBurning;
  }

  #endregion

  #region Draught

  // Fails when the fire's draught is judged by the exhaust back-pressure alone.
  [Fact]
  public void Open_pipe_ends_alone_choke_the_fire() {
    var plant = new Plant().WithOpenJunction();

    plant.Fire(3);

    Assert.True(plant.Choked, "open ends gave the fire draught");
    Assert.False(plant.Burning);
  }

  // Fails when the boiler never finds draught on its exhaust run.
  [Fact]
  public void A_chimney_on_the_outlet_keeps_the_fire_drawing() {
    var plant = new Plant().WithChimney();

    plant.Fire(3);

    Assert.False(plant.Choked, "a chimney left the fire choked");
    Assert.True(plant.Burning);
  }

  // Fails when a choked boiler never puts its pile out, or when the pile has no world to play its
  // snuff through.
  [Fact]
  public void A_choked_fire_is_snuffed_after_its_grace() {
    var plant = new Plant().WithOpenJunction();

    plant.Fire(11);

    Assert.False(plant.PileBurning, "the choked pile still burns");
  }

  #endregion
}
