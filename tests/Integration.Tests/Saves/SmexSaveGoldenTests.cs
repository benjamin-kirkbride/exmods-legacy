using System;
using ExpandedLib.Industry.MechanicalPower;
using ExpandedLib.Structures;
using ExpandedLib.Testing;
using Newtonsoft.Json.Linq;
using PipesAndPowerExpanded.BlockNetworkPipe;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using SteelmakingExpanded.BlockNetworkMolten;
using SteelmakingExpanded.BlockNetworkMolten.BlockEntities;
using SteelmakingExpanded.BlockNetworkMolten.Blocks;
using SteelmakingExpanded.BlockStructures.BlastFurnace;
using SteelmakingExpanded.BlockStructures.BlastFurnace.BlockEntities;
using SteelmakingExpanded.BlockStructures.BlastFurnace.Blocks;
using SteelmakingExpanded.BlockStructures.Converter;
using SteelmakingExpanded.BlockStructures.Converter.BlockEntities;
using SteelmakingExpanded.BlockStructures.Converter.Blocks;
using SteelmakingExpanded.BlockStructures.CowperStove.BlockEntities;
using SteelmakingExpanded.BlockStructures.CowperStove.Blocks;
using SteelmakingExpanded.BlockStructures.Engine.BlockEntities;
using SteelmakingExpanded.BlockStructures.Engine.Blocks;
using SteelmakingExpanded.BlockStructures.SmokeStack.BlockEntities;
using SteelmakingExpanded.BlockStructures.SmokeStack.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Xunit;

namespace Integration.Tests.Saves;

/// <summary>
/// Save goldens for every smex blocktype with a block entity. Each live state is saved, loaded back
/// in the game's order, and must restore what it held; the committed goldens were written by the
/// published build, so a later build that loads them differently fails here.
/// </summary>
public class SmexSaveGoldenTests {
  private static readonly BlockPos At = new(0, 8, 0);

  private const string Blocktypes = "smex/assets/smex/blocktypes/";

  private const string Iron = "game:ingot-iron";

  #region Canals

  [Fact]
  public void A_brick_canal_restores_its_hot_iron() =>
    Cell(
      "smex-moltencanal-straight-brick-hot",
      () => Canal(new BlockMoltenCanal(), "smex:moltencanal-straight-black-ns", "straight", "ns"),
      () => new BlockEntityMoltenCanal(),
      "ns",
      amount: 40,
      temperature: 1450f
    );

  [Fact]
  public void A_cobblestone_canal_restores_its_solidified_iron() =>
    Cell(
      "smex-moltencanal-straight-cobblestone-solidified",
      () => Canal(new BlockMoltenCanal(), "smex:moltencanal-straight-andesite-we", "straight", "we", rock: true),
      () => new BlockEntityMoltenCanal(),
      "we",
      amount: 30,
      temperature: 700f,
      solidified: true
    );

  [Fact]
  public void A_brick_bend_restores_its_sealed_cell() =>
    Cell(
      "smex-moltencanal-bend-brick-sealed",
      () => Canal(new BlockMoltenCanal(), "smex:moltencanal-bend-black-en", "bend", "en"),
      () => new BlockEntityMoltenCanal(),
      "en",
      amount: 20,
      temperature: 1400f,
      sealed_: true
    );

  [Fact]
  public void A_cobblestone_bend_restores_its_hot_iron() =>
    Cell(
      "smex-moltencanal-bend-cobblestone-hot",
      () => Canal(new BlockMoltenCanal(), "smex:moltencanal-bend-andesite-nw", "bend", "nw", rock: true),
      () => new BlockEntityMoltenCanal(),
      "nw",
      amount: 36,
      temperature: 1500f
    );

  [Fact]
  public void A_brick_t_junction_restores_its_hot_iron() =>
    Cell(
      "smex-moltencanal-tjunction-brick-hot",
      () => Canal(new BlockMoltenCanal(), "smex:moltencanal-tjunction-black-nes", "tjunction", "nes"),
      () => new BlockEntityMoltenCanal(),
      "nes",
      amount: 25,
      temperature: 1480f
    );

  [Fact]
  public void A_cobblestone_t_junction_restores_its_hot_iron() =>
    Cell(
      "smex-moltencanal-tjunction-cobblestone-hot",
      () => Canal(new BlockMoltenCanal(), "smex:moltencanal-tjunction-andesite-esw", "tjunction", "esw", rock: true),
      () => new BlockEntityMoltenCanal(),
      "esw",
      amount: 18,
      temperature: 1420f
    );

  [Fact]
  public void A_brick_cross_junction_restores_its_hot_iron() =>
    Cell(
      "smex-moltencanal-xjunction-brick-hot",
      () => Canal(new BlockMoltenCanal(), "smex:moltencanal-xjunction-black-nswe", "xjunction", "nswe"),
      () => new BlockEntityMoltenCanal(),
      "nswe",
      amount: 44,
      temperature: 1510f
    );

  [Fact]
  public void A_cobblestone_cross_junction_restores_its_solidified_iron() =>
    Cell(
      "smex-moltencanal-xjunction-cobblestone-solidified",
      () => Canal(new BlockMoltenCanal(), "smex:moltencanal-xjunction-andesite-nswe", "xjunction", "nswe", rock: true),
      () => new BlockEntityMoltenCanal(),
      "nswe",
      amount: 12,
      temperature: 400f,
      solidified: true
    );

  [Fact]
  public void A_brick_start_restores_its_iron_and_pour_tally() =>
    Cell(
      "smex-moltencanal-start-brick-pouring",
      () => Canal(new BlockMoltenCanalStart(), "smex:moltencanal-start-black-n", "start", "n"),
      () => new BlockEntityMoltenCanalStart(),
      "n",
      amount: 25,
      temperature: 1550f,
      prime: be => ReflectionHelpers.SetField(be, "_pourTally", 12),
      check: be =>
        Assert.Equal(12, (int)ReflectionHelpers.GetField(be, "_pourTally")!)
    );

  [Fact]
  public void A_cobblestone_start_restores_its_iron() =>
    Cell(
      "smex-moltencanal-start-cobblestone-hot",
      () => Canal(new BlockMoltenCanalStart(), "smex:moltencanal-start-andesite-e", "start", "e", rock: true),
      () => new BlockEntityMoltenCanalStart(),
      "e",
      amount: 15,
      temperature: 1490f
    );

  [Fact]
  public void A_brick_pedestal_restores_its_mold_and_cast() =>
    Cell(
      "smex-moltencanal-moldpedestal-brick-casting",
      () => Canal(new BlockMoltenCanalMoldPedestal(), "smex:moltencanal-moldpedestal-black-n", "moldpedestal", "n"),
      () => new BlockEntityMoltenCanalMoldPedestal(),
      "n",
      amount: 10,
      temperature: 1450f,
      prime: be => {
        var pedestal = (BlockEntityMoltenCanalMoldPedestal)be;
        pedestal.IsMold = true;
        SetOn(be, typeof(BlockEntityMoltenCanalMoldPedestal), "MoldStack", MoldStack(be));
        SetOn(be, typeof(BlockEntityMoltenCanalMoldPedestal), "MoldMetalContent", Metal(be, 1450f));
        SetOn(be, typeof(BlockEntityMoltenCanalMoldPedestal), "MoldCurrentUnits", 60);
        SetOn(be, typeof(BlockEntityMoltenCanalMoldPedestal), "MoldMaxUnits", 100);
      },
      check: be => {
        var pedestal = Assert.IsType<BlockEntityMoltenCanalMoldPedestal>(be);
        Assert.True(pedestal.IsMold);
        Assert.Equal(MoldCode, pedestal.MoldStack!.Collectible.Code.ToString());
        Assert.Equal(Iron, pedestal.MoldMetalContent!.Collectible.Code.ToString());
        Assert.Equal(60, pedestal.MoldCurrentUnits);
        Assert.Equal(100, pedestal.MoldMaxUnits);
      }
    );

  [Fact]
  public void A_cobblestone_pedestal_restores_its_empty_mold() =>
    Cell(
      "smex-moltencanal-moldpedestal-cobblestone-empty",
      () => Canal(new BlockMoltenCanalMoldPedestal(), "smex:moltencanal-moldpedestal-andesite-s", "moldpedestal", "s", rock: true),
      () => new BlockEntityMoltenCanalMoldPedestal(),
      "s",
      amount: 0,
      temperature: 20f,
      prime: be => {
        ((BlockEntityMoltenCanalMoldPedestal)be).IsMold = true;
        SetOn(be, typeof(BlockEntityMoltenCanalMoldPedestal), "MoldStack", MoldStack(be));
      },
      check: be => {
        var pedestal = Assert.IsType<BlockEntityMoltenCanalMoldPedestal>(be);
        Assert.True(pedestal.IsMold);
        Assert.Equal(MoldCode, pedestal.MoldStack!.Collectible.Code.ToString());
        Assert.Null(pedestal.MoldMetalContent);
        Assert.Equal(0, pedestal.MoldCurrentUnits);
      }
    );

  [Fact]
  public void A_tap_restores_its_barrel_and_pour() =>
    Cell(
      "smex-moltencanal-tap-barrel",
      () => Canal(new BlockMoltenCanalTap(), "smex:moltencanal-tap-n", "tap", "n", brick: null),
      () => new BlockEntityMoltenCanalTap(),
      "n",
      amount: 8,
      temperature: 1460f,
      prime: be => {
        ReflectionHelpers.SetField(be, "_isPouring", true);
        ((BlockEntityMoltenCanalTap)be).IsBarrel = true;
        SetOn(be, typeof(BlockEntityMoltenCanalTap), "BarrelMetalContent", Metal(be, 1460f));
        SetOn(be, typeof(BlockEntityMoltenCanalTap), "BarrelCurrentUnits", 120);
      },
      check: be => {
        var tap = Assert.IsType<BlockEntityMoltenCanalTap>(be);
        Assert.True(tap.IsPouring);
        Assert.True(tap.IsBarrel);
        Assert.False(tap.IsMold);
        Assert.Equal(Iron, tap.BarrelMetalContent!.Collectible.Code.ToString());
        Assert.Equal(120, tap.BarrelCurrentUnits);
      }
    );

  [Fact]
  public void A_barrel_restores_its_metal() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-moltenbarrel-filled",
        Block = () => Variants(new BlockMoltenBarrel(), "smex:moltenbarrel"),
        Setup = RegisterMetals,
        Live = (world, block) => {
          var be = new BlockEntityMoltenBarrel();
          SaveFixtures.Stand(world, At, block, be);
          be.MetalContent = MoltenMetal.CreateStack(world.World, Iron, 1380f);
          be.CurrentUnitAmount = 150;
          return be;
        },
        Check = (be, _) => {
          var barrel = Assert.IsType<BlockEntityMoltenBarrel>(be);
          Assert.Equal(Iron, barrel.MetalContent!.Collectible.Code.ToString());
          Assert.Equal(150, barrel.CurrentUnitAmount);
        },
      }
    );

  #endregion

  #region Blast furnace

  [Fact]
  public void A_melting_blast_furnace_restores_its_charge_and_heat() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-blastfurnace-melting",
        Block = () => {
          Block block = SaveFixtures.ShippedBehaviors(
            Variants(new BlockBlastFurnaceDoor(), "smex:blastfurnacedoor-tier2", ("refractory", "tier2")),
            Blocktypes + "blastfurnace/door.json"
          );
          block.Attributes ??= new JsonObject(new JObject());
          var door = new BlockBehaviorDoor(block);
          block.BlockBehaviors = [door];
          block.CollectibleBehaviors = [door];
          return block;
        },
        Live = (world, block) => {
          var be = new BlockEntityBlastFurnace();
          SaveFixtures.Stand(world, At, block, be);
          ReflectionHelpers.SetProperty(be, "StructureComplete", true);
          ReflectionHelpers.SetProperty(be, nameof(be.State), BlastFurnaceState.Melting);
          ReflectionHelpers.SetField(be, "_internalTemp", 1620f);
          ReflectionHelpers.SetField(be, "_meltSeconds", 95f);
          ReflectionHelpers.SetField(be, "_moltenIron", 140f);
          ReflectionHelpers.SetField(be, "_moltenSlag", 35f);
          ReflectionHelpers.SetField(be, "_airDrawn", 6f);
          ReflectionHelpers.SetField(be, "_blastPressure", 1.3f);
          ReflectionHelpers.SetField(be, "_cachedBurdenCount", 24);
          be.GetBehavior<BEBehaviorDoor>().RotateYRad = 1.5708f;
          be.BaseAngleRad = 1.5708f;
          return be;
        },
        Check = (be, _) => {
          var furnace = Assert.IsType<BlockEntityBlastFurnace>(be);
          Assert.True(furnace.StructureComplete);
          Assert.Equal(BlastFurnaceState.Melting, furnace.State);
          Assert.False(furnace.IsChoked);
          Assert.Equal(1620f, (float)ReflectionHelpers.GetField(furnace, "_internalTemp")!, 3);
          Assert.Equal(95f, (float)ReflectionHelpers.GetField(furnace, "_meltSeconds")!, 3);
          Assert.Equal(140f, (float)ReflectionHelpers.GetField(furnace, "_moltenIron")!, 3);
          Assert.Equal(35f, (float)ReflectionHelpers.GetField(furnace, "_moltenSlag")!, 3);
          Assert.Equal(6f, (float)ReflectionHelpers.GetField(furnace, "_airDrawn")!, 3);
          Assert.Equal(1.3f, (float)ReflectionHelpers.GetField(furnace, "_blastPressure")!, 3);
          Assert.Equal(24, (int)ReflectionHelpers.GetField(furnace, "_cachedBurdenCount")!);
          Assert.Equal(1.5708f, furnace.BaseAngleRad, 3);
          Assert.Equal(1.5708f, furnace.GetBehavior<BEBehaviorDoor>().RotateYRad, 3);
        },
      }
    );

  [Fact]
  public void A_blast_furnace_tap_restores_its_pour() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-blastfurnacetap-pouring",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockBlastFurnaceTap(), "smex:blastfurnacetap-tier1-north", ("refractory", "tier1"), ("side", "north")),
            Blocktypes + "blastfurnace/tap.json"
          ),
        Live = (world, block) => {
          var be = new BlockEntityBlastFurnaceTap();
          SaveFixtures.Stand(world, At, block, be);
          ReflectionHelpers.SetProperty(be, nameof(be.IsPouring), true);
          return be;
        },
        Check = (be, _) =>
          Assert.True(Assert.IsType<BlockEntityBlastFurnaceTap>(be).IsPouring),
      }
    );

  [Fact]
  public void A_tuyere_restores_its_blast() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-blastfurnace-tuyere-blast",
        Block = () => {
          var block = Variants(new BlockTuyere(), "smex:blastfurnace-tuyere-tier1-n", ("type", "tuyere"), ("refractory", "tier1"), ("orientation", "n"));
          ReflectionHelpers.SetProperty(block, "Type", "tuyere");
          ReflectionHelpers.SetProperty(block, "Orientation", "n");
          return block;
        },
        Setup = world => {
          world.RegisterNetwork("pipe", sys => new PipeNetwork(sys));
          Block rock = TestBlocks.Configure(new Block(), "game:rock-granite", 99);
          foreach (BlockFacing face in BlockFacing.ALLFACES)
            world.Place(At.AddCopy(face), rock);
        },
        Live = (world, block) => {
          var be = new BlockEntityTuyere { Orientation = "n", PossibleOrientations = ["n"] };
          world.Place(At, block, be);
          world.Attach(be);
          ReflectionHelpers.SetProperty(be, nameof(be.NetworkSystem), world.Networks);
          world.AddNode(At, "pipe");
          Assert.True(
            ((PipeNetwork)world.NetworkAt(At)!).TryProduceGas(24f, 600f, "Air", world.Accessor, maxOutputPressure: 2f)
          );
          return be;
        },
        Check = (be, world) => {
          var tuyere = Assert.IsType<BlockEntityTuyere>(be);
          Assert.Equal("n", tuyere.Orientation);
          PipeNetworkState state = ((PipeNetwork)world.NetworkAt(At)!).State!;
          Assert.Equal("Air", state.MediumType);
          Assert.Equal(24f, state.Volume, 3);
          Assert.Equal(600f, state.Temperature, 3);
        },
      }
    );

  [Fact]
  public void A_reinforced_hopper_restores_its_stock() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-hopperreinforced-stocked",
        Block = () => Variants(new BlockHopperReinforced(), "smex:hopperreinforced"),
        Setup = world => {
          world.RegisterItem("game:ore-rich-magnetite-granite");
          world.RegisterItem("game:coke");
          world.RegisterItem("game:lime");
        },
        Live = (world, block) => {
          var be = new BlockEntityHopperReinforced();
          SaveFixtures.Stand(world, At, block, be);
          be.Inventory[0].Itemstack = new ItemStack(world.GetItem(new AssetLocation("game:ore-rich-magnetite-granite"))!, 12);
          be.Inventory[3].Itemstack = new ItemStack(world.GetItem(new AssetLocation("game:coke"))!, 20);
          be.Inventory[6].Itemstack = new ItemStack(world.GetItem(new AssetLocation("game:lime"))!, 5);
          return be;
        },
        Check = (be, _) => {
          var hopper = Assert.IsType<BlockEntityHopperReinforced>(be);
          AssertSlot(hopper, 0, "game:ore-rich-magnetite-granite", 12);
          AssertSlot(hopper, 3, "game:coke", 20);
          AssertSlot(hopper, 6, "game:lime", 5);
          Assert.True(hopper.Inventory[1].Empty);
        },
      }
    );

  [Fact]
  public void A_bell_hopper_restores_its_magazine_and_credits() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-hopperbell-stocked",
        Block = () => Variants(new BlockHopperBell(), "smex:hopperbell"),
        Live = (world, block) => {
          var be = new BlockEntityHopperBell();
          SaveFixtures.Stand(world, At, block, be);
          ReflectionHelpers.SetField(be, "_burdenMagazine", 9);
          ReflectionHelpers.SetField(be, "_oreCredit", 3);
          ReflectionHelpers.SetField(be, "_carbonCredit", 2);
          ReflectionHelpers.SetField(be, "_isDropping", false);
          return be;
        },
        Check = (be, _) => {
          var bell = Assert.IsType<BlockEntityHopperBell>(be);
          Assert.Equal(9, bell.BurdenMagazine);
          Assert.False(bell.IsDropping);
          Assert.Equal(3, (int)ReflectionHelpers.GetField(bell, "_oreCredit")!);
          Assert.Equal(2, (int)ReflectionHelpers.GetField(bell, "_carbonCredit")!);
        },
      }
    );

  [Fact]
  public void Slag_restores_its_count() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-slag",
        Block = () => Variants(new BlockSlag(), "smex:slag"),
        Live = (world, block) => {
          var be = new BlockEntitySlag();
          SaveFixtures.Stand(world, At, block, be);
          be.SlagCount = 5;
          return be;
        },
        Check = (be, _) => Assert.Equal(5, Assert.IsType<BlockEntitySlag>(be).SlagCount),
      }
    );

  [Fact]
  public void Solidified_iron_restores_its_count() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-solidifiediron",
        Block = () => Variants(new BlockSolidifiedIron(), "smex:solidifiediron"),
        Live = (world, block) => {
          var be = new BlockEntitySolidifiedIron();
          SaveFixtures.Stand(world, At, block, be);
          be.IronCount = 7;
          return be;
        },
        Check = (be, _) =>
          Assert.Equal(7, Assert.IsType<BlockEntitySolidifiedIron>(be).IronCount),
      }
    );

  [Fact]
  public void A_blower_restores_its_speed_and_construction() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-mpblower-blowing",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockMpBlower(), "smex:mpblower-east", ("side", "east")),
            Blocktypes + "blastfurnace/mpblower.json"
          ),
        Live = (world, block) => {
          var be = new BlockEntityMpBlower();
          SaveFixtures.Stand(world, At, block, be);
          SaveFixtures.CompleteConstruction(be, world);
          ReflectionHelpers.SetField(be, "_lastSpeed", 1.1f);
          return be;
        },
        Check = (be, _) => {
          var blower = Assert.IsType<BlockEntityMpBlower>(be);
          Assert.Equal(1.1f, (float)ReflectionHelpers.GetField(blower, "_lastSpeed")!, 3);
          Assert.True(blower.IsConstructed);
          Assert.Equal(
            SaveFixtures.ConstructionStages(Blocktypes + "blastfurnace/mpblower.json") - 1,
            SaveFixtures.ConstructionStage(blower)
          );
        },
      }
    );

  [Fact]
  public void An_engine_air_blower_loads_as_itself() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-engineairblower",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockEngineAirBlower(), "smex:engineairblower-north", ("side", "north")),
            Blocktypes + "engine/airblower.json"
          ),
        Live = (world, block) =>
          SaveFixtures.Stand(world, At, block, new BlockEntityEngineAirBlower()),
        Check = (be, _) => {
          var blower = Assert.IsType<BlockEntityEngineAirBlower>(be);
          Assert.NotNull(blower.GetBehavior<BEBehaviorAnimatable>());
        },
      }
    );

  #endregion

  #region Hot blast

  [Fact]
  public void A_cowper_stove_restores_its_heat() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-cowperstove-heating",
        Block = () =>
          Variants(new BlockCowperStoveIntake(), "smex:cowperstove-intake-tier2-west", ("type", "intake"), ("refractory", "tier2"), ("side", "west")),
        Live = (world, block) => {
          var be = new BlockEntityCowperStove();
          SaveFixtures.Stand(world, At, block, be);
          ReflectionHelpers.SetProperty(be, "StructureComplete", true);
          ReflectionHelpers.SetField(be, "_internalTemperature", 870f);
          ReflectionHelpers.SetField(be, "_lastStatus", "smex:cowperstove-status-heating");
          return be;
        },
        Check = (be, _) => {
          var stove = Assert.IsType<BlockEntityCowperStove>(be);
          Assert.True(stove.StructureComplete);
          Assert.Equal(870f, (float)ReflectionHelpers.GetField(stove, "_internalTemperature")!, 3);
          Assert.Equal("smex:cowperstove-status-heating", (string)ReflectionHelpers.GetField(stove, "_lastStatus")!);
        },
      }
    );

  [Fact]
  public void A_heat_sink_restores_its_temperature() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-cowperstoveheatsink-hot",
        Block = () =>
          Variants(new BlockHeatSink(), "smex:cowperstoveheatsink-tier1-north", ("refractory", "tier1"), ("side", "north")),
        Live = (world, block) => {
          var be = new BlockEntityHeatSink();
          SaveFixtures.Stand(world, At, block, be);
          ReflectionHelpers.SetField(be, "_temperature", 640f);
          return be;
        },
        Check = (be, _) =>
          Assert.Equal(
            640f,
            (float)ReflectionHelpers.GetField(Assert.IsType<BlockEntityHeatSink>(be), "_temperature")!,
            3
          ),
      }
    );

  [Fact]
  public void A_smoke_stack_restores_its_draw_and_orientation() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-smokestack-drawing",
        Block = () => {
          var block = Variants(new BlockSmokeStackIntake(), "smex:smokestack-intake-tier1-n", ("type", "intake"), ("refractory", "tier1"), ("orientation", "n"));
          ReflectionHelpers.SetProperty(block, "Type", "intake");
          ReflectionHelpers.SetProperty(block, "Orientation", "n");
          return block;
        },
        Setup = world => world.RegisterNetwork("pipe", sys => new PipeNetwork(sys)),
        Live = (world, block) => {
          var be = new BlockEntitySmokeStack { Orientation = "n", PossibleOrientations = ["n"] };
          SaveFixtures.Stand(world, At, block, be);
          ReflectionHelpers.SetProperty(be, "StructureComplete", true);
          ReflectionHelpers.SetField(be, "_lastConsumedAmount", 7.5f);
          return be;
        },
        Check = (be, world) => {
          var stack = Assert.IsType<BlockEntitySmokeStack>(be);
          Assert.True(stack.StructureComplete);
          Assert.Equal("n", stack.Orientation);
          Assert.Equal(["n"], stack.PossibleOrientations);
          Assert.Equal(7.5f, (float)ReflectionHelpers.GetField(stack, "_lastConsumedAmount")!, 3);
          Assert.NotNull(world.NetworkAt(At));
        },
      }
    );

  #endregion

  #region Converter

  [Fact]
  public void A_converter_control_mid_blow_restores_its_charge() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-convertercontrol-blowing",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockConverterControl(), "smex:convertercontrol-south", ("side", "south")),
            Blocktypes + "converter/control.json"
          ),
        Setup = RegisterMetals,
        Live = (world, block) => {
          var be = new BlockEntityConverterControl();
          SaveFixtures.Stand(world, At, block, be);
          ReflectionHelpers.SetProperty(be, "StructureComplete", true);
          ReflectionHelpers.SetField(be, "_content", MoltenMetal.CreateStack(world.World, Iron, 1600f));
          ReflectionHelpers.SetField(be, "_contentUnits", 240);
          ReflectionHelpers.Invoke(be, "AddScrap", "game:metalbit-iron", 20);
          ReflectionHelpers.SetField(be, "_processSeconds", 45f);
          ReflectionHelpers.SetField(be, "_blastPressure", 1.4f);
          ReflectionHelpers.SetField(be, "_airDrawn", 3f);
          ReflectionHelpers.SetField(be, "_airDemand", 4f);
          ReflectionHelpers.SetField(be, "_status", "smex:bessemer-status-blowing");
          return be;
        },
        Check = (be, _) => {
          var control = Assert.IsType<BlockEntityConverterControl>(be);
          Assert.True(control.StructureComplete);
          Assert.Equal(ConverterOpState.Normal, control.OpState);
          Assert.Equal(Iron, ((ItemStack)ReflectionHelpers.GetField(control, "_content")!).Collectible.Code.ToString());
          Assert.Equal(240, (int)ReflectionHelpers.GetField(control, "_contentUnits")!);
          var scrap = (System.Collections.Generic.Dictionary<string, int>)ReflectionHelpers.GetField(control, "_scrap")!;
          Assert.Equal(20, scrap["game:metalbit-iron"]);
          Assert.Equal(45f, (float)ReflectionHelpers.GetField(control, "_processSeconds")!, 3);
          Assert.Equal(1.4f, (float)ReflectionHelpers.GetField(control, "_blastPressure")!, 3);
          Assert.Equal(3f, (float)ReflectionHelpers.GetField(control, "_airDrawn")!, 3);
          Assert.Equal(4f, (float)ReflectionHelpers.GetField(control, "_airDemand")!, 3);
          Assert.False((bool)ReflectionHelpers.GetField(control, "_solidified")!);
          Assert.Equal("smex:bessemer-status-blowing", (string)ReflectionHelpers.GetField(control, "_status")!);
        },
      }
    );

  [Fact]
  public void A_bessemer_vessel_restores_its_link_charge_and_construction() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-converterbessemer-charged",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockConverterBessemer(), "smex:converterbessemer-south", ("side", "south")),
            Blocktypes + "converter/bessemer.json"
          ),
        Live = (world, block) => {
          var be = new BlockEntityConverterBessemer();
          SaveFixtures.Stand(world, At, block, be);
          SaveFixtures.CompleteConstruction(be, world);
          ReflectionHelpers.SetField(be, "_controlPos", At.AddCopy(0, 0, 2));
          ReflectionHelpers.SetField(be, "_chargeUnits", 240);
          return be;
        },
        Check = (be, _) => {
          var vessel = Assert.IsType<BlockEntityConverterBessemer>(be);
          Assert.Equal(At.AddCopy(0, 0, 2), (BlockPos)ReflectionHelpers.GetField(vessel, "_controlPos")!);
          Assert.Equal(ConverterOpState.Normal, (ConverterOpState)ReflectionHelpers.GetField(vessel, "_opState")!);
          Assert.Equal(240, (int)ReflectionHelpers.GetField(vessel, "_chargeUnits")!);
          Assert.False(vessel.IsSolidified);
          Assert.True(vessel.IsConstructed);
          Assert.Equal(
            SaveFixtures.ConstructionStages(Blocktypes + "converter/bessemer.json") - 1,
            SaveFixtures.ConstructionStage(vessel)
          );
        },
      }
    );

  [Fact]
  public void A_converter_transmission_loads_as_itself() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-convertertransmission",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockConverterTransmission(), "smex:convertertransmission-south", ("side", "south")),
            Blocktypes + "converter/transmission.json"
          ),
        Initialize = false,
        Live = (world, block) =>
          SaveFixtures.Stand(world, At, block, new BlockEntityConverterTransmission(), initialize: false),
        Check = (be, _) =>
          Assert.NotNull(
            Assert.IsType<BlockEntityConverterTransmission>(be).GetBehavior<BEBehaviorMPConverterTransmission>()
          ),
      }
    );

  #endregion

  #region Molds

  [Fact]
  public void A_fired_tool_mold_restores_its_cast() =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = "smex-toolmold-fired-casting",
        Block = () =>
          SaveFixtures.ShippedBehaviors(
            Variants(new BlockToolMold(), "smex:toolmold-black-fired-plate", ("color", "black"), ("materialtype", "fired"), ("tooltype", "plate")),
            Blocktypes + "molds/toolmoldfired.json"
          ),
        Setup = RegisterMetals,
        Live = (world, block) => {
          var be = new BlockEntityToolMold();
          SaveFixtures.Stand(world, At, block, be);
          be.MetalContent = MoltenMetal.CreateStack(world.World, Iron, 1400f);
          be.FillLevel = 50;
          return be;
        },
        Check = (be, _) => {
          var mold = Assert.IsType<BlockEntityToolMold>(be);
          Assert.Equal(Iron, mold.MetalContent!.Collectible.Code.ToString());
          Assert.Equal(50, mold.FillLevel);
          Assert.False(mold.Shattered);
        },
      }
    );

  #endregion

  #region Helpers

  private const string MoldCode = "smex:toolmold-black-fired-plate";

  /// <summary>
  /// A molten-canal cell holding <paramref name="amount"/> units of iron at
  /// <paramref name="temperature"/> degrees C; the load must restore the cell's metal, latches and
  /// placed orientation.
  /// </summary>
  private static void Cell(
    string name,
    Func<Block> block,
    Func<BlockEntityMoltenCanal> entity,
    string orientation,
    int amount,
    float temperature,
    bool solidified = false,
    bool sealed_ = false,
    Action<BlockEntityMoltenCanal>? prime = null,
    Action<BlockEntityMoltenCanal>? check = null
  ) =>
    SaveGoldens.Verify(
      new SaveCase {
        Name = name,
        Block = block,
        Setup = world => {
          RegisterMetals(world);
          world.RegisterNetwork("molten", sys => new MoltenNetwork(sys));
        },
        Live = (world, b) => {
          BlockEntityMoltenCanal be = entity();
          be.Orientation = orientation;
          be.PossibleOrientations = [orientation];
          world.Place(At, b, be);
          world.Attach(be);
          ReflectionHelpers.SetProperty(be, nameof(be.NetworkSystem), world.Networks);
          world.AddNode(At, "molten");
          SetOn(be, typeof(BlockEntityMoltenCanal), nameof(be.CellAmount), amount);
          SetOn(be, typeof(BlockEntityMoltenCanal), nameof(be.CellMetalType), amount > 0 ? Iron : "");
          ReflectionHelpers.SetField(be, "_cellTemperature", temperature);
          SetOn(be, typeof(BlockEntityMoltenCanal), nameof(be.Solidified), solidified);
          SetOn(be, typeof(BlockEntityMoltenCanal), nameof(be.Sealed), sealed_);
          prime?.Invoke(be);
          return be;
        },
        Check = (be, _) => {
          Assert.Equal(entity().GetType(), be.GetType());
          var cell = (BlockEntityMoltenCanal)be;
          Assert.Equal(orientation, cell.Orientation);
          Assert.Equal("molten", cell.NetworkType);
          Assert.Equal(amount, cell.CellAmount);
          Assert.Equal(amount > 0 ? Iron : "", cell.CellMetalType);
          Assert.Equal(temperature, cell.CellTemperature, 3);
          Assert.Equal(solidified, cell.Solidified);
          Assert.Equal(sealed_, cell.Sealed);
          check?.Invoke(cell);
        },
      }
    );

  /// <summary>Registers the metals and the tool mold the goldens carry, in a fixed order so their ids match between worlds.</summary>
  private static void RegisterMetals(TestWorld world) {
    world.RegisterItem(Iron, 1500f);
    world.RegisterItem("game:metalbit-iron");
    world.Register(
      TestBlocks.Configure(new Block(), MoldCode, 60, ("color", "black"), ("materialtype", "fired"), ("tooltype", "plate"))
    );
  }

  private static ItemStack Metal(BlockEntity be, float temperature) =>
    MoltenMetal.CreateStack(be.Api.World, Iron, temperature)!;

  private static ItemStack MoldStack(BlockEntity be) =>
    new(be.Api.World.GetBlock(new AssetLocation(MoldCode)));

  private static void AssertSlot(
    BlockEntityHopperReinforced hopper,
    int slot,
    string code,
    int size
  ) {
    ItemStack stack = hopper.Inventory[slot].Itemstack;
    Assert.Equal(code, stack.Collectible.Code.ToString());
    Assert.Equal(size, stack.StackSize);
  }

  /// <summary>Sets a property through the setter <paramref name="declaring"/> declares, whatever its access.</summary>
  private static void SetOn(
    object target,
    Type declaring,
    string property,
    object? value
  ) => declaring.GetProperty(property)!.SetValue(target, value);

  private static Block Canal(
    BlockMoltenCanal block,
    string code,
    string type,
    string orientation,
    bool rock = false,
    string? brick = "black"
  ) {
    var variants = new System.Collections.Generic.List<(string, string)> { ("type", type) };
    if (rock)
      variants.Add(("rock", "andesite"));
    else if (brick != null)
      variants.Add(("brick", brick));
    variants.Add(("orientation", orientation));
    Variants(block, code, [.. variants]);
    ReflectionHelpers.SetProperty(block, "Type", type);
    ReflectionHelpers.SetProperty(block, "Orientation", orientation);
    return block;
  }

  private static Block Variants(
    Block block,
    string code,
    params (string key, string value)[] variants
  ) => TestBlocks.Configure(block, code, 1, variants);

  #endregion
}
