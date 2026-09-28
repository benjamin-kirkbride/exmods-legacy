using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ExpandedLib.Testing;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Xunit;

namespace Integration.Tests.Saves;

/// <summary>
/// One saved block entity as a chunk stores it: the block's code, the class name the entity is
/// saved under, and its <see cref="TreeAttribute.ToBytes()"/> tree in base64.
/// </summary>
internal sealed record SaveGolden(
  string BlockCode,
  string SavedKey,
  string TreeBase64
);

/// <summary>
/// A block entity in one live state, and what it must hold after a load. <see cref="Live"/>
/// stands the entity up in a fresh world and returns it placed; <see cref="Block"/> builds the
/// block the loaded entity sits on (a new instance per call); <see cref="Setup"/>, when set,
/// prepares either world first (network types, neighbours); <see cref="Check"/> asserts the
/// restored state of the loaded entity, which has been through <c>Initialize</c> unless
/// <see cref="Initialize"/> is false.
/// </summary>
internal sealed class SaveCase {
  public required string Name { get; init; }
  public required System.Func<Block> Block { get; init; }
  public required System.Func<TestWorld, Block, BlockEntity> Live { get; init; }
  public required Action<BlockEntity, TestWorld> Check { get; init; }
  public Action<TestWorld>? Setup { get; init; }
  public bool Initialize { get; init; } = true;
}

/// <summary>
/// Save goldens under <c>tests/goldens/saves</c>, one JSON file per <see cref="SaveCase"/>. A case
/// writes its live state and reads it back in the game's load order; the committed golden is read
/// back the same way, so a golden written by the published build keeps asserting what that build
/// saved. Setting <c>LEGACY_WRITE_SAVE_GOLDENS</c> to <c>1</c>, or to a comma-separated list of
/// case names, rewrites those goldens from the live state before they are read.
/// </summary>
internal static class SaveGoldens {
  private const string WriteVariable = "LEGACY_WRITE_SAVE_GOLDENS";

  private static readonly JsonSerializerOptions Json = new() {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  /// <summary>
  /// Captures <paramref name="c"/>'s live state, writes it when the write variable names the case,
  /// then loads both the fresh capture and the committed golden and runs the case's checks on each.
  /// Fails when the golden file is missing or names another block.
  /// </summary>
  public static void Verify(SaveCase c) {
    TestWorld liveWorld = NewWorld(c);
    Block liveBlock = c.Block();
    BlockEntity live = c.Live(liveWorld, liveBlock);
    SaveGolden fresh = Capture(live);

    if (ShouldWrite(c.Name))
      Write(c.Name, fresh);

    Restore(c, fresh);
    Restore(c, Read(c.Name));
  }

  /// <summary>
  /// Loads <paramref name="golden"/> in the game's order: the entity is created by its saved name,
  /// given the block's behaviours, fed its tree, placed, then initialised unless the case opts out.
  /// </summary>
  public static BlockEntity Load(
    SaveGolden golden,
    TestWorld world,
    Block block,
    bool initialize = true
  ) {
    Assert.Equal(block.Code.ToShortString(), golden.BlockCode);
    var tree = TreeAttribute.CreateFromBytes(
      Convert.FromBase64String(golden.TreeBase64)
    );
    BlockEntity be = SaveRegistry.Instance.CreateBlockEntity(golden.SavedKey);
    be.CreateBehaviors(block, world.World);
    be.FromTreeAttributes(tree, world.World);
    world.Place(be.Pos, block, be);
    if (initialize)
      world.Initialize(be);
    return be;
  }

  /// <summary>The golden <paramref name="be"/> would be saved as, keyed by the registry's saved name.</summary>
  public static SaveGolden Capture(BlockEntity be) {
    string? key = SaveRegistry.Instance.SavedKey(be.GetType());
    Assert.True(key != null, $"{be.GetType().FullName} is not registered");
    var tree = new TreeAttribute();
    be.ToTreeAttributes(tree);
    return new SaveGolden(
      be.Block.Code.ToShortString(),
      key!,
      Convert.ToBase64String(tree.ToBytes())
    );
  }

  /// <summary>The repository root, found by walking up to <c>Legacy.sln</c>.</summary>
  public static string RepoRoot() {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (
      dir != null && !File.Exists(Path.Combine(dir.FullName, "Legacy.sln"))
    )
      dir = dir.Parent;
    Assert.True(dir != null, "could not locate repo root (Legacy.sln)");
    return dir!.FullName;
  }

  /// <summary>The folder the save goldens live in.</summary>
  public static string Folder =>
    Path.Combine(RepoRoot(), "tests", "goldens", "saves");

  private static void Restore(SaveCase c, SaveGolden golden) {
    TestWorld world = NewWorld(c);
    BlockEntity be = Load(golden, world, c.Block(), c.Initialize);
    c.Check(be, world);
  }

  private static TestWorld NewWorld(SaveCase c) {
    TestWorld world = SaveRegistry.Instance.Wire(new TestWorld());
    c.Setup?.Invoke(world);
    return world;
  }

  private static bool ShouldWrite(string name) {
    string value = Environment.GetEnvironmentVariable(WriteVariable) ?? "";
    return value == "1"
      || value.Split(',').Any(n => n.Trim() == name);
  }

  private static void Write(string name, SaveGolden golden) {
    Directory.CreateDirectory(Folder);
    File.WriteAllText(
      Path.Combine(Folder, name + ".json"),
      JsonSerializer.Serialize(golden, Json) + "\n"
    );
  }

  /// <summary>The committed golden <paramref name="name"/>. Fails when the file is missing.</summary>
  public static SaveGolden Read(string name) {
    string path = Path.Combine(Folder, name + ".json");
    Assert.True(
      File.Exists(path),
      $"no save golden {name}.json; write it with {WriteVariable}={name}"
    );
    return JsonSerializer.Deserialize<SaveGolden>(File.ReadAllText(path), Json)!;
  }
}
