using ExpandedLib.Helpers;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Industry.MechanicalPower;
using ExpandedLib.Registries;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PipesAndPowerExpanded;

/// <summary>
/// Main mod system for Pipes and Power Expanded. Loads the gameplay tunables, patches the vanilla
/// chimney look-at info, auto-registers every <c>[BlockRegister]</c>/<c>[ItemRegister]</c>/etc.
/// decorated class, and adds the creative tab. ppex's pipes run on Industry's "pipe" network type.
/// </summary>
public class PipesAndPowerExpandedModSystem : ModSystem {
  private Harmony? _harmony;

  public override void Start(ICoreAPI api) {
    // Load gameplay tunables from the ppex section of ModConfig/ex_values.json (writes defaults
    // on first run), after reporting the tuned pipe values that section does not carry.
    PpexRemovedTunables.Report(api);
    PpexValues.Load(api);
    // Drive the exlib RCC salvage ratio for the engines/boilers from the (live) config.
    ExpandedLib.Blocks.ExRccSettings.RegisterBrokenDropsRatio(
      Mod.Info.ModID,
      () => PpexValues.RccBrokenDropsRatio
    );
    // The steam-machine recipe cost catalogue (the ppex section of ex_recipes.json).
    PpexRecipeValues.Load(api);
    // Register this mod's recipe-cost profile so exlib's shared apply pass and the generic
    // /exmod recipes ppex <level> command can drive it (see ExRecipeProfiles).
    ExRecipeProfiles.Register(
      new RecipeProfile {
        Code = Mod.Info.ModID,
        Catalogue = () => PpexRecipeValues.Recipes,
        Defaults = PpexRecipeConfig.DefaultCatalogue,
        GetLevel = () => PpexValues.RecipeLevel,
        SetLevel = level => PpexValues.Edit(c => c.RecipeLevel = level),
        SaveCatalogue = PpexRecipeValues.Save,
      }
    );

    // Patch the vanilla chimney's look-at info so a chimney venting one of ppex's pipes
    // reports it (the gas draw itself runs in the passthrough's and outlet's ChimneyVent).
    if (!Harmony.HasAnyPatches(Mod.Info.ModID)) {
      _harmony = new Harmony(Mod.Info.ModID);
      _harmony.PatchAll(GetType().Assembly);
    }

    // The shared structure-filler block and network/structure framework live in the exlib
    // mod (a hard dependency); exlib points StructureFillers at exlib:structurefiller and
    // registers its own classes. This registers only ppex's own content.
    EntityRegistry.RegisterAll(api, Mod, GetType().Assembly);
    AliasPipeEntities(api);
  }

  /// <summary>
  /// Registers the class names ppex's own pipe block entities had - <c>ppex.BlockEntityPipe</c>,
  /// <c>ppex.Pipe</c>, <c>ppex.BlockEntityPipePassthrough</c> and <c>ppex.PipePassthrough</c> - as
  /// load-only aliases of Industry's <see cref="BlockEntityPipe"/> and
  /// <see cref="BlockEntityPipePassthrough"/>, so the blocktypes' <c>entityClass</c> strings and saves
  /// naming them resolve while the game saves those entities under Industry's primary keys.
  /// </summary>
  /// <param name="api">The api whose class registry receives the aliases.</param>
  public static void AliasPipeEntities(ICoreAPI api) {
    EntityRegistry.AliasBlockEntity(api, "ppex.BlockEntityPipe", typeof(BlockEntityPipe));
    EntityRegistry.AliasBlockEntity(api, "ppex.Pipe", typeof(BlockEntityPipe));
    EntityRegistry.AliasBlockEntity(
      api,
      "ppex.BlockEntityPipePassthrough",
      typeof(BlockEntityPipePassthrough)
    );
    EntityRegistry.AliasBlockEntity(
      api,
      "ppex.PipePassthrough",
      typeof(BlockEntityPipePassthrough)
    );
  }

  public override void Dispose() {
    _harmony?.UnpatchAll(Mod.Info.ModID);
    _harmony = null;
    base.Dispose();
  }

  public override void StartServerSide(ICoreServerAPI api) {
    // Server-side sub-commands. The recipe-cost level is applied centrally by exlib (ExRecipeProfiles);
    // /exmod recipes ppex <level> is the generic switch.
    CommandRegistry.RegisterAll(api, Mod, GetType().Assembly);
  }

  #region Creative category
  public override void StartClientSide(ICoreClientAPI api) {
    ExCreativeTabs.EnsureTab(Mod.Info.ModID);
    // The recipe cost level is applied centrally by exlib (ExRecipeProfiles) on both sides.
  }
  #endregion
}
