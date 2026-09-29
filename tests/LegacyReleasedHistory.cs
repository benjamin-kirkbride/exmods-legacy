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
  private static readonly Dictionary<string, Dictionary<string, string>> ItemReleases =
    new();

  /// <summary>The golden folder, under the repository root.</summary>
  internal static string Folder =>
    Path.Combine(RepoPaths.Root, "tests", "goldens", "released");

  /// <summary>Registers every golden in <paramref name="folder"/>, per mod, oldest release first by
  /// <see cref="ReleasedVersions.Compare"/>. Each release becomes a
  /// <see cref="ReleasedHistory.Register(string, string, IReadOnlyList{ReleasedCodes.Shipped})"/>
  /// row holding only the codes no older release shipped, in the rows they came in (its own and
  /// the rows for the other mods' files its patches add codes to); a row left empty is dropped.
  /// The mod's full registration carries no rows: it holds the newest version under the mod's own
  /// modid, no migration debt, and the union of every release's entity classes, each once with its
  /// asset paths unioned. Item codes are kept once, under the first release that shipped them.
  /// Replaces what an earlier call registered for the same mods and versions.</summary>
  /// <param name="folder">The folder holding <c>&lt;modid&gt;-&lt;version&gt;.json</c> files;
  /// <see cref="Folder"/> when null.</param>
  /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
  /// <exception cref="KeyNotFoundException">A golden lacks a field.</exception>
  internal static void Register(string? folder = null) {
    var byMod = new Dictionary<string, List<(string Version, JsonDocument Doc)>>();
    foreach (
      string file in Directory
        .EnumerateFiles(folder ?? Folder, "*.json")
        .OrderBy(f => f, StringComparer.Ordinal)
    ) {
      JsonDocument doc = JsonDocument.Parse(File.ReadAllText(file));
      string mod = doc.RootElement.GetProperty("mod").GetString()!;
      string version = doc.RootElement.GetProperty("version").GetString()!;
      if (!byMod.TryGetValue(mod, out var list))
        byMod[mod] = list = [];
      list.Add((version, doc));
    }
    foreach (var (mod, list) in byMod) {
      list.Sort((a, b) => ReleasedVersions.Compare(a.Version, b.Version));
      var seenCodes = new HashSet<string>();
      var classes = new Dictionary<(string Domain, string Class), List<string>>();
      var items = new List<string>();
      var itemReleases = new Dictionary<string, string>();
      foreach (var (version, doc) in list) {
        JsonElement root = doc.RootElement;
        var added = new List<ReleasedCodes.Shipped>();
        foreach (JsonElement s in root.GetProperty("shipped").EnumerateArray()) {
          string[] fresh = [.. Strings(s.GetProperty("codes")).Where(seenCodes.Add)];
          if (fresh.Length > 0)
            added.Add(
              new ReleasedCodes.Shipped(
                s.GetProperty("domain").GetString()!,
                s.GetProperty("assetPath").GetString()!,
                s.GetProperty("baseCode").GetString()!,
                fresh
              )
            );
        }
        if (added.Count > 0)
          ReleasedHistory.Register(mod, version, added);
        foreach (JsonElement e in root.GetProperty("entityClasses").EnumerateArray()) {
          var key = (e.GetProperty("domain").GetString()!, e.GetProperty("class").GetString()!);
          if (!classes.TryGetValue(key, out List<string>? paths))
            classes[key] = paths = [];
          foreach (string path in Strings(e.GetProperty("assetPaths")))
            if (!paths.Contains(path))
              paths.Add(path);
        }
        foreach (JsonElement i in root.GetProperty("items").EnumerateArray())
          foreach (string code in Strings(i.GetProperty("codes")))
            if (itemReleases.TryAdd(code, version))
              items.Add(code);
      }
      ReleasedHistory.Register(
        mod,
        [],
        [
          .. classes.Select(c => new ReleasedCodes.ShippedEntityClass(
            c.Key.Domain,
            c.Key.Class,
            [.. c.Value]
          )),
        ],
        new Dictionary<string, string> { [mod] = list[^1].Version },
        []
      );
      ItemCodes[mod] = [.. items];
      ItemReleases[mod] = itemReleases;
      foreach (var (_, doc) in list)
        doc.Dispose();
    }
  }

  /// <summary>The item codes <paramref name="mod"/>'s releases shipped, each once, in the order
  /// the oldest release that shipped it lists them: its own itemtypes and the codes its patches
  /// add to other mods' itemtypes.</summary>
  /// <param name="mod">The modid the releases were published under.</param>
  /// <returns>The codes; empty when the mod has no registered release.</returns>
  internal static IReadOnlyList<string> Items(string mod) =>
    ItemCodes.TryGetValue(mod, out string[]? codes) ? codes : [];

  /// <summary>The oldest release of <paramref name="mod"/> that shipped each item code.</summary>
  /// <param name="mod">The modid the releases were published under.</param>
  /// <returns>Code to version; empty when the mod has no registered release.</returns>
  internal static IReadOnlyDictionary<string, string> ItemFirstShipped(string mod) =>
    ItemReleases.TryGetValue(mod, out var releases)
      ? releases
      : new Dictionary<string, string>();

  /// <summary>Fails unless ppex (0.6.8, 292 codes, 15 entity classes, 4 item codes) and smex
  /// (0.9.8, 1154 codes, 33 entity classes, 5 item codes) are registered, as the published zips
  /// hold them over every release with smex's patches applied.</summary>
  internal static void AssertRegistered() {
    foreach (
      var (mod, version, codes, classes, items) in new[] {
        ("ppex", "0.6.8", 292, 15, 4),
        ("smex", "0.9.8", 1154, 33, 5),
      }
    ) {
      ReleasedModHistory? history = ReleasedHistory.For(mod);
      Assert.True(history != null, $"{mod} has no released history");
      Assert.Equal(version, history!.Versions[mod]);
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
