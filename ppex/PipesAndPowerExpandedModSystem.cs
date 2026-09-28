using ExpandedLib.Networks;
using ExpandedLib.Helpers;
using ExpandedLib.Industry.Helpers;
using ExpandedLib.Industry.MechanicalPower;
using ExpandedLib.Registries;
using HarmonyLib;
using PipesAndPowerExpanded.BlockNetworkPipe;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PipesAndPowerExpanded;

/// <summary>
/// Main mod system for Pipes and Power Expanded. Loads the gameplay tunables, patches the vanilla
/// chimney look-at info, auto-registers every <c>[BlockRegister]</c>/<c>[ItemRegister]</c>/etc.
/// decorated class, adds the creative tab, and registers the unified "pipe" network type (gases + liquids).
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
    // reports it (the gas draw itself runs in PipeNetwork's tick).
    if (!Harmony.HasAnyPatches(Mod.Info.ModID)) {
      _harmony = new Harmony(Mod.Info.ModID);
      _harmony.PatchAll(GetType().Assembly);
    }

    // The shared structure-filler block and network/structure framework live in the exlib
    // mod (a hard dependency); exlib points StructureFillers at exlib:structurefiller and
    // registers its own classes. This registers only ppex's own content.
    EntityRegistry.RegisterAll(api, Mod, GetType().Assembly);

    // The unified pipe network (gas + liquid pools).
    var netManager = api.ModLoader.GetModSystem<BlockNetworkModSystem>();
    netManager.RegisterNetworkType("pipe", () => new PipeNetwork(netManager));
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
