using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Testing;
using Vintagestory.API.Common;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.Guards;

/// <summary>
/// Every block code and entity class the published ppex 0.6.8 and smex 0.9.8 shipped
/// (<c>tests/goldens/released</c>) reaches a live block in the shipped JSON loaded through the
/// game's own loader (<see cref="LoadedLine"/>): the code loads a block, and the entity class it
/// names, and every released entity class, loads a block entity type from the loaded registry.
/// </summary>
public class ReleasedCodeGuards(ITestOutputHelper output) {
  public static TheoryData<string> Mods => [.. LoadedLine.Mods];

  /// <summary>Finding, and why it stands.</summary>
  private static readonly Dictionary<string, string> Allowed = new();

  /// <summary>Finding, and the defect it records.</summary>
  private static readonly Dictionary<string, string> KnownFindings = new();

  // Fails when a released code loads no block (a variant group renamed, a blocktype file moved out
  // or its code changed), or names an entity class the loaded registry does not hold.
  [Theory]
  [MemberData(nameof(Mods))]
  public void Every_released_code_loads_a_block_whose_entity_class_resolves(
    string modId
  ) {
    TestWorld world = LoadedLine.World;
    ReleasedModHistory history = ReleasedHistory.For(modId)!;
    string[] codes = [.. history.Shipped.SelectMany(s => s.Codes)];
    var findings = new List<string>();
    foreach (string code in codes) {
      Block? block = world.World.GetBlock(new AssetLocation(code));
      if (block == null)
        findings.Add($"{code} loads no block");
      else if (
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
    FindingLists.Assert(findings, Allowed, KnownFindings);
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
    FindingLists.Assert(findings, Allowed, KnownFindings);
  }
}
