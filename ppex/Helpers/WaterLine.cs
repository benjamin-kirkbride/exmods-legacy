using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ExpandedLib;
using ExpandedLib.Industry.Pipes;
using Vintagestory.API.Common;

namespace PipesAndPowerExpanded.Helpers;

/// <summary>
/// The pressure machines read from a water run, set by the machines that hold it and standing
/// between their ticks, so a reader gets one figure whichever order the game ticks them in. A pump
/// that left the run brim-full in its last tick holds it at its head; a relief valve the run topped
/// in its last tick holds it down to its gate. The run's own
/// <see cref="PipeNetworkState.Pressure"/> is a live figure instead: the fill ratio, jumping to the
/// last producer's head the moment a fill brims the run and back the moment anything draws.
/// </summary>
/// <remarks>
/// Server-side state, kept in memory only: a reload starts every run unheld until its pumps and
/// valves tick again. A run that merges or splits is a new network and is held again from the
/// next tick of each pump and valve on it. A pump or valve no longer in the world at its position
/// counts for nothing and is dropped when next read.
/// </remarks>
public static class WaterLine {
  /// <summary>What one pump or relief valve last recorded on a run.</summary>
  private readonly record struct Setting(float Pressure, bool Relief);

  private static readonly ConditionalWeakTable<
    PipeNetwork,
    Dictionary<BlockEntity, Setting>
  > Settings = new();

  /// <summary>The run each pump or valve is recorded on.</summary>
  private static readonly ConditionalWeakTable<BlockEntity, PipeNetwork> Recorded =
    new();

  /// <summary>
  /// Records the end of <paramref name="pump"/>'s tick: it holds <paramref name="run"/> at
  /// <paramref name="head"/> atm when the run is brim-full, and holds no run otherwise, replacing
  /// what it recorded before.
  /// </summary>
  /// <param name="pump">The pump; the key its record is kept under.</param>
  /// <param name="run">Its delivery run, or null when it is not pumping.</param>
  /// <param name="head">The pressure it delivers at (atm).</param>
  public static void Hold(BlockEntity pump, PipeNetwork? run, float head) =>
    Record(pump, IsBrimFull(run) ? run : null, new Setting(head, false));

  /// <summary>
  /// Records <paramref name="valve"/>'s tick: <paramref name="run"/> topped its gate, so it holds
  /// the run down to <paramref name="gate"/> atm, or, with <paramref name="run"/> null, it holds
  /// no run; replacing what it recorded before.
  /// </summary>
  public static void Relieve(BlockEntity valve, PipeNetwork? run, float gate) =>
    Record(valve, run, new Setting(gate, true));

  /// <summary>
  /// The pressure (atm) <paramref name="run"/> is pushed at: the highest head of the pumps holding
  /// it, else its fill ratio (0 to 1). A relief valve opens on this figure. 0 for a run that holds
  /// no water.
  /// </summary>
  public static float Head(PipeNetwork run) {
    float head = 0f;
    bool held = false;
    foreach (Setting setting in Present(run))
      if (!setting.Relief) {
        head = held ? Math.Max(head, setting.Pressure) : setting.Pressure;
        held = true;
      }
    if (held)
      return head;
    float brim = run.Nodes.Count * ExlibValues.LitresPerPipe;
    return run.State is { IsLiquid: true } state && brim > 0f
      ? Math.Min(1f, state.Volume / brim)
      : 0f;
  }

  /// <summary>
  /// The pressure (atm) a machine drawing water from <paramref name="run"/> reads:
  /// <see cref="Head"/>, held down to the lowest gate among the relief valves holding it.
  /// </summary>
  public static float Pressure(PipeNetwork run) {
    float pressure = Head(run);
    foreach (Setting setting in Present(run))
      if (setting.Relief)
        pressure = Math.Min(pressure, setting.Pressure);
    return pressure;
  }

  /// <summary>What the pumps and valves still in the world recorded on <paramref name="run"/>;
  /// drops the rest.</summary>
  private static List<Setting> Present(PipeNetwork run) {
    var present = new List<Setting>();
    if (!Settings.TryGetValue(run, out Dictionary<BlockEntity, Setting>? settings))
      return present;
    List<BlockEntity>? gone = null;
    foreach (var (machine, setting) in settings)
      if (machine.Api?.World?.BlockAccessor.GetBlockEntity(machine.Pos) == machine)
        present.Add(setting);
      else
        (gone ??= []).Add(machine);
    if (gone != null)
      foreach (BlockEntity machine in gone)
        Record(machine, null, default);
    return present;
  }

  /// <summary>Whether <paramref name="run"/> carries water up to the brim of its pipes.</summary>
  private static bool IsBrimFull(PipeNetwork? run) =>
    run?.State is { IsLiquid: true } state
    && state.Volume >= run.Nodes.Count * ExlibValues.LitresPerPipe - 0.001f;

  private static void Record(
    BlockEntity machine,
    PipeNetwork? run,
    Setting setting
  ) {
    if (
      Recorded.TryGetValue(machine, out PipeNetwork? before)
      && !ReferenceEquals(before, run)
    ) {
      if (Settings.TryGetValue(before, out Dictionary<BlockEntity, Setting>? old))
        old.Remove(machine);
      Recorded.Remove(machine);
    }
    if (run == null)
      return;
    Settings.GetOrCreateValue(run)[machine] = setting;
    Recorded.AddOrUpdate(machine, run);
  }
}
