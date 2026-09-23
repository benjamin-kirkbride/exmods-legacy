using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Integration.Tests.Saves;
using Vintagestory.API.Config;

namespace Integration.Tests.Pins;

/// <summary>
/// The observables of one scene, one line per entity per tick, each number rounded to the precision
/// its pin asserts. <see cref="Save()"/> records the text under
/// <c>tests/Integration.Tests/Pins/Traces/{game version}</c>: the committed <c>{Name}.trace</c> is the
/// reference, and a run whose text differs leaves it alone and writes <c>{Name}.received.trace</c>
/// beside it.
/// </summary>
internal sealed class Trace
{
  /// <summary>Decimal places of a volume in litres.</summary>
  public const int VolumeDigits = 2;

  /// <summary>Decimal places of a pressure in atm.</summary>
  public const int PressureDigits = 3;

  /// <summary>Decimal places of a temperature in degrees C.</summary>
  public const int TemperatureDigits = 1;

  /// <summary>Decimal places of a mechanical load, speed or power budget.</summary>
  public const int PowerDigits = 3;

  private const string WriteVariable = "LEGACY_WRITE_TRACES";

  private readonly StringBuilder _text = new();

  /// <summary>The file name the trace is saved under, without its extension.</summary>
  public string Name { get; }

  public Trace(string name)
  {
    Name = name;
    _text.Append("# ").Append(name).Append('\n');
  }

  /// <summary>Appends one line: the tick, then <paramref name="fields"/> separated by spaces.</summary>
  public Trace Line(int tick, params string[] fields)
  {
    _text.Append(tick.ToString(CultureInfo.InvariantCulture));
    foreach (string field in fields)
      _text.Append(' ').Append(field);
    _text.Append('\n');
    return this;
  }

  /// <summary>The trace as recorded so far.</summary>
  public string Text => _text.ToString();

  public static string Litres(float value) => Fixed(value, VolumeDigits);

  public static string Atm(float value) => Fixed(value, PressureDigits);

  public static string Celsius(float value) => Fixed(value, TemperatureDigits);

  public static string Power(float value) => Fixed(value, PowerDigits);

  public static string Flag(bool value) => value ? "1" : "0";

  /// <summary>
  /// <paramref name="value"/> rounded to <paramref name="digits"/> places, invariant culture, with
  /// negative zero written as zero.
  /// </summary>
  public static string Fixed(float value, int digits)
  {
    double rounded = Math.Round(
      (double)value,
      digits,
      MidpointRounding.AwayFromZero
    );
    if (rounded == 0d)
      rounded = 0d;
    return rounded.ToString("F" + digits, CultureInfo.InvariantCulture);
  }

  /// <summary>The folder this game version's traces live in, such as <c>Traces/1.22</c>.</summary>
  public static string Folder =>
    Path.Combine(
      SaveGoldens.RepoRoot(),
      "tests",
      "Integration.Tests",
      "Pins",
      "Traces",
      string.Join('.', GameVersion.ShortGameVersion.Split('.')[..2])
    );

  /// <summary>
  /// Records the trace in <see cref="Folder"/> and returns its text. The committed
  /// <c>{Name}.trace</c> is written only when it is absent, or when <c>LEGACY_WRITE_TRACES</c> is
  /// <c>1</c> or a comma-separated list naming the trace. Otherwise a text that differs from it is
  /// written to <c>{Name}.received.trace</c>, and a text that matches it deletes that file. A
  /// difference does not fail the caller.
  /// </summary>
  public string Save() =>
    Save(Folder, Writes(Environment.GetEnvironmentVariable(WriteVariable), Name));

  /// <summary>
  /// <see cref="Save()"/> into <paramref name="folder"/>, rewriting the committed trace when
  /// <paramref name="write"/> is set. A file is replaced whole, through a temporary file.
  /// </summary>
  internal string Save(string folder, bool write)
  {
    string text = Text;
    string committed = Path.Combine(folder, Name + ".trace");
    string received = Path.Combine(folder, Name + ".received.trace");
    Directory.CreateDirectory(folder);
    if (write || !File.Exists(committed))
    {
      Replace(committed, text);
      File.Delete(received);
    }
    else if (File.ReadAllText(committed) == text)
      File.Delete(received);
    else
      Replace(received, text);
    return text;
  }

  /// <summary>
  /// Whether a write variable's <paramref name="value"/> (<c>1</c>, or a comma-separated list of
  /// trace names; null when unset) names the trace <paramref name="name"/>.
  /// </summary>
  internal static bool Writes(string? value, string name) =>
    value == "1" || (value ?? "").Split(',').Any(n => n.Trim() == name);

  private static void Replace(string path, string text)
  {
    if (File.Exists(path) && File.ReadAllText(path) == text)
      return;
    string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
    File.WriteAllText(temp, text);
    File.Move(temp, path, overwrite: true);
  }
}
