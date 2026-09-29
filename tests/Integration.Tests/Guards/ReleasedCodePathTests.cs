using System.Collections.Generic;
using Xunit;

namespace Integration.Tests.Guards;

public class ReleasedCodePathTests {
  private static readonly IReadOnlySet<string> Live = new HashSet<string> { "m:live" };
  private static readonly IReadOnlySet<string> None = new HashSet<string>();

  private static string? DeadEnd(
    string code,
    Dictionary<string, string> declared,
    IReadOnlySet<string>? removed = null
  ) => ReleasedCodePaths.DeadEnd(code, Live.Contains, declared, removed ?? None);

  // Fails when a code that loads is looked up in the chain instead.
  [Fact]
  public void A_live_code_passes() =>
    Assert.Null(DeadEnd("m:live", new() { ["m:live"] = "m:gone" }));

  // Fails when the chain is not followed: the renamed code is judged as itself.
  [Fact]
  public void A_renamed_code_whose_chain_ends_at_a_live_code_passes() =>
    Assert.Null(DeadEnd("m:a", new() { ["m:a"] = "m:b", ["m:b"] = "m:live" }));

  // Fails when a chain end that loads nothing passes, or when the finding names another code.
  [Fact]
  public void A_chain_that_ends_at_a_code_with_no_block_fails_and_names_that_end() {
    Assert.Equal("m:c", DeadEnd("m:a", new() { ["m:a"] = "m:b", ["m:b"] = "m:c" }));
    Assert.Equal("m:z", DeadEnd("m:z", new()));
  }

  // Fails when removals are ignored.
  [Fact]
  public void A_chain_that_ends_at_a_declared_removal_passes() =>
    Assert.Null(
      DeadEnd("m:a", new() { ["m:a"] = "m:b" }, new HashSet<string> { "m:b" })
    );

  // Fails when the last declaration of a code is followed instead of the first.
  [Fact]
  public void When_one_code_is_declared_twice_the_first_declaration_is_followed() =>
    Assert.Null(
      DeadEnd(
        "m:a",
        ReleasedCodePaths.Declared([("m:a", "m:live"), ("m:a", "m:gone")])
      )
    );
}
