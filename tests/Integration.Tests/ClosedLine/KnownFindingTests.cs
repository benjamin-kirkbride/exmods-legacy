using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ExpandedLib.Checks;
using ExpandedLib.Definitions;
using ExpandedLib.Testing;
using Integration.Tests.Guards;
using Newtonsoft.Json.Linq;
using PipesAndPowerExpanded;
using PipesAndPowerExpanded.ClosedLine;
using SteelmakingExpanded;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.ClosedLine;

/// <summary>
/// exlib's checks as <c>/exmod verify</c> runs them over ppex and smex in
/// <see cref="ClosedLineWorld.Open"/> and <see cref="ClosedLineWorld.Closed"/>, after the exemptions
/// and declarations ppex's, smex's and the closed line's <c>Start</c> register: each exemption takes
/// the findings it names, and none is left matching nothing.
/// </summary>
public class KnownFindingTests(ITestOutputHelper output) {
  /// <summary>The blocktypes whose paid stages store a key again, and the key.</summary>
  private static readonly (string Block, string Key)[] StoredAgain =
  [
    ("ppex:boilercornish", "metal"),
    ("ppex:boilerlancashire", "metal"),
    ("ppex:enginecornish", "metal"),
    ("ppex:enginewatt", "metal"),
    ("ppex:mpfluidpump", "metal"),
    ("smex:converterbessemer", "metal"),
    ("smex:mpblower", "metal"),
    ("smex:mpblower", "wood"),
  ];

  // Fails when a StoredAgain call leaves PpexChecks.Declare or SmexChecks.Declare, e.g. the
  // blower's wood: that pair's rule (g) findings stay errors.
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void Every_rule_g_finding_of_a_key_stored_again_is_exempt(bool closed) {
    List<CheckResult> results = Verify(closed);
    CheckResult[] stages = [.. results.Where(r => r.Check == "StageWildcards")];
    foreach (CheckResult r in stages)
      output.WriteLine(
        $"{r.Domain}: {r.Errors.Count} error(s), {r.Exempted.Count} exempted"
      );

    Assert.Equal(2, stages.Length);
    Assert.All(
      stages,
      r => Assert.DoesNotContain(r.Errors, e => e.Contains(": (g) "))
    );
    Assert.All(
      StoredAgain,
      pair =>
        Assert.Contains(
          stages.SelectMany(r => r.Exempted),
          e =>
            e.StartsWith(pair.Block + " stage ", StringComparison.Ordinal)
            && e.Contains($": (g) key {pair.Key} ")
        )
    );
  }

  // Fails when SmexChecks.Declare drops the solidified iron's declaration or the slag block's
  // exemption: its creative-tab finding stays an error.
  [Fact]
  public void The_open_lines_solidified_iron_is_made_and_its_slag_block_exempt() {
    CheckResult made = Verify(closed: false)
      .Single(r => r.Check == "Obtainability" && r.Domain == "smex");

    Assert.DoesNotContain(
      made.Errors,
      e =>
        e.StartsWith("smex:solidifiediron ", StringComparison.Ordinal)
        || e.StartsWith("smex:slag ", StringComparison.Ordinal)
    );
    Assert.Contains(
      made.Exempted,
      e => e.StartsWith("smex:slag (block, creative tab)", StringComparison.Ordinal)
    );
  }

  // Fails when an exemption stops taking what it names, e.g. rule (g)'s words written "(f)", or
  // the slag block's is registered in the closed line, which takes the block off the creative
  // inventory.
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void No_exemption_matches_nothing(bool closed) {
    CheckResult[] unused =
    [
      .. Verify(closed).Where(r => r.Check == "Exempt"),
    ];
    foreach (string line in unused.SelectMany(r => r.Errors))
      output.WriteLine(line);

    Assert.Empty(unused);
  }

  /// <summary>Drops every exemption and declaration, as a world starting to load does, registers
  /// what ppex's, smex's and the closed line's <c>Start</c> register in the closed or the open
  /// world, and
  /// runs <see cref="ExlibChecks.Verify"/> over ppex and smex.</summary>
  private static List<CheckResult> Verify(bool closed) {
    ClosedLineWorld line = closed ? ClosedLineWorld.Closed : ClosedLineWorld.Open;
    typeof(ExlibChecks)
      .GetMethod("ClearDeclarations", BindingFlags.NonPublic | BindingFlags.Static)!
      .Invoke(null, null);
    PpexChecks.Declare("ppex");
    SmexChecks.Declare(line.World.Api, "smex");
    var game = new LineGame(line);
    return [.. LoadedLine.Mods.SelectMany(mod => ExlibChecks.Verify(game, mod))];
  }

  /// <summary>A <see cref="ClosedLineWorld"/> as a loaded game: its blocks, items and recipe
  /// registries, and ppex's and smex's shipped recipe, lang, blocktype and itemtype JSON.</summary>
  private sealed class LineGame(ClosedLineWorld line) : ILoadedGame {
    public IEnumerable<string> Domains => LoadedLine.Mods;

    public IEnumerable<AssetLocation> BlockCodes =>
      Collectibles.OfType<Block>().Select(b => b.Code);

    public IEnumerable<AssetLocation> ItemCodes =>
      Collectibles.OfType<Item>().Select(i => i.Code);

    public IEnumerable<CollectibleObject> Collectibles =>
      line
        .World.World.Blocks.Cast<CollectibleObject>()
        .Concat(line.World.World.Items)
        .Where(c => c?.Code != null);

    public IEnumerable<LoadedOutput> RecipeOutputs =>
      line
        .Grid.Where(r => r.Output?.Code != null)
        .SelectMany(r => Filled("grid", r.Output.Type, r.Output.Code))
        .Concat(
          line
            .Recipes.SmithingRecipes.Select(r => r.Output)
            .Concat(line.Recipes.ClayFormingRecipes.Select(r => r.Output))
            .Concat(line.Recipes.KnappingRecipes.Select(r => r.Output))
            .Concat(line.Recipes.BarrelRecipes.Select(r => r.Output))
            .Where(s => s?.Code != null)
            .SelectMany(s => Filled("recipe", s.Type, s.Code))
        );

    // The recipes are parsed JSON, whose {name} placeholders the game's recipe loader fills per
    // variant; each loaded code of the output's type that it matches with them read as * is made.
    private IEnumerable<LoadedOutput> Filled(
      string registry,
      EnumItemClass type,
      AssetLocation code
    ) {
      if (!code.Path.Contains('{')) {
        yield return new(registry, type, code);
        yield break;
      }
      var pattern = new AssetLocation(
        code.Domain,
        Placeholder.Replace(code.Path, "*")
      );
      foreach (CollectibleObject c in Collectibles)
        if (c.ItemClass == type && WildcardUtil.Match(pattern, c.Code))
          yield return new(registry, type, c.Code);
    }

    private static readonly Regex Placeholder = new(@"\{\w+\}");

    public IReadOnlyList<AssetLocation>? Tagged(JObject ingredient) => null;

    public IEnumerable<(AssetLocation File, JObject Json)> Recipes(
      string domain
    ) =>
      Files(domain, "recipes")
        .SelectMany(f =>
          (f.Json is JArray array ? array : new JArray(f.Json))
            .OfType<JObject>()
            .Select(o => (f.File, o))
        );

    public IEnumerable<(string Locale, JObject Json)> Lang(string domain) =>
      Files(domain, "lang")
        .Where(f => f.Json is JObject)
        .Select(f =>
          (Path.GetFileNameWithoutExtension(f.File.Path), (JObject)f.Json)
        );

    public IEnumerable<ExBlockDef> BlockDefinitions(string domain) => [];

    public IEnumerable<(AssetLocation File, JObject Json)> BlockTypes(
      string domain
    ) => Objects(domain, "blocktypes");

    public IEnumerable<(AssetLocation File, JObject Json)> ItemTypes(
      string domain
    ) => Objects(domain, "itemtypes");

    private static IEnumerable<(AssetLocation File, JObject Json)> Objects(
      string domain,
      string category
    ) =>
      Files(domain, category)
        .Where(f => f.Json is JObject)
        .Select(f => (f.File, (JObject)f.Json));

    // Every JSON file under the domain's shipped assets/<category>, by its asset location.
    private static IEnumerable<(AssetLocation File, JToken Json)> Files(
      string domain,
      string category
    ) {
      string root = RepoPaths.Assets(domain);
      string dir = Path.Combine(root, category);
      if (!Directory.Exists(dir))
        return [];
      return Directory
        .EnumerateFiles(dir, "*.json", SearchOption.AllDirectories)
        .Order(StringComparer.Ordinal)
        .Select(f =>
          (
            new AssetLocation(
              domain,
              Path.GetRelativePath(root, f).Replace('\\', '/')
            ),
            JToken.Parse(File.ReadAllText(f))
          )
        );
    }
  }
}
