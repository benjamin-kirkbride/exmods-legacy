using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandedLib.Config;
using ExpandedLib.Networks;
using ExpandedLib.Testing;
using Integration.Tests.Saves;
using Newtonsoft.Json.Linq;
using NSubstitute;
using PipesAndPowerExpanded;
using SteelmakingExpanded;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Xunit;

namespace Integration.Tests.Config;

/// <summary>
/// The published flat config files a 0.6.8 ppex and a 0.9.8 smex world carries, loaded through the
/// generated accessors as the mods' <c>Start</c> loads them: each folds once into its mod's section
/// of the shared file with its values, and is left beside it as <c>.migrated</c>. Every test points
/// <see cref="GamePaths.DataPath"/> at a fresh folder and loads the four stores back to their
/// defaults after.
/// </summary>
public sealed class ConfigFoldTests : IDisposable {
  private readonly ModConfigFolder _dir = new();

  public void Dispose() {
    ICoreAPI defaults = ClientApi();
    PpexValues.Load(defaults);
    PpexRecipeValues.Load(defaults);
    SmexValues.Load(defaults);
    SmexRecipeValues.Load(defaults);
    _dir.Dispose();
  }

  [Fact]
  public void A_flat_ppex_values_file_folds_into_the_ppex_section_of_ex_values() {
    _dir.Write(
      "ppex_values.json",
      new JObject {
        ["ConfigVersion"] = "0.6.8",
        ["BoilerSteamPerSecond"] = 60.0,
        // The 0.6.7 migration resets this key; a file stamped 0.6.8 keeps it.
        ["PumpWaterPerSecond"] = 55.0,
      }
    );

    PpexValues.Load(ServerApi([]));

    Assert.Equal(60f, PpexValues.BoilerSteamPerSecond);
    Assert.Equal(55f, PpexValues.PumpWaterPerSecond);
    JObject section = _dir.Section("ex_values.json", "ppex");
    Assert.Equal(60f, section["BoilerSteamPerSecond"]!.Value<float>());
    Assert.Equal(ModVersion("ppex"), section["ConfigVersion"]!.Value<string>());
    Assert.False(_dir.Exists("ppex_values.json"));
    Assert.True(_dir.Exists("ppex_values.json.migrated"));
  }

  [Fact]
  public void A_flat_smex_values_file_folds_into_the_smex_section_of_ex_values() {
    _dir.Write(
      "smex_values.json",
      new JObject {
        ["ConfigVersion"] = "0.9.8",
        ["MoltenCooldownSpeed"] = 30.0,
        // The 0.9.5 migration resets this key; a file stamped 0.9.8 keeps it.
        ["HopperCokeRequired"] = 3,
      }
    );

    SmexValues.Load(ServerApi([]));

    Assert.Equal(30f, SmexValues.MoltenCooldownSpeed);
    Assert.Equal(3, SmexValues.HopperCokeRequired);
    JObject section = _dir.Section("ex_values.json", "smex");
    Assert.Equal(30f, section["MoltenCooldownSpeed"]!.Value<float>());
    Assert.Equal(ModVersion("smex"), section["ConfigVersion"]!.Value<string>());
    Assert.False(_dir.Exists("smex_values.json"));
    Assert.True(_dir.Exists("smex_values.json.migrated"));
  }

  [Fact]
  public void A_flat_ppex_recipes_file_folds_into_the_ppex_section_of_ex_recipes() {
    _dir.Write("ppex_recipes.json", Catalogue("0.6.8", "boilercornish-rcc", "ppex:boilercornish-*"));

    PpexRecipeValues.Load(ServerApi([]));

    Assert.Equal(7, CheapPlanks(PpexRecipeValues.Recipes["boilercornish-rcc"]));
    JObject section = _dir.Section("ex_recipes.json", "ppex");
    Assert.Equal(
      7,
      section["Recipes"]!["boilercornish-rcc"]!["Profiles"]!["cheap"]!["Stages"]!["1"]!["plank"]!.Value<int>()
    );
    Assert.False(_dir.Exists("ppex_recipes.json"));
    Assert.True(_dir.Exists("ppex_recipes.json.migrated"));
  }

  [Fact]
  public void A_flat_smex_recipes_file_folds_into_the_smex_section_of_ex_recipes() {
    _dir.Write("smex_recipes.json", Catalogue("0.9.8", "converterbessemer-rcc", "smex:converter-bessemer-*"));

    SmexRecipeValues.Load(ServerApi([]));

    Assert.Equal(7, CheapPlanks(SmexRecipeValues.Recipes["converterbessemer-rcc"]));
    JObject section = _dir.Section("ex_recipes.json", "smex");
    Assert.Equal(
      7,
      section["Recipes"]!["converterbessemer-rcc"]!["Profiles"]!["cheap"]!["Stages"]!["1"]!["plank"]!.Value<int>()
    );
    Assert.False(_dir.Exists("smex_recipes.json"));
    Assert.True(_dir.Exists("smex_recipes.json.migrated"));
  }

  [Fact]
  public void A_tuned_LitresPerPipe_logs_one_notification_naming_it() {
    _dir.Write(
      "ppex_values.json",
      new JObject {
        ["ConfigVersion"] = "0.6.8",
        ["LitresPerPipe"] = 45.0,
        ["GasLeakRate"] = 8.0,
      }
    );
    var first = new List<string>();

    ICoreAPI api = ServerApi(first);
    PpexRemovedTunables.Report(api);
    PpexValues.Load(api);

    string note = Assert.Single(first, n => n.Contains("LitresPerPipe"));
    Assert.Contains("45", note);
    Assert.Contains("exlib", note);
    Assert.DoesNotContain(first, n => n.Contains("GasLeakRate"));

    // The load saved the section without the key, so the next load has nothing to report.
    var second = new List<string>();
    api = ServerApi(second);
    PpexRemovedTunables.Report(api);
    PpexValues.Load(api);
    Assert.DoesNotContain(second, n => n.Contains("LitresPerPipe"));
    Assert.Null(_dir.Section("ex_values.json", "ppex")["LitresPerPipe"]);
  }

#if GAME_GE_1_21
  // Start applies ppex's Harmony patches; 1.20's Harmony cannot load its native helper on a host
  // that refuses an executable stack.
  [Fact]
  public void Starting_ppex_reports_a_LitresPerPipe_tuned_in_the_flat_file_it_folds() {
    _dir.Write(
      "ppex_values.json",
      new JObject { ["ConfigVersion"] = "0.6.8", ["LitresPerPipe"] = 45.0 }
    );
    var notes = new List<string>();
    ICoreAPI api = ServerApi(notes);
    api.ModLoader.GetModSystem<BlockNetworkModSystem>(Arg.Any<bool>())
      .Returns(new BlockNetworkModSystem());
    var system = new PipesAndPowerExpandedModSystem();
    ReflectionHelpers.SetProperty(system, nameof(ModSystem.Mod), api.ModLoader.GetMod("ppex"));

    try {
      system.Start(api);
    } finally {
      system.Dispose();
    }

    Assert.Single(notes, n => n.Contains("LitresPerPipe"));
    Assert.True(_dir.Exists("ppex_values.json.migrated"));
  }
#endif

  [Fact]
  public void The_values_configs_of_ppex_and_smex_are_managed_from_ex_values() {
    ICoreAPI api = ServerApi([]);
    PpexValues.Load(api);
    SmexValues.Load(api);

    Assert.True(ExConfigProfiles.TryGet("ppex", out IExConfigAccess ppex));
    Assert.Equal("ex_values.json", ppex.FileName);
    Assert.True(ExConfigProfiles.TryGet("smex", out IExConfigAccess smex));
    Assert.Equal("ex_values.json", smex.FileName);
  }

  #region Helpers

  /// <summary>A recipe catalogue file stamped <paramref name="version"/> holding one entry whose
  /// cheap profile costs 7 planks at stage 1.</summary>
  private static JObject Catalogue(string version, string key, string match) =>
    new() {
      ["ConfigVersion"] = version,
      ["Recipes"] = new JObject {
        [key] = new JObject {
          ["Type"] = "rcc",
          ["Match"] = match,
          ["Profiles"] = new JObject {
            ["cheap"] = new JObject {
              ["Stages"] = new JObject { ["1"] = new JObject { ["plank"] = 7 } },
            },
          },
        },
      },
    };

  private static int CheapPlanks(ExpandedLib.Registries.RecipeCostEntry entry) =>
    entry.Profiles["cheap"].Stages!["1"]["plank"];

  /// <summary>The version the shipped modinfo of <paramref name="modId"/> carries.</summary>
  private static string ModVersion(string modId) =>
    JObject
      .Parse(File.ReadAllText(Path.Combine(SaveGoldens.RepoRoot(), modId, "modinfo.json")))["version"]!
      .Value<string>()!;

  /// <summary>
  /// A server API over the test's config folder: <c>LoadModConfig</c> and <c>StoreModConfig</c>
  /// read and write its files, ppex and smex report their shipped versions, and every Notification
  /// is formatted into <paramref name="notes"/>.
  /// </summary>
  private ICoreAPI ServerApi(List<string> notes) {
    var api = Substitute.For<ICoreAPI>();
    api.Side.Returns(EnumAppSide.Server);
    var logger = Substitute.For<ILogger>();
    logger
      .When(l => l.Notification(Arg.Any<string>(), Arg.Any<object[]>()))
      .Do(ci => notes.Add(string.Format(ci.ArgAt<string>(0), ci.ArgAt<object[]>(1))));
    api.Logger.Returns(logger);
    api.LoadModConfig<JObject>(Arg.Any<string>())
      .Returns(ci => _dir.Read(ci.ArgAt<string>(0)));
    api.When(a => a.StoreModConfig(Arg.Any<JObject>(), Arg.Any<string>()))
      .Do(ci => _dir.Write(ci.ArgAt<string>(1), ci.ArgAt<JObject>(0)));
    var loader = Substitute.For<IModLoader>();
    foreach (string modId in new[] { "ppex", "smex" }) {
      var mod = Substitute.For<Mod>();
      typeof(Mod)
        .GetProperty("Info")!
        .SetValue(mod, new ModInfo { ModID = modId, Version = ModVersion(modId) });
      loader.GetMod(modId).Returns(mod);
    }
    api.ModLoader.Returns(loader);
    return api;
  }

  /// <summary>A client API with no config on disk: a load through it restores coded defaults and
  /// writes nothing.</summary>
  private static ICoreAPI ClientApi() {
    var api = Substitute.For<ICoreAPI>();
    api.Side.Returns(EnumAppSide.Client);
    api.Logger.Returns(Substitute.For<ILogger>());
    api.LoadModConfig<JObject>(Arg.Any<string>()).Returns((JObject?)null);
    return api;
  }

  /// <summary>Points <see cref="GamePaths.DataPath"/> at a fresh folder holding an empty
  /// <c>ModConfig</c>; <see cref="Dispose"/> restores the previous path and deletes the folder.</summary>
  private sealed class ModConfigFolder : IDisposable {
    private readonly string _previous = GamePaths.DataPath;
    private readonly string _root = System.IO.Path.Combine(
      System.IO.Path.GetTempPath(),
      "legacy_cfgfold_" + Guid.NewGuid().ToString("N")
    );

    public ModConfigFolder() {
      GamePaths.DataPath = _root;
      Directory.CreateDirectory(GamePaths.ModConfig);
    }

    public string Path(string file) =>
      System.IO.Path.Combine(GamePaths.ModConfig, file);

    public bool Exists(string file) => File.Exists(Path(file));

    public JObject? Read(string file) =>
      Exists(file) ? JObject.Parse(File.ReadAllText(Path(file))) : null;

    public void Write(string file, JObject content) =>
      File.WriteAllText(Path(file), content.ToString());

    /// <summary>The <paramref name="modId"/> section of <paramref name="file"/>; fails when either
    /// is absent.</summary>
    public JObject Section(string file, string modId) {
      JObject? doc = Read(file);
      Assert.True(doc != null, $"{file} was not written");
      Assert.True(doc![modId] is JObject, $"{file} has no {modId} section");
      return (JObject)doc[modId]!;
    }

    public void Dispose() {
      GamePaths.DataPath = _previous;
      try {
        Directory.Delete(_root, recursive: true);
      } catch (IOException) { }
    }
  }

  #endregion
}
