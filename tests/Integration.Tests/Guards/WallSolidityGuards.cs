using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Testing;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests.Guards;

/// <summary>
/// A block with full-cube collision can be built into a wall, and vanilla's room check reads a
/// face as closed only when it is side-solid. Every loaded ppex and smex block with one unit
/// collision box is side-solid on every face, or side-solid and side-opaque on exactly the faces
/// its shape fills edge to edge (<see cref="ShapeFaces"/>), and a blocktype left open anywhere is
/// listed with why. The rule is siex's <c>WallBlockSolidityGuards</c> with the shape read in, over
/// the blocks <see cref="LoadedLine"/> loaded.
/// </summary>
/// <remarks>A block side-solid on every face is not probed, unless a ruling holds it solid
/// beyond its shape: then its opacity still follows the shape.</remarks>
public class WallSolidityGuards(ITestOutputHelper output) {
  /// <summary>Blocktype held side-solid on every face whatever its shape fills, and the
  /// ruling.</summary>
  private static readonly Dictionary<string, string> SolidBeyondItsShape = new() {
    ["smex:blastfurnacetap"] = "fallen 2026-09-23: taps are solid and close rooms",
  };

  private const string OpenShape = "full-cube collision, open where its shape is open";

  private const string NoFace =
    "F-24 \"Faces the shape closes\": the shape fills no face of the cell edge to edge";

  /// <summary>Finding, and why it stands.</summary>
  private static readonly Dictionary<string, string> Allowed = new[] {
    "ppex:boilercornish",
    "ppex:boilerlancashire",
    "ppex:enginecornish",
    "ppex:enginefluidpump",
    "ppex:enginempgenerator",
    "ppex:enginewatt",
    "ppex:manualfluidpump",
    "ppex:mpfluidpump",
    "ppex:pipe",
    "smex:converter",
    "smex:convertercontrol",
    "smex:cowperstoveheatsink",
    "smex:engineairblower",
    "smex:hopperreinforced",
    "smex:moltenbarrel",
    "smex:mpblower",
  }
    .ToDictionary(b => $"{b}: {OpenShape}", _ => NoFace)
    .Concat(
      new Dictionary<string, string> {
        [$"smex:convertertransmission: {OpenShape}"] =
          "F-24: the shape closes its top and leaves the sides and floor open",
        [$"smex:hopperbell: {OpenShape}"] =
          "F-24: the charge passes through the open top and floor",
        [$"smex:moltencanal: {OpenShape}"] =
          "F-24: the trough's top and the ends its run passes through; the mold "
          + "pedestal closes only its floor",
        [$"smex:smokestack: {OpenShape}"] =
          "F-24: the intake's two pipe ends",
      }
    )
    .ToDictionary(e => e.Key, e => e.Value);

  /// <summary>Finding, and the defect it records.</summary>
  private static readonly Dictionary<string, string> KnownFindings = new();

  // Fails when a full-cube block's side-solid or side-opaque faces differ from the faces its
  // shape closes, e.g. the canal's down face turned from true to false, or when an open blocktype
  // is not listed.
  [Fact]
  public void Every_full_cube_block_is_side_solid_where_its_shape_is_closed() {
    List<Block> blocks = [.. LoadedLine.Blocks];
    var findings = new SortedDictionary<string, string>(System.StringComparer.Ordinal);
    var table = new List<string>();
    var shown = new HashSet<string>(System.StringComparer.Ordinal);
    int probed = 0;
    foreach (Block block in blocks.Where(IsFullCube)) {
      string blocktype = LoadedLine.Blocktype(block);
      bool ruled = SolidBeyondItsShape.ContainsKey(blocktype);
      if (!ruled && !OpenFaces(block).Any())
        continue;
      probed++;
      FaceJudgement[] faces = ShapeFaces.Of(block);
      string row = Row(block, faces);
      CompositeShape shape = block.Shape;
      if (shown.Add($"{blocktype} {shape.Base}@{shape.rotateX}/{shape.rotateY}/{shape.rotateZ}"))
        table.Add(row);

      foreach (string what in Findings(block, faces, ruled))
        findings.TryAdd($"{blocktype}: {what}", row);
    }
    output.WriteLine(
      $"{blocks.Count} blocks, {blocks.Count(IsFullCube)} full-cube, {probed} probed, "
        + $"{findings.Count} finding(s)"
    );
    foreach (var (finding, first) in findings)
      output.WriteLine($"  {finding} (first {first})");
    output.WriteLine("Allowed:");
    foreach (var (entry, reason) in Allowed.Concat(SolidBeyondItsShape))
      output.WriteLine($"  {entry}: {reason}");
    output.WriteLine("Probed, one row per blocktype, shape and turn:");
    foreach (string line in table.Order(System.StringComparer.Ordinal))
      output.WriteLine("  " + line);

    Assert.True(blocks.Count > 0, "neither mod loaded a block");
    FindingLists.Assert(findings.Keys, Allowed, KnownFindings);
  }

  // Fails when a partial box, a box list or no collision is counted as a full cube.
  [Fact]
  public void Only_one_unit_collision_box_is_a_full_cube() {
    Block Boxed(params Cuboidf[]? boxes) => new() { CollisionBoxes = boxes };

    Assert.True(IsFullCube(Boxed(new Cuboidf(0, 0, 0, 1, 1, 1))));
    Assert.False(IsFullCube(Boxed(new Cuboidf(0, 0, 0, 1, 0.5f, 1))));
    Assert.False(
      IsFullCube(
        Boxed(new Cuboidf(0, 0, 0, 1, 1, 1), new Cuboidf(0, 0, 0, 1, 1, 1))
      )
    );
    Assert.False(IsFullCube(Boxed(null)));
  }

  // Fails when a ruled block is judged by its shape's faces instead of all six, when opacity or
  // an unjudged face goes unreported, or when a ruled block is listed as open.
  [Fact]
  public void A_block_is_found_where_it_differs_from_its_shape() {
    FaceJudgement[] floor =
    [
      .. BlockFacing.ALLFACES.Select(f =>
        f == BlockFacing.DOWN
          ? new FaceJudgement(FaceFill.Closed, 0)
          : new FaceJudgement(FaceFill.Open, 256)
      ),
    ];
    Block Declared(bool[] solid, bool[] opaque) {
      var block = new Block();
      for (int i = 0; i < 6; i++) {
        block.SideSolid[i] = solid[i];
        block.SideOpaque[i] = opaque[i];
      }
      return block;
    }
    bool[] all = [true, true, true, true, true, true];
    bool[] down = [false, false, false, false, false, true];

    Assert.Empty(Findings(Declared(all, down), floor, ruled: true));
    Assert.Equal(
      ["side-opaque differs from the faces its shape closes"],
      Findings(Declared(all, all), floor, ruled: true)
    );
    Assert.Equal(
      ["not side-solid on every face, as ruled"],
      Findings(Declared(down, down), floor, ruled: true)
    );
    Assert.Equal([OpenShape], Findings(Declared(down, down), floor, ruled: false));
    Assert.Equal(
      ["side-solid differs from the faces its shape closes", OpenShape],
      Findings(Declared(all, down), floor, ruled: false)
    );
    FaceJudgement[] unjudged = [.. floor];
    unjudged[BlockFacing.NORTH.Index] = new FaceJudgement(FaceFill.Undecided, 0);
    Assert.Equal(
      ["north face not judged, a turned element holds it", OpenShape],
      Findings(Declared(down, down), unjudged, ruled: false)
    );
  }

  // Fails when the probe samples outside the cell instead of inside it (the slab's floor reads
  // open, its top closed) or miscounts the open area.
  [Fact]
  public void A_face_is_closed_only_where_the_shape_fills_it() {
    FaceJudgement[] slab = ShapeFaces.Of(Shaped(Cube("0,0,0", "16,8,16")), 0, 0, 0);

    Assert.Equal("d", Closed(slab));
    Assert.Equal(128, slab[BlockFacing.NORTH.Index].OpenArea, 6);
    Assert.Equal(256, slab[BlockFacing.UP.Index].OpenArea, 6);
  }

  // Fails when the variant's turn is not applied: the north wall turned 90 degrees stays north.
  [Fact]
  public void A_turned_variant_closes_the_turned_face() {
    Assert.Equal(
      "w",
      Closed(ShapeFaces.Of(Shaped(Cube("0,0,0", "16,16,2")), 0, 90, 0))
    );
  }

  // Fails when a child is placed without its parent's offset, closing north instead of south.
  [Fact]
  public void A_child_element_sits_where_its_parent_puts_it() {
    string child =
      """{ "name": "p", "from": [0,0,8], "to": [0,0,8], "children": [ """
      + Cube("0,0,0", "16,16,8")
      + " ] }";
    Assert.Equal("s", Closed(ShapeFaces.Of(Shaped(child), 0, 0, 0)));
  }

  // Fails when an element without faces is counted as solid.
  [Fact]
  public void An_element_without_faces_holds_nothing() {
    string group = """{ "name": "g", "from": [0,0,0], "to": [16,16,16] }""";
    Assert.Equal("", Closed(ShapeFaces.Of(Shaped(group), 0, 0, 0)));
  }

  // Fails when a flat element is given no thickness, leaving the face it lies on open.
  [Fact]
  public void A_flat_element_closes_the_face_it_lies_on() {
    Assert.Equal(
      "s",
      Closed(ShapeFaces.Of(Shaped(Cube("0,0,16", "16,16,16")), 0, 0, 0))
    );
  }

  // Fails when element edges are left off the sampling grid: a gap of 0.05 between two blocks
  // falls inside one 1/64 cell whose centre the second block holds.
  [Fact]
  public void A_gap_narrower_than_the_grid_is_open() {
    FaceJudgement[] split = ShapeFaces.Of(
      Shaped(Cube("0,0,0", "8,16,16"), Cube("8.05,0,0", "16,16,16")),
      0,
      0,
      0
    );

    Assert.Equal("e,w", Closed(split));
    Assert.Equal(0.8, split[BlockFacing.NORTH.Index].OpenArea, 6);
  }

  // Fails when a turned element's cover is taken as proven.
  [Fact]
  public void A_face_only_a_turned_element_holds_is_not_judged() {
    string turned =
      """{ "name": "t", "from": [-1,0,-1], "to": [17,16,17], "rotationOrigin": [8,8,8], "rotationY": 1, """
      + Faces
      + " }";
    FaceJudgement[] faces = ShapeFaces.Of(Shaped(turned), 0, 0, 0);

    Assert.All(faces, f => Assert.Equal(FaceFill.Undecided, f.Fill));
  }

  private const string Faces =
    """
    "faces": { "north": { "texture": "#t" }, "east": { "texture": "#t" },
      "south": { "texture": "#t" }, "west": { "texture": "#t" },
      "up": { "texture": "#t" }, "down": { "texture": "#t" } }
    """;

  private static string Cube(string from, string to) =>
    $$"""{ "name": "c", "from": [{{from}}], "to": [{{to}}], {{Faces}} }""";

  private static Shape Shaped(params string[] elements) =>
    JsonUtil.FromString<Shape>(
      $$"""{ "textures": { "t": "game:block/t" }, "elements": [ {{string.Join(", ", elements)}} ] }"""
    )!;

  private static string Closed(FaceJudgement[] faces) =>
    string.Join(
      ",",
      BlockFacing.ALLFACES.Where(f => faces[f.Index].Fill == FaceFill.Closed)
        .Select(f => f.Code[0])
    );

  /// <summary>The findings one full-cube block gives against the faces its shape closes, each
  /// without its blocktype.</summary>
  private static List<string> Findings(Block block, FaceJudgement[] faces, bool ruled) {
    var findings = new List<string>();
    foreach (BlockFacing facing in BlockFacing.ALLFACES)
      if (faces[facing.Index].Fill == FaceFill.Undecided)
        findings.Add($"{facing.Code} face not judged, a turned element holds it");
    bool[] closed = [.. faces.Select(f => f.Fill == FaceFill.Closed)];
    bool[] solid = [.. BlockFacing.ALLFACES.Select(f => block.SideSolid[f.Index])];
    bool[] opaque = [.. BlockFacing.ALLFACES.Select(f => block.SideOpaque[f.Index])];
    if (ruled ? solid.Any(s => !s) : !solid.SequenceEqual(closed))
      findings.Add(
        ruled
          ? "not side-solid on every face, as ruled"
          : "side-solid differs from the faces its shape closes"
      );
    if (!opaque.SequenceEqual(closed))
      findings.Add("side-opaque differs from the faces its shape closes");
    if (!ruled && closed.Any(c => !c))
      findings.Add(OpenShape);
    return findings;
  }

  private static bool IsFullCube(Block block) =>
    block.CollisionBoxes is [var box]
    && box.X1 == 0
    && box.Y1 == 0
    && box.Z1 == 0
    && box.X2 == 1
    && box.Y2 == 1
    && box.Z2 == 1;

  private static IEnumerable<string> OpenFaces(Block block) =>
    BlockFacing.ALLFACES.Where(f => !block.SideSolid[f.Index]).Select(f => f.Code);

  // One variant: the faces its shape closes, the open area of the rest in square shape units, and
  // the faces it declares side-solid and side-opaque.
  private static string Row(Block block, FaceJudgement[] faces) {
    string Faces(System.Func<BlockFacing, bool> which) =>
      string.Join(",", BlockFacing.ALLFACES.Where(which).Select(f => f.Code[0]));
    string open = string.Join(
      " ",
      BlockFacing.ALLFACES
        .Where(f => faces[f.Index].Fill != FaceFill.Closed)
        .Select(f =>
          faces[f.Index].Fill == FaceFill.Undecided
            ? $"{f.Code[0]}?"
            : $"{f.Code[0]}{faces[f.Index].OpenArea:0.#}"
        )
    );
    return $"{block.Code}: shape {block.Shape.Base} turned {block.Shape.rotateY}, "
      + $"closes [{Faces(f => faces[f.Index].Fill == FaceFill.Closed)}], open [{open}], "
      + $"solid [{Faces(f => block.SideSolid[f.Index])}], "
      + $"opaque [{Faces(f => block.SideOpaque[f.Index])}]";
  }
}
