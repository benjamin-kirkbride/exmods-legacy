using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Integration.Tests.Saves;

/// <summary>
/// The published line's saved-key table, <c>tests/goldens/saved-keys.json</c>: for every block-entity
/// type exlib 0.7.2, ppex 0.6.8 and smex 0.9.8 registered, the name their worlds saved it under (the
/// lowercase short id, registered last) and the four names it was registered by. Written from the
/// published build and never rewritten; old worlds hold these names whatever the port saves under.
/// </summary>
internal static class PublishedSaveKeys {
  /// <summary>One published type: its saved name, its full type name, and its registered names in
  /// registration order.</summary>
  public sealed record Row(string Key, string Type, string[] Names);

  // Published type -> the type its names load since the port: ppex's two pipe entities are
  // Industry's, and exlib 0.8 moved the filler's namespace.
  private static readonly Dictionary<string, string> Replacements = new() {
    ["PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities.BlockEntityPipe"] =
      "ExpandedLib.Industry.Pipes.BlockEntityPipe",
    [
      "PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities.BlockEntityPipePassthrough"
    ] = "ExpandedLib.Industry.Pipes.BlockEntityPipePassthrough",
    ["ExpandedLib.Blocks.Structures.BlockEntityStructureFiller"] =
      "ExpandedLib.Structures.BlockEntityStructureFiller",
  };

  private static readonly JsonSerializerOptions Json = new() {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  /// <summary>The golden's path.</summary>
  public static string Path =>
    System.IO.Path.Combine(
      SaveGoldens.RepoRoot(),
      "tests",
      "goldens",
      "saved-keys.json"
    );

  /// <summary>The golden's rows, in its order.</summary>
  public static IReadOnlyList<Row> Rows() =>
    JsonSerializer.Deserialize<List<Row>>(File.ReadAllText(Path), Json)!;

  /// <summary>Whether <paramref name="type"/>, a published full type name, is still the class its
  /// names load, not replaced.</summary>
  public static bool Kept(string type) => !Replacements.ContainsKey(type);

  /// <summary>The full name of the type a name registered for published <paramref name="type"/>
  /// loads since the port: its replacement, else <paramref name="type"/> itself.</summary>
  public static string LoadsAs(string type) =>
    Replacements.TryGetValue(type, out string? replacement)
      ? replacement
      : type;
}
