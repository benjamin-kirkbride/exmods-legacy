using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandedLib.Blocks;
using ExpandedLib.Testing;
using Newtonsoft.Json.Linq;
using NSubstitute;
using PipesAndPowerExpanded.BlockStructures.MpPump.BlockEntities;
using SteelmakingExpanded.BlockStructures.BlastFurnace.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Xunit;
using System;
#if GAME_GE_1_22
using Vintagestory.GameContent;
#endif

namespace Integration.Tests;

/// <summary>
/// Builds the MP fluid pump and the twin-tub blower from their shipped JSON to each stage through
/// the real <see cref="ExRightClickConstructable"/> (vanilla's <c>RightClickConstruction</c> on
/// 1.22, exlib's port on 1.20 and 1.21), paying from a survival player's hotbar or building as a
/// creative player, then breaks the structure and compares the refund with what was paid.
/// </summary>
public class ConstructionBreakTests {
  private const string Pump = "ppex/assets/ppex/blocktypes/mpfluidpump.json";
  private const string Blower =
    "smex/assets/smex/blocktypes/blastfurnace/mpblower.json";

  public static TheoryData<string, int> EveryStage() {
    var data = new TheoryData<string, int>();
    foreach (string path in new[] { Pump, Blower })
      for (int built = 0; built < Stages(path).Count; built++)
        data.Add(path, built);
    return data;
  }

  #region Break

  [Theory]
  [MemberData(nameof(EveryStage))]
  public void A_structure_broken_at_any_stage_refunds_what_was_paid(
    string path,
    int built
  ) {
    var rig = new Rig(path);
    var paid = new List<ItemStack>();
    for (int stage = 1; stage <= built; stage++)
      paid.AddRange(rig.Pay(stage));

    rig.Behavior.OnBlockBroken(null);

    Assert.Equal(Tally(paid), Tally(rig.Drops));
  }

  #endregion

  #region Saved structures

  private const string ShippedPump =
    "tests/Integration.Tests/Machines/Shipped/mpfluidpump-0.6.8.json";
  private const string ShippedBlower =
    "tests/Integration.Tests/Machines/Shipped/mpblower-0.9.8.json";

  public static TheoryData<string, string, int> SavedUnderTheShippedJson() {
    var data = new TheoryData<string, string, int>();
    foreach (
      (string path, string shipped) in new[] {
        (Pump, ShippedPump),
        (Blower, ShippedBlower),
      }
    ) {
      data.Add(path, shipped, 1);
      data.Add(path, shipped, Stages(path).Count - 1);
    }
    return data;
  }

  [Theory]
  [MemberData(nameof(SavedUnderTheShippedJson))]
  public void A_structure_saved_without_a_wood_key_refunds_its_wood_as_oak(
    string path,
    string shipped,
    int built
  ) {
    var old = new Rig(shipped, "pine");
    var paid = new List<ItemStack>();
    for (int stage = 1; stage <= built; stage++)
      paid.AddRange(old.Pay(stage));

    var rig = new Rig(path, "oak", Entity(path));
    rig.Load(old.Save());
    rig.Behavior.OnBlockBroken(null);

    Assert.Equal(
      Tally(paid).ToDictionary(p => p.Key.Replace("-pine", "-oak"), p => p.Value),
      Tally(rig.Drops)
    );
  }

  [Theory]
  [InlineData(Pump)]
  [InlineData(Blower)]
  public void A_saved_structure_refunds_the_wood_and_metal_it_recorded(
    string path
  ) {
    var built = new Rig(path, "pine", metal: "steel");
    var paid = new List<ItemStack>();
    for (int stage = 1; stage < Stages(path).Count; stage++)
      paid.AddRange(built.Pay(stage));

    var rig = new Rig(path, "pine", Entity(path), "steel");
    rig.Load(built.Save());
    rig.Behavior.OnBlockBroken(null);

    Assert.Equal(Tally(paid), Tally(rig.Drops));
  }

  [Theory]
  [InlineData(Pump)]
  [InlineData(Blower)]
  public void A_structure_loaded_without_wood_or_metal_records_oak_and_iron(
    string path
  ) {
    var creative = new Rig(path);
    creative.Creative();
    Assert.True(creative.Interact());
    Assert.Empty((TreeAttribute)creative.Save()["wildcards"]);

    var rig = new Rig(path, "oak", Entity(path));
    rig.Load(creative.Save());

    var wildcards = (TreeAttribute)rig.Save()["wildcards"];
    Assert.Equal("oak", wildcards.GetString("wood"));
    Assert.Equal("iron", wildcards.GetString("metal"));
  }

  private static Func<BlockEntity> Entity(string path) =>
    path == Pump
      ? () => new BlockEntityMpFluidPump()
      : () => new BlockEntityMpBlower();

  #endregion

  #region Creative builds

  [Theory]
  [InlineData(Pump, true)]
  [InlineData(Pump, false)]
  [InlineData(Blower, true)]
  [InlineData(Blower, false)]
  public void A_creative_build_broken_without_a_reload_refunds_oak_and_iron(
    string path,
    bool emptyHotbar
  ) {
    var rig = new Rig(path, "oak", Entity(path));
    rig.Creative();
    rig.Hotbar(emptyHotbar ? Array.Empty<ItemStack>() : rig.Bill(1));
    var bill = new List<ItemStack>();
    // exlib's 1.20 and 1.21 port refuses the pipe stage of an empty-hotbar build, whose {metal}
    // was never recorded; 1.22 builds it.
    for (int stage = 1; stage < Stages(path).Count && rig.Interact(); stage++)
      bill.AddRange(rig.Bill(stage));
    Assert.NotEmpty(bill);

    rig.Break();

    Assert.Equal(Tally(bill), Tally(rig.Drops));
  }

  #endregion

  #region Payment

  [Theory]
  [InlineData("supportbeam-oak", true)]
  [InlineData("supportbeam-tarnishedmetal-iron", false)]
  public void The_blower_beam_stage_takes_wooden_beams_only(
    string beam,
    bool accepted
  ) {
    var rig = new Rig(Blower);
    Block block = rig.Block("game:" + beam);

    var stacks = rig.Bill(1).ToList();
    stacks[0] = new ItemStack(block, stacks[0].StackSize);
    rig.Hotbar(stacks);

    Assert.Equal(accepted, rig.Interact());
  }

  #endregion

  private static Dictionary<string, int> Tally(
    IEnumerable<ItemStack> stacks
  ) =>
    stacks
      .GroupBy(s => s.Collectible.Code.ToString())
      .OrderBy(g => g.Key)
      .ToDictionary(g => g.Key, g => g.Sum(s => s.StackSize));

  private static JArray Stages(string path) {
    JToken root = JToken.Parse(
      File.ReadAllText(Path.Combine(ShippedJsonAssetTests.RepoRoot(), path))
    );
    return (JArray)Properties(root)["stages"]!;
  }

  private static JObject Properties(JToken root) =>
    (JObject)
      root["entityBehaviors"]!
        .First(b => (string?)b["name"] == "ExRightClickConstructable")[
        "properties"
      ]!;

  /// <summary>
  /// One structure placed from <c>path</c> in a <see cref="TestWorld"/> whose registry holds the
  /// variants of one wood and one metal the two structures take, plus the iron support beam. The
  /// block entity is a bare host unless <c>makeEntity</c> supplies the production one.
  /// </summary>
  private sealed class Rig {
    private readonly TestWorld _world = new();
    private readonly List<CollectibleObject> _collectibles = [];
    private readonly List<ItemSlot> _hotbar = [];
    private readonly IPlayer _player;
    private readonly EntityPlayer _agent;
    private readonly JArray _stages;
    private readonly string _metal;
    private int _nextBlockId = 100;
    private readonly BlockEntity _entity;

    public ExRightClickConstructable Behavior { get; }
    public List<ItemStack> Drops => _world.Drops;

    public Rig(
      string path,
      string wood = "oak",
      Func<BlockEntity>? makeEntity = null,
      string metal = "iron"
    ) {
      JToken root = JToken.Parse(
        File.ReadAllText(Path.Combine(ShippedJsonAssetTests.RepoRoot(), path))
      );
      _stages = Stages(path);
      _metal = metal;

      Item("game:plank-" + wood, ("wood", wood));
      Item("game:rod-" + metal, ("metal", metal));
      Item("game:metalplate-" + metal, ("metal", metal));
      Item("game:metalnailsandstrips-" + metal, ("metal", metal));
      Block("game:supportbeam-" + wood, ("wood", wood));
      Block("game:supportbeam-tarnishedmetal-iron", ("metal", "iron"));
      Block("game:woodenaxle-ud", ("type", "ud"));
      Block(
        "ppex:pipe-straight-ns-" + metal,
        ("type", "straight"),
        ("orientation", "ns"),
        ("material", metal)
      );
      _world.World.Collectibles.Returns(_collectibles);

      var host = TestBlocks.Configure(
        new Block(),
        (string)root["code"]! + "-north",
        1,
        ("side", "north")
      );
      host.Shape = new CompositeShape {
        Base = new AssetLocation(host.Code.Domain, "shape"),
      };
      _entity = makeEntity?.Invoke() ?? new HostEntity();
      _world.Place(new BlockPos(0, 0, 0), host, _entity).Attach(_entity);
      Behavior = new ExRightClickConstructable(_entity);
      Behavior.Initialize(_world.Api, new JsonObject(Properties(root)));
      _entity.Behaviors.Add(Behavior);

      var hotbar = Substitute.For<IInventory>();
      ((IEnumerable<ItemSlot>)hotbar)
        .GetEnumerator()
        .Returns(_ => _hotbar.GetEnumerator());
      _player = Substitute.For<IPlayer>();
      _player.InventoryManager.GetHotbarInventory().Returns(hotbar);
      _player.WorldData.CurrentGameMode.Returns(EnumGameMode.Survival);
      _agent = Substitute.For<EntityPlayer>();
      _agent.World = _world.World;
      _agent.WatchedAttributes.SetString("playerUID", "tester");
      _player.Entity.Returns(_agent);
      _world.World.PlayerByUid(Arg.Any<string>()).Returns(_player);
    }

    /// <summary>
    /// The stacks <paramref name="stage"/> costs, each ingredient paid with the one registered
    /// collectible other than the metal beam its code matches once <c>{metal}</c> is filled.
    /// </summary>
    public IEnumerable<ItemStack> Bill(int stage) {
      foreach (JToken ing in _stages[stage]["requireStacks"]!) {
        var code = new AssetLocation(
          ((string)ing["code"]!).Replace("{metal}", _metal)
        );
        bool block = (string?)ing["type"] == "block";
        CollectibleObject match = _collectibles.Single(c =>
          (c is Block) == block
          && c.Code.Domain == code.Domain
          && WildcardUtil.Match(code.Path, c.Code.Path)
          && !c.Code.Path.Contains("tarnishedmetal")
        );
        yield return match is Block b
          ? new ItemStack(b, (int)ing["quantity"]!)
          : new ItemStack((Item)match, (int)ing["quantity"]!);
      }
    }

    /// <summary>Pays <paramref name="stage"/> in full from the hotbar and returns what it took.</summary>
    public List<ItemStack> Pay(int stage) {
      var bill = Bill(stage).ToList();
      Hotbar(bill);
      Assert.True(Interact(), $"stage {stage} was not accepted");
      Assert.All(
        _hotbar,
        s => Assert.True(s.Empty, $"stage {stage} left {s.Itemstack}")
      );
      return bill.Select(s => s.Clone()).ToList();
    }

    public void Hotbar(IEnumerable<ItemStack> stacks) {
      _hotbar.Clear();
      foreach (ItemStack s in stacks)
        _hotbar.Add(new DummySlot(s.Clone()));
    }

    /// <summary>Builds from here on as a creative player holding Ctrl, who pays nothing.</summary>
    public void Creative() {
      _player.WorldData.CurrentGameMode.Returns(EnumGameMode.Creative);
      _agent.Controls.CtrlKey = true;
    }

    /// <summary>Breaks the structure through its block entity, as a survival player does.</summary>
    public void Break() => _entity.OnBlockBroken(null);

    /// <summary>The construction state as the block entity saves it.</summary>
    public TreeAttribute Save() {
      var tree = new TreeAttribute();
      Behavior.ToTreeAttributes(tree);
      return tree;
    }

    /// <summary>Loads <paramref name="tree"/> through the block entity, as a chunk load does.</summary>
    public void Load(TreeAttribute tree) =>
      _entity.FromTreeAttributes(tree, _world.World);

    /// <summary>One right click; true when it completed the next stage.</summary>
    public bool Interact() {
      int before = Completed;
      var handling = EnumHandling.PassThrough;
      Behavior.OnBlockInteractStart(
        _world.World,
        _player,
        new BlockSelection { Position = new BlockPos(0, 0, 0) },
        ref handling
      );
      return Completed == before + 1;
    }

    private int Completed =>
      (int)
        ReflectionHelpers.GetField(
          ReflectionHelpers.GetField(Behavior, "rcc")!,
          "CurrentCompletedStage"
        )!;

    public Block Block(string code, params (string, string)[] variants) {
      var block = _collectibles
        .OfType<Block>()
        .FirstOrDefault(b => b.Code.ToString() == code);
      if (block != null)
        return block;
      block = TestBlocks.Configure(
        new Block(),
        code,
        _nextBlockId++,
        variants
      );
      _world.Register(block);
      Add(block);
      return block;
    }

    private void Item(
      string code,
      params (string key, string value)[] variants
    ) {
      Item item = _world.RegisterItem(code);
      foreach (var (key, value) in variants)
        item.VariantStrict[key] = value;
      item.Variant = new RelaxedReadOnlyDictionary<string, string>(
        item.VariantStrict
      );
      Add(item);
    }

    // Stack comparison reads the collectible's api, which the game sets on load.
    private void Add(CollectibleObject collectible) {
      ReflectionHelpers.SetField(collectible, "api", _world.Api);
      _collectibles.Add(collectible);
    }
  }

  private sealed class HostEntity : BlockEntity { }
}
