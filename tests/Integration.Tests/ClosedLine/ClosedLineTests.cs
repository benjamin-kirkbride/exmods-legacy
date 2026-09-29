using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandedLib.Testing;
using Newtonsoft.Json.Linq;
using NSubstitute;
using PipesAndPowerExpanded.ClosedLine;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Xunit;
using Xunit.Abstractions;
#if GAME_GE_1_22
using Integration.Tests.Guards;
#endif

namespace Integration.Tests.ClosedLine;

/// <summary>
/// The closed line in <see cref="ClosedLineWorld.Closed"/>, where iiex is enabled: no ppex or smex
/// recipe is left, no ppex or smex collectible is in the creative inventory or the handbook, no ppex
/// or smex stack drops from a ppex or smex block, and each player is told once, by the names of the
/// successors enabled.
/// </summary>
[Collection(ClosedLineWorld.Collection)]
public class ClosedLineTests(ITestOutputHelper output) {
  private static ClosedLineWorld Closed => ClosedLineWorld.Closed;

  // Fails when ClosedLineRecipes.Remove removes nothing: the ppex and smex recipes stay in their
  // registries.
  [Fact]
  public void No_recipe_with_a_ppex_or_smex_output_or_source_is_left() {
    Dictionary<string, (int All, int OldLine)> after = Closed.CountRecipes();
    foreach (string registry in ClosedLineRecipes.Registries) {
      var before = Closed.RecipesBefore[registry];
      output.WriteLine(
        $"{registry}: {before.All} before, {before.OldLine} ppex or smex, {after[registry].All} after"
      );
    }

    Assert.All(
      ClosedLineRecipes.Registries,
      registry => {
        Assert.Equal(0, after[registry].OldLine);
        Assert.Equal(
          Closed.RecipesBefore[registry].All - Closed.RecipesBefore[registry].OldLine,
          after[registry].All
        );
      }
    );
    Assert.All(
      new[] { "grid", "smithing", "clayforming", "barrel" },
      registry => Assert.True(Closed.RecipesBefore[registry].OldLine > 0, registry)
    );
  }

  // Fails when ClosedLineModSystem hides nothing, or keeps every guide page.
  [Fact]
  public void Every_ppex_and_smex_collectible_is_off_the_creative_inventory_and_the_handbook() {
    output.WriteLine(
      $"{Closed.OldLine.Count} ppex and smex collectibles, {Closed.InCreativeBefore.Count} in the creative inventory before"
    );
    Assert.NotEmpty(Closed.InCreativeBefore);
    Assert.All(
      Closed.OldLine,
      c => {
        Assert.Empty(c.CreativeInventoryTabs);
        Assert.Null(c.CreativeInventoryStacks);
        Assert.Null(c.GetHandBookStacks(Closed.World.ClientApi));
      }
    );

    List<GuiHandbookPage> pages = GuidePages(out List<GuiHandbookPage> vanilla);
    output.WriteLine($"{pages.Count} guide pages, {vanilla.Count} of the game");
    Assert.True(pages.Count > vanilla.Count && vanilla.Count > 0);
    typeof(ModSystemSurvivalHandbook)
      .GetMethod(
        "TriggerOnInitCustomPages",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
      )!
      .Invoke(Closed.Handbook, [pages]);
    Assert.Equal(vanilla, pages);
  }

  // Fails when WithoutOldLine keeps every stack, or the GetDrops postfix does not apply it: a pipe
  // drops itself, and a paid construction refunds the ppex pipe its stage took.
  [Fact]
  public void No_ppex_or_smex_stack_drops_from_a_ppex_or_smex_block() {
    TestWorld world = Closed.World;
    var spawned = new List<ItemStack>();
    var readied = new HashSet<Block>();
#if GAME_GE_1_22
    StructureBreakGuards.HoldWoods(world);
    StructureBreaks.Result breaks = StructureBreaks.Run(
      world,
      b => ClosedLineModSystem.IsOldLine(b.Code)
    );
    output.WriteLine(
      $"StructureBreaks: {breaks.Blocks} blocktypes, {breaks.Variants} variants, {breaks.Breaks} breaks"
    );
    spawned.AddRange(breaks.Spawned.SelectMany(s => s.Stacks));
    readied.UnionWith(
      world.World.Blocks.Where(b =>
        ClosedLineModSystem.IsOldLine(b?.Code) && ClosedLineWorld.IsStructure(b)
      )
    );
    Assert.True(breaks.Blocks >= 8, $"{breaks.Blocks} blocktypes broken");
#endif
    var dropped = new List<string>();
    var threw = new List<string>();
    foreach (Block block in Closed.OldLine.OfType<Block>()) {
      if (!readied.Contains(block) && Closed.Ready(block) is { } loadFailure)
        threw.Add($"{block.Code} OnLoaded: {loadFailure.GetType().Name}");
      var (codes, failure) = Closed.DropsOf(block);
      if (failure != null)
        threw.Add($"{block.Code} GetDrops: {failure.GetType().Name}");
      else
        dropped.AddRange(codes!);
    }
    output.WriteLine(
      $"GetDrops: {Closed.OldLine.OfType<Block>().Count()} blocks, {dropped.Count} codes dropped, {threw.Count} threw"
    );
    foreach (string line in threw)
      output.WriteLine("  " + line);

    var oldLine = spawned
      .Select(s => s.Collectible.Code.ToString())
      .Concat(dropped)
      .Where(c => ClosedLineModSystem.IsOldLine(new AssetLocation(c)))
      .Distinct()
      .ToList();
    Assert.Empty(oldLine);
    Assert.True(threw.Count < Closed.OldLine.OfType<Block>().Count() / 2);
#if GAME_GE_1_22
    Assert.Contains(spawned, s => s.Collectible.Code.Domain == "game");
#endif
  }

  // Fails when the save's record is not read: the notice is sent on every join.
  [Fact]
  public void The_notice_is_sent_once_over_two_joins_of_one_player() {
    TestPlayer joiner = Closed.World.Player("joiner");
    IServerPlayer player = Assert.IsAssignableFrom<IServerPlayer>(joiner.ServerPlayer);
    player.LanguageCode.Returns("en");
    player.InventoryManager.Inventories.Returns([]);

    for (int join = 0; join < 2; join++)
      Closed.World.Api.Event.PlayerJoin += Raise.Event<PlayerDelegate>(player);

    player
      .Received(1)
      .SendMessage(
        GlobalConstants.GeneralChatGroup,
        Arg.Any<string>(),
        EnumChatType.Notification,
        Arg.Any<string>()
      );
    Assert.Contains("joiner", Closed.SaveData["ppex:closedline-notified"]);
    Assert.Contains(
      Closed.World.Log.Entries,
      e =>
        e.Type == EnumLogType.Notification
        && e.Message.Contains("ppex and smex are closed", StringComparison.Ordinal)
    );
  }

  // Fails when a locale lacks the notice or its join, or either carries an angle bracket, which
  // cuts or blanks a chat line.
  [Theory]
  [InlineData("en")]
  [InlineData("ru")]
  [InlineData("uk")]
  public void The_notice_reads_in_every_locale_without_angle_brackets(string locale) {
    JObject lang = JObject.Parse(
      File.ReadAllText(
        Path.Combine(RepoPaths.Root, "ppex", "assets", "ppex", "lang", locale + ".json")
      )
    );
    foreach (string key in new[] { ClosedLineNotice.LangKey, ClosedLineNotice.AndKey }) {
      string? text = (string?)lang[key.Split(':')[1]];
      output.WriteLine($"{locale} {key}: {text}");

      Assert.False(string.IsNullOrWhiteSpace(text), key);
      Assert.Contains("{0}", text);
      Assert.DoesNotContain('<', text);
      Assert.DoesNotContain('>', text);
    }
  }

  // Fails when the notice names iiex whatever is enabled: a world with siex alone is told of iiex.
  [Theory]
  [InlineData("en")]
  [InlineData("ru")]
  [InlineData("uk")]
  public void The_notice_names_siex_when_only_siex_is_enabled(string locale) {
    string notice = NoticeIn(locale, "siex");

    Assert.Contains("Steel Industry Expanded", notice);
    Assert.DoesNotContain("Iron Industry Expanded", notice);
    Assert.DoesNotContain("{", notice);
  }

  // Fails when the join keeps only the last name: a world with both is told of siex alone.
  [Theory]
  [InlineData("en", "and")]
  [InlineData("ru", "\u0438")]
  [InlineData("uk", "\u0456")]
  public void The_notice_names_both_when_both_are_enabled(string locale, string and) {
    string notice = NoticeIn(locale, "iiex", "siex");

    Assert.Contains($"Iron Industry Expanded {and} Steel Industry Expanded", notice);
  }

  /// <summary>The chat line <see cref="ClosedLineNotice.Send"/> sends a player reading
  /// <paramref name="locale"/> in a fresh world where only <paramref name="successors"/> are
  /// enabled, each under its modinfo name.</summary>
  private string NoticeIn(string locale, params string[] successors) {
    var world = new TestWorld();
    foreach (string id in successors) {
      world.Mods.Add(id, "0.1.0");
      world.Mods.GetMod(id)!.Info.Name = SuccessorNames[id];
    }
    IServerPlayer player = Assert.IsAssignableFrom<IServerPlayer>(
      world.Player("told").ServerPlayer
    );
    player.LanguageCode.Returns(NoticeLocales.Prefix + locale);
    string? sent = null;
    player
      .When(p =>
        p.SendMessage(
          Arg.Any<int>(),
          Arg.Any<string>(),
          Arg.Any<EnumChatType>(),
          Arg.Any<string>()
        )
      )
      .Do(ci => sent = ci.ArgAt<string>(1));

    Assert.True(ClosedLineNotice.Send(world.Api, player));
    output.WriteLine($"{locale}, {string.Join(" and ", successors)}: {sent}");
    return Assert.IsType<string>(sent);
  }

  private static readonly Dictionary<string, string> SuccessorNames = new() {
    ["iiex"] = "Iron Industry Expanded",
    ["siex"] = "Steel Industry Expanded",
  };

  /// <summary>The guide pages of the game install, ppex and smex, as the handbook reads them from
  /// <c>config/handbook</c>; <paramref name="game"/> receives the game's.</summary>
  internal static List<GuiHandbookPage> GuidePages(out List<GuiHandbookPage> game) {
    var dirs = new List<(string Domain, string Dir)> {
      (
        "game",
        Path.Combine(VsAssemblyResolver.InstallPath!, "assets", "survival", "config", "handbook")
      ),
    };
    foreach (string mod in new[] { "ppex", "smex" })
      dirs.Add((mod, Path.Combine(RepoPaths.Root, mod, "assets", mod, "config", "handbook")));
    var pages = new List<GuiHandbookPage>();
    game = [];
    foreach ((string domain, string dir) in dirs)
      if (Directory.Exists(dir))
        foreach (string file in Directory.EnumerateFiles(dir, "*.json").Order(StringComparer.Ordinal))
          if (JToken.Parse(File.ReadAllText(file)).ToObject<GuiHandbookTextPage>(domain) is { } page) {
            pages.Add(page);
            if (domain == "game")
              game.Add(page);
          }
    return pages;
  }
}
