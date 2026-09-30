# Legacy: ppex 0.7 / smex 0.10 on exlib 0.8

This is the maintenance repository of the published Fallenstar's Expanded line, Pipes and Power
Expanded and Steelmaking Expanded, ported onto Expanded Library 0.8. It was split out of the exmods
repository, where it lived under `legacy/`, with its history kept, and it builds, tests and
packages on its own: nothing here reads the exmods repository or the family's (iiex, siex) central
package management and version manifest.

| Path             | modid   | Version | What it is                                                      |
| ---------------- | ------- | ------- | ---------------------------------------------------------------- |
| `ppex/`          | `ppex`  | 0.7.1   | Pipe networks (gas + water), boilers, steam engines and their sub-machines. |
| `smex/`          | `smex`  | 0.10.1  | Blast furnace, cowper stoves, molten-metal canals and casting, Bessemer converter. |
| `generators/`    | -       | -       | Roslyn source generators the mods use at compile time (config accessors, block-attribute bakers). |
| `tests/`         | -       | -       | Headless xUnit test projects: per-mod unit tests + cross-mod integration. |
| `dist/CakeBuild/`| -       | -       | Cake build project that publishes per-game-version release zips into `dist/Releases/`. |
| `docs/`          | -       | -       | Diagrams, screenshots, moddb listing + handbook drafts. |
| `scripts/`       | -       | -       | Game/.NET provisioning, mod staging, test runners. |
| `wiki/`          | -       | -       | The current-mods wiki pages - home, FAQ and instructions per mod (read by the wiki site). |
| `Legacy.sln`     | -       | -       | Solution tying the projects together. |

Both mods need Expanded Library 0.8.4 or later, and smex needs ppex 0.7.1. `smex` references
`exlib` and `ppex` with `Private=false`, so players install all three mods separately. The mods
build against the exlib checkout beside this repository when the workspace's
`Directory.Build.props` is above it, else against the ExpandedLib packages.

## What never happens here

No new features, no refactors, no format pass - this tree predates the family's CSharpier gate
and is not formatted by it. exlib 0.8.4 refuses ppex below 0.7.1 and smex below 0.10.1: it logs one
Error naming them and repeats it to every joining player.

**iiex (Iron Industry Expanded) and siex (Steel Industry Expanded) are the successors** - new
mods rather than updates to ppex/smex. Worlds do not carry over between the two lines. When iiex
or siex is enabled in a world, ppex and smex close there: what is built keeps working, and
nothing new can be built from them (`ppex/ClosedLine/`).

## Building and testing

Prerequisites: Git, a .NET 10 SDK (`global.json` pins it), and PowerShell 7+ or bash. Game
binaries and .NET runtimes are found in the nearest `.game/<version>` and `.dotnet/` from the
repository upward. When none exists, the build and `scripts/run-tests.*` fetch them on demand into
the workspace root when an `exmod.workspace.json` is above the repository, else into the repository
root, so checkouts in one workspace share one download. The `VINTAGE_STORY`, `VINTAGE_STORY_121` and
`VINTAGE_STORY_120` environment variables point the build at any other install instead.

```sh
dotnet build Legacy.sln -clp:ErrorsOnly
bash scripts/run-tests.sh latest    # or 1.21 / 1.20 / all
```

`pwsh scripts/run-tests.ps1` is the Windows equivalent. Both build the test projects for the
requested game version (auto-provisioning binaries and, if missing, the matching .NET runtime)
and run the projects in parallel; the build phase itself is serial, since the test projects share
the mod projects and a concurrent build races on the same intermediate DLLs.

A project whose build fails is reported `FAIL ... build failed` and its tests do not run, and a
project whose run prints no test summary fails. A green run records each project's test count per
game version in `.exmod/census/<branch>.json`; a later run that finds fewer tests fails, naming the
version, the project and both counts, until `--accept-drop` (`-AcceptDrop` in PowerShell) records
the lower count.

## Packaging a release

`dotnet run --project dist/CakeBuild` publishes every mod for every supported game version
and zips the result into `dist/Releases/<gameVersion>/<modid>_<version>[_<gameVersion>].zip`
(current version unsuffixed, legacy game versions carry a trailing suffix). It needs the exlib
checkout beside this repository: the ExpandedLib packages ship net10.0 only, so the 1.21 and 1.20
builds come from source. exlib is not packaged here; players install its own release.
