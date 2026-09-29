using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ExpandedLib.Testing;
using Integration.Tests.Guards;
using Newtonsoft.Json.Linq;
using NSubstitute;
using PipesAndPowerExpanded.ClosedLine;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Integration.Tests.ClosedLine;

/// <summary>
/// ppex and smex loaded into a fresh <see cref="TestWorld"/> (<see cref="LoadedLine.Load"/>), with
/// the shipped recipes of ppex, smex and the game install in its grid and
/// <see cref="RecipeRegistrySystem"/> registries, the survival handbook's mod system, save data kept
/// in memory, and its client api reading the same mod loader and collectibles; then
/// <see cref="ClosedLineModSystem"/> started on both sides. The line is closed when iiex is
/// enabled before the start. At the start the world already holds a loaded chunk with
/// <see cref="StartRack"/> and a loaded <see cref="StartDrop"/>.
/// </summary>
/// <remarks>Each world is loaded once per test process and never disposed. Recipes are the parsed
/// JSON objects, one per recipe, before the game expands their ingredient variants.</remarks>
internal sealed class ClosedLineWorld {
  /// <summary>The xunit collection of the test classes that write to <see cref="Closed"/>'s save
  /// data, so they run one at a time.</summary>
  public const string Collection = "ClosedLine";

  private static readonly Lazy<ClosedLineWorld> ClosedWorld = new(() =>
    new ClosedLineWorld(closed: true)
  );

  private static readonly Lazy<ClosedLineWorld> OpenWorld = new(() =>
    new ClosedLineWorld(closed: false)
  );

  /// <summary>The world in which iiex is enabled.</summary>
  public static ClosedLineWorld Closed => ClosedWorld.Value;

  /// <summary>The world in which neither iiex nor siex is enabled.</summary>
  public static ClosedLineWorld Open => OpenWorld.Value;

  public TestWorld World { get; }

  public List<GridRecipe> Grid { get; } = [];

  public RecipeRegistrySystem Recipes { get; } = new();

  public ModSystemSurvivalHandbook Handbook { get; } = new();

  /// <summary>Each registry's recipes before the start: all of them, and those
  /// <see cref="ClosedLineRecipes.Closes"/> names.</summary>
  public Dictionary<string, (int All, int OldLine)> RecipesBefore { get; }

  /// <summary>The codes of the ppex and smex collectibles that had a creative tab or creative stacks
  /// before the start.</summary>
  public HashSet<string> InCreativeBefore { get; }

  /// <summary>The ppex and smex blocks and items of the world.</summary>
  public List<CollectibleObject> OldLine { get; }

  /// <summary>The save's mod data, as the save holds it.</summary>
  public Dictionary<string, List<string>> SaveData { get; } = [];

  /// <summary>ppex's iron straight pipe.</summary>
  public Block Pipe { get; }

  /// <summary>A smex tool mold.</summary>
  public Block Mold { get; }

  /// <summary>A stand-in for the game's iron ingot.</summary>
  public Item Ingot { get; }

  /// <summary>A vanilla mold rack in a chunk loaded before the start, holding <see cref="Mold"/>
  /// and <see cref="Ingot"/>.</summary>
  public BlockEntityMoldRack StartRack { get; }

  /// <summary>A dropped <see cref="Pipe"/> loaded before the start.</summary>
  public EntityItem StartDrop { get; }

  private ClosedLineWorld(bool closed) {
    World = LoadedLine.Load("ppex", "smex");
    LoadRecipes();
    World.World.GridRecipes.Returns(Grid);
    World.Mods.Register(Recipes).Register(Handbook);
    World.ClientApi.ModLoader.Returns(World.Mods);
    World.ClientApi.World.Blocks.Returns(World.World.Blocks);
    World.ClientApi.World.Items.Returns(World.World.Items);
    KeepSaveData();
    if (closed) {
      World.Mods.Add("iiex", "0.7.0");
      World.Mods.GetMod("iiex")!.Info.Name = "Iron Industry Expanded";
    }

    OldLine = [
      .. World
        .World.Blocks.Cast<CollectibleObject>()
        .Concat(World.World.Items)
        .Where(c => ClosedLineModSystem.IsOldLine(c?.Code)),
    ];
    Pipe = OldLine
      .OfType<Block>()
      .First(b => b.Code.ToString() == "ppex:pipe-straight-ns-iron");
    Mold = OldLine
      .OfType<Block>()
      .First(b =>
        b.Code.Domain == "smex"
        && b.Code.Path.StartsWith("toolmold-", StringComparison.Ordinal)
      );
    Ingot = World.RegisterItem("game:ingot-iron");
    StartRack = new BlockEntityMoldRack {
      Api = World.Api,
      Block = new Block { Code = new AssetLocation("game:moldrack-normal") },
      Pos = new BlockPos(0, 1, 0),
    };
    StartRack.Inventory[0].Itemstack = new ItemStack(Mold);
    StartRack.Inventory[1].Itemstack = new ItemStack(Ingot);
    IServerChunk loaded = Substitute.For<IServerChunk>();
    loaded.BlockEntities.Returns(
      new Dictionary<BlockPos, BlockEntity> { [StartRack.Pos] = StartRack }
    );
    World.Api.WorldManager.AllLoadedChunks.Returns(
      new Dictionary<long, IServerChunk> { [0] = loaded }
    );
    StartDrop = new EntityItem { Itemstack = new ItemStack(Pipe) };
    World.World.LoadedEntities.Returns(
      new ConcurrentDictionary<long, Entity> { [1] = StartDrop }
    );
    RecipesBefore = CountRecipes();
    InCreativeBefore = [
      .. OldLine
        .Where(c =>
          c.CreativeInventoryTabs is { Length: > 0 }
          || c.CreativeInventoryStacks is { Length: > 0 }
        )
        .Select(c => c.Code.ToString()),
    ];

    var system = new ClosedLineModSystem();
    system.StartServerSide(World.Api);
    system.StartClientSide(World.ClientApi);
  }

  /// <summary>Each registry's recipes now: all of them, and those
  /// <see cref="ClosedLineRecipes.Closes"/> names.</summary>
  public Dictionary<string, (int All, int OldLine)> CountRecipes() =>
    new() {
      ["grid"] = Count(Grid, r => (r.Name, r.Output?.Code)),
      ["smithing"] = Count(Recipes.SmithingRecipes, r => (r.Name, r.Output?.Code)),
      ["clayforming"] = Count(
        Recipes.ClayFormingRecipes,
        r => (r.Name, r.Output?.Code)
      ),
      ["knapping"] = Count(Recipes.KnappingRecipes, r => (r.Name, r.Output?.Code)),
      ["barrel"] = Count(Recipes.BarrelRecipes, r => (r.Name, r.Output?.Code)),
    };

  /// <summary>The distinct codes of the stacks <paramref name="block"/> drops when a survival player
  /// breaks it, or the exception its <c>GetDrops</c> threw.</summary>
  public (HashSet<string>? Codes, Exception? Threw) DropsOf(Block block) {
    TestPlayer player = World.Player("breaker");
    player.GameMode = EnumGameMode.Survival;
    try {
      ItemStack[]? drops = block.GetDrops(
        World.World,
        new BlockPos(0, 200, 0),
        player.Player
      );
      return (
        [.. (drops ?? []).Select(s => s.Collectible.Code.ToString())],
        null
      );
    } catch (Exception e) {
      return (null, e);
    }
  }

  /// <summary>Resolves <paramref name="block"/>'s <c>drops</c> in the world, registering a stand-in
  /// for each code it does not hold, and runs its <c>OnLoaded</c>, as the engine does after the
  /// object loader.</summary>
  /// <returns>The exception <c>OnLoaded</c> threw, or null.</returns>
  public Exception? Ready(Block block) {
    foreach (BlockDropItemStack drop in block.Drops ?? []) {
      if (drop.Resolve(World.World, "ClosedLineWorld", block.Code))
        continue;
      if (drop.Type == EnumItemClass.Item)
        World.RegisterItem(drop.Code.ToString());
      else {
        Block standIn = TestBlocks.Configure(
          new Block(),
          drop.Code.ToString(),
          Interlocked.Increment(ref _nextStandIn)
        );
        ReflectionHelpers.SetField(standIn, "api", World.Api);
        World.Register(standIn);
      }
      drop.Resolve(World.World, "ClosedLineWorld", block.Code);
    }
    try {
      block.OnLoaded(World.Api);
      return null;
    } catch (Exception e) {
      return e;
    }
  }

  private static int _nextStandIn = 61000;

  /// <summary>The codes of the stacks <paramref name="inventory"/> holds, slot by slot.</summary>
  public static List<string> Codes(IInventory inventory) =>
    [
      .. inventory
        .Where(slot => slot.Itemstack != null)
        .Select(slot => slot.Itemstack.Collectible.Code.ToString()),
    ];

  /// <summary>Whether <paramref name="block"/> reserves filler cells or carries construction
  /// stages, the blocks <see cref="StructureBreaks"/> stands up.</summary>
  public static bool IsStructure(Block block) =>
    block.Attributes?["fillerOffsets"].Exists == true
    || block.BlockEntityBehaviors?.Any(b => b.Name == "ExRightClickConstructable")
      == true;

  private static (int, int) Count<T>(
    List<T> recipes,
    System.Func<T, (AssetLocation?, AssetLocation?)> read
  ) =>
    (
      recipes.Count,
      recipes.Count(r => {
        var (source, output) = read(r);
        return ClosedLineRecipes.Closes(source, output);
      })
    );

  private void LoadRecipes() {
    Grid.AddRange(Parse<GridRecipe>("grid", (r, at) => r.Name ??= at));
    Recipes.SmithingRecipes.AddRange(
      Parse<SmithingRecipe>("smithing", (r, at) => r.Name ??= at)
    );
    Recipes.ClayFormingRecipes.AddRange(
      Parse<ClayFormingRecipe>("clayforming", (r, at) => r.Name ??= at)
    );
    Recipes.KnappingRecipes.AddRange(
      Parse<KnappingRecipe>("knapping", (r, at) => r.Name ??= at)
    );
    Recipes.BarrelRecipes.AddRange(
      Parse<BarrelRecipe>("barrel", (r, at) => r.Name ??= at)
    );
  }

  /// <summary>Every recipe object under <c>recipes/&lt;registry&gt;</c> of the game install, ppex
  /// and smex, parsed as the game's recipe loader parses it, each named by its asset when it names
  /// none. A file or object that does not parse is skipped.</summary>
  private static List<T> Parse<T>(string registry, Action<T, AssetLocation> name)
    where T : class {
    var sources = new List<(string Domain, string Dir)> {
      (
        "game",
        Path.Combine(
          VsAssemblyResolver.InstallPath!,
          "assets",
          "survival",
          "recipes",
          registry
        )
      ),
    };
    foreach (string mod in LoadedLine.Mods)
      sources.Add(
        (mod, Path.Combine(RepoPaths.Root, mod, "assets", mod, "recipes", registry))
      );

    var recipes = new List<T>();
    foreach ((string domain, string dir) in sources) {
      if (!Directory.Exists(dir))
        continue;
      foreach (
        string file in Directory
          .EnumerateFiles(dir, "*.json", SearchOption.AllDirectories)
          .Order(StringComparer.Ordinal)
      ) {
        var at = new AssetLocation(
          domain,
          $"recipes/{registry}/{Path.GetRelativePath(dir, file).Replace('\\', '/')}"
        );
        JToken json;
        try {
          json = JToken.Parse(File.ReadAllText(file));
        } catch (Exception) {
          continue;
        }
        foreach (JToken token in json is JArray array ? array : new JArray(json)) {
          T? recipe;
          try {
            recipe = token.ToObject<T>(domain);
          } catch (Exception) {
            continue;
          }
          if (recipe == null)
            continue;
          name(recipe, at);
          recipes.Add(recipe);
        }
      }
    }
    return recipes;
  }

  private void KeepSaveData() {
    ISaveGame save = World.Api.WorldManager.SaveGame;
    save.GetData(Arg.Any<string>(), Arg.Any<List<string>?>())
      .Returns(ci =>
        SaveData.TryGetValue(ci.ArgAt<string>(0), out List<string>? kept)
          ? new List<string>(kept)
          : ci.ArgAt<List<string>?>(1)
      );
    save.When(s => s.StoreData(Arg.Any<string>(), Arg.Any<List<string>?>()))
      .Do(ci => SaveData[ci.ArgAt<string>(0)] = [.. ci.ArgAt<List<string>>(1)]);
  }
}
