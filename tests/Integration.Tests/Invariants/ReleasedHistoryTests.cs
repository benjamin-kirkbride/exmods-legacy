using System;
using System.IO;
using System.Linq;
using ExpandedLib.Testing;
using Legacy.Tests;
using Xunit;

namespace Integration.Tests;

/// <summary>The published ppex and smex releases reach exlib's release-history registry through
/// this assembly's <c>ModuleInit</c>.</summary>
public class ReleasedHistoryTests {
  private static readonly string[] Published =
  [
    .. new[] { "0.5.0", "0.6.0", "0.6.1", "0.6.2", "0.6.3", "0.6.4", "0.6.5", "0.6.7", "0.6.8" }
      .Select(v => $"ppex-{v}"),
    .. new[]
    {
      "0.8.0", "0.8.1", "0.8.2", "0.8.3", "0.8.4", "0.8.5", "0.8.6", "0.8.7",
      "0.9.0", "0.9.1", "0.9.2", "0.9.3", "0.9.4", "0.9.6", "0.9.7", "0.9.8",
    }.Select(v => $"smex-{v}"),
  ];

  [Fact]
  public void The_published_ppex_and_smex_releases_are_registered() =>
    LegacyReleasedHistory.AssertRegistered();

  // Fails when a golden is missing, or one is added without the list here.
  [Fact]
  public void Every_published_release_has_a_golden() =>
    Assert.Equal(
      Published.Order(StringComparer.Ordinal),
      Directory
        .EnumerateFiles(LegacyReleasedHistory.Folder, "*.json")
        .Select(f => Path.GetFileNameWithoutExtension(f))
        .Order(StringComparer.Ordinal)
    );

  // Fails when the newest golden is picked by file name: 0.9.8 sorts after 0.10.0.
  [Fact]
  public void Register_takes_the_newest_release_of_a_mod_by_version() {
    string folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    Directory.CreateDirectory(folder);
    try {
      foreach (var (version, code) in new[] { ("0.9.8", "old"), ("0.10.0", "new") })
        File.WriteAllText(
          Path.Combine(folder, $"smex-{version}.json"),
          $$"""
          {
            "mod": "smex",
            "version": "{{version}}",
            "shipped": [
              { "domain": "smex", "assetPath": "a", "baseCode": "smex:a", "codes": ["smex:a-{{code}}"] }
            ],
            "entityClasses": [],
            "items": [
              { "domain": "smex", "assetPath": "b", "baseCode": "smex:b", "codes": ["smex:b-{{code}}"] }
            ]
          }
          """
        );
      try {
        LegacyReleasedHistory.Register(folder);
        ReleasedModHistory history = ReleasedHistory.For("smex")!;
        Assert.Equal("0.10.0", history.Versions["smex"]);
        Assert.Equal(["smex:a-new"], history.Shipped.SelectMany(s => s.Codes));
        Assert.Equal(["smex:b-new"], LegacyReleasedHistory.Items("smex"));
      } finally {
        LegacyReleasedHistory.Register();
      }
    } finally {
      Directory.Delete(folder, true);
    }
  }
}
