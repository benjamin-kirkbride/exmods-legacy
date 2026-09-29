using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandedLib.Networks;
using ExpandedLib.Testing;
using Integration.Tests.Guards;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Xunit;

namespace Integration.Tests.OrientedParts;

/// <summary>
/// The shipped layouts' <c>multiblockFacings</c> tables pin only fixed parts: a network node
/// re-picks its orientation from its neighbours, so its facing is checked through
/// <c>multiblockConnectors</c> instead. exlib's <c>PinnedNetworkNodes</c> reads code-first
/// definitions only, so this scan covers the JSON layouts of ppex and smex.
/// </summary>
public class FacingTableTests {
  /// <summary>Every (layout, facings key) pair in the blocktypes of ppex and smex.</summary>
  private static IEnumerable<(string Layout, string Key, JObject Attributes)> Keys() {
    foreach (string mod in LoadedLine.Mods)
      foreach (
        string file in Directory.GetFiles(
          Path.Combine(RepoPaths.Root, mod, "assets", mod, "blocktypes"),
          "*.json",
          SearchOption.AllDirectories
        )
      ) {
        if (
          JObject.Parse(File.ReadAllText(file))["attributes"] is not JObject attributes
          || attributes["multiblockFacings"] is not JObject facings
        )
          continue;
        foreach (JProperty key in facings.Properties())
          yield return (Path.GetFileName(file), key.Name, attributes);
      }
  }

  // Fails when a pipe outlet ("ppex:pipe-outlet-fire-n": [3]) is added to a facings table.
  [Fact]
  public void No_facings_key_names_a_network_node() {
    var found = Keys().ToList();
    Assert.NotEmpty(found);

    var pinnedNodes = found
      .SelectMany(k =>
        OrientedPartsScene
          .Matching(new AssetLocation(k.Key))
          .Where(b => b is BlockNetworkNode)
          .Select(b => $"{k.Layout}: {k.Key} pins node {b.Code}")
      )
      .ToList();

    Assert.Empty(pinnedNodes);
  }

  // Fails when a facings key names a code the layout's blockNumbers do not use.
  [Fact]
  public void Every_facings_key_is_a_legend_of_its_layout() {
    var dead = Keys()
      .Where(k =>
        k.Attributes["multiblockStructure"]?["blockNumbers"]?[k.Key] == null
      )
      .Select(k => $"{k.Layout}: {k.Key}")
      .ToList();

    Assert.Empty(dead);
  }

  // Fails when a mod's facings key matches none of that mod's loaded blocks.
  [Fact]
  public void Every_mod_facings_key_matches_a_loaded_block() {
    var unmatched = Keys()
      .Where(k => LoadedLine.Mods.Contains(new AssetLocation(k.Key).Domain))
      .Where(k => !OrientedPartsScene.Matching(new AssetLocation(k.Key)).Any())
      .Select(k => $"{k.Layout}: {k.Key}")
      .ToList();

    Assert.Empty(unmatched);
  }
}
