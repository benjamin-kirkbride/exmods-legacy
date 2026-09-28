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
/// modids from the goldens in <c>tests/goldens/released</c> (one file per mod, written from the
/// published zips). Compiled into every test project and called from its <c>ModuleInit</c>.
/// </summary>
internal static class LegacyReleasedHistory {
  /// <summary>The golden folder, under the repository root.</summary>
  internal static string Folder =>
    Path.Combine(RepoPaths.Root, "tests", "goldens", "released");

  /// <summary>Registers every golden in <see cref="Folder"/>: its shipped blocktypes, its entity
  /// classes, its version under its own modid, and no migration debt. Throws when the folder or a
  /// field is missing.</summary>
  internal static void Register() {
    foreach (
      string file in Directory
        .EnumerateFiles(Folder, "*.json")
        .OrderBy(f => f, StringComparer.Ordinal)
    ) {
      using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(file));
      JsonElement root = doc.RootElement;
      string mod = root.GetProperty("mod").GetString()!;
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
        new Dictionary<string, string> {
          [mod] = root.GetProperty("version").GetString()!,
        },
        []
      );
    }
  }

  /// <summary>Fails unless ppex 0.6.8 (19 blocktypes, 292 codes, 15 entity classes) and smex 0.9.8
  /// (35, 679, 21) are registered, as the published zips hold them.</summary>
  internal static void AssertRegistered() {
    foreach (
      var (mod, version, blocktypes, codes, classes) in new[] {
        ("ppex", "0.6.8", 19, 292, 15),
        ("smex", "0.9.8", 35, 679, 21),
      }
    ) {
      ReleasedModHistory? history = ReleasedHistory.For(mod);
      Assert.True(history != null, $"{mod} has no released history");
      Assert.Equal(version, history!.Versions[mod]);
      Assert.Equal(blocktypes, history.Shipped.Count);
      Assert.Equal(codes, history.Shipped.Sum(s => s.Codes.Length));
      Assert.Equal(classes, history.EntityClasses.Count);
    }
    Assert.Contains("ppex:boilercornish-north", ReleasedCodes.AllCodes);
    Assert.Equal("0.9.8", ReleasedHistory.AllVersions["smex"]);
  }

  private static string[] Strings(JsonElement array) =>
    [.. array.EnumerateArray().Select(v => v.GetString()!)];
}
