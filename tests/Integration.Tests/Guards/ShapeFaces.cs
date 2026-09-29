using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandedLib.Testing;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Integration.Tests.Guards;

/// <summary>How far a shape fills one face of its cell.</summary>
internal enum FaceFill {
  /// <summary>Every point of the face lies in an unturned element.</summary>
  Closed,

  /// <summary>Some point of the face lies in no element.</summary>
  Open,

  /// <summary>Every sampled point lies in an element, some only in a turned one, whose cover
  /// between the samples is not proven.</summary>
  Undecided,
}

/// <summary>One face of a cell as <see cref="ShapeFaces"/> judged it.</summary>
/// <param name="Fill">Whether the shape fills the face edge to edge.</param>
/// <param name="OpenArea">The area no element holds, in square shape units (a whole face is
/// 256); exact where only unturned elements cross the face, sampled on a 1/64 block grid where a
/// turned one does.</param>
internal readonly record struct FaceJudgement(FaceFill Fill, double OpenArea);

/// <summary>
/// Which faces of its cell a shape fills edge to edge: a face is closed when every point of it, a
/// 1/1024 block inside the cell, lies in an element of the shape as the block's variant turns it.
/// </summary>
/// <remarks>Every element that draws a face counts, whatever the block's <c>selectiveElements</c>
/// name, since a built machine's block entity draws the rest; an element with no face is a group
/// and holds nothing. An element no thicker than 1/512 block holds the face it lies on. Parts of a
/// shape outside the cell count only where they reach into it. Each face is judged at the centre of
/// every cell of a grid laid on the 1/64 block lines and on every edge an unturned element draws
/// across the face, so unturned elements are judged exactly.</remarks>
internal static class ShapeFaces {
  private const double Depth = 1.0 / 1024;
  private const double Thin = 2 * Depth;
  private const double Epsilon = 1e-6;
  private const int Steps = 64;

  private static readonly Dictionary<string, FaceJudgement[]> Probed = [];

  /// <summary>Each face of <paramref name="block"/>'s cell, by <see cref="BlockFacing.Index"/>,
  /// as its shape fills it; probed once per shape and turn.</summary>
  /// <param name="block">A block whose <see cref="Block.Shape"/> names a shape file of ppex or
  /// smex, read from the mod's <c>assets</c> in this repository.</param>
  /// <exception cref="InvalidOperationException">The block names no shape, or its shape file is
  /// missing or does not parse.</exception>
  public static FaceJudgement[] Of(Block block) {
    CompositeShape composite =
      block.Shape?.Base == null
        ? throw new InvalidOperationException($"{block.Code} names no shape")
        : block.Shape;
    string key =
      $"{composite.Base}@{composite.rotateX}/{composite.rotateY}/{composite.rotateZ}";
    lock (Probed) {
      if (Probed.TryGetValue(key, out FaceJudgement[]? known))
        return known;
      string domain = composite.Base.Domain;
      string file = Path.Combine(
        RepoPaths.Root,
        domain,
        "assets",
        domain,
        "shapes",
        composite.Base.Path + ".json"
      );
      Shape shape =
        (File.Exists(file) ? JsonUtil.FromString<Shape>(File.ReadAllText(file)) : null)
        ?? throw new InvalidOperationException(
          $"{block.Code}: shape {file} does not load"
        );
      return Probed[key] = Of(
        shape,
        composite.rotateX,
        composite.rotateY,
        composite.rotateZ
      );
    }
  }

  /// <summary>Each face of the cell, by <see cref="BlockFacing.Index"/>, as
  /// <paramref name="shape"/> fills it when turned about the cell's centre.</summary>
  /// <param name="shape">The shape, as the game loads it.</param>
  /// <param name="rotateX">The variant's turn about x, in degrees.</param>
  /// <param name="rotateY">The variant's turn about y, in degrees.</param>
  /// <param name="rotateZ">The variant's turn about z, in degrees.</param>
  public static FaceJudgement[] Of(
    Shape shape,
    double rotateX,
    double rotateY,
    double rotateZ
  ) {
    double[] turn = Mul(
      Mul(Translate(0.5, 0.5, 0.5), Rotate(rotateX, rotateY, rotateZ)),
      Translate(-0.5, -0.5, -0.5)
    );
    var solids = new List<Solid>();
    foreach (ShapeElement element in shape.Elements ?? [])
      Collect(element, turn, solids);
    return [.. BlockFacing.ALLFACES.Select(f => Judge(f, solids))];
  }

  private sealed record Solid(
    double[] Inverse,
    double[] Size,
    double[] Min,
    double[] Max,
    bool Turned
  );

  private static void Collect(
    ShapeElement element,
    double[] parent,
    List<Solid> solids
  ) {
    double[] from = Scaled(element.From);
    double[] to = Scaled(element.To);
    double[] origin = element.RotationOrigin == null
      ? [0, 0, 0]
      : Scaled(element.RotationOrigin);
    double[] world = Mul(
      parent,
      Mul(
        Mul(
          Mul(
            Translate(origin[0], origin[1], origin[2]),
            Rotate(element.RotationX, element.RotationY, element.RotationZ)
          ),
          ScaleBy(element.ScaleX, element.ScaleY, element.ScaleZ)
        ),
        Translate(from[0] - origin[0], from[1] - origin[1], from[2] - origin[2])
      )
    );
    if (element.FacesResolved?.Any(f => f != null) == true) {
      double[] size = [to[0] - from[0], to[1] - from[1], to[2] - from[2]];
      double[] min = [double.MaxValue, double.MaxValue, double.MaxValue];
      double[] max = [double.MinValue, double.MinValue, double.MinValue];
      for (int corner = 0; corner < 8; corner++) {
        double[] p = Apply(
          world,
          (corner & 4) != 0 ? size[0] : 0,
          (corner & 2) != 0 ? size[1] : 0,
          (corner & 1) != 0 ? size[2] : 0
        );
        for (int a = 0; a < 3; a++) {
          min[a] = Math.Min(min[a], p[a]);
          max[a] = Math.Max(max[a], p[a]);
        }
      }
      bool turned = Enumerable
        .Range(0, 3)
        .Any(row =>
          Enumerable.Range(0, 3).Count(col => Math.Abs(world[col * 4 + row]) > Epsilon)
          != 1
        );
      solids.Add(new Solid(Invert(world), size, min, max, turned));
    }
    foreach (ShapeElement child in element.Children ?? [])
      Collect(child, world, solids);
  }

  private static FaceJudgement Judge(BlockFacing facing, List<Solid> solids) {
    int axis = facing.Axis switch {
      EnumAxis.X => 0,
      EnumAxis.Y => 1,
      _ => 2,
    };
    int u = axis == 0 ? 1 : 0;
    int v = axis == 2 ? 1 : 2;
    double depth = facing.Normali[axis] > 0 ? 1 - Depth : Depth;
    var near = solids
      .Where(s =>
        s.Min[axis] <= depth + Thin
        && s.Max[axis] >= depth - Thin
        && s.Max[u] > 0
        && s.Min[u] < 1
        && s.Max[v] > 0
        && s.Min[v] < 1
      )
      .ToList();
    double[] us = Lines(near.Where(s => !s.Turned), u);
    double[] vs = Lines(near.Where(s => !s.Turned), v);
    double open = 0;
    bool unproven = false;
    for (int i = 0; i + 1 < us.Length; i++)
      for (int j = 0; j + 1 < vs.Length; j++) {
        double[] point = new double[3];
        point[axis] = depth;
        point[u] = (us[i] + us[i + 1]) / 2;
        point[v] = (vs[j] + vs[j + 1]) / 2;
        var holding = near.Where(s => Holds(s, point)).ToList();
        if (holding.Count == 0)
          open += (us[i + 1] - us[i]) * (vs[j + 1] - vs[j]) * 256;
        else if (holding.All(s => s.Turned))
          unproven = true;
      }
    return new FaceJudgement(
      open > 0 ? FaceFill.Open
        : unproven ? FaceFill.Undecided
        : FaceFill.Closed,
      open
    );
  }

  private static double[] Lines(IEnumerable<Solid> solids, int axis) =>
    [
      .. Enumerable
        .Range(0, Steps + 1)
        .Select(i => (double)i / Steps)
        .Concat(solids.SelectMany(s => new[] { s.Min[axis], s.Max[axis] }))
        .Where(c => c >= 0 && c <= 1)
        .Select(c => Math.Round(c, 9))
        .Distinct()
        .Order(),
    ];

  private static bool Holds(Solid solid, double[] point) {
    double[] local = Apply(solid.Inverse, point[0], point[1], point[2]);
    for (int a = 0; a < 3; a++) {
      double slack = solid.Size[a] <= Thin ? Thin : Epsilon;
      if (local[a] < -slack || local[a] > solid.Size[a] + slack)
        return false;
    }
    return true;
  }

  // Column-major 4x4 matrices in block units, as the game's Mat4f.

  private static double[] Scaled(double[]? shapeUnits) =>
    shapeUnits == null
      ? [0, 0, 0]
      : [shapeUnits[0] / 16, shapeUnits[1] / 16, shapeUnits[2] / 16];

  private static double[] Identity() =>
    [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1];

  private static double[] Translate(double x, double y, double z) {
    double[] m = Identity();
    m[12] = x;
    m[13] = y;
    m[14] = z;
    return m;
  }

  private static double[] ScaleBy(double x, double y, double z) {
    double[] m = Identity();
    m[0] = x;
    m[5] = y;
    m[10] = z;
    return m;
  }

  // Mat4f.RotateByXYZ: about x, then y, then z, in degrees.
  private static double[] Rotate(double degX, double degY, double degZ) {
    double sx = Math.Sin(degX * Math.PI / 180),
      cx = Math.Cos(degX * Math.PI / 180);
    double sy = Math.Sin(degY * Math.PI / 180),
      cy = Math.Cos(degY * Math.PI / 180);
    double sz = Math.Sin(degZ * Math.PI / 180),
      cz = Math.Cos(degZ * Math.PI / 180);
    double[] m = Identity();
    m[0] = cy * cz;
    m[1] = sx * sy * cz + cx * sz;
    m[2] = -cx * sy * cz + sx * sz;
    m[4] = -cy * sz;
    m[5] = cx * cz - sx * sy * sz;
    m[6] = sx * cz + cx * sy * sz;
    m[8] = sy;
    m[9] = -sx * cy;
    m[10] = cx * cy;
    return m;
  }

  private static double[] Mul(double[] a, double[] b) {
    double[] m = new double[16];
    for (int col = 0; col < 4; col++)
      for (int row = 0; row < 4; row++)
        for (int k = 0; k < 4; k++)
          m[col * 4 + row] += a[k * 4 + row] * b[col * 4 + k];
    return m;
  }

  private static double[] Apply(double[] m, double x, double y, double z) =>
    [
      m[0] * x + m[4] * y + m[8] * z + m[12],
      m[1] * x + m[5] * y + m[9] * z + m[13],
      m[2] * x + m[6] * y + m[10] * z + m[14],
    ];

  // An affine inverse: the transposed inverse of the upper 3x3, then the translation undone.
  private static double[] Invert(double[] m) {
    double a = m[0], b = m[4], c = m[8];
    double d = m[1], e = m[5], f = m[9];
    double g = m[2], h = m[6], k = m[10];
    double det = a * (e * k - f * h) - b * (d * k - f * g) + c * (d * h - e * g);
    double[] r = Identity();
    r[0] = (e * k - f * h) / det;
    r[4] = (c * h - b * k) / det;
    r[8] = (b * f - c * e) / det;
    r[1] = (f * g - d * k) / det;
    r[5] = (a * k - c * g) / det;
    r[9] = (c * d - a * f) / det;
    r[2] = (d * h - e * g) / det;
    r[6] = (b * g - a * h) / det;
    r[10] = (a * e - b * d) / det;
    for (int row = 0; row < 3; row++)
      r[12 + row] = -(r[row] * m[12] + r[4 + row] * m[13] + r[8 + row] * m[14]);
    return r;
  }
}
