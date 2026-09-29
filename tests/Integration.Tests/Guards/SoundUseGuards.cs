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

  private const string Outpaced =
    "F-21: a throttled one-shot is played again before its clip ends, so plays overlap";

  /// <summary>Finding, keyed without its line number, and the defect it records.</summary>
  private static readonly Dictionary<string, string> ShortRepeatFindings =
    new[] {
      "BlockEntityBoiler.cs: ExSounds.Lava repeats every 2500 ms but lasts 56630 ms",
      "BlockEntityMoltenCanalMoldPedestal.cs: ExSounds.MoltenMetal repeats every 2000 ms "
        + "but lasts 3381 ms",
      "BlockEntityMoltenCanalStart.cs: ExSounds.PourMetal repeats every 2000 ms but lasts "
        + "3941 ms",
      "BlockEntityMoltenCanalTap.cs: ExSounds.MoltenMetal repeats every 2000 ms but lasts "
        + "3381 ms",
      "BlockEntityBlastFurnace.cs: ExSounds.MoltenMetal repeats every 2000 ms but lasts "
        + "3381 ms",
      "BlockEntityBlastFurnace.cs: ExSounds.Fire repeats every 5000 ms but lasts 9260 ms",
      "BlockEntityConverterControl.cs: ExSounds.Embers repeats every 4000 ms but lasts "
        + "22094 ms",
      "BlockEntityConverterControl.cs: ExSounds.Fire repeats every 3000 ms but lasts 9260 ms",
      "BlockEntityConverterControl.cs: ExSounds.Sizzle repeats every 1500 ms but lasts "
        + "3564 ms",
      "BlockEntityConverterControl.cs: ExSounds.MoltenMetal repeats every 1500 ms but lasts "
        + "3381 ms",
      "BlockEntityCowperStove.cs: ExSounds.Fire repeats every 5000 ms but lasts 9260 ms",
      "BlockEntitySmokeStack.cs: ExSounds.Fire repeats every 6000 ms but lasts 9260 ms",
    }.ToDictionary(k => k, _ => Outpaced);

  private const string Direct =
    "F-22: the sound is played past ExSounds and ignores the machine volume and sound type";

  /// <summary>Finding, keyed without its line number, and the defect it records.</summary>
  private static readonly Dictionary<string, string> DirectSoundFindings =
    new[] {
      "BlockMoltenBarrel.cs",
      "BlockMoltenCanal.cs",
      "BlockEntityHopperBell.cs",
      "BlockBlastFurnaceTap.cs",
      "ToolMoldPatches.cs",
    }.ToDictionary(
      f => $"{f}: PlaySoundAt called directly; use ExSounds",
      _ => Direct
    );

  /// <summary>Finding, and the defect it records.</summary>
  private static readonly Dictionary<string, string> LoopFindings = new();

  private static readonly Regex LineNumber = new(
    @"^([^:]+):\d+:",
    RegexOptions.CultureInvariant
  );

  // Fails when a call site repeats a sound faster than its clip where the known findings do not
  // name it, e.g. the mold pedestal's MoltenMetal interval cut from 2000 to 1000 ms.
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
