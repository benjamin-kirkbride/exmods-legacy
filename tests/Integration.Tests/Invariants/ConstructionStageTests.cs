using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Right-click construction refunds a broken structure through vanilla's <c>GetDrops</c>, which
/// resolves each paid ingredient again. A <c>*</c> ingredient resolves to a block only when it
/// carries <c>storeWildCard</c>: <c>GetDrops</c> replaces the one <c>*</c> with the value stored
/// under that key, taken from the paying stack's <c>Variant[key]</c>. Without the key the stack is
/// null and the break throws; with a key whose group is not the whole of what the <c>*</c> covers
/// the refund names a code that does not exist and is dropped. Vanilla's creative (ctrl) build
/// seeds only <c>wood</c> and <c>metal</c>, so any other key throws when a creative-built structure
/// breaks. A <c>{key}</c> placeholder is filled from what earlier paid stages stored.
/// </summary>
public class ConstructionStageTests {
  private static readonly Regex Placeholder = new(@"\{(\w+)\}");

  /// <summary>Every rule broken by <paramref name="stages"/>, one line each; empty when none.</summary>
  internal static List<string> Violations(
    JArray stages,
    VariantCatalogue catalogue
  ) {
    var found = new List<string>();
    var storedBefore = new HashSet<string>();
    for (int i = 0; i < stages.Count; i++) {
      var stored = new List<string>();
      foreach (
        JToken ing in VariantCatalogue.Get(stages[i], "requireStacks")
          ?? new JArray()
      ) {
        string code = (string)VariantCatalogue.Get(ing, "code")!;
        string kind =
          ((string?)VariantCatalogue.Get(ing, "type"))?.ToLowerInvariant()
          == "block"
            ? "block"
            : "item";
        string? key = (string?)VariantCatalogue.Get(ing, "storeWildCard");
        string where = $"stage {i} {code}";

        foreach (Match m in Placeholder.Matches(code))
          if (!storedBefore.Contains(m.Groups[1].Value))
            found.Add(
              $"{where}: {{{m.Groups[1].Value}}} is stored by no earlier paid stage"
            );

        if (code.Contains('*')) {
          if (key == null)
            found.Add($"{where}: a wildcard without storeWildCard");
          else {
            var spans = catalogue.Spans(
              kind,
              code,
              Strings(ing, "allowedVariants"),
              Strings(ing, "skipVariants")
            );
            if (spans == null)
              found.Add($"{where}: matches nothing");
            else if (!spans.SequenceEqual([key]))
              found.Add(
                $"{where}: * spans [{string.Join(", ", spans)}], stores {key}"
              );
          }
        }

        if (key != null) {
          if (key != "wood" && key != "metal")
            found.Add(
              $"{where}: key {key} is not seeded by the creative build"
            );
          if (i > 0)
            stored.Add(key);
        }
      }
      storedBefore.UnionWith(stored);
    }
    return found;
  }

  public static TheoryData<string> EveryConstructionFile() {
    var data = new TheoryData<string>();
    foreach (string path in ShippedJsonAssetTests.AssetFiles())
      if (path.Contains("/blocktypes/") && Stages(path) != null)
        data.Add(path);
    return data;
  }

  [Theory]
  [MemberData(nameof(EveryConstructionFile))]
  public void Every_wildcard_stage_ingredient_stores_the_one_group_its_star_covers(
    string path
  ) {
    var found = Violations(Stages(path)!, VariantCatalogue.Instance);

    Assert.True(
      found.Count == 0,
      $"{path}:\n  " + string.Join("\n  ", found)
    );
  }

  [Theory]
  [MemberData(nameof(EveryConstructionFile))]
  public void Every_shape_shows_only_the_first_stage(string path) {
    JToken root = Root(path);
    var expected = Strings(Stages(path)![0], "addElements")!
      .Select(e => e + "/*")
      .ToArray();
    var shapes = new List<(string Name, JToken Shape)>();
    if (VariantCatalogue.Get(root, "shape") is JToken shape)
      shapes.Add(("shape", shape));
    if (VariantCatalogue.Get(root, "shapebytype") is JObject byType)
      foreach (JProperty p in byType.Properties())
        shapes.Add((p.Name, p.Value));

    var found = new List<string>();
    foreach (var (name, entry) in shapes) {
      var actual = Strings(entry, "selectiveElements");
      if (actual == null || !actual.SequenceEqual(expected))
        found.Add(
          $"{name}: selectiveElements [{string.Join(", ", actual ?? [])}], stage 0 is [{string.Join(", ", expected)}]"
        );
    }

    Assert.True(shapes.Count > 0, $"{path}: no shape");
    Assert.True(
      found.Count == 0,
      $"{path}:\n  " + string.Join("\n  ", found)
    );
  }

  [Fact]
  public void A_wildcard_without_a_stored_key_is_refused() =>
    Assert.Equal(
      ["stage 1 game:plank-*: a wildcard without storeWildCard"],
      Violations(
        Build("""{ "type": "item", "code": "game:plank-*", "quantity": 4 }"""),
        VariantCatalogue.Instance
      )
    );

  [Fact]
  public void A_star_over_two_groups_is_refused() =>
    Assert.Equal(
      [
        "stage 1 ppex:pipe-straight-*: * spans [material, orientation], stores metal",
      ],
      Violations(
        Build(
          """{ "type": "block", "code": "ppex:pipe-straight-*", "storeWildCard": "metal", "quantity": 2 }"""
        ),
        VariantCatalogue.Instance
      )
    );

  [Fact]
  public void A_key_naming_another_group_is_refused() =>
    Assert.Equal(
      ["stage 1 game:plank-*: * spans [wood], stores metal"],
      Violations(
        Build(
          """{ "type": "item", "code": "game:plank-*", "storeWildCard": "metal", "quantity": 4 }"""
        ),
        VariantCatalogue.Instance
      )
    );

  [Fact]
  public void A_star_reaching_into_another_definition_is_refused() {
    // supportbeam-* also matches the metal beams, supportbeam-tarnishedmetal-{metal}.
    var found = Violations(
      Build(
        """{ "type": "block", "code": "game:supportbeam-*", "storeWildCard": "wood", "quantity": 4 }"""
      ),
      VariantCatalogue.Instance
    );

    Assert.Equal(
      [
        "stage 1 game:supportbeam-*: * spans [code:supportbeam-tarnishedmetal, metal, wood], stores wood",
      ],
      found
    );
  }

  [Fact]
  public void Allowed_variants_narrow_what_the_star_covers() =>
    Assert.Empty(
      Violations(
        Build(
          """{ "type": "block", "code": "game:supportbeam-*", "allowedVariants": ["oak", "birch"], "storeWildCard": "wood", "quantity": 4 }"""
        ),
        VariantCatalogue.Instance
      )
    );

  [Fact]
  public void A_key_the_creative_build_never_seeds_is_refused() =>
    Assert.Equal(
      ["stage 1 game:rock-*: key rock is not seeded by the creative build"],
      Violations(
        Build(
          """{ "type": "block", "code": "game:rock-*", "storeWildCard": "rock", "quantity": 1 }"""
        ),
        VariantCatalogue.Instance
      )
    );

  [Fact]
  public void A_placeholder_no_earlier_stage_stores_is_refused() =>
    Assert.Equal(
      [
        "stage 1 ppex:pipe-straight-ns-{metal}: {metal} is stored by no earlier paid stage",
      ],
      Violations(
        Build(
          """{ "type": "item", "code": "game:metalplate-*", "allowedVariants": ["iron", "steel"], "storeWildCard": "metal", "quantity": 2 }""",
          """{ "type": "block", "code": "ppex:pipe-straight-ns-{metal}", "quantity": 2 }"""
        ),
        VariantCatalogue.Instance
      )
    );

  [Fact]
  public void A_key_stored_only_by_the_unpaid_stage_0_is_refused() {
    var stages = Build(
      """{ "type": "block", "code": "ppex:pipe-straight-ns-{metal}", "quantity": 2 }"""
    );
    stages[0] = JToken.Parse(
      """{ "requireStacks": [{ "type": "item", "code": "game:metalplate-*", "allowedVariants": ["iron", "steel"], "storeWildCard": "metal", "quantity": 2 }] }"""
    );

    Assert.Equal(
      [
        "stage 1 ppex:pipe-straight-ns-{metal}: {metal} is stored by no earlier paid stage",
      ],
      Violations(stages, VariantCatalogue.Instance)
    );
  }

  [Fact]
  public void A_placeholder_an_earlier_stage_stores_passes() {
    var stages = Build(
      """{ "type": "item", "code": "game:metalplate-*", "allowedVariants": ["iron", "steel"], "storeWildCard": "metal", "quantity": 2 }"""
    );
    stages.Add(
      JToken.Parse(
        """{ "requireStacks": [{ "type": "block", "code": "ppex:pipe-straight-ns-{metal}", "quantity": 2 }] }"""
      )
    );

    Assert.Empty(Violations(stages, VariantCatalogue.Instance));
  }

  /// <summary>An unpaid stage 0 followed by one stage requiring <paramref name="ingredients"/>.</summary>
  private static JArray Build(params string[] ingredients) =>
    new(
      new JObject(),
      new JObject(
        new JProperty(
          "requireStacks",
          new JArray(ingredients.Select(JToken.Parse))
        )
      )
    );

  private static string[]? Strings(JToken ing, string name) =>
    VariantCatalogue
      .Get(ing, name)
      ?.Select(t => (string)t!)
      .ToArray();

  private static JToken Root(string path) =>
    JToken.Parse(
      File.ReadAllText(Path.Combine(ShippedJsonAssetTests.RepoRoot(), path))
    );

  /// <summary>The stages of the file's ExRightClickConstructable behaviour; null when it has none.</summary>
  private static JArray? Stages(string path) {
    JToken root = Root(path);
    foreach (
      JToken b in VariantCatalogue.Get(root, "entityBehaviors") ?? new JArray()
    )
      if ((string?)VariantCatalogue.Get(b, "name") == "ExRightClickConstructable")
        return VariantCatalogue.Get(
            VariantCatalogue.Get(b, "properties"),
            "stages"
          ) as JArray;
    return null;
  }
}
