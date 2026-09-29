using System.Linq;
using Newtonsoft.Json.Linq;
using PipesAndPowerExpanded.ClosedLine;
using Vintagestory.API.Common;
using Xunit;

namespace Integration.Tests.ClosedLine;

/// <summary>
/// The MP fluid pump built from its shipped JSON (<see cref="ConstructionBreakTests.Rig"/>) with its
/// pipe stage paid in a stand-in for iiex's plated straight pipe: taken when iiex is enabled,
/// refused when it is not. Runs through vanilla's <c>RightClickConstruction</c> on 1.22 and exlib's
/// port on 1.20 and 1.21.
/// </summary>
public class ClosedLineConstructionTests {
  private const string NewPipe = "iiex:pipe-plated-straight-ns";

  // Fails when the pipe postfix leaves the match false: the stage refuses the new line's pipe.
  [Fact]
  public void A_part_built_pump_takes_the_new_lines_pipe_and_completes() {
    var rig = new ConstructionBreakTests.Rig(
      ConstructionBreakTests.Pump,
      domain: "ppex"
    );
    rig.World.Mods.Add("iiex", "0.7.0");
    ClosedLinePatches.Apply(rig.World.Api);
    int stages = ConstructionBreakTests.Stages(ConstructionBreakTests.Pump).Count;
    int pipeStage = PipeStage();

    for (int stage = 1; stage < pipeStage; stage++)
      rig.Pay(stage);
    rig.Hotbar(WithNewPipe(rig, pipeStage));
    Assert.True(rig.Interact(), "the pipe stage refused the new line's pipe");
    Assert.All(rig.Held, s => Assert.NotEqual(NewPipe, s.Collectible.Code.ToString()));
    for (int stage = pipeStage + 1; stage < stages; stage++)
      rig.Pay(stage);

    Assert.Equal(stages - 1, rig.Completed);
  }

  // Fails when the refund patch takes nothing out: the break refunds the ppex pipe the stage took.
  [Fact]
  public void A_broken_pump_refunds_no_ppex_pipe_and_its_other_materials() {
    var rig = new ConstructionBreakTests.Rig(
      ConstructionBreakTests.Pump,
      domain: "ppex"
    );
    rig.World.Mods.Add("iiex", "0.7.0");
    ClosedLinePatches.Apply(rig.World.Api);
    int pipeStage = PipeStage();
    for (int stage = 1; stage <= pipeStage; stage++)
      rig.Pay(stage);

    rig.Break();

    Assert.DoesNotContain(rig.Drops, s => s.Collectible.Code.Domain == "ppex");
    Assert.Contains(rig.Drops, s => s.Collectible.Code.ToString() == "game:metalplate-iron");
  }

  // Fails when the pipe postfix ignores the switch: an open pump takes the new line's pipe.
  [Fact]
  public void An_open_pump_refuses_the_new_lines_pipe() {
    var rig = new ConstructionBreakTests.Rig(
      ConstructionBreakTests.Pump,
      domain: "ppex"
    );
    ClosedLinePatches.Apply(rig.World.Api);
    int pipeStage = PipeStage();
    for (int stage = 1; stage < pipeStage; stage++)
      rig.Pay(stage);
    rig.Hotbar(WithNewPipe(rig, pipeStage));

    Assert.False(rig.Interact());
  }

  /// <summary>The pump's stage whose ingredients name a ppex straight pipe.</summary>
  private static int PipeStage() {
    var stages = ConstructionBreakTests.Stages(ConstructionBreakTests.Pump);
    return Enumerable
      .Range(0, stages.Count)
      .Single(i =>
        stages[i]["requireStacks"] is JArray required
        && required.Any(r =>
          ClosedLinePatches.IsOldStraightPipe(new AssetLocation((string)r["code"]!))
        )
      );
  }

  /// <summary>What <paramref name="stage"/> costs, its ppex pipe replaced by as many of the stand-in
  /// new-line pipe.</summary>
  private static ItemStack[] WithNewPipe(ConstructionBreakTests.Rig rig, int stage) {
    Block pipe = rig.Block(
      NewPipe,
      ("tier", "plated"),
      ("type", "straight"),
      ("orientation", "ns")
    );
    return [
      .. rig.Bill(stage)
        .Select(s =>
          ClosedLinePatches.IsOldStraightPipe(s.Collectible.Code)
            ? new ItemStack(pipe, s.StackSize)
            : s
        ),
    ];
  }
}
