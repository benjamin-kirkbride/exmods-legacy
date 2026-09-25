using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// <c>game:supportbeam-*</c> also matches the game's metal beams,
/// <c>supportbeam-tarnishedmetal-{metal}</c>. Every support beam a shipped grid recipe or
/// construction stage asks for is a wooden one: its <c>*</c> covers the <c>wood</c> group only.
/// </summary>
public class SupportBeamIngredientTests {
  public static TheoryData<string> EveryRecipeAndBlocktype() {
    var data = new TheoryData<string>();
    foreach (string path in ShippedJsonAssetTests.AssetFiles())
      if (path.Contains("/recipes/grid/") || path.Contains("/blocktypes/"))
        data.Add(path);
    return data;
  }

  [Theory]
  [MemberData(nameof(EveryRecipeAndBlocktype))]
  public void Every_support_beam_ingredient_takes_wooden_beams_only(
    string path
  ) {
    JToken root = JToken.Parse(
      File.ReadAllText(Path.Combine(ShippedJsonAssetTests.RepoRoot(), path))
    );
    var found = new List<string>();
    foreach (JObject ing in root.SelectTokens("$..*").OfType<JObject>()) {
      if ((string?)VariantCatalogue.Get(ing, "code") is not { } code)
        continue;
      if (!code.StartsWith("game:supportbeam-") || !code.Contains('*'))
        continue;
      var spans = VariantCatalogue.Instance.Spans(
        "block",
        code,
        Strings(ing, "allowedVariants"),
        Strings(ing, "skipVariants")
      );
      if (spans == null || !spans.SequenceEqual(["wood"]))
        found.Add(
          $"{ing.Path}: {code} covers [{string.Join(", ", spans ?? [])}]"
        );
    }

    Assert.True(found.Count == 0, $"{path}:\n  " + string.Join("\n  ", found));
  }

  private static string[]? Strings(JToken ing, string name) =>
    VariantCatalogue
      .Get(ing, name)
      ?.Select(t => (string)t!)
      .ToArray();
}
