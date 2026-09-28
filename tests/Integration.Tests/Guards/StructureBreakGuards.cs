#if GAME_GE_1_22
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ExpandedLib.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.Guards;

/// <summary>
/// Every ppex and smex blocktype with filler offsets or construction stages, loaded from its
/// shipped JSON (<see cref="LoadedLine"/>), broken by <see cref="StructureBreaks"/> in every
/// variant, from every cell, at every stage and in every payment. Each failure is keyed by its
/// variant and its kind, and the keys are judged against the known findings.
/// </summary>
[GuardOf(typeof(StructureBreaks), nameof(StructureBreaks.Run))]
public class StructureBreakGuards(ITestOutputHelper output) {
  private static readonly Lazy<StructureBreaks.Result> Breaks = new(() =>
    StructureBreaks.Run(
      LoadedLine.World,
      b => LoadedLine.Mods.Contains(b.Code.Domain)
    )
  );

  /// <summary>Blocktypes and variants the run covered at its last green run; fewer means the load
  /// or the scope stopped seeing structures.</summary>
  private const int BlockFloor = 8;

  private const int VariantFloor = 32;

  /// <summary>Finding, and why it stands.</summary>
  private static readonly Dictionary<string, string> Allowed = new();

  private const string DropsNorth =
    "F-17: vanilla's HorizontalOrientable drops the -north variant (dropBlockFace), where the "
    + "check expects the variant broken";

  private const string LastMetal =
    "F-18: several paid stages store metal, and the refund pays every stage in the metal paid "
    + "last";

  private const string NoAllowedVariant =
    "F-19: stage wildcards name no allowedVariants, so the check cannot pay stage 1 and breaks "
    + "no stage past 0";

  private const string PlacementCost =
    "F-20: BlockConverterBessemer.GetDrops hands back the control's placement cost, a large "
    + "gear and 8 iron rods, beside the construction refund";

  private static readonly string[] Facings = ["north", "east", "south", "west"];

  /// <summary>Finding, and the defect it records.</summary>
  private static readonly Dictionary<string, string> KnownFindings = new[] {
    DropsItsNorth("ppex:boilercornish"),
    DropsItsNorth("ppex:boilerlancashire"),
    DropsItsNorth("ppex:enginecornish"),
    DropsItsNorth("ppex:enginewatt"),
    DropsItsNorth("ppex:manualfluidpump"),
    DropsItsNorth("ppex:mpfluidpump"),
    DropsItsNorth("smex:mpblower"),
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
      "smex:converterbessemer",
      PlacementCost,
      ["drops too many game:rod-iron", "drops too many ppex:largegear-iron"]
    ),
    Each(
      "ppex:mpfluidpump",
      NoAllowedVariant,
      [
        "could not be stood up: InvalidOperationException: stage 1 stores wildcard 'wood' "
          + "for game:plank-* but names no allowed variant",
      ]
    ),
    Each(
      "smex:mpblower",
      NoAllowedVariant,
      [
        "could not be stood up: InvalidOperationException: stage 1 stores wildcard 'wood' "
          + "for game:supportbeam-* but names no allowed variant",
      ]
    ),
  }
    .SelectMany(e => e)
    .GroupBy(e => e.Key)
    .ToDictionary(g => g.Key, g => string.Join("; ", g.Select(e => e.Value)));

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

  /// <summary>The east, south and west variants of <paramref name="blocktype"/> each drop the
  /// north variant in place of their own.</summary>
  private static IEnumerable<KeyValuePair<string, string>> DropsItsNorth(
    string blocktype
  ) =>
    from side in Facings.Skip(1)
    from kind in new[] {
      $"drops too few {blocktype}-{side}",
      $"drops too many {blocktype}-north",
    }
    select KeyValuePair.Create($"{blocktype}-{side} {kind}", DropsNorth);

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
