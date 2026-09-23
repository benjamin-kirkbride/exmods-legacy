using System;
using System.Globalization;
using System.IO;
using System.Text;
using Integration.Tests.Saves;
using Vintagestory.API.Config;

namespace Integration.Tests.Pins;

/// <summary>
/// The observables of one scene, one line per entity per tick, each number rounded to the precision
/// its pin asserts. <see cref="Save"/> writes the text to
/// <c>tests/Integration.Tests/Pins/Traces/{game version}</c>, where a replay of the same script on
/// another build of the same game version is diffed against it.
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
  /// Writes the trace to <see cref="Folder"/> as <c>{Name}.trace</c> and returns its text. An
  /// unchanged file is left untouched; a changed one is replaced through a temporary file, so runs
  /// on several game versions at once never leave a partial trace.
  /// </summary>
  public string Save()
  {
    string text = Text;
    string path = Path.Combine(Folder, Name + ".trace");
    if (File.Exists(path) && File.ReadAllText(path) == text)
      return text;
    Directory.CreateDirectory(Folder);
    string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
    File.WriteAllText(temp, text);
    File.Move(temp, path, overwrite: true);
    return text;
  }
}
