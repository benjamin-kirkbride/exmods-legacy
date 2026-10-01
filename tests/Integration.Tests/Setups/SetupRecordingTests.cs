using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Testing;
using Integration.Tests.Pins;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using PipesAndPowerExpanded.Tests;
using Vintagestory.API.MathTools;
using Xunit;

namespace Integration.Tests.Setups;

/// <summary>
/// How a setup is recorded: change lists, steady figures, the file written beside the committed one,
/// in a scratch folder.
/// </summary>
public class SetupRecordingTests : IDisposable {
  private const string Name = "setup";
  private const int Seconds = 12;

  private readonly string _folder = Path.Combine(
    Path.GetTempPath(),
    "legacy-setup-" + Guid.NewGuid().ToString("N")
  );

  private string Committed => Path.Combine(_folder, Name + ".json");

  private string Received => Path.Combine(_folder, Name + ".received.json");

  public void Dispose() {
    if (Directory.Exists(_folder))
      Directory.Delete(_folder, recursive: true);
  }

  #region Fixtures

  private static readonly BlockPos Fed = new(0, 0, 0);
  private static readonly BlockPos Dry = new(5, 0, 0);

  /// <summary>Two capped three-pipe runs side by side: one fed steam every second, one empty.</summary>
  private sealed class TwoRuns {
    public readonly Scene Scene = new Scene().Network(
      "pipe",
      s => new PipeNetwork(s)
    );
    private readonly BlockEntityPipe _inlet;

    public TwoRuns() {
      var pipe = PipeTestWorld.MakePipe();
      BlockEntityPipe? inlet = null;
      foreach (int x in new[] { Fed.X, Dry.X }) {
        var entities = new BlockEntityPipe[3];
        for (int z = 0; z < 3; z++) {
          entities[z] = new BlockEntityPipe();
          Scene.World.Place(new BlockPos(x, 0, z), pipe, entities[z]);
        }
        Scene.Block(new BlockPos(x, 0, -1), PpexScenes.Cap(90));
        Scene.Block(new BlockPos(x, 0, 3), PpexScenes.Cap(90));
        inlet ??= entities[0];
        foreach (BlockEntityPipe be in entities)
          Scene.World.Initialize(be);
      }
      _inlet = inlet!;
      Scene.Build();
    }

    public void Step() {
      _inlet.TryProduce(10f, 150f, "Steam", maxOutputPressure: 5f);
      Scene.Step(1);
    }
  }

  /// <summary>A recording whose machine "m" reads the scripted value and word of each second.</summary>
  private static SetupRecording Scripted(
    float[] values,
    string[]? words = null,
    int seconds = Seconds
  ) {
    var recording = new SetupRecording(Name, new Scene(), seconds);
    int t = 0;
    recording
      .Machine("m", "ppex:m-north")
      .State(() => words?[t] ?? "on")
      .Value("v", "atm", () => values[t]);
    for (; t < seconds; t++)
      recording.Sample(t);
    return recording;
  }

  private static float[] Repeat(float value, int count) =>
    Enumerable.Repeat(value, count).ToArray();

  private static JsonNode Parse(string text) => JsonNode.Parse(text)!;

  #endregion

  #region Series

  // Fails when a second is written although its rounded value equals the last one's, or when a
  // change is judged on the unrounded value.
  [Fact]
  public void A_series_writes_a_second_only_when_its_rounded_value_changes() {
    var values = Repeat(1f, Seconds);
    values[1] = 1.0004f;
    values[5] = 2f;
    values[6] = 2f;
    var recording = Scripted(values);

    string text = recording.Save(_folder, write: false);

    var v = Parse(text)["machines"]!["m"]!["series"]!["v"]!;
    Assert.Equal("[[0,1.000],[5,2.000],[7,1.000]]", v.ToJsonString());
  }

  // Fails when the steady number is the last sample, the mean of the whole run, or an unweighted
  // mean of the change list's values.
  [Fact]
  public void The_steady_number_is_the_time_weighted_mean_of_the_last_third() {
    var values = Repeat(9f, Seconds);
    values[8] = 1f;
    values[9] = 1f;
    values[10] = 1f;
    values[11] = 5f;
    var recording = Scripted(values);

    object steady = recording.Steady()["m.v"];

    Assert.Equal(2d, (double)steady, 6);
  }

  // Fails when the steady word is the last one, the first, or the one with most changes.
  [Fact]
  public void The_steady_word_is_the_one_held_longest_in_the_window() {
    string[] words = [..Enumerable.Repeat("idle", 8), "run", "run", "run", "idle"];
    // window 8..12: run 3 s, idle 1 s
    var recording = Scripted(Repeat(1f, Seconds), words);

    Assert.Equal("run", recording.Steady()["m.state"]);
  }

  // Fails when a tie in held time goes to the later word.
  [Fact]
  public void A_tied_steady_word_goes_to_the_one_held_first() {
    string[] words = [..Enumerable.Repeat("idle", 8), "run", "run", "idle", "idle"];
    var recording = Scripted(Repeat(1f, Seconds), words);

    Assert.Equal("run", recording.Steady()["m.state"]);
  }

  #endregion

  #region Steadiness

  // Fails when steadiness is judged on the whole run instead of the two halves of the window.
  [Fact]
  public void A_number_still_climbing_in_the_window_is_unsteady() {
    var values = Repeat(10f, Seconds);
    values[10] = 11f;
    values[11] = 11f;
    var recording = Scripted(values);

    Assert.Contains("m.v", recording.Unsteady());
  }

  // Fails when the tolerance is under 5% of the larger half mean, or over it.
  [Theory]
  [InlineData(100f, 104f, true)]
  [InlineData(100f, 106f, false)]
  public void Halves_of_a_large_number_differ_by_at_most_five_percent(
    float first,
    float second,
    bool steady
  ) {
    var values = Repeat(first, Seconds);
    for (int t = 10; t < Seconds; t++)
      values[t] = second;
    var recording = Scripted(values);

    Assert.Equal(steady, !recording.Unsteady().Contains("m.v"));
  }

  // Fails when a number below 1 is judged by 5% of itself instead of by 0.05.
  [Theory]
  [InlineData(0.5f, 0.54f, true)]
  [InlineData(0.5f, 0.56f, false)]
  public void Halves_of_a_small_number_differ_by_at_most_five_hundredths(
    float first,
    float second,
    bool steady
  ) {
    var values = Repeat(first, Seconds);
    for (int t = 10; t < Seconds; t++)
      values[t] = second;
    var recording = Scripted(values);

    Assert.Equal(steady, !recording.Unsteady().Contains("m.v"));
  }

  // Fails when a word is judged steady although the halves hold different words longest.
  [Fact]
  public void A_word_that_changes_between_the_halves_is_unsteady() {
    string[] words = [..Enumerable.Repeat("idle", 10), "run", "run"];
    var recording = Scripted(Repeat(1f, Seconds), words);

    Assert.Contains("m.state", recording.Unsteady());
  }

  // Fails when a steady scenario reports an unsteady series.
  [Fact]
  public void A_flat_scenario_is_steady() {
    var recording = Scripted(Repeat(3f, Seconds));

    Assert.Empty(recording.Unsteady());
  }

  #endregion

  #region Sampling

  // Fails when Sample accepts a second out of order.
  [Fact]
  public void Sampling_out_of_order_throws() {
    var recording = new SetupRecording(Name, new Scene(), Seconds);
    recording.Machine("m", "ppex:m-north").State(() => "on");

    Assert.Throws<InvalidOperationException>(() => recording.Sample(1));
  }

  // Fails when Sample accepts a second past the recording's length.
  [Fact]
  public void Sampling_past_the_end_throws() {
    var recording = Scripted(Repeat(1f, Seconds));

    Assert.Throws<InvalidOperationException>(() => recording.Sample(Seconds));
  }

  // Fails when the steady figures are read before every second is sampled.
  [Fact]
  public void An_incomplete_recording_has_no_steady_figures() {
    var recording = new SetupRecording(Name, new Scene(), Seconds);
    recording.Machine("m", "ppex:m-north").State(() => "on");
    recording.Sample(0);

    Assert.Throws<InvalidOperationException>(() => recording.Steady());
  }

  // Fails when a machine with no state is recorded with an empty one.
  [Fact]
  public void A_machine_with_no_state_throws_when_sampled() {
    var recording = new SetupRecording(Name, new Scene(), Seconds);
    recording.Machine("m", "ppex:m-north");

    Assert.Throws<InvalidOperationException>(() => recording.Sample(0));
  }

  #endregion

  #region Runs

  // Fails when a run reads another network or another field of its state than the one it names.
  [Fact]
  public void A_run_records_its_own_network_and_an_empty_one_records_zeros() {
    var plant = new TwoRuns();
    var recording = new SetupRecording(Name, plant.Scene, Seconds);
    recording.Run("fed", Fed);
    recording.Run("dry", Dry);
    for (int t = 0; t < Seconds; t++) {
      plant.Step();
      recording.Sample(t);
    }

    float temperature = plant.Scene.NetworkAt<PipeNetwork>(Fed)!.State!.Temperature;
    var runs = Parse(recording.Save(_folder, write: false))["runs"]!;
    var fed = runs["fed"]!;
    var dry = runs["dry"]!;
    Assert.Equal("Steam", fed["steady"]!["medium"]!.GetValue<string>());
    Assert.True(fed["steady"]!["flow"]!.GetValue<double>() > 0d);
    Assert.True(fed["steady"]!["pressure"]!.GetValue<double>() > 0d);
    Assert.Equal(temperature, fed["steady"]!["temperature"]!.GetValue<double>(), 5d);
    Assert.True(fed["steady"]!["volume"]!.GetValue<double>() > 0d);
    Assert.Equal("[0,0,0]", fed["at"]!.ToJsonString());
    Assert.Equal("[5,0,0]", dry["at"]!.ToJsonString());
    Assert.Equal("", dry["steady"]!["medium"]!.GetValue<string>());
    Assert.Equal(0d, dry["steady"]!["flow"]!.GetValue<double>());
    Assert.Equal("l/s", fed["units"]!["flow"]!.GetValue<string>());
  }

  #endregion

  #region Save

  // Fails when the header names another schema, game folder, setup or mod versions than the tree's.
  [Fact]
  public void The_file_names_its_schema_setup_game_and_mod_versions() {
    var root = Parse(Scripted(Repeat(1f, Seconds)).Save(_folder, write: false));

    Assert.Equal(1, root["schema"]!.GetValue<int>());
    Assert.Equal(Name, root["setup"]!.GetValue<string>());
    Assert.Equal(SetupRecording.GameFolder, root["game"]!.GetValue<string>());
    Assert.Equal(Seconds, root["seconds"]!.GetValue<int>());
    Assert.Equal(
      new[] { 8, 12 },
      root["steady"]!.AsArray().Select(n => n!.GetValue<int>()).ToArray()
    );
    string ppex = File.ReadAllText(Path.Combine(Integration.Tests.Saves.SaveGoldens.RepoRoot(), "ppex", "modinfo.json"));
    Assert.Equal("0.7.1", root["mods"]!["ppex"]!.GetValue<string>());
    Assert.Contains("\"exlib\": \"" + root["mods"]!["exlib"]!.GetValue<string>() + "\"", ppex);
    Assert.Equal("0.10.1", root["mods"]!["smex"]!.GetValue<string>());
  }

  // Fails when the same scenario writes different bytes the second time, or a timestamp is added.
  [Fact]
  public void The_same_scenario_writes_the_same_text() {
    string first = Scripted(Repeat(1f, Seconds)).Save(_folder, write: false);

    string second = Scripted(Repeat(1f, Seconds)).Save(_folder, write: false);

    Assert.Equal(first, second);
    Assert.Equal(first, File.ReadAllText(Committed));
  }

  // Fails when an absent recording is not written as the committed file.
  [Fact]
  public void A_recording_with_no_committed_file_is_written_as_the_committed_file() {
    string text = Scripted(Repeat(1f, Seconds)).Save(_folder, write: false);

    Assert.Equal(text, File.ReadAllText(Committed));
    Assert.False(File.Exists(Received));
  }

  // Fails when a differing steady figure passes, leaves no received file, or overwrites the committed one.
  [Fact]
  public void Differing_steady_figures_write_a_received_file_and_fail() {
    string committed = Scripted(Repeat(1f, Seconds)).Save(_folder, write: false);
    var other = Scripted(Repeat(2f, Seconds));

    Assert.ThrowsAny<Exception>(() => other.Save(_folder, write: false));

    Assert.Equal(committed, File.ReadAllText(Committed));
    Assert.Contains("2.000", File.ReadAllText(Received));
  }

  // Fails when a differing recording named for writing does not replace the committed file.
  [Fact]
  public void A_differing_recording_named_for_writing_replaces_the_committed_file() {
    Scripted(Repeat(1f, Seconds)).Save(_folder, write: false);

    string text = Scripted(Repeat(2f, Seconds)).Save(_folder, write: true);

    Assert.Equal(text, File.ReadAllText(Committed));
    Assert.False(File.Exists(Received));
  }

  // Fails when a matching recording leaves a stale received file.
  [Fact]
  public void A_matching_recording_deletes_a_stale_received_file() {
    Scripted(Repeat(1f, Seconds)).Save(_folder, write: false);
    Assert.ThrowsAny<Exception>(() =>
      Scripted(Repeat(2f, Seconds)).Save(_folder, write: false)
    );

    Scripted(Repeat(1f, Seconds)).Save(_folder, write: false);

    Assert.False(File.Exists(Received));
  }

  // Fails when the comparison reads only the steady figures and constants: a changed start-up
  // leaves both alone.
  [Fact]
  public void A_changed_start_with_the_same_steady_figures_fails() {
    string committed = Scripted(Repeat(1f, Seconds)).Save(_folder, write: false);
    var values = Repeat(1f, Seconds);
    values[2] = 7f;

    Assert.ThrowsAny<Exception>(() => Scripted(values).Save(_folder, write: false));

    Assert.Equal(committed, File.ReadAllText(Committed));
    Assert.Contains("[2,7.000]", File.ReadAllText(Received));
  }

  // Fails when the comparison skips the header: a committed file naming an older ppex passes.
  [Fact]
  public void A_committed_file_with_a_stale_header_fails() {
    string text = Scripted(Repeat(1f, Seconds)).Save(_folder, write: false);
    string ppex = Parse(text)["mods"]!["ppex"]!.GetValue<string>();
    File.WriteAllText(
      Committed,
      text.Replace($"\"ppex\":\"{ppex}\"", "\"ppex\":\"0.0.1\"")
    );

    Assert.ThrowsAny<Exception>(() =>
      Scripted(Repeat(1f, Seconds)).Save(_folder, write: false)
    );

    Assert.Equal(text, File.ReadAllText(Received));
  }

  // Fails when a changed constant passes the comparison.
  [Fact]
  public void A_changed_constant_fails_the_comparison() {
    ConstantRecording(5f).Save(_folder, write: false);

    Assert.ThrowsAny<Exception>(() =>
      ConstantRecording(6f).Save(_folder, write: false)
    );
  }

  private static SetupRecording ConstantRecording(float max) {
    var recording = new SetupRecording(Name, new Scene(), Seconds);
    recording.Machine("k", "ppex:k").State(() => "on").Constant("max", "atm", max);
    for (int t = 0; t < Seconds; t++)
      recording.Sample(t);
    return recording;
  }

  // Fails when the write variable's value names another setup, or "1" does not name every one.
  [Theory]
  [InlineData("1", true)]
  [InlineData("starter-steam-power, setup", true)]
  [InlineData("starter-steam-power", false)]
  [InlineData(null, false)]
  public void The_write_variable_names_the_setups_it_rewrites(string? value, bool writes) {
    var recording = new SetupRecording(Name, new Scene(), Seconds);

    Assert.Equal(writes, recording.Writes(value));
  }

  // Fails when the limit is doubled (the recording a kibibyte over it is written) or halved (the
  // one a kibibyte under it is refused).
  [Theory]
  [InlineData(1024, true)]
  [InlineData(-1024, false)]
  public void A_recording_over_a_mebibyte_is_refused(int beyond, bool refused) {
    // The state word is written twice, in its series and as the steady word, beside a few hundred
    // bytes of header and keys.
    var recording = new SetupRecording(Name, new Scene(), Seconds);
    recording
      .Machine("m", "ppex:m-north")
      .State(() => new string('x', (SetupRecording.MaxBytes + beyond) / 2));
    for (int t = 0; t < Seconds; t++)
      recording.Sample(t);

    if (refused)
      Assert.Throws<InvalidOperationException>(() => recording.Save(_folder, write: false));
    else
      recording.Save(_folder, write: false);

    Assert.Equal(!refused, File.Exists(Committed));
  }

  // Fails when the recording folder is not Recordings/<major.minor> beside Trace's version folder.
  [Fact]
  public void The_folder_is_named_for_the_game_version_as_traces_are() {
    Assert.Equal(Path.GetFileName(Trace.Folder), Path.GetFileName(SetupRecording.Folder));
    Assert.EndsWith(
      Path.Combine("Setups", "Recordings", Path.GetFileName(Trace.Folder)),
      SetupRecording.Folder
    );
  }

  #endregion
}
