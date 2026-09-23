using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Integration.Tests.Saves;

/// <summary>
/// The class names old worlds hold their block entities under. A chunk saves each entity under the
/// last name registered for its type, so the table records that name beside all four names the
/// registry passed, for every block-entity type exlib, ppex and smex register. It is written to
/// <c>tests/goldens/saved-keys.json</c> by <c>LEGACY_WRITE_SAVE_GOLDENS</c> (<c>1</c> or
/// <c>saved-keys</c>) and compared against it on every run.
/// </summary>
public class SavedKeyTableTests {
  private const string Name = "saved-keys";

  private sealed record SavedKeyRow(string Key, string Type, string[] Names);

  private static readonly JsonSerializerOptions Json = new() {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  private static string TablePath =>
    Path.Combine(SaveGoldens.RepoRoot(), "tests", "goldens", Name + ".json");

  private static List<SavedKeyRow> Table() {
    var registry = SaveRegistry.Instance;
    return registry
      .Registrations.GroupBy(r => r.Type)
      .Select(g => new SavedKeyRow(
        registry.SavedKey(g.Key)!,
        g.Key.FullName!,
        g.Select(r => r.Name).ToArray()
      ))
      .ToList();
  }

  #region Table

  [Fact]
  public void The_table_matches_the_committed_golden() {
    List<SavedKeyRow> table = Table();
    string text = JsonSerializer.Serialize(table, Json) + "\n";
    string write =
      Environment.GetEnvironmentVariable("LEGACY_WRITE_SAVE_GOLDENS") ?? "";
    if (write == "1" || write.Split(',').Any(n => n.Trim() == Name)) {
      Directory.CreateDirectory(Path.GetDirectoryName(TablePath)!);
      File.WriteAllText(TablePath, text);
    }

    Assert.True(File.Exists(TablePath), $"no {Name}.json golden");
    Assert.Equal(File.ReadAllText(TablePath), text);
  }

  [Fact]
  public void Every_ppex_smex_and_filler_type_has_one_row() {
    List<SavedKeyRow> table = Table();

    Assert.Equal(15, table.Count(r => r.Type.StartsWith("PipesAndPowerExpanded.")));
    Assert.Equal(20, table.Count(r => r.Type.StartsWith("SteelmakingExpanded.")));
    Assert.Equal(
      "ExpandedLib.Blocks.Structures.BlockEntityStructureFiller",
      Assert.Single(table, r => r.Type.StartsWith("ExpandedLib.")).Type
    );
  }

  [Fact]
  public void Every_saved_key_is_the_lowercase_short_id_registered_last() {
    foreach (SavedKeyRow row in Table()) {
      string shortId = row.Type[(row.Type.LastIndexOf('.') + 1)..][
        "BlockEntity".Length..
      ];
      Assert.Equal(shortId.ToLowerInvariant(), row.Key);
      Assert.Equal(row.Key, row.Names[^1]);
      Assert.Equal(4, row.Names.Length);
    }
  }

  [Fact]
  public void Every_saved_key_loads_its_own_type() {
    foreach (SavedKeyRow row in Table())
      Assert.Equal(
        row.Type,
        SaveRegistry.Instance.CreateBlockEntity(row.Key).GetType().FullName
      );
  }

  [Fact]
  public void Every_saved_key_has_a_save_golden() {
    var golden = Directory
      .GetFiles(SaveGoldens.Folder, "*.json")
      .Select(f =>
        JsonDocument
          .Parse(File.ReadAllText(f))
          .RootElement.GetProperty("savedKey")
          .GetString()
      )
      .ToHashSet();
    foreach (SavedKeyRow row in Table())
      Assert.Contains(row.Key, golden);
  }

  #endregion
}
