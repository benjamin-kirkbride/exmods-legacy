using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using ExpandedLib.Config;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace PipesAndPowerExpanded;

/// <summary>
/// Pipe tunables a ppex config file can hold that <see cref="PpexConfig"/> does not carry. The pipe
/// network and the machines read exlib's copies, in the <c>exlib</c> section of
/// <c>ModConfig/ex_values.json</c>, which iiex shares, so a value tuned in ppex's config has no
/// effect.
/// </summary>
public static class PpexRemovedTunables {
  /// <summary>Each removed key with the default it had in ppex, which equals exlib's default.</summary>
  public static readonly IReadOnlyList<(string Key, float Default)> Keys =
  [
    ("LitresPerPipe", 30f),
    ("GasLeakRate", 8f),
    ("LiquidLeakRate", 10f),
    ("EvaporationLitresPerDay", 50f),
    ("PipeOverpressureSeconds", 30f),
  ];

  /// <summary>
  /// Logs one Notification for each removed key that ppex's config holds at a value other than its
  /// default, naming the key, the value and exlib's section. The config read is the <c>ppex</c>
  /// section of <see cref="PpexValues.ConfigFileName"/>, or, while that section is absent, the first
  /// legacy flat file <see cref="PpexValues.Load"/> would fold into it. Logs nothing for a key that
  /// is absent. Server side only.
  /// </summary>
  /// <remarks>Call before <see cref="PpexValues.Load"/>: its save drops the removed keys from the
  /// section, so a later call finds none.</remarks>
  public static void Report(ICoreAPI api) {
    if (api.Side != EnumAppSide.Server)
      return;
    JObject? held = HeldConfig(api);
    if (held == null)
      return;

    foreach ((string key, float def) in Keys) {
      JToken? value = held.GetValue(key, StringComparison.OrdinalIgnoreCase);
      if (value == null || IsDefault(value, def))
        continue;
      api.Logger.Notification(
        "[ppex] Config value {0} = {1} is not read from ppex's config; the pipe network reads "
          + "{0} from the exlib section of {2}.",
        key,
        value is JValue v
          ? Convert.ToString(v.Value, CultureInfo.InvariantCulture)
          : value.ToString(),
        PpexValues.ConfigFileName
      );
    }
  }

  private static JObject? HeldConfig(ICoreAPI api) {
    var doc = ExConfigDocument.ForFile(api, PpexValues.ConfigFileName);
    if (doc.HasSection("ppex"))
      return doc.GetSection<JObject>("ppex");

    string[] legacy =
      typeof(PpexConfig)
        .GetCustomAttribute<ExConfigRegisterAttribute>()
        ?.LegacyFileNames ?? [];
    foreach (string name in legacy) {
      string path = Path.Combine(GamePaths.ModConfig, name);
      if (!File.Exists(path))
        continue;
      try {
        return JObject.Parse(File.ReadAllText(path));
      } catch (Exception) {
        // The fold in PpexValues.Load reports an unreadable file.
        return null;
      }
    }
    return null;
  }

  private static bool IsDefault(JToken value, float def) =>
    value.Type is JTokenType.Float or JTokenType.Integer
    && (float)value.Value<double>() == def;
}
