using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace PipesAndPowerExpanded.ClosedLine;

/// <summary>The recipes of the closed line: every recipe a ppex or smex asset provides, and every
/// recipe whose output is a ppex or smex code, in the grid, smithing, clay forming, knapping and
/// barrel registries.</summary>
public static class ClosedLineRecipes {
  /// <summary>The registries <see cref="Remove"/> reads, in the order it reports them.</summary>
  public static readonly string[] Registries = [
    "grid",
    "smithing",
    "clayforming",
    "knapping",
    "barrel",
  ];

  /// <summary>Removes the closed line's recipes from <paramref name="api"/>'s registries. A recipe's
  /// source is its <c>Name</c>, the asset it was loaded from.</summary>
  /// <remarks>Run on the server once recipes have loaded; clients receive the registries from it on
  /// joining. A registry the game does not hold is skipped.</remarks>
  /// <returns>The count removed from each of <see cref="Registries"/>.</returns>
  public static Dictionary<string, int> Remove(ICoreServerAPI api) {
    var removed = new Dictionary<string, int>();
    foreach (string registry in Registries)
      removed[registry] = 0;

    if (api.World.GridRecipes is { } grid)
      removed["grid"] = grid.RemoveAll(r => Closes(r.Name, r.Output?.Code));

    if (api.ModLoader.GetModSystem<RecipeRegistrySystem>() is not { } recipes)
      return removed;
    removed["smithing"] = recipes.SmithingRecipes.RemoveAll(r =>
      Closes(r.Name, r.Output?.Code)
    );
    removed["clayforming"] = recipes.ClayFormingRecipes.RemoveAll(r =>
      Closes(r.Name, r.Output?.Code)
    );
    removed["knapping"] = recipes.KnappingRecipes.RemoveAll(r =>
      Closes(r.Name, r.Output?.Code)
    );
    removed["barrel"] = recipes.BarrelRecipes.RemoveAll(r =>
      Closes(r.Name, r.Output?.Code)
    );
    return removed;
  }

  /// <summary>Whether a recipe loaded from <paramref name="source"/> with
  /// <paramref name="output"/> belongs to the closed line.</summary>
  public static bool Closes(AssetLocation? source, AssetLocation? output) =>
    ClosedLineModSystem.IsOldLine(source) || ClosedLineModSystem.IsOldLine(output);
}
