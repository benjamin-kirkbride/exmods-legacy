using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ExpandedLib.Testing;
using PipesAndPowerExpanded;
using SteelmakingExpanded;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.Guards;

/// <summary>
/// ppex's and smex's sounds under <see cref="SoundUse"/>: no repeating one-shot outpaces its clip,
/// every sound goes through ExSounds, and every loaded loop is an ExSoundLoop disposed when its
/// block is removed or unloaded. Each rule runs over both mods, and its findings are judged
/// against the known findings.
/// </summary>
[GuardOf(typeof(SoundUse), nameof(SoundUse.ShortRepeats))]
[GuardOf(typeof(SoundUse), nameof(SoundUse.DirectSounds))]
[GuardOf(typeof(SoundUse), nameof(SoundUse.UndisposedLoops))]
public class SoundUseGuards(ITestOutputHelper output) {
  /// <summary>Finding, and why it stands.</summary>
  private static readonly Dictionary<string, string> Allowed = new();

  /// <summary>Finding, keyed without its line number, and the defect it records.</summary>
  private static readonly Dictionary<string, string> ShortRepeatFindings =
    new();

  /// <summary>Finding, keyed without its line number, and the defect it records.</summary>
  private static readonly Dictionary<string, string> DirectSoundFindings =
    new();

  /// <summary>Finding, and the defect it records.</summary>
  private static readonly Dictionary<string, string> LoopFindings = new();

  private static readonly Regex LineNumber = new(
    @"^([^:]+):\d+:",
    RegexOptions.CultureInvariant
  );

  // Fails when a call site repeats a sound faster than its clip where the known findings do not
  // name it, e.g. the mold pedestal's MoltenMetal interval set to 1000 ms.
  [Fact]
  public void Repeating_one_shots_never_outpace_their_clips() {
    IReadOnlyList<string> files = Premise.NotEmpty(Sources(), "source files");
    IReadOnlyList<string> findings = SoundUse.ShortRepeats(files);
    Report($"{files.Count} source files", findings);

    Assert.Contains(
      files,
      f => File.ReadAllText(f).Contains("ExSounds.PlayThrottled(")
    );
    FindingLists.Assert(findings, Allowed, ShortRepeatFindings, Unnumbered);
  }

  // Fails when a call site plays or loads a sound past ExSounds, e.g. a world.PlaySoundAt added to
  // ppex's BlockValve, whose sound then ignores the machine volume.
  [Fact]
  public void Every_sound_goes_through_ExSounds() {
    IReadOnlyList<string> files = Premise.NotEmpty(Sources(), "source files");
    IReadOnlyList<string> findings = SoundUse.DirectSounds(files);
    Report($"{files.Count} source files", findings);

    FindingLists.Assert(
      findings,
      Allowed,
      DirectSoundFindings,
      Unnumbered
    );
  }

  // Fails when a block entity holds an ILoadedSound itself, or an ExSoundLoop its OnBlockRemoved()
  // or OnBlockUnloaded() never disposes, e.g. an ILoadedSound field added to the blast furnace.
  [Fact]
  public void Loaded_loops_are_released_on_removal_and_unload() {
    List<string> findings =
    [
      .. Assemblies().SelectMany(a => SoundUse.UndisposedLoops(a)),
    ];
    Report($"{Assemblies().Length} assemblies", findings);

    FindingLists.Assert(findings, Allowed, LoopFindings);
  }

  /// <summary><paramref name="finding"/> without the line number after its file name.</summary>
  private static string Unnumbered(string finding) =>
    LineNumber.Replace(finding, "$1:");

  private void Report(string scope, IReadOnlyList<string> findings) {
    output.WriteLine($"{scope}, {findings.Count} finding(s)");
    foreach (string finding in findings)
      output.WriteLine("  " + finding);
  }

  private static Assembly[] Assemblies() =>
    [
      typeof(PipesAndPowerExpandedModSystem).Assembly,
      typeof(SteelmakingExpandedModSystem).Assembly,
    ];

  /// <summary>The C# files under ppex and smex, their build output left out.</summary>
  private static string[] Sources() =>
    [
      .. LoadedLine
        .Mods.SelectMany(m =>
          Directory.EnumerateFiles(
            RepoPaths.Src(m),
            "*.cs",
            SearchOption.AllDirectories
          )
        )
        .Where(f =>
          !f.Contains("/bin/", StringComparison.Ordinal)
          && !f.Contains("/obj/", StringComparison.Ordinal)
        )
        .OrderBy(f => f, StringComparer.Ordinal),
    ];
}
