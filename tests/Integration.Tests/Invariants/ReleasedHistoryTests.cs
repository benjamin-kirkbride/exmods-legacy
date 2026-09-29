using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

  // Fails when every release is registered whole: a code then appears once per release.
  [Fact]
  public void Each_released_code_is_registered_once() {
    foreach (
      var (mod, codes, own, classes, items) in new[]
      {
        ("ppex", 292, 292, 15, 4),
        ("smex", 1154, 1052, 33, 5),
      }
    ) {
      ReleasedModHistory history = ReleasedHistory.For(mod)!;
      string[] shipped = [.. history.Shipped.SelectMany(s => s.Codes)];
      Assert.Equal(shipped.Length, shipped.Distinct().Count());
      Assert.Equal(codes, shipped.Length);
      Assert.Equal(
        own,
        history.Shipped.Where(s => s.Domain == mod).Sum(s => s.Codes.Length)
      );
      Assert.Equal(classes, history.EntityClasses.Count);
      Assert.Equal(
        history.EntityClasses.Count,
        history.EntityClasses.Select(e => e.Class).Distinct().Count()
      );
      Assert.Equal(items, LegacyReleasedHistory.Items(mod).Count);
      Assert.Equal(
        items,
        LegacyReleasedHistory.Items(mod).Distinct().Count()
      );
    }
  }

  // Fails when an item is registered under a later release than the first that shipped it.
  [Fact]
  public void The_renamed_smex_item_is_registered_under_its_first_release() {
    Assert.Equal("0.8.0", LegacyReleasedHistory.ItemFirstShipped("smex")["smex:blastmix"]);
    Assert.DoesNotContain(
      "smex:blastmix",
      ReleasedHistory.For("smex")!.Shipped.SelectMany(s => s.Codes)
    );
  }

  // Fails when the releases are taken in file-name order (0.10.0 before 0.9.8), or the newest
  // by file name is registered as the version.
  [Fact]
  public void Register_reads_the_releases_oldest_first_by_version() {
    string folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    Directory.CreateDirectory(folder);
    try {
      foreach (var (version, codes) in new[] { ("0.9.8", "\"smex:a-old\", \"smex:a-both\""), ("0.10.0", "\"smex:a-both\", \"smex:a-new\"") })
        File.WriteAllText(
          Path.Combine(folder, $"zzex-{version}.json"),
          $$"""
          {
            "mod": "zzex",
            "version": "{{version}}",
            "shipped": [
              { "domain": "zzex", "assetPath": "a", "baseCode": "smex:a", "codes": [{{codes}}] }
            ],
            "entityClasses": [],
            "items": [
              { "domain": "zzex", "assetPath": "b", "baseCode": "smex:b", "codes": ["smex:b-{{version}}"] }
            ]
          }
          """
        );
      try {
        LegacyReleasedHistory.Register(folder);
        ReleasedModHistory history = ReleasedHistory.For("zzex")!;
        Assert.Equal("0.10.0", history.Versions["zzex"]);
        Assert.Equal(
          ["smex:a-old", "smex:a-both", "smex:a-new"],
          history.Shipped.SelectMany(s => s.Codes)
        );
        Assert.Equal(
          ["smex:a-old", "smex:a-both"],
          ReleasedHistory.Releases("zzex").Single(r => r.Version == "0.9.8").Added.SelectMany(s => s.Codes)
        );
        Assert.Equal(
          ["smex:a-new"],
          ReleasedHistory.Releases("zzex").Single(r => r.Version == "0.10.0").Added.SelectMany(s => s.Codes)
        );
        Assert.Equal(["smex:b-0.9.8", "smex:b-0.10.0"], LegacyReleasedHistory.Items("zzex"));
      } finally {
        typeof(ReleasedHistory)
          .GetMethod("Forget", BindingFlags.NonPublic | BindingFlags.Static)!
          .Invoke(null, ["zzex"]);
      }
    } finally {
      Directory.Delete(folder, true);
    }
  }
}
