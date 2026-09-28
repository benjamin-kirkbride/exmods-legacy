using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ExpandedLib.Registries;
using Integration.Tests.Saves;
using PipesAndPowerExpanded;
using SteelmakingExpanded;
using Vintagestory.API.Common;
using Xunit;

namespace Integration.Tests.Coexistence;

/// <summary>
/// The class-registry law when ppex and smex load beside another line: every name the published
/// line registered loads the type it loaded then, or that type's replacement, alone and with another
/// mod claiming the same bare keys before ppex or after smex; every block entity ppex and smex
/// register is saved under its primary key; both assemblies declare their bare keys published; and
/// neither mod's source registers a network type. The claimant is a generated assembly with an
/// unmarked <c>BlockEntity{ShortId}</c> stand-in for every published type ppex and smex keep, as
/// iiex and siex claim thirty of those keys.
/// </summary>
public class SharedRegistrationTests {
  private static readonly Lazy<Assembly> Claimant = new(BuildClaimant);

  /// <summary>The registry orders: the three mods alone, the claimant loading before ppex, and the
  /// claimant loading after smex.</summary>
  public static TheoryData<string> Orders =>
    ["alone", "claimant first", "claimant last"];

  public static TheoryData<string> Mods => ["ppex", "smex"];

  #region Saved keys

  // Red with [assembly: ExPublishedSaveKeys] removed from smex: the claimant loading last takes
  // smex's bare keys ("SmokeStack" loads Claimant.BlockEntitySmokeStack).
  [Theory]
  [MemberData(nameof(Orders))]
  public void Every_published_name_loads_its_type_or_its_replacement(
    string order
  ) {
    SaveRegistry registry = Registry(order);
    foreach (PublishedSaveKeys.Row row in PublishedSaveKeys.Rows())
    foreach (string name in row.Names)
      Assert.Equal(
        (name, PublishedSaveKeys.LoadsAs(row.Type)),
        (name, registry.CreateBlockEntity(name).GetType().FullName)
      );
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void The_claimant_registers_a_stand_in_for_every_kept_type(
    bool first
  ) {
    SaveRegistry registry = SaveRegistry.Beside(Claimant.Value, first);
    List<Type> standIns = Claimant.Value.GetTypes().ToList();

    Assert.Equal(
      PublishedSaveKeys.Rows().Count(r => PublishedSaveKeys.Kept(r.Type)),
      standIns.Count
    );
    foreach (Type standIn in standIns)
      Assert.Equal(
        standIn,
        registry
          .CreateBlockEntity($"{SaveRegistry.ClaimantModId}.{standIn.Name}")
          .GetType()
      );
  }

  // Red with ppex's pipe aliases registered through a plain RegisterBlockEntityClass: Industry's
  // pipe entities are then saved under "ppex.Pipe" and "ppex.PipePassthrough".
  [Fact]
  public void Every_ppex_and_smex_block_entity_is_saved_under_its_primary_key() {
    SaveRegistry registry = SaveRegistry.Instance;
    foreach (
      IGrouping<Type, SaveRegistry.Registration> type in registry
        .Registrations.GroupBy(r => r.Type)
        .Where(g => g.Any(r => r.ModId is "ppex" or "smex"))
    ) {
      string primary = EntityRegistry.KeyFor(type.First().ModId, type.Key);
      Assert.Equal(
        (type.Key.Name, primary),
        (type.Key.Name, registry.SavedKey(type.Key))
      );
      Assert.Equal(primary, type.Last().Name);
    }
  }

  [Fact]
  public void Every_kept_type_keeps_its_four_names_primary_key_last() {
    foreach (
      IGrouping<Type, SaveRegistry.Registration> type in SaveRegistry
        .Instance.Registrations.Where(r => r.ModId is "ppex" or "smex")
        .GroupBy(r => r.Type)
        .Where(g => g.Key.Assembly == ModAssembly(g.First().ModId))
    ) {
      string modId = type.First().ModId;
      string shortId = type.Key.Name["BlockEntity".Length..];
      Assert.Equal(
        [
          $"{modId}.{shortId}",
          shortId,
          shortId.ToLowerInvariant(),
          $"{modId}.{type.Key.Name}",
        ],
        type.Select(r => r.Name)
      );
    }
  }

  [Theory]
  [MemberData(nameof(Mods))]
  public void The_mod_declares_its_bare_keys_published(string modId) =>
    Assert.True(
      ModAssembly(modId).IsDefined(typeof(ExPublishedSaveKeysAttribute))
    );

  #endregion

  #region Network types

  // Red with a RegisterNetworkType call added anywhere under smex/.
  [Theory]
  [MemberData(nameof(Mods))]
  public void The_mod_source_never_registers_a_network_type(string modId) {
    string root = Path.Combine(SaveGoldens.RepoRoot(), modId);
    List<string> calls = Directory
      .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
      .Where(f => !IsBuildOutput(Path.GetRelativePath(root, f)))
      .SelectMany(f =>
        File.ReadLines(f)
          .Select(
            (line, i) =>
              (Line: line, At: $"{Path.GetRelativePath(root, f)}:{i + 1}")
          )
      )
      .Where(l => l.Line.Contains("RegisterNetworkType"))
      .Select(l => l.At)
      .ToList();

    Assert.Empty(calls);
  }

  #endregion

  #region Helpers

  private static SaveRegistry Registry(string order) =>
    order switch {
      "alone" => SaveRegistry.Instance,
      "claimant first" => SaveRegistry.Beside(Claimant.Value, first: true),
      _ => SaveRegistry.Beside(Claimant.Value, first: false),
    };

  private static Assembly ModAssembly(string modId) =>
    modId == "ppex"
      ? typeof(PipesAndPowerExpandedModSystem).Assembly
      : typeof(SteelmakingExpandedModSystem).Assembly;

  private static bool IsBuildOutput(string relativePath) =>
    relativePath.Split(Path.DirectorySeparatorChar)[0] is "bin" or "obj";

  /// <summary>An assembly without <see cref="ExPublishedSaveKeysAttribute"/> holding one
  /// <c>[BlockEntityRegister]</c> type, named as the published one, for every published type ppex
  /// and smex keep.</summary>
  private static Assembly BuildClaimant() {
    var assembly = AssemblyBuilder.DefineDynamicAssembly(
      new AssemblyName("Coexistence.Claimant"),
      AssemblyBuilderAccess.Run
    );
    ModuleBuilder module = assembly.DefineDynamicModule("Coexistence.Claimant");
    var register = new CustomAttributeBuilder(
      typeof(BlockEntityRegisterAttribute).GetConstructor([typeof(string)])!,
      [null]
    );
    foreach (
      PublishedSaveKeys.Row row in PublishedSaveKeys
        .Rows()
        .Where(r => PublishedSaveKeys.Kept(r.Type))
    ) {
      TypeBuilder type = module.DefineType(
        "Claimant." + row.Type[(row.Type.LastIndexOf('.') + 1)..],
        TypeAttributes.Public | TypeAttributes.Sealed,
        typeof(BlockEntity)
      );
      type.SetCustomAttribute(register);
      type.DefineDefaultConstructor(MethodAttributes.Public);
      type.CreateType();
    }
    return assembly;
  }

  #endregion
}
