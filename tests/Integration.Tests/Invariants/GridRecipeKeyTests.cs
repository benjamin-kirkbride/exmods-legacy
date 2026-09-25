using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// A grid recipe costs only the ingredients its pattern places. A key declared in
/// <c>ingredients</c> and absent from <c>ingredientPattern</c> is never consumed and never shown,
/// and two recipes differing only in such a key take the same input.
/// </summary>
public class GridRecipeKeyTests {
  /// <summary>The ingredient keys of <paramref name="recipe"/> its pattern never uses.</summary>
  internal static List<string> UnusedKeys(JToken recipe) {
    string pattern =
      (string?)VariantCatalogue.Get(recipe, "ingredientPattern") ?? "";
    return
    [
      .. (VariantCatalogue.Get(recipe, "ingredients") as JObject ?? [])
        .Properties()
        .Select(p => p.Name)
        .Where(k => !pattern.Contains(k)),
    ];
  }

  public static TheoryData<string> EveryGridRecipeFile() {
    var data = new TheoryData<string>();
    foreach (string path in ShippedJsonAssetTests.AssetFiles())
      if (path.Contains("/recipes/grid/"))
        data.Add(path);
    return data;
  }

  [Theory]
  [MemberData(nameof(EveryGridRecipeFile))]
  public void Every_ingredient_key_appears_in_its_pattern(string path) {
    JToken root = JToken.Parse(
      File.ReadAllText(Path.Combine(ShippedJsonAssetTests.RepoRoot(), path))
    );
    IEnumerable<JToken> recipes = root is JArray a ? a : new[] { root };
    var found = recipes
      .Select(r => (r, keys: UnusedKeys(r)))
      .Where(x => x.keys.Count > 0)
      .Select(x => {
        var name = VariantCatalogue.Get(x.r, "name");
        var pattern = VariantCatalogue.Get(x.r, "ingredientPattern");
        return $"{name} ({pattern}): {string.Join(", ", x.keys)}";
      })
      .ToList();

    Assert.True(
      found.Count == 0,
      $"{path}, keys the pattern never uses:\n  " + string.Join("\n  ", found)
    );
  }

  [Fact]
  public void A_key_missing_from_the_pattern_is_reported_and_a_used_one_is_not() =>
    Assert.Equal(
      ["G"],
      UnusedKeys(
        JToken.Parse(
          """
          {
            "ingredientPattern": "_H_,PRP",
            "ingredients": {
              "P": { "type": "item", "code": "game:metalplate-iron" },
              "R": { "type": "item", "code": "game:rod-iron" },
              "G": { "type": "item", "code": "game:gear-rusty" },
              "H": { "type": "item", "code": "game:hammer-*", "isTool": true }
            }
          }
          """
        )
      )
    );
}
