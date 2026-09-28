using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Integration.Tests.Saves;

/// <summary>
/// The published line's saved-key table (<see cref="PublishedSaveKeys"/>) against the registry the
/// ported mods fill: every published type is still registered, as itself or as its replacement, and
/// every published saved name has a save golden that loads it. The block-entity types exlib, ppex and
/// smex register now are saved under their primary keys, each loading its own type.
/// </summary>
public class SavedKeyTableTests {
  private sealed record SavedKeyRow(string Key, string Type, string[] ModIds);

  private static List<SavedKeyRow> Table() {
    var registry = SaveRegistry.Instance;
    return registry
      .Registrations.GroupBy(r => r.Type)
      .Select(g => new SavedKeyRow(
        registry.SavedKey(g.Key)!,
        g.Key.FullName!,
        g.Select(r => r.ModId).Distinct().ToArray()
      ))
      .ToList();
  }

  #region Table

  [Fact]
  public void Every_published_type_is_registered_as_itself_or_its_replacement() {
    List<string> published = PublishedSaveKeys
      .Rows()
      .Select(r => PublishedSaveKeys.LoadsAs(r.Type))
      .Order()
      .ToList();
    List<string> registered = Table()
      .Where(r =>
        r.ModIds.Any(m => m is "ppex" or "smex")
        || r.Type == "ExpandedLib.Structures.BlockEntityStructureFiller"
      )
      .Select(r => r.Type)
      .Order()
      .ToList();

    Assert.Equal(published, registered);
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
  public void Every_published_saved_key_has_a_save_golden() {
    var golden = Directory
      .GetFiles(SaveGoldens.Folder, "*.json")
      .Select(f =>
        JsonDocument
          .Parse(File.ReadAllText(f))
          .RootElement.GetProperty("savedKey")
          .GetString()
      )
      .ToHashSet();
    foreach (PublishedSaveKeys.Row row in PublishedSaveKeys.Rows())
      Assert.Contains(row.Key, golden);
  }

  #endregion
}
