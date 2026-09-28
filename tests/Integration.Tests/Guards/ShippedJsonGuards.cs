using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandedLib.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.Guards;

/// <summary>
/// ppex's and smex's asset trees under <see cref="ShippedJson"/>: every JSON asset parses, carries
/// no stray control character, and every patch entry declares the side it runs on.
/// </summary>
[GuardOf(typeof(ShippedJson), nameof(ShippedJson.Check))]
public class ShippedJsonGuards(ITestOutputHelper output) {
  /// <summary>Finding, and why it stands.</summary>
  private static readonly Dictionary<string, string> Allowed = new();

  private const string NoSide =
    "F-25: the patch entry declares no side, so it runs on both";

  private const string Refractory =
    "smex/assets/smex/patches/compat/ppex/refractory.json";

  /// <summary>Finding, and the defect it records.</summary>
  private static readonly Dictionary<string, string> KnownFindings = Enumerable
    .Range(0, 27)
    .Select(i => $"{Refractory} [{i}] declares no side")
    .Append("smex/assets/smex/patches/vanilla/coalpile.json [0] declares no side")
    .ToDictionary(k => k, _ => NoSide);

  // Fails when a shipped JSON stops parsing or a patch entry loses its side, e.g. a smex patch's
  // "side": "Server" removed.
  [Fact]
  public void Every_shipped_json_parses_and_every_patch_declares_its_side() {
    var findings = new List<string>();
    foreach (string mod in LoadedLine.Mods) {
      string tree = RepoPaths.Assets(mod);
      int files = Directory
        .EnumerateFiles(tree, "*.json", SearchOption.AllDirectories)
        .Count();
      IReadOnlyList<string> found = ShippedJson.Check(tree);
      output.WriteLine(
        $"{mod}: {files} JSON files, {ShippedJson.PatchFiles(tree).Count} patch "
          + $"file(s), {found.Count} finding(s)"
      );
      foreach (string finding in found)
        output.WriteLine("  " + finding);
      Assert.True(files > 0, $"{tree} holds no JSON");
      findings.AddRange(found);
    }

    FindingLists.Assert(findings, Allowed, KnownFindings);
  }
}
