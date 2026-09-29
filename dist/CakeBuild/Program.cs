using System;
using System.Collections.Generic;
using System.IO;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace CakeBuild;

public static class Program {
  public static int Main(string[] args) {
    return new CakeHost().UseContext<BuildContext>().Run(args);
  }
}

/// <summary>One buildable mod project in the repository. Folder is the csproj's own name (e.g.
/// "PipesAndPowerExpanded"), Dir is the folder it lives in (e.g. "ppex").</summary>
public record ModProject(string Folder, string Dir, string ModId, string Version);

/// <summary>A supported game version to publish for: its TFM, the game version stamped into the
/// packaged modinfo, and whether it's the current (non-legacy) target. Keep in sync with the version
/// manifest in Directory.Build.props (Cake can't read MSBuild, so this is the one duplication -
/// same as the CI workflows).</summary>
public record GameTarget(string Tfm, string GameVersion, bool IsCurrent);

public class BuildContext : FrostingContext {
  // Build order matters: ppex first (referenced by smex), then smex. exlib is not packaged here:
  // both mods reference it with Private=false, and players install its own release.
  public static readonly (string Folder, string Dir)[] ProjectFolders =
  [
    ("PipesAndPowerExpanded", "ppex"),
    ("SteelmakingExpanded", "smex"),
  ];

  // Every supported game version. The legacy ones (IsCurrent=false) build with -p:Legacy=true and
  // land in a per-TFM output path; their packaged modinfo gets its game dependency rewritten.
  public static readonly GameTarget[] GameTargets =
  [
    new GameTarget("net10.0", "1.22.0", true),
    new GameTarget("net8.0", "1.21.0", false),
    new GameTarget("net7.0", "1.20.0", false),
  ];

  // The game dependency the source modinfo.json files declare (= the current version's floor).
  public const string SourceGameVersion = "1.22.0";

  public string BuildConfiguration { get; }
  public bool SkipJsonValidation { get; }
  public List<ModProject> Projects { get; } = [];

  public BuildContext(ICakeContext context)
    : base(context) {
    BuildConfiguration = context.Argument("configuration", "Release");
    SkipJsonValidation = context.Argument("skipJsonValidation", false);

    foreach (var (folder, dir) in ProjectFolders) {
      var modInfo = context.DeserializeJsonFromFile<ModInfo>(
        $"../../{dir}/modinfo.json"
      );
      Projects.Add(new ModProject(folder, dir, modInfo.ModID, modInfo.Version));
    }
  }

  /// <summary>The publish output for a project+target. The current version uses the flat
  /// Mods/mod path; legacy targets append their TFM (see the mod csproj OutputPath).</summary>
  public string PublishDir(ModProject project, GameTarget target) =>
    target.IsCurrent
      ? $"../../{project.Dir}/bin/{BuildConfiguration}/Mods/mod/publish"
      : $"../../{project.Dir}/bin/{BuildConfiguration}/{target.Tfm}/Mods/mod/publish";
}

[TaskName("ValidateJson")]
public sealed class ValidateJsonTask : FrostingTask<BuildContext> {
  public override void Run(BuildContext context) {
    if (context.SkipJsonValidation)
      return;

    foreach (var project in context.Projects) {
      var jsonFiles = context.GetFiles(
        $"../../{project.Dir}/assets/**/*.json"
      );
      foreach (var file in jsonFiles) {
        try {
          JToken.Parse(File.ReadAllText(file.FullPath));
        } catch (JsonException ex) {
          throw new Exception(
            $"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}",
            ex
          );
        }
      }
    }
  }
}

[TaskName("Build")]
[IsDependentOn(typeof(ValidateJsonTask))]
public sealed class BuildTask : FrostingTask<BuildContext> {
  public override void Run(BuildContext context) {
    foreach (var project in context.Projects) {
      string csproj = $"../../{project.Dir}/{project.Folder}.csproj";
      // Wipe the whole bin so stale per-version outputs can't leak into a package.
      string binDir =
        $"../../{project.Dir}/bin/{context.BuildConfiguration}";
      context.EnsureDirectoryExists(binDir);
      context.CleanDirectory(binDir);

      foreach (var target in BuildContext.GameTargets) {
        context.DotNetPublish(
          csproj,
          new DotNetPublishSettings {
            Configuration = context.BuildConfiguration,
            Framework = target.Tfm,
            // -p:Legacy=true makes the mod multi-target so the legacy TFMs exist; harmless for the
            // current one (it stays the flat, non-legacy output).
            MSBuildSettings = new DotNetMSBuildSettings().WithProperty(
              "Legacy",
              "true"
            ),
          }
        );
      }
    }
  }
}

[TaskName("Package")]
[IsDependentOn(typeof(BuildTask))]
public sealed class PackageTask : FrostingTask<BuildContext> {
  public override void Run(BuildContext context) {
    context.EnsureDirectoryExists("../Releases");
    context.CleanDirectory("../Releases");

    // One archive per (game version, mod), grouped into a per-version folder. Legacy
    // targets get a trailing game-version suffix so the files are distinguishable; the
    // current version stays unsuffixed:
    //   Releases/<gameVersion>/<modid>_<modVersion>.zip            (current)
    //   Releases/<gameVersion>/<modid>_<modVersion>_<gameVersion>.zip (legacy)
    foreach (var target in BuildContext.GameTargets) {
      foreach (var project in context.Projects) {
        string stageDir = $"../Releases/{target.GameVersion}/{project.ModId}";
        context.EnsureDirectoryExists(stageDir);

        context.CopyFiles($"{context.PublishDir(project, target)}/*", stageDir);
        // Assets come from the per-target publish output, not raw source: the csproj applies
        // per-game-version `Content Remove` filtering (e.g. legacy-only patches whose crushed codes
        // don't resolve on newer versions), and only the publish output reflects it. Copying from
        // raw source would ship both versions' files into every package and crash clients on
        // world-load when the wrong patch references a non-existent stack.
        if (context.DirectoryExists($"{context.PublishDir(project, target)}/assets"))
          context.CopyDirectory(
            $"{context.PublishDir(project, target)}/assets",
            $"{stageDir}/assets"
          );
        if (context.FileExists($"../../{project.Dir}/modicon.png"))
          context.CopyFile(
            $"../../{project.Dir}/modicon.png",
            $"{stageDir}/modicon.png"
          );

        // Authoritative modinfo: the source declares the current game version, so point the game
        // dependency at this target's version (no-op for the current one).
        string modinfoSource = File.ReadAllText(
          $"../../{project.Dir}/modinfo.json"
        );
        string modinfo = modinfoSource.Replace(
          $"\"game\": \"{BuildContext.SourceGameVersion}\"",
          $"\"game\": \"{target.GameVersion}\""
        );
        // The rewrite is a literal match, so a source modinfo that spells its game dependency any
        // other way ("1.21" for "1.21.0", say) silently no-ops and ships a legacy package carrying
        // the current game dependency, which the target game then rejects. Fail the build instead.
        if (!target.IsCurrent && modinfo == modinfoSource)
          throw new Exception(
            $"{project.Dir}/modinfo.json: expected to rewrite \"game\": \"{BuildContext.SourceGameVersion}\" "
              + $"to \"{target.GameVersion}\", but the text was unchanged. The legacy package would ship "
              + "the current game dependency and be rejected by the game."
          );
        File.WriteAllText($"{stageDir}/modinfo.json", modinfo);

        string versionSuffix = target.IsCurrent ? "" : $"_{target.GameVersion}";
        string zip =
          $"../Releases/{target.GameVersion}/{project.ModId}_{project.Version}{versionSuffix}.zip";
        context.Zip(stageDir, zip);
        context.Information(
          "{0}: {1} ({2} bytes)",
          target.GameVersion,
          Path.GetFileName(zip),
          new FileInfo(zip).Length
        );
      }
    }
  }
}

[TaskName("Default")]
[IsDependentOn(typeof(PackageTask))]
public class DefaultTask : FrostingTask { }
