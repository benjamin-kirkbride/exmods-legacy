using Legacy.Tests;
using Xunit;

namespace Integration.Tests;

/// <summary>The published ppex and smex releases reach exlib's release-history registry through
/// this assembly's <c>ModuleInit</c>.</summary>
public class ReleasedHistoryTests {
  [Fact]
  public void The_published_ppex_and_smex_releases_are_registered() =>
    LegacyReleasedHistory.AssertRegistered();
}
