using System;
using System.IO;
using System.Linq;
using ExpandedLib.Blocks.Construction;
using ExpandedLib.Testing;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Xunit;

namespace Integration.Tests.Saves;

/// <summary>
/// Stands block entities up for the save goldens the way placement does: the block carries the
/// entity behaviours its shipped blocktype declares, the entity is placed, given those behaviours
/// and initialised, and a right-click construction can be finished.
/// </summary>
internal static class SaveFixtures {
  /// <summary>
  /// Sets the entity behaviours the shipped blocktype file at <paramref name="assetPath"/>
  /// (repo-relative, e.g. <c>ppex/assets/ppex/blocktypes/boiler/cornish.json</c>) declares on
  /// <paramref name="block"/>, with their properties. Returns <paramref name="block"/>.
  /// </summary>
  public static Block ShippedBehaviors(Block block, string assetPath) {
    var root = JObject.Parse(
      File.ReadAllText(Path.Combine(SaveGoldens.RepoRoot(), assetPath))
    );
    var list =
      root.Properties()
        .FirstOrDefault(p =>
          string.Equals(
            p.Name,
            "entityBehaviors",
            StringComparison.OrdinalIgnoreCase
          )
        )
        ?.Value as JArray;
    block.BlockEntityBehaviors = (list ?? [])
      .OfType<JObject>()
      .Select(b => new BlockEntityBehaviorType {
        Name = (string)b["name"]!,
        properties = new JsonObject(b["properties"] ?? new JObject()),
      })
      .ToArray();
    return block;
  }

  /// <summary>
  /// Places <paramref name="be"/> on <paramref name="block"/> at <paramref name="pos"/>, creates
  /// the block's entity behaviours and, when <paramref name="initialize"/>, runs the entity's
  /// <c>Initialize</c>. Returns <paramref name="be"/>.
  /// </summary>
  public static BlockEntity Stand(
    TestWorld world,
    BlockPos pos,
    Block block,
    BlockEntity be,
    bool initialize = true
  ) {
    world.Place(pos, block, be);
    be.CreateBehaviors(block, world.World);
    if (initialize)
      world.Initialize(be);
    else
      world.Attach(be);
    return be;
  }

  /// <summary>
  /// Finishes <paramref name="be"/>'s right-click construction: its
  /// <see cref="ExRightClickConstructable"/> is initialised from its declared stages if it has not
  /// been, and its completed stage is set to the last. Fails when the entity has no such behaviour.
  /// </summary>
  public static void CompleteConstruction(BlockEntity be, TestWorld world) {
    var behavior = be.GetBehavior<ExRightClickConstructable>();
    Assert.NotNull(behavior);
    object rcc = ReflectionHelpers.GetField(behavior, "rcc")!;
    if (rcc.GetType().GetField("Stages")!.GetValue(rcc) is not Array { Length: > 0 })
      behavior.Initialize(world.Api, behavior.properties);
    var stages = (Array)rcc.GetType().GetField("Stages")!.GetValue(rcc)!;
    rcc.GetType()
      .GetField("CurrentCompletedStage")!
      .SetValue(rcc, stages.Length - 1);
  }

  /// <summary>The completed construction stage <paramref name="be"/>'s right-click construction holds.</summary>
  public static int ConstructionStage(BlockEntity be) {
    var behavior = be.GetBehavior<ExRightClickConstructable>();
    Assert.NotNull(behavior);
    object rcc = ReflectionHelpers.GetField(behavior, "rcc")!;
    return (int)rcc.GetType().GetField("CurrentCompletedStage")!.GetValue(rcc)!;
  }

  /// <summary>The number of stages the shipped blocktype at <paramref name="assetPath"/> declares for its construction.</summary>
  public static int ConstructionStages(string assetPath) {
    var behaviors = ShippedBehaviors(new Block(), assetPath).BlockEntityBehaviors;
    var rcc = behaviors.Single(b => b.Name == "ExRightClickConstructable");
    return ((JArray)rcc.properties["stages"].Token).Count;
  }
}
