using SteelmakingExpanded;
using Xunit;

namespace SteelmakingExpanded.Tests;

public class ExlibCompatibilityTests {
  [Theory]
  [InlineData("0.7.2", false)]
  [InlineData("0.7.99", false)]
  [InlineData("0.8.0", true)]
  [InlineData("0.8.0-preview.3", true)]
  [InlineData("1.0.0", true)]
  public void Exlib_0_8_and_later_are_too_new(string version, bool tooNew) =>
    Assert.Equal(tooNew, ExlibCompatibility.IsTooNew(version));
}
