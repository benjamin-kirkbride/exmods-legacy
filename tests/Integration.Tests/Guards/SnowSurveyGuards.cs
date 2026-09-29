using System;
using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Structures;
using ExpandedLib.Testing;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.Guards;

/// <summary>
/// Where weather snow meets a ppex or smex layout open to the sky, read from the
/// <c>multiblockStructure</c> of every block <see cref="LoadedLine"/> loaded: in a column topped by
/// an open cell, the open cell just above the first solid cell and that solid cell carry
/// <c>nosnow</c> in <c>multiblockRoles</c>, since snow settles in the one and lies on the other.
/// The rule is <see cref="OpenLayoutCells"/>', which reads only code-first definitions.
/// </summary>
/// <remarks>An open cell is one whose wanted code's first alternative is air. Findings name the
/// blocktype and the structure-local cell, so a layout shared by every variant is one
/// finding.</remarks>
public class SnowSurveyGuards(ITestOutputHelper output) {
  /// <summary>Finding, and why it stands.</summary>
  private static readonly Dictionary<string, string> Allowed = new();

  /// <summary>Finding, and the defect it records.</summary>
  private static readonly Dictionary<string, string> KnownFindings = new();

  // Fails when an open column's marks differ from the known findings, e.g. the smoke stack's
  // nosnow role removed, or when the survey stops reading open cells as air.
  [Fact]
  public void Every_open_column_marks_where_snow_settles_and_lies() {
    var layouts = new HashSet<string>(StringComparer.Ordinal);
    var findings = new SortedSet<string>(StringComparer.Ordinal);
    int cells = 0;
    foreach (Block block in LoadedLine.Blocks) {
      if (
        block.Attributes?["multiblockStructure"]?.Token
        is not JObject structure
      )
        continue;
      string blocktype = LoadedLine.Blocktype(block);
      bool first = layouts.Add(blocktype);
      var marked = MultiblockCellRoles
        .FromAttributes(block.Attributes)
        .CellsOf(CellRoles.NoSnow);
      foreach (var (cell, what) in Required(structure)) {
        if (first)
          cells++;
        if (!marked.Contains(cell))
          findings.Add(
            $"{blocktype}: ({cell.X},{cell.Y},{cell.Z}) {what} carries no nosnow"
          );
      }
    }
    output.WriteLine(
      $"{layouts.Count} layouts, {cells} cells required, {findings.Count} finding(s)"
    );
    foreach (string finding in findings)
      output.WriteLine("  " + finding);

    Assert.True(layouts.Count > 0, "no ppex or smex block carries a layout");
    FindingLists.Assert(findings, Allowed, KnownFindings);
  }

  // Fails when an air cell is read as solid or a solid one as open, or when a column topped by a
  // solid cell or holding no solid cell requires a mark.
  [Fact]
  public void An_open_column_requires_the_cell_above_its_floor_and_the_floor() {
    var structure = JObject.Parse(
      """
      {
        "blockNumbers": { "game:air": 1, "game:cobblestone-granite": 2,
          "game:@(air|log-.*)": 3 },
        "offsets": [
          { "x": 0, "y": 0, "z": 0, "w": 2 }, { "x": 0, "y": 1, "z": 0, "w": 1 },
          { "x": 0, "y": 2, "z": 0, "w": 3 },
          { "x": 1, "y": 0, "z": 0, "w": 1 }, { "x": 1, "y": 1, "z": 0, "w": 2 },
          { "x": 2, "y": 0, "z": 0, "w": 1 }
        ]
      }
      """
    );

    Assert.Equal(
      [((0, 0, 0), "snow lies on"), ((0, 1, 0), "snow settles in")],
      Required(structure).OrderBy(c => c.Cell.Y)
    );
  }

  /// <summary>The cells <paramref name="structure"/> must mark: in each column whose top cell is
  /// open and which holds a solid cell, the first solid cell below the open run and the open cell
  /// just above it.</summary>
  private static IEnumerable<((int X, int Y, int Z) Cell, string What)> Required(
    JObject structure
  ) {
    var numbers = ((JObject)structure["blockNumbers"]!)
      .Properties()
      .ToDictionary(p => (int)p.Value!, p => p.Name);
    var open = ((JArray)structure["offsets"]!).ToDictionary(
      o => ((int)o["x"]!, (int)o["y"]!, (int)o["z"]!),
      o => AirSatisfied(numbers[(int)o["w"]!])
    );
    foreach (
      var column in open
        .Keys.GroupBy(c => (X: c.Item1, Z: c.Item3))
        .OrderBy(g => g.Key.X)
        .ThenBy(g => g.Key.Z)
    ) {
      var down = column.OrderByDescending(c => c.Item2).ToList();
      if (!open[down[0]])
        continue;
      int solid = down.FindIndex(c => !open[c]);
      if (solid < 0)
        continue;
      yield return (down[solid - 1], "snow settles in");
      yield return (down[solid], "snow lies on");
    }
  }

  // Satisfied by leaving the cell empty: the first alternative of the wanted path is air.
  private static bool AirSatisfied(string wanted) {
    string path = wanted[(wanted.IndexOf(':') + 1)..];
    if (path.StartsWith("@(", StringComparison.Ordinal)) {
      int depth = 0,
        end = 2;
      for (; end < path.Length; end++) {
        char c = path[end];
        if (c == '(')
          depth++;
        else if (c == ')' && depth-- == 0)
          break;
        else if (c == '|' && depth == 0)
          break;
      }
      path = path[2..end];
    }
    return path.Replace("*", "x") == "air";
  }
}
