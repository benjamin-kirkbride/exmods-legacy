using System;
using ExpandedLib.Industry.Pipes;
using Xunit;

namespace Integration.Tests.Saves;

/// <summary>
/// The class names ppex's own pipe block entities had, still named by the ppex blocktypes'
/// <c>entityClass</c>, load Industry's pipe entities, which the game keeps saving under Industry's
/// primary keys. Red with an alias call removed from
/// <c>PipesAndPowerExpandedModSystem.AliasPipeEntities</c>, and with an alias registered through a
/// plain <c>RegisterBlockEntityClass</c>.
/// </summary>
public class PipeAliasTests {
  [Theory]
  [InlineData("ppex.BlockEntityPipe", typeof(BlockEntityPipe))]
  [InlineData("ppex.Pipe", typeof(BlockEntityPipe))]
  [InlineData("ppex.BlockEntityPipePassthrough", typeof(BlockEntityPipePassthrough))]
  [InlineData("ppex.PipePassthrough", typeof(BlockEntityPipePassthrough))]
  public void A_ppex_pipe_class_name_loads_Industrys_entity_saved_under_its_primary_key(
    string name,
    Type type
  ) {
    Assert.Equal(type, SaveRegistry.Instance.CreateBlockEntity(name).GetType());
    Assert.Equal("exlib." + type.Name, SaveRegistry.Instance.SavedKey(type));
  }
}
