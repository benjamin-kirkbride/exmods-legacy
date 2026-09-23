using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Binds the published-facing docs to the three mods' own <c>modinfo.json</c>: 0.6.9/0.9.9 sat in
/// three of these pages until the R1 audit found them by hand, because no check read these pages
/// and nothing else read these version strings back.
/// </summary>
public class VersionPinTests {
  // Which mods' current version each published page is expected to name. ppex's and smex's own
  // wiki pages name their own version and the exlib floor, not the sibling mod's version.
  private static readonly (string Page, string[] ModDirs)[] Pages = [
    ("README.md", ["exlib", "ppex", "smex"]),
    (Path.Combine("wiki", "Home.md"), ["exlib", "ppex", "smex"]),
    (Path.Combine("wiki", "exlib", "Home.md"), ["exlib", "ppex", "smex"]),
    (Path.Combine("wiki", "ppex", "Home.md"), ["ppex", "exlib"]),
    (Path.Combine("wiki", "smex", "Home.md"), ["smex", "exlib"]),
    (Path.Combine("docs", "moddb-notes.md"), ["exlib", "ppex", "smex"]),
  ];

  private static string Version(string modDir) {
    string text = File.ReadAllText(
      Path.Combine(RepoRoot(), modDir, "modinfo.json")
    );
    Match m = Regex.Match(text, @"""version""\s*:\s*""([^""]+)""");
    Assert.True(m.Success, $"{modDir}/modinfo.json names no \"version\".");
    return m.Groups[1].Value;
  }

  public static TheoryData<string, string> EveryPageAndMod() {
    var data = new TheoryData<string, string>();
    foreach (var (page, modDirs) in Pages)
      foreach (string modDir in modDirs)
        data.Add(page, modDir);
    return data;
  }

  [Theory]
  [MemberData(nameof(EveryPageAndMod))]
  public void Published_page_names_the_mods_current_version(
    string page,
    string modDir
  ) {
    string version = Version(modDir);
    string text = File.ReadAllText(Path.Combine(RepoRoot(), page));
    Assert.True(
      text.Contains(version, StringComparison.Ordinal),
      $"{page} does not name {modDir}'s current version ({version}, from {modDir}/modinfo.json)."
    );
  }

  private static string RepoRoot() {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (
      dir != null
      && !File.Exists(Path.Combine(dir.FullName, "Legacy.sln"))
    )
      dir = dir.Parent;
    Assert.True(dir != null, "could not locate repo root (Legacy.sln)");
    return dir!.FullName;
  }
}
