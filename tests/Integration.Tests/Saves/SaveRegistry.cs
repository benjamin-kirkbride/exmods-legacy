using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExpandedLib;
using ExpandedLib.Industry;
using ExpandedLib.Registries;
using ExpandedLib.Testing;
using NSubstitute;
using PipesAndPowerExpanded;
using SteelmakingExpanded;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Integration.Tests.Saves;

/// <summary>
/// The game's own class registry (<c>Vintagestory.Common.ClassRegistry</c> from VintagestoryLib),
/// filled by running exlib's (its Industry module's assembly, then its own), ppex's and smex's
/// <see cref="EntityRegistry.RegisterAll"/> in load order against a recording API, plus the vanilla
/// classes the ppex and smex blocktypes name (<c>Animatable</c>, <c>Door</c>,
/// <c>TemperatureSensitive</c> and the <c>ToolMold</c> entity).
/// A block entity is saved under the last name registered for its type, and loaded by
/// <see cref="CreateBlockEntity"/> on that name, both through the real registry.
/// </summary>
internal sealed class SaveRegistry {
  /// <summary>One <c>RegisterBlockEntityClass</c> call, in the order the mods made it.</summary>
  public readonly record struct Registration(
    string ModId,
    string Name,
    Type Type
  );

  private static readonly Lazy<SaveRegistry> Shared = new(() => new());

  /// <summary>The registry filled once per test process.</summary>
  public static SaveRegistry Instance => Shared.Value;

  private static readonly Type RegistryType = Type.GetType(
    "Vintagestory.Common.ClassRegistry, VintagestoryLib",
    throwOnError: true
  )!;

  private static readonly Type RegistryApiType = Type.GetType(
    "Vintagestory.Common.ClassRegistryAPI, VintagestoryLib",
    throwOnError: true
  )!;

  private readonly object _registry = Activator.CreateInstance(RegistryType)!;
  private readonly List<Registration> _registrations = [];

  /// <summary>Every block-entity class registration exlib, ppex and smex make, in call order.</summary>
  public IReadOnlyList<Registration> Registrations => _registrations;

  private SaveRegistry() {
    Register("exlib", typeof(IndustryModule).Assembly);
    Register("exlib", typeof(ExpandedLibModSystem).Assembly);
    Register("ppex", typeof(PipesAndPowerExpandedModSystem).Assembly);
    Register("smex", typeof(SteelmakingExpandedModSystem).Assembly);

    Call("RegisterBlockEntityBehaviorClass", "Animatable", typeof(BEBehaviorAnimatable));
    Call("RegisterBlockEntityBehaviorClass", "Door", typeof(BEBehaviorDoor));
    Call(
      "RegisterBlockEntityBehaviorClass",
      "TemperatureSensitive",
      typeof(BEBehaviorTemperatureSensitive)
    );
    Call("RegisterBlockEntityType", "ToolMold", typeof(BlockEntityToolMold));
  }

  /// <summary>
  /// The name a chunk saves <paramref name="type"/> under: the registry's type-to-name map, which
  /// holds the last name registered for it. Null when the type is not registered.
  /// </summary>
  public string? SavedKey(Type type) =>
    ((IDictionary<Type, string>)Field("blockEntityTypeToClassnameMapping"))
      .TryGetValue(type, out string? key)
      ? key
      : null;

  /// <summary>Creates a block entity by its saved name, as chunk loading does.</summary>
  /// <exception cref="TargetInvocationException">The name is not registered.</exception>
  public BlockEntity CreateBlockEntity(string savedKey) =>
    (BlockEntity)Call("CreateBlockEntity", savedKey)!;

  /// <summary>
  /// Wires the game's <c>ClassRegistryAPI</c> over this registry into <paramref name="world"/>'s
  /// world and API, so <see cref="BlockEntity.CreateBehaviors"/> and hosted filler behaviours
  /// resolve their classes by name. Returns <paramref name="world"/>.
  /// </summary>
  public TestWorld Wire(TestWorld world) {
    var api = (IClassRegistryAPI)
      Activator.CreateInstance(RegistryApiType, world.World, _registry)!;
    world.World.ClassRegistry.Returns(api);
    world.Api.ClassRegistry.Returns(api);
    ((ICoreAPI)world.Api).ClassRegistry.Returns(api);
    return world;
  }

  private void Register(string modId, Assembly assembly) {
    var api = Substitute.For<ICoreAPI>();
    api.When(a =>
        a.RegisterBlockEntityClass(Arg.Any<string>(), Arg.Any<Type>())
      )
      .Do(ci => {
        string name = ci.ArgAt<string>(0);
        Type type = ci.ArgAt<Type>(1);
        _registrations.Add(new Registration(modId, name, type));
        Call("RegisterBlockEntityType", name, type);
      });
    api.When(a =>
        a.RegisterBlockEntityBehaviorClass(Arg.Any<string>(), Arg.Any<Type>())
      )
      .Do(ci =>
        Call(
          "RegisterBlockEntityBehaviorClass",
          ci.ArgAt<string>(0),
          ci.ArgAt<Type>(1)
        )
      );

    var mod = Substitute.For<Mod>();
    typeof(Mod).GetProperty("Info")!.SetValue(mod, new ModInfo { ModID = modId });
    EntityRegistry.RegisterAll(api, mod, assembly);
  }

  private object? Call(string method, params object[] args) =>
    RegistryType
      .GetMethods()
      .Single(m => m.Name == method && Accepts(m, args))
      .Invoke(_registry, args);

  private static bool Accepts(MethodInfo method, object[] args) {
    ParameterInfo[] parameters = method.GetParameters();
    return parameters.Length == args.Length
      && parameters
        .Zip(args)
        .All(p => p.First.ParameterType.IsInstanceOfType(p.Second));
  }

  private object Field(string name) =>
    RegistryType.GetField(name)!.GetValue(_registry)!;
}
