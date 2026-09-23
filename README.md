# Legacy: exlib 0.7 / ppex 0.6 / smex 0.9

This is the maintenance repository of the published Fallenstar's Expanded line - the code players
actually run today. It was split out of the exmods repository, where it lived under `legacy/`, with
its history kept, and it builds, tests and packages on its own: nothing here reads the exmods
repository or the family's (iiex, siex) central package management and version manifest.

| Path             | modid   | Version | What it is                                                      |
| ---------------- | ------- | ------- | ---------------------------------------------------------------- |
| `exlib/`         | `exlib` | 0.7.2   | Shared framework: block networks, multiblock structures, registries, save migrations. |
| `ppex/`          | `ppex`  | 0.6.9   | Pipe networks (gas + water), boilers, steam engines and their sub-machines. |
| `smex/`          | `smex`  | 0.9.9   | Blast furnace, cowper stoves, molten-metal canals and casting, Bessemer converter. |
| `generators/`    | -       | -       | Roslyn source generators the mods use at compile time (config accessors, block-attribute bakers). |
| `tests/`         | -       | -       | Headless xUnit test projects: per-mod unit tests + cross-mod integration. |
| `dist/CakeBuild/`| -       | -       | Cake build project that publishes per-game-version release zips into `dist/Releases/`. |
| `docs/`          | -       | -       | Diagrams, screenshots, moddb listing + handbook drafts. |
| `scripts/`       | -       | -       | Game/.NET provisioning, mod staging, test runners. |
| `wiki/`          | -       | -       | The current-mods wiki pages - home, FAQ and instructions per mod (read by the wiki site). |
| `Legacy.sln`     | -       | -       | Solution tying the projects together. |

`smex` project-references `exlib` and `ppex` (with `Private=false`), so players install all
three mods separately; the network manager identity lives in `exlib` only.

## What never happens here

No new features, no refactors, no format pass - this tree predates the family's CSharpier gate
and is not formatted by it. ppex and smex carry no
guard against a newer exlib, so the game's loader simply fails to load them at all once exlib
0.8 or later replaces the 0.7.2 they need. exlib 0.8 itself logs one Error naming the clash and
repeats it to every joining player.

**iiex (Iron Industry Expanded) and siex (Steel Industry Expanded) are the successors** - new
mods rather than updates to ppex/smex. Worlds do not carry over between the two lines.

## Building and testing

Prerequisites: Git, a .NET 10 SDK (`global.json` pins it), and PowerShell 7+ or bash. Game
binaries and .NET runtimes are fetched on demand into `.game/` and `.dotnet/` at the repository
root by `scripts/provision-*`. In the modding-vsex workspace both are symlinks to the shared
installs one level up (`ln -s ../.game .game`, `ln -s ../.dotnet .dotnet`), so nothing is
downloaded twice; the `VINTAGE_STORY`, `VINTAGE_STORY_121` and `VINTAGE_STORY_120` environment
variables point the build at any other install instead.

```sh
dotnet build Legacy.sln -clp:ErrorsOnly
bash scripts/run-tests.sh latest    # or 1.21 / 1.20 / all
```

`pwsh scripts/run-tests.ps1` is the Windows equivalent. Both build the test projects for the
requested game version (auto-provisioning binaries and, if missing, the matching .NET runtime)
and run the projects in parallel; the build phase itself is serial, since the test projects share
the mod projects and a concurrent build races on the same intermediate DLLs.

## Packaging a release

`dotnet run --project dist/CakeBuild` publishes every mod for every supported game version
and zips the result into `dist/Releases/<gameVersion>/<modid>_<version>[_<gameVersion>].zip`
(current version unsuffixed, legacy game versions carry a trailing suffix). exlib 0.7.2 is not
rebuilt by this tree - its published zip is the one already on the mod database.
