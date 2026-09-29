using System;
using System.Collections.Generic;

namespace Integration.Tests.Guards;

/// <summary>The rule that decides whether a released code still has a path into the loaded
/// line.</summary>
internal static class ReleasedCodePaths {
  /// <summary>The end of the remap chain that starts at <paramref name="from"/>: the first code
  /// with no declared remap, or the last one reached before the chain returns to a code it has
  /// passed.</summary>
  /// <param name="from">The code the walk starts at.</param>
  /// <param name="declared">Old code to new code; the caller puts the first declaration of each
  /// old code here.</param>
  /// <returns><paramref name="from"/> when nothing declares it.</returns>
  internal static string Terminal(
    string from,
    IReadOnlyDictionary<string, string> declared
  ) {
    string cursor = from;
    var seen = new HashSet<string> { cursor };
    while (declared.TryGetValue(cursor, out string? next) && seen.Add(next))
      cursor = next;
    return cursor;
  }

  /// <summary>Whether <paramref name="code"/> has a path: it loads, or its chain ends at a code that
  /// loads or at a declared removal.</summary>
  /// <param name="code">The released code.</param>
  /// <param name="loads">Whether a code loads a block or an item in the loaded line.</param>
  /// <param name="declared">Old code to new code, first declaration of each old code.</param>
  /// <param name="removed">The codes a removal declares.</param>
  /// <returns>Null when there is a path; otherwise the code the chain ends at, which is
  /// <paramref name="code"/> itself when nothing declares it.</returns>
  internal static string? DeadEnd(
    string code,
    Func<string, bool> loads,
    IReadOnlyDictionary<string, string> declared,
    IReadOnlySet<string> removed
  ) {
    if (loads(code))
      return null;
    string end = Terminal(code, declared);
    return removed.Contains(end) || loads(end) ? null : end;
  }

  /// <summary>The remap table the chain walk reads.</summary>
  /// <param name="remaps">Every declared (old code, new code) pair, in discovery order.</param>
  /// <returns>Old code to new code; when a code is declared twice, its first declaration.</returns>
  internal static Dictionary<string, string> Declared(
    IEnumerable<(string Old, string New)> remaps
  ) {
    var declared = new Dictionary<string, string>();
    foreach (var (old, next) in remaps)
      declared.TryAdd(old, next);
    return declared;
  }

  /// <summary>The key of a finding line: its first word, the code or class it names.</summary>
  internal static string KeyOf(string finding) => finding.Split(' ')[0];
}
