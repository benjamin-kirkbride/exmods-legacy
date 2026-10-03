using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ExpandedLib.Industry.MechanicalPower;
using ExpandedLib.Industry.Pipes;
using ExpandedLib.Testing;
using Integration.Tests.Saves;
using PipesAndPowerExpanded.BlockStructures.Engine;
using PipesAndPowerExpanded.BlockStructures.Engine.BlockEntities;
using PipesAndPowerExpanded.BlockStructures.Engine.Blocks;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;
using Xunit;

namespace PipesAndPowerExpanded.Tests;

/// <summary>
/// The MP-generator sub-machine's mechanical-power behavior - the mod's first torque source, a
/// constant-power model. Drives the engine→generator half of the MP chain: the generator turns the
/// engine's <see cref="BlockEntityEngine.MpPowerBudget"/> into network torque (more torque under
/// heavier load, tapering to zero as the line reaches the engine's own
/// <see cref="BlockEntityEngine.ShaftSpeed"/>), and cuts its power demand when the load overstresses
/// the engine.
/// </summary>
public class MpGeneratorBehaviorTests {
  private static (
    Scene scene,
    MpGeneratorPlant plant,
    BEBehaviorEngineMPGenerator mp
  ) Rig(float availablePower) {
    var scene = new Scene().Network("pipe", s => new PipeNetwork(s));
    var plant = new MpGeneratorPlant(scene, new BlockPos(0, 8, 0));
    scene.Build();
    // Set the engine's available power directly (deterministic) instead of driving steam.
    ReflectionHelpers.SetField(
      plant.Engine,
      "<AvailablePower>k__BackingField",
      availablePower
    );
    var mp = new BEBehaviorEngineMPGenerator(plant.Generator);
    return (scene, plant, mp);
  }

  #region Torque curve

  [Fact]
  public void Resistance_is_the_constant_rotor_drag() {
    var (_, _, mp) = Rig(0.5f);
    Assert.Equal(0.0005f, mp.GetResistance(), 5);
  }

  [Fact]
  public void Below_the_taper_the_torque_is_the_budget_over_the_speed() {
    var (_, plant, mp) = Rig(0.5f);
    float budget = plant.Engine.MpPowerBudget;
    Assert.True(budget > 0f, "the engine should have a power budget");

    // Well under the taper, where the constant-power law is undiluted.
    float speed = 0.5f * plant.Engine.ShaftSpeed;
    float torque = mp.GetTorque(0, speed, out float resistance);

    Assert.Equal(budget / speed, torque, 3);
    Assert.Equal(0f, resistance, 5);
  }

  [Fact]
  public void It_is_a_constant_power_source_more_torque_at_lower_speed() {
    var (_, plant, mp) = Rig(0.5f);
    float budget = plant.Engine.MpPowerBudget;
    // Both below the taper, which starts at two thirds of the cap, and both above the divisor clamp
    // at a quarter of rated speed - between those the constant-power law is undiluted.
    float fast = 0.6f * plant.Engine.ShaftSpeed;
    float slow = 0.45f * plant.Engine.ShaftSpeed;

    float atFast = mp.GetTorque(0, fast, out _);
    float atSlow = mp.GetTorque(0, slow, out _);

    Assert.True(
      atSlow > atFast,
      "torque should rise as the shaft slows (constant power)"
    );
    // power = torque × speed is held at the budget across the band
    Assert.Equal(budget, atFast * fast, 3);
    Assert.Equal(budget, atSlow * slow, 3);
  }

  [Fact]
  public void Torque_tapers_to_zero_at_the_engines_own_shaft_speed() {
    // The cap is the engine's rotation rate, not a constant: a shaft coupled straight to the engine
    // cannot turn faster than the engine turns it.
    var (_, plant, mp) = Rig(0.5f);
    float cap = plant.Engine.ShaftSpeed;

    Assert.True(mp.GetTorque(0, cap * 2f / 3f, out _) > 0f); // taper has not begun
    Assert.Equal(0f, mp.GetTorque(0, cap, out _), 4); // at the cap
    Assert.Equal(0f, mp.GetTorque(0, cap * 1.5f, out _), 4); // beyond it
  }

  [Fact]
  public void The_taper_spans_the_top_third_rather_than_cutting_hard() {
    // A hard cut sawtooths: the solver's speed step overshoots the crossing and the torque flips
    // between full and nothing either side of it. The band has to stay a third of the cap - narrowing
    // it brings the oscillation back.
    var (_, plant, mp) = Rig(0.5f);
    float cap = plant.Engine.ShaftSpeed;
    float taperStart = cap * 2f / 3f;
    float midway = 0.5f * (taperStart + cap);

    float undiluted = plant.Engine.MpPowerBudget / midway;

    Assert.Equal(0.5f * undiluted, mp.GetTorque(0, midway, out _), 3);
  }

  [Fact]
  public void The_speed_cap_follows_the_engines_power() {
    // A throttled or steam-starved engine genuinely turns its line slower, where the old fixed cap
    // let any engine drive a light load to the same top speed.
    var (_, weak, weakMp) = Rig(0.2f);
    var (_, strong, strongMp) = Rig(0.8f);

    Assert.True(strong.Engine.ShaftSpeed > weak.Engine.ShaftSpeed);

    float between = 0.5f * (weak.Engine.ShaftSpeed + strong.Engine.ShaftSpeed);
    Assert.Equal(0f, weakMp.GetTorque(0, between, out _), 5);
    Assert.True(strongMp.GetTorque(0, between, out _) > 0f);
  }

  [Fact]
  public void Chaining_engines_buys_load_capacity_not_shaft_speed() {
    // Players chained engines onto a single helve hammer and it hammered at frame speed. Each
    // generator stops pushing at ITS OWN engine's rate, and the solver only raises the speed while
    // some node still makes net torque, so the line settles at the fastest engine's rate however
    // many are on it. Below that speed the extra engines still add torque, which is load capacity -
    // what a second engine is actually for.
    var (_, plant, mp) = Rig(0.3f);
    float cap = plant.Engine.ShaftSpeed;

    // No number of identical generators can drive the line past the cap: each contributes zero.
    Assert.Equal(0f, mp.GetTorque(0, cap, out _), 5);
    Assert.Equal(0f, mp.GetTorque(0, cap + 0.05f, out _), 5);
    // Under it they do contribute, which is what stacks.
    Assert.True(mp.GetTorque(0, 0.5f * cap, out _) > 0f);
  }

  [Fact]
  public void A_chained_line_settles_no_faster_than_a_single_engine() {
    // The same rule at the level it actually bit, run through the vanilla solver's own speed
    // integration rather than inferred from the torque curve.
    float one = SettledSpeed(engines: 1, load: HelveHammerLoad);
    float eight = SettledSpeed(engines: 8, load: HelveHammerLoad);

    Assert.True(
      eight - one < 0.1f,
      $"one engine settles the line at {one} but eight take it to {eight}"
    );
  }

  [Fact]
  public void A_chained_line_still_turns_a_load_one_engine_cannot() {
    // The other half of the ruling: chaining has to buy something, and what it buys is the ability
    // to turn a load that stalls a single engine.
    float heavy = 8f * HelveHammerLoad;

    Assert.True(
      SettledSpeed(engines: 3, load: heavy)
        > SettledSpeed(engines: 1, load: heavy) + 0.05f,
      "a second and third engine should turn a heavy line faster than one alone"
    );
  }

  /// <summary>A vanilla active helve hammer's network resistance.</summary>
  private const float HelveHammerLoad = 0.125f;

  /// <summary>Engine output the solver simulation runs at; a Watt at full steam.</summary>
  private const float SimPower = 0.3f;

  /// <summary>
  /// Where the network settles with <paramref name="engines"/> identical generators against
  /// <paramref name="load"/>, stepping <c>MechanicalNetwork.updateNetwork</c>'s own integration:
  /// surplus is <c>|torque| - resistance</c>, and the speed moves by <c>min(0.05, surplus/n^0.25)</c>
  /// while that surplus is positive. Reproduced here because the vanilla solver needs a live world.
  /// </summary>
  private static float SettledSpeed(int engines, float load, int ticks = 600) {
    var (_, _, mp) = Rig(SimPower);
    int nodes = engines + 2;
    float step = 1f / MathF.Max(1f, MathF.Pow(nodes, 0.25f));
    float speed = 0f;

    for (int i = 0; i < ticks; i++) {
      float torque = 0f;
      for (int e = 0; e < engines; e++)
        torque += mp.GetTorque(0, speed, out _);
      float resistance = load + speed * speed / 1000f * nodes;
      float surplus = torque - resistance;
      speed =
        surplus > 0f
          ? speed + MathF.Min(0.05f, step * surplus)
          : MathF.Max(1e-6f, speed + step * MathF.Max(surplus, -speed));
    }
    return speed;
  }

  [Fact]
  public void Without_steam_the_generator_makes_no_torque() {
    var (_, _, mp) = Rig(0f); // no available power
    Assert.Equal(0f, mp.GetTorque(0, PpexValues.MpRatedSpeed, out _), 5);
    Assert.Equal(0f, mp.GetTorque(0, 0.1f, out _), 5);
  }

  #endregion

  #region Orientation

  [Fact]
  public void Orientation_seeds_the_axle_axis_from_the_side_variant() {
    var (_, _, mp) = Rig(0.5f); // the plant's generator block is "east"
    mp.SetOrientations();

    Assert.Equal(BlockFacing.WEST, mp.OutFacingForNetworkDiscovery);
    Assert.Equal(new[] { -1, 0, 0 }, mp.AxisSign); // single sign per axis (X)
  }

  // The beam has to follow the axle in every facing. The renderer turns the axle by
  // AngleRad × AxisSign about the world axes; the clip cranks its rod about the shape's -X, which
  // the placement turns onto -MpCycleCrankFace. The two angular vectors have to be one and the same.
  [Theory]
  [InlineData("north", "west", true)]
  [InlineData("east", "north", true)]
  [InlineData("south", "east", false)]
  [InlineData("west", "south", false)]
  public void The_beam_turns_with_the_generator_axle_in_every_facing(
    string side,
    string crankFace,
    bool reversed
  ) {
    var engineBlock = TestBlocks.Configure(
      new BlockEngineCornish(),
      $"ppex:enginecornish-{side}",
      40,
      ("side", side)
    );
    string genSide = BlockEngine.SubmachineSide(side);
    var genBlock = TestBlocks.Configure(
      new BlockEngineMPGenerator(),
      $"ppex:enginempgenerator-{genSide}",
      43,
      ("side", genSide)
    );
    var mp = new BEBehaviorEngineMPGenerator(
      new BlockEntityEngineMpGenerator { Block = genBlock }
    );
    mp.SetOrientations();

    BlockFacing crank = engineBlock.MpCycleCrankFace;
    Assert.Equal(crankFace, crank.Code);
    bool reverse = BlockEntityEngine.MpCycleRunsReversed(mp.AxisSign, crank);
    Assert.Equal(reversed, reverse);

    const float angle = 1f;
    float clip = reverse ? -angle : angle;
    Vec3i n = crank.Normali;
    Assert.Equal(
      new[] { mp.AxisSign[0] * angle, 0f, mp.AxisSign[2] * angle },
      new[] { -n.X * clip, 0f, -n.Z * clip }
    );
  }

  // Which way the axle turns is the network's to say, not the generator's: vanilla mirrors a node's
  // angle by its propagationDir (IsRotationReversed), and that is the generator's own seed only
  // when it starts the line. Joined to a standing line, discovery reaches it from the far end and
  // its axle turns the other way. Each facing is run both ways round, and again with the network
  // itself running backwards; the beam has to turn with the axle in all of them.
  [Theory]
  [InlineData("north", "west", -1)] // the generator's own seed
  [InlineData("north", "east", 1)] // discovered from the far end
  [InlineData("east", "south", 1)]
  [InlineData("east", "north", -1)]
  [InlineData("south", "west", -1)]
  [InlineData("south", "east", 1)]
  [InlineData("west", "south", 1)]
  [InlineData("west", "north", -1)]
  public void The_beam_follows_the_axle_whichever_way_the_network_turns_it(
    string side,
    string propagation,
    int axleSpin
  ) {
    var engineBlock = TestBlocks.Configure(
      new BlockEngineCornish(),
      $"ppex:enginecornish-{side}",
      40,
      ("side", side)
    );
    string genSide = BlockEngine.SubmachineSide(side);
    var genBlock = TestBlocks.Configure(
      new BlockEngineMPGenerator(),
      $"ppex:enginempgenerator-{genSide}",
      43,
      ("side", genSide)
    );
    var mp = new BEBehaviorEngineMPGenerator(
      new BlockEntityEngineMpGenerator { Block = genBlock }
    );
    mp.SetOrientations();
    BlockFacing propagationDir = BlockFacing.FromCode(propagation);
    Assert.Equal(mp.OutFacingForNetworkDiscovery.Axis, propagationDir.Axis);
    ReflectionHelpers.SetField(mp, "propagationDir", propagationDir);

    BlockFacing crank = engineBlock.MpCycleCrankFace;
    bool reverse = BlockEntityEngine.MpCycleRunsReversed(mp.AxisSign, crank);
    // The world axis the line lies on, +X or +Z; the spins below are measured about it.
    int axis = propagationDir.Axis == EnumAxis.X ? 0 : 2;
    int crankSign = axis == 0 ? crank.Normali.X : crank.Normali.Z;

    foreach (
      EnumRotDirection turnDir in new[] {
        EnumRotDirection.Clockwise,
        EnumRotDirection.Counterclockwise,
      }
    ) {
      var network = MechPower.Network(speed: 1f);
      network.TurnDir = turnDir;
      ReflectionHelpers.SetField(mp, "network", network);
      int expected =
        turnDir == EnumRotDirection.Clockwise ? axleSpin : -axleSpin;

      float lastAngle = mp.AngleRad;
      float lastClip = ClipAngle(lastAngle, reverse);
      // Enough steps to carry the angle past its wrap at 2π at least once.
      for (int i = 0; i < 40; i++) {
        // The step MechanicalNetwork.ClientTick takes: forwards clockwise, backwards otherwise.
        network.UpdateAngle(turnDir == EnumRotDirection.Clockwise ? 2f : -2f);
        float angle = mp.AngleRad;
        float clip = ClipAngle(angle, reverse);

        // The renderer turns the axle by AngleRad × AxisSign; the clip cranks about the shape's
        // -X, which the placement turns onto -crank.
        float axleStep =
          mp.AxisSign[axis] * GameMath.AngleRadDistance(lastAngle, angle);
        float beamStep = -crankSign * GameMath.AngleRadDistance(lastClip, clip);

        Assert.Equal(expected, Math.Sign(axleStep));
        Assert.Equal(axleStep, beamStep, 3);
        lastAngle = angle;
        lastClip = clip;
      }
    }
  }

  // The angle the cyclemp clip stands at once MPAnim.LockFrameToAngle has pinned its frame.
  private static float ClipAngle(float angleRad, bool reverse) {
    const int frames = 120;
    return MPAnim.FrameFromAngle(reverse ? -angleRad : angleRad, frames)
      / frames
      * GameMath.TWOPI;
  }

  // MpCycleRunsReversed leans on the clip cranking its rod about the shape's -X. Pins that against
  // the shipped shapes: the rod's lower end, where it meets the crank pin, has to go round -X.
  [Theory]
  [InlineData("watt")]
  [InlineData("cornish")]
  public void The_cyclemp_clip_cranks_the_rod_about_the_shapes_minus_X(
    string engine
  ) {
    string path = Path.Combine(
      SaveGoldens.RepoRoot(),
      "ppex",
      "assets",
      "ppex",
      "shapes",
      "engine",
      engine + ".json"
    );
    using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
    JsonElement root = doc.RootElement;

    JsonElement rod = FindElement(root.GetProperty("elements"), "Rod")!.Value;
    double pivotY = rod.GetProperty("rotationOrigin")[1].GetDouble();
    double pivotZ = rod.GetProperty("rotationOrigin")[2].GetDouble();
    double length = pivotY - LowestY(rod, 0);
    Assert.True(length > 0, "the rod should hang below its pivot");

    JsonElement clip = root
      .GetProperty("animations")
      .EnumerateArray()
      .Single(a => a.GetProperty("code").GetString() == "cyclemp");
    var pins = new List<(double Y, double Z)>();
    foreach (JsonElement kf in clip.GetProperty("keyframes").EnumerateArray()) {
      if (!kf.GetProperty("elements").TryGetProperty("Rod", out JsonElement r))
        continue;
      double rx =
        (r.TryGetProperty("rotationX", out JsonElement x) ? x.GetDouble() : 0)
        * Math.PI
        / 180;
      double dy = r.TryGetProperty("offsetY", out JsonElement y)
        ? y.GetDouble()
        : 0;
      // The game's RotateX takes +Y toward +Z for a positive angle; the rod hangs along -Y.
      pins.Add((pivotY + dy - length * Math.Cos(rx), pivotZ - length * Math.Sin(rx)));
    }
    Assert.True(pins.Count >= 3, "the clip should key the rod round its cycle");

    // Signed area of the pin's path in the YZ plane: positive turns about +X, negative about -X.
    double cy = pins.Average(p => p.Y);
    double cz = pins.Average(p => p.Z);
    double turn = 0;
    for (int i = 0; i < pins.Count; i++) {
      var (y1, z1) = pins[i];
      var (y2, z2) = pins[(i + 1) % pins.Count];
      turn += (y1 - cy) * (z2 - cz) - (z1 - cz) * (y2 - cy);
    }
    Assert.True(turn < 0, $"{engine}'s cyclemp should crank about -X (got {turn:0.0})");
  }

  private static JsonElement? FindElement(JsonElement elements, string name) {
    foreach (JsonElement e in elements.EnumerateArray()) {
      if (e.GetProperty("name").GetString() == name)
        return e;
      if (
        e.TryGetProperty("children", out JsonElement c)
        && FindElement(c, name) is { } hit
      )
        return hit;
    }
    return null;
  }

  // The lowest point of an element's subtree; children's coordinates are relative to their parent's from.
  private static double LowestY(JsonElement e, double parentY) {
    double y = parentY + e.GetProperty("from")[1].GetDouble();
    double low = y;
    if (e.TryGetProperty("children", out JsonElement c))
      foreach (JsonElement child in c.EnumerateArray())
        low = Math.Min(low, LowestY(child, y));
    return low;
  }

  #endregion

  #region Load management (PowerDemand)

  [Fact]
  public void Power_demand_is_full_under_a_normal_load() {
    var (_, plant, mp) = Rig(0.5f);
    MechPower.Attach(
      plant.Generator,
      mp,
      MechPower.Network(speed: 1f, resistance: 0f)
    );
    ReflectionHelpers.SetField(plant.Generator, "_mp", mp);

    Assert.Equal(1f, plant.Generator.PowerDemand, 3);
  }

  [Fact]
  public void An_overloaded_engine_labours_instead_of_cutting_out() {
    // It used to drop demand to zero, which stopped the whole line dead and left a waterwheel -
    // which merely bogs down and keeps grinding - looking like the stronger machine. The engine now
    // keeps its full demand and the shaft simply crawls.
    var (_, plant, mp) = Rig(0.5f);
    float overload = 3f * plant.Engine.MpRatedLoad;
    MechPower.Attach(
      plant.Generator,
      mp,
      MechPower.Network(speed: 1f, resistance: overload)
    );
    ReflectionHelpers.SetField(plant.Generator, "_mp", mp);

    Assert.Equal(1f, plant.Generator.PowerDemand, 3);
  }

  [Fact]
  public void A_heavier_load_turns_the_shaft_slower_rather_than_stopping_it() {
    // The bog-down, as a curve: every load below the generator's own torque ceiling still turns.
    var (_, plant, _) = Rig(0.5f);
    float rated = plant.Engine.MpRatedLoad;

    float light = SettledSpeed(engines: 1, load: rated);
    float heavy = SettledSpeed(engines: 1, load: 3f * rated);

    Assert.True(light > 0f);
    Assert.True(heavy > 0f, "an overloaded shaft should crawl, not stop");
    Assert.True(heavy < light, "a heavier load should turn the shaft slower");
  }

  [Fact]
  public void The_shaft_stands_only_past_the_generators_own_torque_ceiling() {
    // The one genuine stall left: GetTorque clamps its divisor at a quarter of rated speed, so the
    // most torque the generator can raise is four times the budget. Past that nothing turns - a real
    // limit rather than an arbitrary multiple of a nominal rating.
    var (_, plant, _) = Rig(SimPower);
    float ceiling = 4f * plant.Engine.MpPowerBudget;

    Assert.True(SettledSpeed(engines: 1, load: 0.8f * ceiling) > 0.01f);
    Assert.True(SettledSpeed(engines: 1, load: 1.5f * ceiling) < 0.01f);
  }

  #endregion
}
