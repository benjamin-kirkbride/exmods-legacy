using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandedLib.Checks;
using ExpandedLib.Definitions;
using ExpandedLib.Testing;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.Guards;

/// <summary>
/// ppex's and smex's lang under <see cref="LangCallSites"/> and <see cref="LangCoverage"/>'s rule:
/// every literal key the source asks for is in every locale the mod ships, and every loaded block
/// has a name in every locale. The coverage rule (<see cref="LangCoverageCheck"/>) reads the codes
/// <see cref="LoadedLine"/> loaded from the JSON blocktypes, since neither mod declares a code-first
/// definition for <see cref="LangCoverage.MissingNames"/> to read.
/// </summary>
[GuardOf(typeof(LangCallSites), nameof(LangCallSites.Unresolvable))]
public class LangGuards(ITestOutputHelper output) {
  /// <summary>Finding, and why it stands.</summary>
  private static readonly Dictionary<string, string> Allowed = new();

  /// <summary>Finding, and the defect it records.</summary>
  private static readonly Dictionary<string, string> CallSiteFindings = new();

  /// <summary>Finding, and the defect it records.</summary>
  private static readonly Dictionary<string, string> CoverageFindings = new();

  // Fails when a literal key a call site names is missing from a locale, e.g. a key renamed in
  // smex's en.json and not at its Lang.Get.
  [Fact]
  public void Every_key_a_call_site_names_is_in_every_locale() {
    var findings = new List<string>();
    foreach (string mod in LoadedLine.Mods) {
      int keys = LangCallSites.Keys(mod, RepoPaths.Src(mod)).Count;
      IReadOnlyList<string> found = LangCallSites.Unresolvable(
        mod,
        RepoPaths.Src(mod),
        LangDir(mod)
      );
      Report($"{mod}: {keys} call-site keys", found);
      Assert.True(keys > 0, $"{mod}'s source names no lang key");
      findings.AddRange(found);
    }

    FindingLists.Assert(findings, Allowed, CallSiteFindings);
  }

  // Fails when a loaded block has no block- name in a locale, e.g. a smex blocktype's name key
  // removed from ru.json.
  [Fact]
  public void Every_loaded_block_is_named_in_every_locale() {
    var findings = new List<string>();
    foreach (string mod in LoadedLine.Mods) {
      var source = new LoadedLang(mod);
      IReadOnlyList<string> found = LangCoverageCheck
        .Run(source, mod, allLocales: true)
        .Errors;
      Report(
        $"{mod}: {source.BlockCodes.Count()} blocks, {source.Lang(mod).Count()} locales",
        found
      );
      Assert.True(source.BlockCodes.Any(), $"{mod} loaded no block");
      findings.AddRange(found);
    }

    FindingLists.Assert(findings, Allowed, CoverageFindings);
  }

  private void Report(string scope, IReadOnlyList<string> findings) {
    output.WriteLine($"{scope}, {findings.Count} finding(s)");
    foreach (string finding in findings)
      output.WriteLine("  " + finding);
  }

  private static string LangDir(string mod) =>
    Path.Combine(RepoPaths.Assets(mod), "lang");

  /// <summary>One mod's loaded block codes and its shipped lang files; everything else is
  /// empty.</summary>
  private sealed class LoadedLang(string mod) : ICheckSource {
    public IEnumerable<string> Domains => [mod];

    public IEnumerable<AssetLocation> BlockCodes =>
      LoadedLine.Blocks.Where(b => b.Code.Domain == mod).Select(b => b.Code);

    public IEnumerable<AssetLocation> ItemCodes => [];

    public IEnumerable<(AssetLocation File, JObject Json)> Recipes(
      string domain
    ) => [];

    public IEnumerable<(string Locale, JObject Json)> Lang(string domain) =>
      Directory
        .EnumerateFiles(LangDir(domain), "*.json")
        .OrderBy(f => f, StringComparer.Ordinal)
        .Select(f =>
          (
            Path.GetFileNameWithoutExtension(f),
            JObject.Parse(File.ReadAllText(f))
          )
        );

    public IEnumerable<ExBlockDef> BlockDefinitions(string domain) => [];
  }
}
