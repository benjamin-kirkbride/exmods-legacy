using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ExpandedLib.Testing;
using Xunit;

namespace Legacy.Tests;

/// <summary>
/// The published ppex and smex releases, registered with <see cref="ReleasedHistory"/> under their
/// modids from the goldens in <c>tests/goldens/released</c> (one file per release, written from the
/// published zips by <c>scripts/released-goldens.py</c>). Compiled into every test project and
/// called from its <c>ModuleInit</c>.
/// </summary>
internal static class LegacyReleasedHistory {
  private static readonly Dictionary<string, string[]> ItemCodes = new();

  /// <summary>The golden folder, under the repository root.</summary>
  internal static string Folder =>
    Path.Combine(RepoPaths.Root, "tests", "goldens", "released");

  /// <summary>Registers, per mod, the newest golden in <paramref name="folder"/> by
  /// <see cref="ReleasedVersions.Compare"/>: its shipped rows (the release's own and the rows for
  /// the other mods' files its patches add codes to), its entity classes, its version under its
  /// own modid, and no migration debt. Replaces what an earlier call registered for the same
  /// mods.</summary>
  /// <param name="folder">The folder holding <c>&lt;modid&gt;-&lt;version&gt;.json</c> files;
  /// <see cref="Folder"/> when null.</param>
  /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
  /// <exception cref="KeyNotFoundException">A golden lacks a field.</exception>
  internal static void Register(string? folder = null) {
    var newest = new Dictionary<string, (string Version, JsonElement Root)>();
    var docs = new List<JsonDocument>();
    foreach (
      string file in Directory
        .EnumerateFiles(folder ?? Folder, "*.json")
        .OrderBy(f => f, StringComparer.Ordinal)
    ) {
      JsonDocument doc = JsonDocument.Parse(File.ReadAllText(file));
      docs.Add(doc);
      string mod = doc.RootElement.GetProperty("mod").GetString()!;
      string version = doc.RootElement.GetProperty("version").GetString()!;
      if (
        !newest.TryGetValue(mod, out var seen)
        || ReleasedVersions.Compare(version, seen.Version) > 0
      )
        newest[mod] = (version, doc.RootElement);
    }
    foreach (var (mod, (version, root)) in newest) {
      ReleasedHistory.Register(
        mod,
        [
          .. root.GetProperty("shipped")
            .EnumerateArray()
            .Select(s => new ReleasedCodes.Shipped(
              s.GetProperty("domain").GetString()!,
              s.GetProperty("assetPath").GetString()!,
              s.GetProperty("baseCode").GetString()!,
              Strings(s.GetProperty("codes"))
            )),
        ],
        [
          .. root.GetProperty("entityClasses")
            .EnumerateArray()
            .Select(e => new ReleasedCodes.ShippedEntityClass(
              e.GetProperty("domain").GetString()!,
              e.GetProperty("class").GetString()!,
              Strings(e.GetProperty("assetPaths"))
            )),
        ],
        new Dictionary<string, string> { [mod] = version },
        []
      );
      ItemCodes[mod] = [
        .. root.GetProperty("items")
          .EnumerateArray()
          .SelectMany(i => Strings(i.GetProperty("codes"))),
      ];
    }
    foreach (JsonDocument doc in docs)
      doc.Dispose();
  }

  /// <summary>The item codes of <paramref name="mod"/>'s newest registered release: its own
  /// itemtypes and the codes its patches add to other mods' itemtypes, sorted per row.</summary>
  /// <param name="mod">The modid the release was published under.</param>
  /// <returns>The codes; empty when the mod has no registered release.</returns>
  internal static IReadOnlyList<string> Items(string mod) =>
    ItemCodes.TryGetValue(mod, out string[]? codes) ? codes : [];

  /// <summary>Fails unless ppex 0.6.8 (19 blocktypes, 292 codes, 15 entity classes, 4 item codes)
  /// and smex 0.9.8 (38 blocktype rows, 742 codes, 21 entity classes, 4 item codes) are registered,
  /// as the published zips hold them with smex's patches applied.</summary>
  internal static void AssertRegistered() {
    foreach (
      var (mod, version, blocktypes, codes, classes, items) in new[] {
        ("ppex", "0.6.8", 19, 292, 15, 4),
        ("smex", "0.9.8", 38, 742, 21, 4),
      }
    ) {
      ReleasedModHistory? history = ReleasedHistory.For(mod);
      Assert.True(history != null, $"{mod} has no released history");
      Assert.Equal(version, history!.Versions[mod]);
      Assert.Equal(blocktypes, history.Shipped.Count);
      Assert.Equal(codes, history.Shipped.Sum(s => s.Codes.Length));
      Assert.Equal(classes, history.EntityClasses.Count);
      Assert.Equal(items, Items(mod).Count);
    }
    Assert.Contains("game:crushed-coke", Items("smex"));
    Assert.Contains("ppex:boilercornish-north", ReleasedCodes.AllCodes);
    Assert.Equal("0.9.8", ReleasedHistory.AllVersions["smex"]);
  }

  private static string[] Strings(JsonElement array) =>
    [.. array.EnumerateArray().Select(v => v.GetString()!)];
}
