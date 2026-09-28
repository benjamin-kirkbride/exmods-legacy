using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Registries;
using ExpandedLib.Testing;
using Integration.Tests.Saves;
using PipesAndPowerExpanded;
using SteelmakingExpanded;
using Vintagestory.API.Common;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.Guards;

/// <summary>
/// The saved-key law of <c>Coexistence/SharedRegistrationTests</c> over the class registry that
/// loading ppex and smex through the game's own loader filled (<see cref="LoadedLine"/>): every
/// name the published line registered loads its type or that type's replacement, and every block
/// entity ppex and smex register, with Industry's two pipe entities ppex aliases, is saved under
/// its primary key. ppex's and Industry's entities are read from <see cref="LoadedLine.Ppex"/>,
/// where exlib's classes were registered before ppex's alone, as in game.
/// </summary>
public class LoadedRegistryGuards(ITestOutputHelper output) {
  /// <summary>Finding, and why it stands.</summary>
  private static readonly Dictionary<string, string> Allowed = new();

  /// <summary>Finding, and the defect it records.</summary>
  private static readonly Dictionary<string, string> KnownFindings = new();

  // Fails when a published name is unregistered or loads another type: ppex's pipe aliases not
  // registered, or a type's register code changed.
  [Fact]
  public void Every_published_name_loads_its_type_or_its_replacement() {
    IClassRegistryAPI registry = LoadedLine.World.Api.ClassRegistry;
    var findings = new List<string>();
    int names = 0;
    foreach (PublishedSaveKeys.Row row in PublishedSaveKeys.Rows())
    foreach (string name in row.Names) {
      names++;
      string expected = PublishedSaveKeys.LoadsAs(row.Type);
      string? loaded = registry.GetBlockEntity(name)?.FullName;
      if (loaded != expected)
        findings.Add(
          $"{name} loads {loaded ?? "no type"}, expected {expected}"
        );
    }
    output.WriteLine(
      $"{names} published names, {findings.Count} finding(s)"
    );
    foreach (string finding in findings)
      output.WriteLine("  " + finding);

    Assert.True(names > 0, "the published table holds no names");
    FindingLists.Assert(findings, Allowed, KnownFindings);
  }

  // Fails when a ppex or smex block entity is saved under a bare or alias key: an alias
  // registered after the primary key through a plain RegisterBlockEntityClass.
  [Fact]
  public void Every_ppex_and_smex_block_entity_is_saved_under_its_primary_key() {
    IClassRegistryAPI ppex = LoadedLine.Ppex.Api.ClassRegistry;
    IClassRegistryAPI line = LoadedLine.World.Api.ClassRegistry;
    List<(string ModId, Type Type, IClassRegistryAPI Registry)> types =
    [
      .. Registered("ppex", typeof(PipesAndPowerExpandedModSystem).Assembly)
        .Select(t => (t.ModId, t.Type, ppex)),
      ("exlib", typeof(BlockEntityPipe), ppex),
      ("exlib", typeof(BlockEntityPipePassthrough), ppex),
      .. Registered("smex", typeof(SteelmakingExpandedModSystem).Assembly)
        .Select(t => (t.ModId, t.Type, line)),
    ];
    List<string> findings =
    [
      .. types
        .Select(t =>
          (
            t.Type,
            Primary: EntityRegistry.KeyFor(t.ModId, t.Type),
            Saved: t.Registry.GetBlockEntityClass(t.Type)
          )
        )
        .Where(t => t.Saved != t.Primary)
        .Select(t =>
          $"{t.Type.FullName} is saved under {t.Saved ?? "no name"}, expected {t.Primary}"
        ),
    ];
    output.WriteLine(
      $"{types.Count} block entity types, {findings.Count} finding(s)"
    );
    foreach (string finding in findings)
      output.WriteLine("  " + finding);

    Assert.True(types.Count > 2, "neither mod registers a block entity");
    FindingLists.Assert(findings, Allowed, KnownFindings);
  }

  /// <summary>The concrete block entity types in <paramref name="assembly"/> that carry a
  /// <see cref="RegisterAttribute"/>, each with <paramref name="modId"/>.</summary>
  private static IEnumerable<(string ModId, Type Type)> Registered(
    string modId,
    Assembly assembly
  ) =>
    assembly
      .GetTypes()
      .Where(t =>
        typeof(BlockEntity).IsAssignableFrom(t)
        && !t.IsAbstract
        && t.GetCustomAttributes().OfType<RegisterAttribute>().Any()
      )
      .Select(t => (modId, t));
}
