using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Integration.Tests.Pins;

/// <summary>
/// Where a replayed trace departs from its committed text: the tick of the first line that differs,
/// and the sorted names of the fields whose values differ anywhere in the trace. A line whose tick,
/// words or field names differ, or that one side lacks, adds <c>lines</c>; a scene that recorded no
/// trace is <c>unrecorded</c>, from the committed trace's first tick.
/// </summary>
internal sealed record TraceDiff(int FirstTick, string Moved) {
  /// <summary>
  /// The difference, or null when <paramref name="replayed"/> equals
  /// <paramref name="committed"/>. A null <paramref name="replayed"/> is a scene that recorded no trace.
  /// </summary>
  public static TraceDiff? Of(string committed, string? replayed) {
    if (replayed == committed)
      return null;
    string[] was = Lines(committed);
    if (replayed == null)
      return new(was.Skip(1).Select(Tick).FirstOrDefault(), "unrecorded");
    string[] now = Lines(replayed);
    var moved = new SortedSet<string>(StringComparer.Ordinal);
    int? first = null;
    for (int i = 0; i < Math.Max(was.Length, now.Length); i++) {
      string? a = i < was.Length ? was[i] : null;
      string? b = i < now.Length ? now[i] : null;
      if (a == b)
        continue;
      first ??= Tick((a ?? b)!);
      if (a == null || b == null) {
        moved.Add("lines");
        continue;
      }
      var (wordsA, fieldsA) = Parse(a);
      var (wordsB, fieldsB) = Parse(b);
      if (
        wordsA != wordsB
        || !fieldsA
          .Select(f => f.Key)
          .SequenceEqual(fieldsB.Select(f => f.Key))
      ) {
        moved.Add("lines");
        continue;
      }
      for (int f = 0; f < fieldsA.Count; f++)
        if (fieldsA[f].Value != fieldsB[f].Value)
          moved.Add(fieldsA[f].Key);
    }
    return new(first ?? 0, string.Join(',', moved));
  }

  private static string[] Lines(string text) =>
    text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

  /// <summary>The line's leading tick, or 0 for the header.</summary>
  private static int Tick(string line) =>
    int.TryParse(line.Split(' ')[0], out int tick) ? tick : 0;

  /// <summary>The tick and bare words of a line, and its <c>key=value</c> fields in order.</summary>
  private static (
    string Words,
    List<KeyValuePair<string, string>> Fields
  ) Parse(string line) {
    var words = new List<string>();
    var fields = new List<KeyValuePair<string, string>>();
    foreach (string token in line.Split(' ')) {
      int eq = token.IndexOf('=');
      if (eq < 0)
        words.Add(token);
      else
        fields.Add(new(token[..eq], token[(eq + 1)..]));
    }
    return (string.Join(' ', words), fields);
  }
}

/// <summary>
/// Every pin and sequence trace scene replayed on the ported build and diffed against the trace the
/// published ppex and smex recorded for this game version. Each row names its verdict: <c>same</c>;
/// <c>expected-different</c>, with the change of the port that is meant to move it; or
/// <c>finding</c>, with the defect that moves it. A row asserts the first tick that differs and the
/// fields that differ (<see cref="TraceDiff"/>), so a trace that starts or stops differing fails its
/// row. The rows are the same on 1.20, 1.21 and 1.22.
/// </summary>
public class TraceReplayTests {
  private const string Same = "same";
  private const string Expected = "expected-different";
  private const string Finding = "finding";

  /// <summary>Industry's passive gas cooling, 2 deg C/s toward 20 deg C, which ppex never ran.</summary>
  private const string Cooling = "cooling";

  private static readonly PipePinTests Pipe = new();
  private static readonly PipeNetworkDiffPinTests Diff = new();
  private static readonly MachinePinTests Machine = new();
  private static readonly MoltenPinTests Molten = new();

  private static readonly Dictionary<string, Action> Scenes = new()
  {
    ["pipe-charge-iron"] = () =>
      Pipe.A_sealed_run_charges_to_its_weakest_burst_rating("iron", 5f, 1500f),
    ["pipe-charge-steel"] = () =>
      Pipe.A_sealed_run_charges_to_its_weakest_burst_rating(
        "steel",
        10f,
        3000f
      ),
    ["pipe-charge-mixed"] = () =>
      Pipe.A_sealed_run_charges_to_its_weakest_burst_rating("mixed", 5f, 1500f),
    ["pipe-burst-iron"] = () =>
      Pipe.A_run_held_at_its_rating_loses_one_weakest_pipe_on_the_thirtieth_second(
        "iron"
      ),
    ["pipe-burst-steel"] = () =>
      Pipe.A_run_held_at_its_rating_loses_one_weakest_pipe_on_the_thirtieth_second(
        "steel"
      ),
    ["pipe-burst-mixed"] = () =>
      Pipe.A_run_held_at_its_rating_loses_one_weakest_pipe_on_the_thirtieth_second(
        "mixed"
      ),
    ["pipe-throughput-500"] = () =>
      Pipe.Five_hundred_litres_a_second_cross_a_ten_pipe_run(),
    ["pipe-leak-gas-1"] = () =>
      Pipe.An_open_run_leaks_eight_litres_of_gas_a_second_whatever_its_open_ends(
        1
      ),
    ["pipe-leak-gas-2"] = () =>
      Pipe.An_open_run_leaks_eight_litres_of_gas_a_second_whatever_its_open_ends(
        2
      ),
    ["pipe-leak-water"] = () =>
      Pipe.An_open_run_leaks_ten_litres_of_water_a_second(),
    ["pipe-chimney"] = () =>
      Pipe.A_chimney_over_a_passthrough_draws_sixteen_litres_a_second(),
    ["pipe-standing-steam"] = () =>
      Pipe.Steam_standing_a_minute_in_a_ten_pipe_run_cools_to_30(),
    ["pipe-throughput-water-250"] = () =>
      Diff.Two_hundred_fifty_litres_of_water_a_second_cross_a_ten_pipe_run(),
    ["pipe-chimney-outlet"] = () =>
      Diff.A_chimney_over_an_outlet_draws_sixteen_litres_a_second(),
    ["machine-cowper-exhaust"] = () =>
      Machine.Exhaust_at_900_through_five_pipes_heats_a_cowper_core_to_80_in_a_minute(),
    ["machine-boiler-watt"] = () =>
      Machine.A_boiling_boiler_runs_a_watt_engine_47_seconds_of_a_minute_through_a_ten_pipe_main(),
    ["machine-blower-load"] = () =>
      Machine.The_blower_port_carries_the_shaft_load_of_the_main_pressure(),
    ["molten-rate"] = () =>
      Molten.A_full_start_hands_its_neighbour_a_hundred_units_in_one_tick(),
    ["molten-reach-10"] = () =>
      Molten.A_fed_ten_cell_run_reaches_its_far_cell_on_the_ninth_tick_and_fills_it(),
    ["molten-minimum-canal"] = () =>
      Molten.A_raised_minimum_holds_a_nine_unit_gap_between_canal_cells(),
    ["molten-minimum-pedestal"] = () =>
      Molten.A_raised_minimum_still_lets_a_pedestal_take_a_nine_unit_remainder(),
    ["molten-pedestal-fill"] = () =>
      Molten.A_fed_pedestal_fills_its_mold_on_the_fifth_tick(),
    ["molten-cooling"] = () =>
      Molten.A_canal_cell_of_iron_at_1700_cools_as_the_game_version_decays_it(),
    ["sequence-pipe-seed1"] = () =>
      new PipeSequence(1).Run("sequence-pipe-seed1"),
    ["sequence-pipe-seed2"] = () =>
      new PipeSequence(2).Run("sequence-pipe-seed2"),
    ["sequence-pipe-seed3"] = () =>
      new PipeSequence(3).Run("sequence-pipe-seed3"),
    ["sequence-canal-seed1"] = () =>
      new CanalSequence(1).Run("sequence-canal-seed1"),
    ["sequence-canal-seed2"] = () =>
      new CanalSequence(2).Run("sequence-canal-seed2"),
    ["sequence-canal-seed3"] = () =>
      new CanalSequence(3).Run("sequence-canal-seed3"),
  };

  /// <summary>Trace, verdict, cause, first tick that differs, fields that differ.</summary>
  public static TheoryData<string, string, string, int, string> Verdicts() =>
    new()
    {
      { "pipe-charge-iron", Expected, Cooling, 1, "temp" },
      { "pipe-charge-steel", Expected, Cooling, 1, "temp" },
      { "pipe-charge-mixed", Expected, Cooling, 1, "temp" },
      { "pipe-burst-iron", Expected, Cooling, 1, "temp" },
      { "pipe-burst-steel", Expected, Cooling, 1, "temp" },
      { "pipe-burst-mixed", Expected, Cooling, 1, "temp" },
      { "pipe-throughput-500", Same, "", 0, "" },
      { "pipe-leak-gas-1", Expected, Cooling, 1, "temp" },
      { "pipe-leak-gas-2", Expected, Cooling, 1, "temp" },
      { "pipe-leak-water", Same, "", 0, "" },
      { "pipe-chimney", Expected, Cooling, 1, "temp" },
      { "pipe-standing-steam", Expected, Cooling, 1, "temp" },
      { "pipe-throughput-water-250", Same, "", 0, "" },
      { "pipe-chimney-outlet", Expected, Cooling, 1, "temp" },
      { "machine-cowper-exhaust", Expected, Cooling, 1, "core,temp" },
      { "machine-boiler-watt", Expected, Cooling, 1, "temp" },
      { "machine-blower-load", Same, "", 0, "" },
      { "molten-rate", Same, "", 0, "" },
      { "molten-reach-10", Same, "", 0, "" },
      { "molten-minimum-canal", Same, "", 0, "" },
      { "molten-minimum-pedestal", Same, "", 0, "" },
      { "molten-pedestal-fill", Same, "", 0, "" },
      { "molten-cooling", Same, "", 0, "" },
      { "sequence-pipe-seed1", Expected, Cooling, 21, "temp" },
      { "sequence-pipe-seed2", Expected, Cooling, 7, "temp" },
      { "sequence-pipe-seed3", Expected, Cooling, 7, "temp" },
      { "sequence-canal-seed1", Same, "", 0, "" },
      { "sequence-canal-seed2", Same, "", 0, "" },
      { "sequence-canal-seed3", Same, "", 0, "" },
    };

  [Theory]
  [MemberData(nameof(Verdicts))]
  public void A_replayed_trace_differs_only_as_its_verdict_says(
    string trace,
    string verdict,
    string cause,
    int firstTick,
    string moved
  ) {
    string committed = File.ReadAllText(
      Path.Combine(Trace.Folder, trace + ".trace")
    );
    var (traces, error) = Trace.Record(Scenes[trace]);
    string? replayed = traces.SingleOrDefault(t => t.Name == trace)?.Text;

    TraceDiff? diff = TraceDiff.Of(committed, replayed);

    TraceDiff? expected = verdict == Same ? null : new(firstTick, moved);
    Assert.True(
      expected == diff,
      $"{trace} ({cause}): expected {expected?.ToString() ?? Same}, replayed "
        + $"{diff?.ToString() ?? Same}; scene ended by "
        + (error?.GetType().Name ?? "its last line")
    );
  }

  [Fact]
  public void Every_committed_trace_has_a_verdict_and_a_scene() {
    string[] committed = Directory
      .GetFiles(Trace.Folder, "*.trace")
      .Select(Path.GetFileName)
      .Where(f => !f!.EndsWith(".received.trace", StringComparison.Ordinal))
      .Select(f => f![..^".trace".Length])
      .Order(StringComparer.Ordinal)
      .ToArray();

    Assert.Equal(
      committed,
      Verdicts().Select(row => (string)row[0]).Order(StringComparer.Ordinal)
    );
    Assert.Equal(committed, Scenes.Keys.Order(StringComparer.Ordinal));
  }
}
