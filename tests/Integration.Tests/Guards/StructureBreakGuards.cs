#if GAME_GE_1_22
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ExpandedLib.Testing;
using Vintagestory.API.Common;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.Guards;

/// <summary>
/// Every ppex and smex blocktype with filler offsets or construction stages, loaded from its
/// shipped JSON (<see cref="LoadedLine"/>), broken by <see cref="StructureBreaks"/> in every
/// variant, from every cell, at every stage and in every payment. Each failure is keyed by its
/// variant and its kind, and the keys are judged against the allowed and the known findings.
/// </summary>
[GuardOf(typeof(StructureBreaks), nameof(StructureBreaks.Run))]
public class StructureBreakGuards(ITestOutputHelper output) {
  private static readonly Lazy<StructureBreaks.Result> Breaks = new(() => {
    HoldWoods(LoadedLine.World);
    return StructureBreaks.Run(
      LoadedLine.World,
      b => LoadedLine.Mods.Contains(b.Code.Domain)
    );
  });

  private static readonly string[] Woods = ["birch", "oak"];

  /// <summary>Registers vanilla's <c>game:plank-*</c> item and <c>game:supportbeam-*</c> block in
  /// each of <see cref="Woods"/>, which the test world does not load, so the pump's and the
  /// blower's stage wildcards without allowed variants are paid in them.</summary>
  private static void HoldWoods(TestWorld world) {
    int id = 58000;
    foreach (string wood in Woods) {
      world.RegisterItem($"game:plank-{wood}");
      Block beam = TestBlocks.Configure(
        new Block(),
        $"game:supportbeam-{wood}",
        id++,
        ("wood", wood)
      );
      ReflectionHelpers.SetField(beam, "api", world.Api);
      world.Register(beam);
    }
  }

  /// <summary>Blocktypes and variants the run covered at its last green run; fewer means the load
  /// or the scope stopped seeing structures.</summary>
  private const int BlockFloor = 8;

  private const int VariantFloor = 32;

  private const string LastMetal =
    "F-18, accepted (fallen 2026-09-29, \"Ship shared keys, same\"): several paid stages store "
    + "metal, and the refund pays every stage in the metal paid last";

  private const string PlacementCost =
    "F-20, accepted (fallen 2026-09-29, \"By design, guard lists it\"): "
    + "BlockConverterBessemer.GetDrops hands back the control's placement cost, a large gear and 8 "
    + "iron rods, beside the construction refund";

  private const string SharedKeys =
    "accepted (fallen 2026-09-23, \"Ship shared keys\"): the pump's and the blower's paid stages "
    + "share wood and metal, and the refund pays every stage in the wood and the metal paid last";

  private static readonly string[] Facings = ["north", "east", "south", "west"];

  /// <summary>Finding, and why it stands.</summary>
  private static readonly Dictionary<string, string> Allowed = new[] {
    Each(
      "ppex:boilercornish",
      LastMetal,
      Few("metalnailsandstrips-iron", "metalnailsandstrips-steel"),
      Few("metalplate-iron", "metalplate-steel", "rod-steel"),
      Many("metalnailsandstrips-iron", "metalnailsandstrips-steel"),
      Many("metalplate-iron", "metalplate-steel", "rod-iron")
    ),
    Each(
      "ppex:boilerlancashire",
      LastMetal,
      Few("metalnailsandstrips-iron", "metalnailsandstrips-steel"),
      Many("metalnailsandstrips-iron", "metalnailsandstrips-steel")
    ),
    Each(
      "ppex:enginecornish",
      LastMetal,
      Few("metalnailsandstrips-steel", "metalplate-iron", "metalplate-steel"),
      Few("rod-iron", "rod-steel"),
      Many("metalnailsandstrips-iron", "metalplate-iron", "metalplate-steel"),
      Many("rod-iron", "rod-steel")
    ),
    Each(
      "ppex:enginewatt",
      LastMetal,
      Few("metalnailsandstrips-iron", "metalnailsandstrips-steel"),
      Few("metalplate-iron", "rod-iron", "rod-steel"),
      Many("metalnailsandstrips-iron", "metalnailsandstrips-steel"),
      Many("metalplate-steel", "rod-iron", "rod-steel")
    ),
    Each(
      "smex:converterbessemer",
      LastMetal,
      Few("metalnailsandstrips-iron", "metalnailsandstrips-steel"),
      Few("metalplate-iron", "metalplate-steel", "rod-iron"),
      Many("metalnailsandstrips-iron", "metalnailsandstrips-steel"),
      Many("metalplate-iron", "metalplate-steel", "rod-steel")
    ),
    Each(
      "ppex:mpfluidpump",
      SharedKeys,
      Few("metalnailsandstrips-iron", "metalplate-iron", "metalplate-steel"),
      Few("rod-iron", "rod-steel"),
      Many("metalnailsandstrips-steel", "metalplate-iron", "metalplate-steel"),
      Many("rod-iron", "rod-steel")
    ),
    Each(
      "smex:mpblower",
      SharedKeys,
      Few("metalnailsandstrips-iron", "metalplate-iron", "plank-oak"),
      Few("rod-steel", "supportbeam-birch"),
      Many("metalnailsandstrips-steel", "metalplate-steel", "plank-birch"),
      Many("rod-iron", "supportbeam-oak")
    ),
    Each(
      "smex:converterbessemer",
      PlacementCost,
      ["drops too many game:rod-iron", "drops too many ppex:largegear-iron"]
    ),
  }
    .SelectMany(e => e)
    .GroupBy(e => e.Key)
    .ToDictionary(g => g.Key, g => string.Join("; ", g.Select(e => e.Value)));

  /// <summary>Finding, and the defect it records.</summary>
  private static readonly Dictionary<string, string> KnownFindings = new();

  /// <summary>Each of <paramref name="kinds"/> for every facing of
  /// <paramref name="blocktype"/>.</summary>
  private static IEnumerable<KeyValuePair<string, string>> Each(
    string blocktype,
    string defect,
    params IEnumerable<string>[] kinds
  ) =>
    from side in Facings
    from kind in kinds.SelectMany(k => k)
    select KeyValuePair.Create($"{blocktype}-{side} {kind}", defect);

  private static IEnumerable<string> Few(params string[] items) =>
    items.Select(i => $"drops too few game:{i}");

  private static IEnumerable<string> Many(params string[] items) =>
    items.Select(i => $"drops too many game:{i}");

  // Fails when a structure throws, leaves a cell standing, or drops a code fewer or more times than
  // its drops and what was paid, where the known findings do not name it (a stage wildcard without
  // storeWildCard refunds nothing), or when a known finding stops failing.
  [Fact]
  public void Every_structure_breaks_whole_and_refunds_what_was_paid() {
    StructureBreaks.Result result = Breaks.Value;
    var keyed = result
      .Failures.SelectMany(f => Keys(f).Select(k => (Key: k, Failure: f)))
      .GroupBy(k => k.Key, StringComparer.Ordinal)
      .OrderBy(g => g.Key, StringComparer.Ordinal)
      .ToList();
    output.WriteLine(
      $"{result.Blocks} blocktypes, {result.Variants} variants, {result.Breaks} breaks, "
        + $"{result.Failures.Count} failed, {keyed.Count} finding(s)"
    );
    foreach (var finding in keyed)
      output.WriteLine(
        $"  {finding.Key} ({finding.Count()} breaks; first: {finding.First().Failure})"
      );

    Assert.True(
      result.Blocks >= BlockFloor && result.Variants >= VariantFloor,
      $"{result.Blocks} blocktypes and {result.Variants} variants, below the floors of "
        + $"{BlockFloor} and {VariantFloor}"
    );
    FindingLists.Assert(keyed.Select(g => g.Key), Allowed, KnownFindings);
  }

  private static readonly Regex Mismatch = new(
    @"([a-z]+:[\w*-]+) x(\d+) \(expected: ([\d.]+)\.\.[\d.]+\)",
    RegexOptions.CultureInvariant
  );

  /// <summary>The findings one <see cref="StructureBreaks"/> failure line carries, each the variant
  /// code and one kind: the exception a stand-up or a break threw (its first frame dropped), cells
  /// left standing, or one code dropped fewer or more times than its drops and what was
  /// paid.</summary>
  private static IEnumerable<string> Keys(string failure) {
    string code = failure[..failure.IndexOf(' ')];
    string Exception(string marker) {
      string rest = failure[
        (failure.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..
      ];
      int frame = rest.LastIndexOf(" (", StringComparison.Ordinal);
      return frame < 0 ? rest : rest[..frame];
    }

    if (failure.Contains(" could not be stood up: ", StringComparison.Ordinal))
      return [$"{code} could not be stood up: {Exception(" could not be stood up: ")}"];
    if (failure.Contains(" threw ", StringComparison.Ordinal))
      return [$"{code} threw {Exception(" threw ")}"];
    if (failure.Contains(" left ", StringComparison.Ordinal))
      return [$"{code} left cells standing"];
    return Mismatch
      .Matches(failure)
      .Select(m =>
        int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)
        < float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture)
          ? $"{code} drops too few {m.Groups[1].Value}"
          : $"{code} drops too many {m.Groups[1].Value}"
      );
  }
}
#endif
