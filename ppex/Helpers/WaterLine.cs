using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ExpandedLib;
using ExpandedLib.Industry.Pipes;
using Vintagestory.API.Common;

namespace PipesAndPowerExpanded.Helpers;

/// <summary>
/// The pressure machines read from a water run, set by the machines that hold it and standing
/// between their ticks, so a reader gets one figure whichever order the game ticks them in. A pump,
/// a condenser passing water on or a relief valve spilling into its output that left the run
/// brim-full in its last tick holds it at the head it delivers at; a relief valve the run topped
/// in its last tick holds it down to its gate. The run's own
/// <see cref="PipeNetworkState.Pressure"/> is a live figure instead: the fill ratio, jumping to the
/// last producer's head the moment a fill brims the run and back the moment anything draws.
/// </summary>
/// <remarks>
/// Server-side state, kept in memory only: a reload starts every run unheld until its machines
/// tick again. A run that merges or splits is a new network and is held again from the next tick
/// of each machine on it. Each machine holds at most one run and relieves at most one run. A
/// machine no longer in the world at its position counts for nothing and is dropped when next
/// read.
/// </remarks>
public static class WaterLine {
  /// <summary>One machine's record on a run: a hold at a head, or a relief down to a
  /// gate.</summary>
  private readonly record struct Holder(BlockEntity Machine, bool Relief);

  private static readonly ConditionalWeakTable<
    PipeNetwork,
    Dictionary<Holder, float>
  > Settings = new();

  /// <summary>The run each machine holds.</summary>
  private static readonly ConditionalWeakTable<BlockEntity, PipeNetwork> Held =
    new();

  /// <summary>The run each relief valve relieves.</summary>
  private static readonly ConditionalWeakTable<BlockEntity, PipeNetwork> Relieved =
    new();

  /// <summary>
  /// Records the end of <paramref name="pump"/>'s tick: it holds <paramref name="run"/> at
  /// <paramref name="head"/> atm when the run is brim-full, and holds no run otherwise, replacing
  /// the hold it recorded before.
  /// </summary>
  /// <param name="pump">The pump, condenser or valve; the key its hold is kept under.</param>
  /// <param name="run">Its delivery run, or null when it is not delivering.</param>
  /// <param name="head">The pressure it delivers at (atm).</param>
  public static void Hold(BlockEntity pump, PipeNetwork? run, float head) =>
    Record(new Holder(pump, false), IsBrimFull(run) ? run : null, head);

  /// <summary>
  /// Records <paramref name="valve"/>'s tick: <paramref name="run"/> topped its gate, so it holds
  /// the run down to <paramref name="gate"/> atm, or, with <paramref name="run"/> null, it holds
  /// no run down; replacing the relief it recorded before.
  /// </summary>
  public static void Relieve(BlockEntity valve, PipeNetwork? run, float gate) =>
    Record(new Holder(valve, true), run, gate);

  /// <summary>
  /// The pressure (atm) <paramref name="run"/> is pushed at: the highest head of the machines
  /// holding it, else its fill ratio (0 to 1). A relief valve opens on this figure. 0 for a run
  /// that holds no water.
  /// </summary>
  public static float Head(PipeNetwork run) {
    float head = 0f;
    bool held = false;
    foreach (var (pressure, relief) in Present(run))
      if (!relief) {
        head = held ? Math.Max(head, pressure) : pressure;
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
    foreach (var (gate, relief) in Present(run))
      if (relief)
        pressure = Math.Min(pressure, gate);
    return pressure;
  }

  /// <summary>What the machines still in the world recorded on <paramref name="run"/>; drops the
  /// rest.</summary>
  private static List<(float Pressure, bool Relief)> Present(PipeNetwork run) {
    var present = new List<(float, bool)>();
    if (!Settings.TryGetValue(run, out Dictionary<Holder, float>? settings))
      return present;
    List<Holder>? gone = null;
    foreach (var (holder, pressure) in settings)
      if (
        holder.Machine.Api?.World?.BlockAccessor.GetBlockEntity(
          holder.Machine.Pos
        ) == holder.Machine
      )
        present.Add((pressure, holder.Relief));
      else
        (gone ??= []).Add(holder);
    if (gone != null)
      foreach (Holder holder in gone)
        Record(holder, null, 0f);
    return present;
  }

  /// <summary>Whether <paramref name="run"/> carries water up to the brim of its pipes.</summary>
  private static bool IsBrimFull(PipeNetwork? run) =>
    run?.State is { IsLiquid: true } state
    && state.Volume >= run.Nodes.Count * ExlibValues.LitresPerPipe - 0.001f;

  private static void Record(Holder holder, PipeNetwork? run, float pressure) {
    ConditionalWeakTable<BlockEntity, PipeNetwork> recorded = holder.Relief
      ? Relieved
      : Held;
    if (
      recorded.TryGetValue(holder.Machine, out PipeNetwork? before)
      && !ReferenceEquals(before, run)
    ) {
      if (Settings.TryGetValue(before, out Dictionary<Holder, float>? old))
        old.Remove(holder);
      recorded.Remove(holder.Machine);
    }
    if (run == null)
      return;
    Settings.GetOrCreateValue(run)[holder] = pressure;
    recorded.AddOrUpdate(holder.Machine, run);
  }
}
