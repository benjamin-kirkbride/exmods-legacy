using System;
using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Migrations;
using ExpandedLib.Testing;
using Legacy.Tests;
using PipesAndPowerExpanded;
using SteelmakingExpanded;
using Vintagestory.API.Common;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.Guards;

/// <summary>
/// Every block code, item code and entity class any published ppex or smex release shipped
/// (<c>tests/goldens/released</c>) still has a path into the shipped JSON loaded through the game's
/// own loader (<see cref="LoadedLine"/>). A block code loads a block, or its declared remap chain
/// ends at a code that does or at a declared removal; a loaded block's entity class resolves. An
/// item code loads an item, or an item migration of the two mods maps it to one. Every released
/// entity class loads a block entity type from the loaded registry.
/// </summary>
public class ReleasedCodeGuards(ITestOutputHelper output) {
  public static TheoryData<string> Mods => [.. LoadedLine.Mods];

  /// <summary>Finding, and why it stands.</summary>
  private static readonly Dictionary<string, string> Allowed = new();

  private static readonly Dictionary<string, string> NoFindings = new();

  /// <summary>Mod, then finding, and the defect it records.</summary>
  private static readonly Dictionary<string, Dictionary<string, string>> KnownBlocks = new() {
    ["ppex"] = new(),
    ["smex"] = new(),
  };

  /// <summary>Mod, then finding, and the defect it records.</summary>
  private static readonly Dictionary<string, Dictionary<string, string>> KnownClasses = new() {
    ["ppex"] = new(),
    ["smex"] = new() {
        ["smex.BlockEntityBessemerControl"] =
          "named by smex 0.8.0 to 0.8.7; loads no type in the port",
        ["smex.BlockEntityBessemerConverter"] =
          "named by smex 0.8.0 to 0.8.7; loads no type in the port",
        ["smex.BlockEntityBessemerGasIntake"] =
          "named by smex 0.8.0 to 0.8.7; loads no type in the port",
        ["smex.BlockEntityBessemerTransmission"] =
          "named by smex 0.8.0 to 0.8.7; loads no type in the port",
        ["smex.BlockEntityGasBlower"] =
          "named by smex 0.8.0 to 0.8.7; loads no type in the port",
        ["smex.BlockEntityGasHeatedIntake"] =
          "named by smex 0.8.0 to 0.8.7; loads no type in the port",
        ["smex.BlockEntityGasIntake"] =
          "named by smex 0.8.0 to 0.8.7; loads no type in the port",
        ["smex.BlockEntityGasOutlet"] =
          "named by smex 0.8.0 to 0.8.7; loads no type in the port",
        ["smex.BlockEntityGasPassthrough"] =
          "named by smex 0.8.0 to 0.8.7; loads no type in the port",
        ["smex.BlockEntityGasPipe"] =
          "named by smex 0.8.0 to 0.8.7; loads no type in the port",
        ["smex.BlockEntityGasPressureValve"] =
          "named by smex 0.8.0 to 0.8.7; loads no type in the port",
        ["smex.BlockEntityGasValve"] =
          "named by smex 0.8.0 to 0.8.7; loads no type in the port",
    },
  };

  // Fails when a released code has no path (a variant group renamed with no remap, a blocktype
  // file moved out or its code changed), or a loaded block names an entity class the loaded
  // registry does not hold.
  [Theory]
  [MemberData(nameof(Mods))]
  public void Every_released_code_reaches_a_block_whose_entity_class_resolves(
    string modId
  ) {
    TestWorld world = LoadedLine.World;
    ReleasedModHistory history = ReleasedHistory.For(modId)!;
    string[] codes = [.. history.Shipped.SelectMany(s => s.Codes)];
    var firstShipped = new Dictionary<string, string>();
    foreach (var release in ReleasedHistory.Releases(modId))
      foreach (string code in release.Added.SelectMany(s => s.Codes))
        firstShipped.TryAdd(code, release.Version);
    Dictionary<string, string> declared = ReleasedCodePaths.Declared(
      BlockMigrationModSystem
        .DeclaredBlockRemaps(world.Api)
        .Select(r => (r.OldCode.ToString(), r.NewCode.ToString()))
    );
    HashSet<string> removed =
    [
      .. BlockMigrationModSystem
        .DeclaredRemovals(world.Api)
        .Select(r => r.Code.ToString()),
    ];
    bool Loads(string code) =>
      world.World.GetBlock(new AssetLocation(code)) != null;

    var findings = new List<string>();
    foreach (string code in codes) {
      Block? block = world.World.GetBlock(new AssetLocation(code));
      if (block == null) {
        string? end = ReleasedCodePaths.DeadEnd(code, Loads, declared, removed);
        if (end != null)
          findings.Add(
            $"{code} (first shipped in {modId} {firstShipped[code]}) loads no block, "
              + (end == code ? "no migration declares it" : $"its chain ends at {end}")
          );
      } else if (
        block.EntityClass is { } entityClass
        && world.Api.ClassRegistry.GetBlockEntity(entityClass) == null
      )
        findings.Add(
          $"{code} names entity class {entityClass}, which loads no type"
        );
    }
    output.WriteLine(
      $"{modId}: {codes.Length} released codes, {findings.Count} finding(s)"
    );
    foreach (string finding in findings)
      output.WriteLine("  " + finding);

    Assert.NotEmpty(codes);
    FindingLists.Assert(findings, Allowed, KnownBlocks[modId], ReleasedCodePaths.KeyOf);
  }

  // Fails when an entity class the release's blocktypes named is unregistered: its type renamed,
  // removed, or registered under another key.
  [Theory]
  [MemberData(nameof(Mods))]
  public void Every_released_entity_class_loads_a_type(string modId) {
    TestWorld world = LoadedLine.World;
    ReleasedModHistory history = ReleasedHistory.For(modId)!;
    List<string> findings =
    [
      .. history
        .EntityClasses.Where(e =>
          world.Api.ClassRegistry.GetBlockEntity(e.Class) == null
        )
        .Select(e =>
          $"{e.Class} ({string.Join(", ", e.AssetPaths)}) loads no type"
        ),
    ];
    output.WriteLine(
      $"{modId}: {history.EntityClasses.Count} released entity classes, "
        + $"{findings.Count} finding(s)"
    );
    foreach (string finding in findings)
      output.WriteLine("  " + finding);

    Assert.NotEmpty(history.EntityClasses);
    FindingLists.Assert(findings, Allowed, KnownClasses[modId], ReleasedCodePaths.KeyOf);
  }

  // Fails when a gas pipe code of smex 0.8.0 or 0.8.5 drops out of the declared removals.
  [Fact]
  public void The_unmapped_gas_pipes_are_declared_removed() {
    string[] facings = ["de", "dn", "ds", "dw", "en", "nw", "se", "ue", "un", "us", "uw", "ws"];
    string[] expected =
    [
      .. new[] { "ew", "ns", "sn", "we" }.Select(f => "smex:gaspipe-heated-" + f),
      .. facings.Select(f => "smex:gaspipe-passthroughbend-" + f),
    ];
    string[] removed =
    [
      .. BlockMigrationModSystem
        .DeclaredRemovals(LoadedLine.World.Api)
        .Select(r => r.Code.ToString()),
    ];
    Assert.Empty(expected.Except(removed));
  }

  // Fails when a released item code has no path: an itemtype renamed or removed with no item
  // migration, a patch that added the code dropped, or the migrations ignored.
  [Theory]
  [MemberData(nameof(Mods))]
  public void Every_released_item_code_reaches_an_item(string modId) {
    TestWorld world = LoadedLine.World;
    IReadOnlyList<string> codes = LegacyReleasedHistory.Items(modId);
    IReadOnlyDictionary<string, string> firstShipped =
      LegacyReleasedHistory.ItemFirstShipped(modId);
    Dictionary<string, string> declared = ReleasedCodePaths.Declared(
      new[]
      {
        typeof(PipesAndPowerExpandedModSystem).Assembly,
        typeof(SteelmakingExpandedModSystem).Assembly,
      }
        .SelectMany(a => a.GetTypes())
        .Where(t =>
          !t.IsAbstract
          && typeof(IItemCodeMigration).IsAssignableFrom(t)
          && t.GetConstructor(Type.EmptyTypes) != null
        )
        .SelectMany(t =>
          ((IItemCodeMigration)Activator.CreateInstance(t)!).GetRemaps(world.Api)
        )
        .Select(r => (r.oldCode.ToString(), r.newCode.ToString()))
    );
    List<string> findings =
    [
      .. codes
        .Select(c =>
          (
            Code: c,
            End: ReleasedCodePaths.DeadEnd(
              c,
              code => world.World.GetItem(new AssetLocation(code)) != null,
              declared,
              new HashSet<string>()
            )
          )
        )
        .Where(c => c.End != null)
        .Select(c =>
          $"{c.Code} (first shipped in {modId} {firstShipped[c.Code]}) loads no item, "
            + (c.End == c.Code ? "no migration maps it" : $"its chain ends at {c.End}")
        ),
    ];
    output.WriteLine(
      $"{modId}: {codes.Count} released item codes, {findings.Count} finding(s)"
    );
    foreach (string finding in findings)
      output.WriteLine("  " + finding);

    Assert.NotEmpty(codes);
    FindingLists.Assert(findings, Allowed, NoFindings, ReleasedCodePaths.KeyOf);
  }
}
