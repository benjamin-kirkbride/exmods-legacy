using System;
using ExpandedLib.Networks;
using ExpandedLib.Testing;
using Newtonsoft.Json.Linq;
using NSubstitute;
using PipesAndPowerExpanded;
using SteelmakingExpanded;
using Vintagestory.API.Common;
using Xunit;

namespace Integration.Tests.Networks;

/// <summary>
/// ppex and smex run on the "pipe" and "molten" networks Industry registers and register no network
/// type of their own, so their load order never replaces Industry's factories. Red with smex's
/// <c>Start</c> registering "molten" again.
/// </summary>
public class NetworkTypeTests {
  [Theory]
  [InlineData("ppex")]
  [InlineData("smex")]
  public void Starting_the_mod_registers_no_network_type(string modId) {
    var networks = new BlockNetworkModSystem();
    ICoreAPI api = ClientApi(modId, networks);
    ModSystem system =
      modId == "ppex"
        ? new PipesAndPowerExpandedModSystem()
        : new SteelmakingExpandedModSystem();
    ReflectionHelpers.SetProperty(
      system,
      nameof(ModSystem.Mod),
      api.ModLoader.GetMod(modId)
    );

    try {
      system.Start(api);
    } finally {
      system.Dispose();
    }

    Assert.Empty(networks.RegisteredNetworkTypes);
  }

  #region Helpers

  /// <summary>A client API with no config on disk whose mod loader hands out
  /// <paramref name="networks"/> and a mod <paramref name="modId"/>.</summary>
  private static ICoreAPI ClientApi(string modId, BlockNetworkModSystem networks) {
    var api = Substitute.For<ICoreAPI>();
    api.Side.Returns(EnumAppSide.Client);
    api.Logger.Returns(Substitute.For<ILogger>());
    api.LoadModConfig<JObject>(Arg.Any<string>()).Returns((JObject?)null);
    var loader = Substitute.For<IModLoader>();
    var mod = Substitute.For<Mod>();
    typeof(Mod)
      .GetProperty("Info")!
      .SetValue(mod, new ModInfo { ModID = modId, Version = "0.0.0" });
    loader.GetMod(modId).Returns(mod);
    loader.GetModSystem<BlockNetworkModSystem>(Arg.Any<bool>()).Returns(networks);
    api.ModLoader.Returns(loader);
    return api;
  }

  #endregion
}
