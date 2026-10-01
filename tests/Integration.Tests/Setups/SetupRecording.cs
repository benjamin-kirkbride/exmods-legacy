using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Testing;
using Integration.Tests.Pins;
using Integration.Tests.Saves;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Xunit;

namespace Integration.Tests.Setups;

/// <summary>
/// What a setup's scene did, per second: every pipe run's medium, flow, pressure, temperature and
/// volume, and every machine's state and values. A scenario calls <see cref="Sample"/> once after
/// each server second, then <see cref="Save()"/> writes <c>{setup}.json</c> under
/// <c>tests/Integration.Tests/Setups/Recordings/{game version}</c> (schema 1) and compares the
/// steady figures with the committed file. Numbers carry <see cref="Trace"/>'s rounding. A series is a
/// change list: <c>[second, value]</c> is written when the rounded value differs from the last one.
/// The steady window is the last third of the run.
/// </summary>
internal sealed class SetupRecording {
  /// <summary>Largest file <see cref="Save()"/> writes, in bytes.</summary>
  public const int MaxBytes = 1024 * 1024;

  private const string WriteVariable = "LEGACY_WRITE_SETUPS";

  private readonly string _setup;
  private readonly Scene _scene;
  private readonly List<RecordedRun> _runs = [];
  private readonly List<RecordedMachine> _machines = [];
  private int _samples;

  /// <summary>The last <see cref="Sample"/> second plus one.</summary>
  public int Samples => _samples;

  /// <summary>Length of the recording in seconds.</summary>
  public int Seconds { get; }

  /// <summary>First second of the steady window.</summary>
  public int SteadyFrom => Seconds - Seconds / 3;

  /// <param name="setup">The file name the recording is saved under, without its extension.</param>
  /// <param name="scene">The scene whose pipe networks the runs are read from.</param>
  /// <param name="seconds">How many seconds the scenario samples, at least 3.</param>
  public SetupRecording(string setup, Scene scene, int seconds) {
    if (seconds < 3)
      throw new ArgumentOutOfRangeException(nameof(seconds));
    _setup = setup;
    _scene = scene;
    Seconds = seconds;
  }

  /// <summary>
  /// Records the pipe run owning the cell <paramref name="pos"/>, as <paramref name="id"/>. A second
  /// with no network or no state there reads as an empty medium and zeros.
  /// </summary>
  public RecordedRun Run(string id, BlockPos pos) {
    var run = new RecordedRun(_scene, id, pos);
    _runs.Add(run);
    return run;
  }

  /// <summary>Records a machine as <paramref name="id"/>, placed in game as <paramref name="code"/>.</summary>
  public RecordedMachine Machine(string id, string code) {
    var machine = new RecordedMachine(id, code);
    _machines.Add(machine);
    return machine;
  }

  /// <summary>
  /// Reads every run and machine as second <paramref name="second"/>, which is the number of
  /// <c>Scene.Step(1)</c> calls made so far less one. Seconds are sampled in order from 0.
  /// </summary>
  public void Sample(int second) {
    if (second != _samples)
      throw new InvalidOperationException(
        $"Sample({second}) after {_samples} samples."
      );
    if (second >= Seconds)
      throw new InvalidOperationException($"Sample({second}) past {Seconds} s.");
    foreach (RecordedRun run in _runs)
      run.Sample(second);
    foreach (RecordedMachine machine in _machines)
      machine.Sample(second);
    _samples++;
  }

  /// <summary>
  /// The steady figure of every series, keyed <c>{id}.{series}</c>: a number is the time-weighted
  /// mean over the steady window, a word the one held longest in it (the earliest on a tie).
  /// </summary>
  public IReadOnlyDictionary<string, object> Steady() {
    RequireComplete();
    var steady = new Dictionary<string, object>();
    foreach (var (owner, series) in AllSeries())
      steady[owner + "." + series.Name] = series.Steady(SteadyFrom, Seconds);
    return steady;
  }

  /// <summary>
  /// The series, keyed <c>{id}.{series}</c>, that are not steady: a number whose means over the two
  /// halves of the steady window differ by more than 5% of the larger (0.05 when that is below 1), a
  /// word that is not the longest held in both halves.
  /// </summary>
  public IReadOnlyList<string> Unsteady() {
    RequireComplete();
    return AllSeries()
      .Where(s => !s.Series.IsSteady(SteadyFrom, Seconds))
      .Select(s => s.Owner + "." + s.Series.Name)
      .ToList();
  }

  /// <summary>The folder this game version's recordings live in, such as <c>Recordings/1.22</c>.</summary>
  public static string Folder =>
    Path.Combine(
      SaveGoldens.RepoRoot(),
      "tests",
      "Integration.Tests",
      "Setups",
      "Recordings",
      GameFolder
    );

  /// <summary>The game version's major.minor, as <see cref="Trace.Folder"/> names its folder.</summary>
  public static string GameFolder =>
    string.Join('.', GameVersion.ShortGameVersion.Split('.')[..2]);

  /// <summary>
  /// Records the setup in <see cref="Folder"/> and returns its text. <c>{setup}.json</c> is written
  /// only when it is absent, or when <c>LEGACY_WRITE_SETUPS</c> is <c>1</c> or a comma-separated
  /// list naming the setup. Otherwise the steady figures and constants are compared with the
  /// committed file's: equal ones delete <c>{setup}.received.json</c>, differing ones write it and
  /// fail the caller.
  /// </summary>
  /// <exception cref="InvalidOperationException">The recording is incomplete or over <see cref="MaxBytes"/>.</exception>
  public string Save() =>
    Save(
      Folder,
      Writes(Environment.GetEnvironmentVariable(WriteVariable))
    );

  /// <summary>
  /// Whether a write variable's <paramref name="value"/> (<c>1</c>, or a comma-separated list of
  /// setup names; null when unset) names this setup.
  /// </summary>
  internal bool Writes(string? value) => Trace.Writes(value, _setup);

  /// <summary>
  /// <see cref="Save()"/> into <paramref name="folder"/>, rewriting the committed file when
  /// <paramref name="write"/> is set. A file is replaced whole, through a temporary file.
  /// </summary>
  internal string Save(string folder, bool write) {
    string text = Text();
    if (Encoding.UTF8.GetByteCount(text) > MaxBytes)
      throw new InvalidOperationException(
        $"{_setup} recording is over {MaxBytes} bytes."
      );
    string committed = Path.Combine(folder, _setup + ".json");
    string received = Path.Combine(folder, _setup + ".received.json");
    Directory.CreateDirectory(folder);
    if (write || !File.Exists(committed)) {
      Replace(committed, text);
      File.Delete(received);
      return text;
    }
    List<string> differences = Differences(File.ReadAllText(committed), text);
    if (differences.Count == 0) {
      File.Delete(received);
      return text;
    }
    Replace(received, text);
    Assert.Fail(
      $"{_setup} steady figures differ from {committed}:\n"
        + string.Join('\n', differences)
    );
    return text;
  }

  /// <summary>
  /// Every steady figure and constant of <paramref name="recorded"/> that differs from
  /// <paramref name="committed"/> (both schema 1 texts), one line each; empty when they agree.
  /// </summary>
  internal static List<string> Differences(string committed, string recorded) {
    var differences = new List<string>();
    JsonNode? was = JsonNode.Parse(committed);
    JsonNode? now = JsonNode.Parse(recorded);
    foreach (string group in new[] { "runs", "machines" }) {
      JsonObject wasGroup = was?[group]?.AsObject() ?? [];
      JsonObject nowGroup = now?[group]?.AsObject() ?? [];
      foreach (string id in wasGroup.Select(p => p.Key).Union(nowGroup.Select(p => p.Key))) {
        foreach (string part in new[] { "steady", "constants" }) {
          JsonNode? a = wasGroup[id]?[part];
          JsonNode? b = nowGroup[id]?[part];
          if (!JsonNode.DeepEquals(a, b))
            differences.Add(
              $"{group}/{id}/{part}: committed {a?.ToJsonString() ?? "absent"}, recorded {b?.ToJsonString() ?? "absent"}"
            );
        }
      }
    }
    return differences;
  }

  private void RequireComplete() {
    if (_samples != Seconds)
      throw new InvalidOperationException(
        $"{_setup} has {_samples} of {Seconds} samples."
      );
  }

  private IEnumerable<(string Owner, Series Series)> AllSeries() =>
    _runs
      .SelectMany(r => r.All.Select(s => (r.Id, s)))
      .Concat(_machines.SelectMany(m => m.All.Select(s => (m.Id, s))));

  private string Text() {
    RequireComplete();
    var text = new StringBuilder();
    text.Append("{\"schema\":1,\"setup\":").Append(Quote(_setup));
    text.Append(",\"game\":").Append(Quote(GameFolder));
    text.Append(",\"mods\":{\"exlib\":").Append(Quote(ModVersion("ppex", "exlib")));
    text.Append(",\"ppex\":").Append(Quote(ModVersion("ppex", "version")));
    text.Append(",\"smex\":").Append(Quote(ModVersion("smex", "version")));
    text.Append("},\"seconds\":").Append(Seconds.ToString(CultureInfo.InvariantCulture));
    text.Append(",\"steady\":[")
      .Append(SteadyFrom.ToString(CultureInfo.InvariantCulture))
      .Append(',')
      .Append(Seconds.ToString(CultureInfo.InvariantCulture))
      .Append("],\n\"runs\":{");
    text.Append(string.Join(",\n", _runs.Select(r => Entry(r.Id, r.Json(SteadyFrom, Seconds)))));
    text.Append("},\n\"machines\":{");
    text.Append(string.Join(",\n", _machines.Select(m => Entry(m.Id, m.Json(SteadyFrom, Seconds)))));
    text.Append("}}\n");
    return text.ToString();
  }

  private static string Entry(string id, string body) => Quote(id) + ":" + body;

  internal static string Quote(string value) => JsonSerializer.Serialize(value);

  private static string ModVersion(string modDir, string key) {
    JsonNode? node = JsonNode.Parse(
      File.ReadAllText(Path.Combine(SaveGoldens.RepoRoot(), modDir, "modinfo.json"))
    );
    string? version = key == "exlib" ? node?["dependencies"]?[key]?.GetValue<string>() : node?[key]?.GetValue<string>();
    return version
      ?? throw new InvalidOperationException($"{modDir}/modinfo.json names no \"{key}\".");
  }

  private static void Replace(string path, string text) {
    if (File.Exists(path) && File.ReadAllText(path) == text)
      return;
    string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
    File.WriteAllText(temp, text);
    File.Move(temp, path, overwrite: true);
  }

  /// <summary>One recorded quantity: its change list and how it is rounded.</summary>
  internal sealed class Series {
    private readonly List<(int Second, string Text, double Number)> _changes = [];
    private readonly bool _word;

    public string Name { get; }
    public string? Unit { get; }
    private readonly int _digits;

    public Series(string name, string? unit, int digits, bool word) {
      Name = name;
      Unit = unit;
      _digits = digits;
      _word = word;
    }

    public static Series Number(string name, string unit, int digits) =>
      new(name, unit, digits, word: false);

    public static Series Word(string name) => new(name, null, 0, word: true);

    public void Add(int second, float value) =>
      Add(second, Trace.Fixed(value, _digits));

    public void Add(int second, string text) {
      if (_changes.Count > 0 && _changes[^1].Text == text)
        return;
      double number = _word
        ? 0d
        : double.Parse(text, CultureInfo.InvariantCulture);
      _changes.Add((second, text, number));
    }

    private (string Text, double Number) At(int second) {
      var change = _changes.Last(c => c.Second <= second);
      return (change.Text, change.Number);
    }

    public double Mean(int from, int to) {
      double sum = 0d;
      for (int t = from; t < to; t++)
        sum += At(t).Number;
      return sum / (to - from);
    }

    public string Longest(int from, int to) =>
      Enumerable
        .Range(from, to - from)
        .Select(t => At(t).Text)
        .GroupBy(w => w)
        .OrderByDescending(g => g.Count())
        .ThenBy(g => Enumerable.Range(from, to - from).First(t => At(t).Text == g.Key))
        .First()
        .Key;

    public object Steady(int from, int to) =>
      _word
        ? Longest(from, to)
        : double.Parse(SteadyText(from, to), CultureInfo.InvariantCulture);

    private string SteadyText(int from, int to) =>
      Trace.Fixed((float)Mean(from, to), _digits);

    public bool IsSteady(int from, int to) {
      int mid = from + (to - from) / 2;
      if (_word)
        return Longest(from, mid) == Longest(mid, to);
      double a = Mean(from, mid);
      double b = Mean(mid, to);
      double scale = Math.Max(Math.Abs(a), Math.Abs(b));
      double tolerance = scale < 1d ? 0.05 : 0.05 * scale;
      return Math.Abs(a - b) <= tolerance;
    }

    public string SteadyJson(int from, int to) =>
      _word
        ? Quote(Longest(from, to))
        : SteadyText(from, to);

    public string Json() =>
      Quote(Name)
      + ":["
      + string.Join(
        ",",
        _changes.Select(c =>
          "[" + c.Second.ToString(CultureInfo.InvariantCulture) + "," + (_word ? Quote(c.Text) : c.Text) + "]"
        )
      )
      + "]";
  }

  /// <summary>A pipe run read by the cell it passes through.</summary>
  internal sealed class RecordedRun {
    private readonly Scene _scene;
    private readonly BlockPos _pos;
    private readonly Series _medium = Series.Word("medium");
    private readonly Series _flow = Series.Number("flow", "l/s", Trace.VolumeDigits);
    private readonly Series _pressure = Series.Number("pressure", "atm", Trace.PressureDigits);
    private readonly Series _temperature = Series.Number("temperature", "C", Trace.TemperatureDigits);
    private readonly Series _volume = Series.Number("volume", "L", Trace.VolumeDigits);

    public string Id { get; }

    public RecordedRun(Scene scene, string id, BlockPos pos) {
      _scene = scene;
      _pos = pos.Copy();
      Id = id;
    }

    public IEnumerable<Series> All =>
      [_medium, _flow, _pressure, _temperature, _volume];

    public void Sample(int second) {
      PipeNetworkState? s = _scene.NetworkAt<PipeNetwork>(_pos)?.State;
      _medium.Add(second, s?.MediumType ?? "");
      _flow.Add(second, s?.FlowRate ?? 0f);
      _pressure.Add(second, s?.Pressure ?? 0f);
      _temperature.Add(second, s?.Temperature ?? 0f);
      _volume.Add(second, s?.Volume ?? 0f);
    }

    public string Json(int from, int to) =>
      "{\"at\":["
      + $"{_pos.X},{_pos.Y},{_pos.Z}"
      + "],\"units\":{"
      + string.Join(",", All.Skip(1).Select(s => Quote(s.Name) + ":" + Quote(s.Unit!)))
      + "},\n\"series\":{"
      + string.Join(",\n", All.Select(s => s.Json()))
      + "},\n\"steady\":{"
      + string.Join(",", All.Select(s => Quote(s.Name) + ":" + s.SteadyJson(from, to)))
      + "}}";
  }

  /// <summary>A machine: a state word, named values read each second, and constants.</summary>
  internal sealed class RecordedMachine {
    private readonly Series _state = Series.Word("state");
    private readonly List<(Series Series, Func<float> Read)> _values = [];
    private readonly List<(string Name, string Unit, float Value)> _constants = [];
    private Func<string>? _readState;

    public string Id { get; }
    public string Code { get; }

    public RecordedMachine(string id, string code) {
      Id = id;
      Code = code;
    }

    public IEnumerable<Series> All =>
      new[] { _state }.Concat(_values.Select(v => v.Series));

    /// <summary>Sets the word read as the machine's state each second.</summary>
    public RecordedMachine State(Func<string> read) {
      _readState = read;
      return this;
    }

    /// <summary>
    /// Records <paramref name="read"/> each second as <paramref name="name"/>, in
    /// <paramref name="unit"/> (<c>L</c>, <c>l/s</c>, <c>atm</c> or <c>C</c> set its rounding; any
    /// other unit, or none, rounds as a power).
    /// </summary>
    public RecordedMachine Value(string name, string unit, Func<float> read) {
      _values.Add((Series.Number(name, unit, DigitsOf(unit)), read));
      return this;
    }

    /// <summary>Records a figure the machine's code fixes, written once beside the series.</summary>
    public RecordedMachine Constant(string name, string unit, float value) {
      _constants.Add((name, unit, value));
      return this;
    }

    public void Sample(int second) {
      _state.Add(
        second,
        (_readState ?? throw new InvalidOperationException($"{Id} has no state.")).Invoke()
      );
      foreach (var (series, read) in _values)
        series.Add(second, read());
    }

    public string Json(int from, int to) =>
      "{\"code\":"
      + Quote(Code)
      + ",\"units\":{"
      + string.Join(",", _values.Select(v => Quote(v.Series.Name) + ":" + Quote(v.Series.Unit!)))
      + "},\n\"series\":{"
      + string.Join(",\n", All.Select(s => s.Json()))
      + "},\n\"steady\":{"
      + string.Join(",", All.Select(s => Quote(s.Name) + ":" + s.SteadyJson(from, to)))
      + "},\n\"constants\":{"
      + string.Join(
        ",",
        _constants.Select(c =>
          Quote(c.Name) + ":[" + Trace.Fixed(c.Value, DigitsOf(c.Unit)) + "," + Quote(c.Unit) + "]"
        )
      )
      + "}}";

    private static int DigitsOf(string unit) =>
      unit switch {
        "L" or "l/s" => Trace.VolumeDigits,
        "atm" => Trace.PressureDigits,
        "C" => Trace.TemperatureDigits,
        _ => Trace.PowerDigits,
      };
  }
}
