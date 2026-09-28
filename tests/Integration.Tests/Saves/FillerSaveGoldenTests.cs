using ExpandedLib.Industry.MechanicalPower;
using ExpandedLib.Structures;
using ExpandedLib.Testing;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Xunit;

namespace Integration.Tests.Saves;

/// <summary>
/// Save goldens for the structure filler, one per kind of cell the ppex and smex megablocks place: a
/// plain cell, a cell that lets blocks attach, a network port cell (the boiler's steam outlet, the
/// blower's blast outlet, the power pump's ports) and the blower's mechanical-power intake, which
/// hosts a behaviour.
/// </summary>
public class FillerSaveGoldenTests {
  private static readonly BlockPos At = new(0, 8, 0);

  private static readonly BlockPos Principal = new(1, 8, -2);

  #region Kinds

  [Fact]
  public void A_plain_filler_restores_its_principal() =>
    Verify(
      "exlib-structurefiller-plain",
      filler => { },
      (filler, _) => {
        Assert.False(filler.AllowAttach);
        Assert.Null(filler.PortFace);
        Assert.Null(filler.PortNetworkType);
        Assert.Null(filler.HostedBehaviors);
      }
    );

  [Fact]
  public void An_attachable_filler_restores_its_attach_flag() =>
    Verify(
      "exlib-structurefiller-attach",
      filler => filler.AllowAttach = true,
      (filler, _) => {
        Assert.True(filler.AllowAttach);
        Assert.Null(filler.PortFace);
        Assert.Null(filler.HostedBehaviors);
      }
    );

  [Fact]
  public void A_steam_port_filler_restores_its_port() =>
    Verify(
      "exlib-structurefiller-port",
      filler => {
        filler.AllowAttach = true;
        filler.PortFace = "u";
        filler.PortNetworkType = "pipe";
      },
      (filler, _) => {
        Assert.True(filler.AllowAttach);
        Assert.Equal("u", filler.PortFace);
        Assert.Equal("pipe", filler.PortNetworkType);
        Assert.Null(filler.HostedBehaviors);
      }
    );

  [Fact]
  public void The_blower_intake_filler_restores_its_hosted_port() =>
    Verify(
      "exlib-structurefiller-mpport",
      filler => {
        filler.AllowAttach = true;
        filler.HostedBehaviors = [
          new FillerBehavior(
            "exlib.BEBehaviorMPFillerPort",
            BlockFacing.EAST,
            new JsonObject(JObject.Parse("{ \"resistance\": 0.05 }"))
          ),
        ];
      },
      (filler, world) => {
        Assert.True(filler.AllowAttach);
        FillerBehavior hosted = Assert.Single(filler.HostedBehaviors!);
        Assert.Equal("exlib.BEBehaviorMPFillerPort", hosted.Code);
        Assert.Equal(
          typeof(BEBehaviorMPFillerPort),
          world.Api.ClassRegistry.GetBlockEntityBehaviorClass(hosted.Code)
        );
        Assert.Equal(BlockFacing.EAST, hosted.ConnectorFace);
        Assert.Equal(0.05f, hosted.Properties!["resistance"].AsFloat(), 3);
      },
      initialize: false
    );

  #endregion

  #region Helpers

  /// <summary>
  /// A filler cell at <see cref="At"/> linked to <see cref="Principal"/>, shaped by
  /// <paramref name="prime"/>; the load must restore the link and whatever
  /// <paramref name="check"/> asserts on it in the world it was loaded into. A cell hosting a
  /// mechanical-power behaviour is loaded without <c>Initialize</c>, which would join it to a live
  /// power network, so its hosted behaviour is resolved by class code only in <paramref name="check"/>.
  /// </summary>
  private static void Verify(
    string name,
    System.Action<BlockEntityStructureFiller> prime,
    System.Action<BlockEntityStructureFiller, TestWorld> check,
    bool initialize = true
  ) =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = name,
        Block = () =>
          TestBlocks.Configure(new BlockStructureFiller(), "exlib:structurefiller", 1),
        Initialize = initialize,
        Live = (world, block) => {
          var be = new BlockEntityStructureFiller { Principal = Principal.Copy() };
          SaveFixtures.Stand(world, At, block, be, initialize: false);
          prime(be);
          return be;
        },
        Check = (be, world) => {
          var filler = Assert.IsType<BlockEntityStructureFiller>(be);
          Assert.Equal(Principal, filler.Principal);
          check(filler, world);
        },
      }
    );

  #endregion
}
