# Legacy: exlib 0.7 / ppex 0.6 / smex 0.9

This is the maintenance tree of the published Fallenstar's Expanded line - the code players
actually run today, joined into this repository under `legacy/` by a subtree merge of the old
`0.9-support` monorepo. It builds and tests in isolation from the family (iiex, siex) above it:
its own `Directory.Build.props` and `Directory.Packages.props` are the nearest MSBuild finds for
every project below this folder, so the workspace's central package management and version
manifest never reach it.

| Path             | modid   | Version | What it is                                                      |
| ---------------- | ------- | ------- | ---------------------------------------------------------------- |
| `exlib/`         | `exlib` | 0.7.2   | Shared framework: block networks, multiblock structures, registries, save migrations. |
| `ppex/`          | `ppex`  | 0.6.8   | Pipe networks (gas + water), boilers, steam engines and their sub-machines. |
| `smex/`          | `smex`  | 0.9.8   | Blast furnace, cowper stoves, molten-metal canals and casting, Bessemer converter. |
| `generators/`    | -       | -       | Roslyn source generators the mods use at compile time (config accessors, block-attribute bakers). |
| `tests/`         | -       | -       | Headless xUnit test projects: per-mod unit tests + cross-mod integration. |
| `dist/CakeBuild/`| -       | -       | Cake build project that publishes per-game-version release zips into `dist/Releases/`. |
| `docs/`          | -       | -       | Diagrams, screenshots, moddb listing + handbook drafts. |
| `scripts/`       | -       | -       | Game/.NET provisioning, mod staging, test runners. |
| `wiki/`          | -       | -       | The current-mods wiki home pages (fed to the wiki site). |
| `Legacy.sln`     | -       | -       | Solution tying the projects together. |

`smex` project-references `exlib` and `ppex` (with `Private=false`), so players install all
three mods separately; the network manager identity lives in `exlib` only.

## What never happens here

No new features, no refactors, no format pass - this tree predates the family's CSharpier gate
and stays excluded from it (`bash scripts/exmod.sh check` never walks `legacy/`; its own build
and test run separately, see below). There is no maintenance release: ppex and smex carry no
guard against a newer exlib, so the game's loader simply fails to load them at all once exlib
0.8 or later replaces the 0.7.2 they need.

**iiex (Iron Industry Expanded) and siex (Steel Industry Expanded) are the successors** - new
mods rather than updates to ppex/smex. Worlds do not carry over between the two lines.

## Building and testing

Prerequisites and provisioning work the same way the old monorepo described them: Git, a .NET 10
SDK (the root `global.json` pins it), and PowerShell 7+ or bash. Game binaries and .NET runtimes
are fetched on demand into `.game/` and `.dotnet/` under this folder by `scripts/provision-*` if
nothing else is pointed at. Prefer the workspace's shared installs instead of a second download:

```sh
# From the workspace root (exmods/):
export VINTAGE_STORY=$PWD/.game/1.22
dotnet build legacy/Legacy.sln -clp:ErrorsOnly
bash legacy/scripts/run-tests.sh latest    # or 1.21 / 1.20 / all
```

`pwsh legacy/scripts/run-tests.ps1` is the Windows equivalent. Both build the test projects for
the requested game version (auto-provisioning binaries and, if missing, the matching .NET runtime)
and run the projects in parallel; the build phase itself is serial, since the test projects share
the mod projects and a concurrent build races on the same intermediate DLLs.

## Packaging a release

`dotnet run --project legacy/dist/CakeBuild` publishes every mod for every supported game version
and zips the result into `legacy/dist/Releases/<gameVersion>/<modid>_<version>[_<gameVersion>].zip`
(current version unsuffixed, legacy game versions carry a trailing suffix). exlib 0.7.2 is not
rebuilt by this tree - its published zip is the one already on the mod database.
