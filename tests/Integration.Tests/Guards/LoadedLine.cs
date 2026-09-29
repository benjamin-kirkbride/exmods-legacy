using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandedLib.Testing;
using PipesAndPowerExpanded;
using SteelmakingExpanded;
using Vintagestory.API.Common;

namespace Integration.Tests.Guards;

/// <summary>
/// ppex and then smex loaded into one <see cref="TestWorld"/> through the game's own asset manager
/// and object loader (<see cref="TestWorld.LoadAssets"/>), as a server loads them: the shipped JSON
/// under <c>ppex/assets</c> and <c>smex/assets</c>, the assemblies this test process runs, and smex's
/// patches, which add the refractory tiers to ppex's pipes and reach the game's <c>coalpile</c>,
/// <c>crushed</c> and <c>nugget</c>. exlib starts once, before ppex and smex.
/// </summary>
/// <remarks>Each mod is staged under the test output's <c>loaded-line/&lt;modid&gt;</c>, a fresh
/// copy once per test process: its <c>modinfo.json</c>, its <c>assets</c>, and its assembly as
/// <c>bin/&lt;modid&gt;.dll</c>, the name <see cref="TestWorld.LoadAssets"/> looks for. Every
/// loaded block and item is given the world's api, as the engine gives it; their drops and
/// <c>OnLoaded</c> are left to the caller. <see cref="World"/> and <see cref="Ppex"/> are each loaded
/// once per test process and never disposed.</remarks>
internal static class LoadedLine {
  private static readonly Lazy<TestWorld> Shared = new(() =>
    Load("ppex", "smex")
  );

  private static readonly Lazy<TestWorld> PpexOnly = new(() => Load("ppex"));

  /// <summary>The world holding both mods' loaded blocks, items and classes.</summary>
  public static TestWorld World => Shared.Value;

  /// <summary>A world that loaded ppex alone.</summary>
  public static TestWorld Ppex => PpexOnly.Value;

  /// <summary>The two mod ids, in load order.</summary>
  public static readonly string[] Mods = ["ppex", "smex"];

  /// <summary>The loaded blocks of ppex and smex in <see cref="World"/>.</summary>
  public static IEnumerable<Block> Blocks =>
    World.World.Blocks.Where(b => b?.Code != null && Mods.Contains(b.Code.Domain));

  /// <summary><paramref name="block"/>'s code without its variant states, e.g.
  /// <c>smex:smokestack</c> for <c>smex:smokestack-intake-north</c> when both states are
  /// variants.</summary>
  /// <remarks>Each state is taken off the end of the code in reverse order of its group; a state
  /// that is not a suffix leaves the rest in place.</remarks>
  public static string Blocktype(Block block) {
    string path = block.Code.Path;
    foreach (string state in block.Variant.Values.Reverse())
      if (path.EndsWith("-" + state, StringComparison.Ordinal))
        path = path[..^(state.Length + 1)];
    return block.Code.Domain + ":" + path;
  }

  /// <summary>A fresh world that loaded <paramref name="mods"/> through one asset list, in order, as
  /// <see cref="World"/> did.</summary>
  internal static TestWorld Load(params string[] mods) {
    var world = new TestWorld();
    world.LoadAssets(
      [
        .. mods.Select(modId =>
          Stage(
            modId,
            modId == "ppex"
              ? typeof(PipesAndPowerExpandedModSystem)
              : typeof(SteelmakingExpandedModSystem)
          )
        ),
      ]
    );
    foreach (CollectibleObject collectible in world.World.Collectibles)
      if (collectible != null)
        ReflectionHelpers.SetField(collectible, "api", world.Api);
    return world;
  }

  private static readonly Dictionary<string, string> Staged = [];

  private static string Stage(string modId, Type system) {
    lock (Staged) {
      if (!Staged.TryGetValue(modId, out string? root))
        Staged[modId] = root = StageOnce(modId, system);
      return root;
    }
  }

  private static string StageOnce(string modId, Type system) {
    string source = Path.Combine(RepoPaths.Root, modId);
    string root = Path.Combine(AppContext.BaseDirectory, "loaded-line", modId);
    if (Directory.Exists(root))
      Directory.Delete(root, recursive: true);
    Directory.CreateDirectory(Path.Combine(root, "bin"));
    File.Copy(
      Path.Combine(source, "modinfo.json"),
      Path.Combine(root, "modinfo.json")
    );
    Copy(Path.Combine(source, "assets"), Path.Combine(root, "assets"));
    File.Copy(
      system.Assembly.Location,
      Path.Combine(root, "bin", modId + ".dll")
    );
    return root;
  }

  private static void Copy(string from, string to) {
    foreach (string file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories)) {
      string target = Path.Combine(to, Path.GetRelativePath(from, file));
      Directory.CreateDirectory(Path.GetDirectoryName(target)!);
      File.Copy(file, target);
    }
  }
}
