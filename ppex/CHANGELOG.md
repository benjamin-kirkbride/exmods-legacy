# Changelog - Pipes and Power Expanded (`ppex`)

All notable changes to this mod are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/), and the project follows
[Semantic Versioning](https://semver.org/). For changes before this file existed,
see the git history.

## [0.7.0] - 2026-09-29

Requires Expanded Library 0.8.3 or later; it no longer loads with exlib 0.7.2. Back up your world
before updating: a world saved on this version cannot go back to 0.6.9 or earlier. Its pipes would
lose their contents, the config would be back at its defaults, and some pipes might not read their
facing.

### Changed

- **Built on Expanded Library 0.8.** Pipes run on exlib's shared pipe network. A world from 0.6.8 or
  0.6.9 loads with every pipe's contents and pressure, and the limits are the same: iron pipe bursts
  at 5 atm, steel at 10 atm, and a run has no throughput cap. exlib 0.8.3 names this mod in the log
  and in chat when a version below 0.7.0 is installed beside it.
- **Gas in a pipe run cools.** Steam, air and exhaust lose 2 C a second toward 20 C, whether or not
  anything draws from the run: steam left standing a minute in a ten-pipe run drops from 150 C to
  30 C. Pressure is unchanged, and engines run as before. The rate is `PipeGasCoolPerSecond` and
  the floor `PipeAmbientTemperature`, in the `exlib` section of `ModConfig/ex_values.json`.
- **The config moves into the shared files:** the tunables into the `ppex` section of
  `ModConfig/ex_values.json`, the recipe costs into the `ppex` section of
  `ModConfig/ex_recipes.json`. On first load `ppex_values.json` (or the older `ppex.json`) and
  `ppex_recipes.json` are folded in with their values kept, and renamed to `.migrated`.
- **A turned boiler part stops the boiler.** The coke oven door of a Cornish or Lancashire boiler
  has to face out, and the flue passthroughs and the passthrough bend have to run along the flue. A
  part turned in place leaves the boiler incomplete until it is turned back: the look-at line counts
  it as missing, and the build outline (ctrl+shift+right-click) marks it red. This applies to
  boilers already standing in a world.
- **A construction stage takes one kind of material.** A stage that needs four planks takes four of
  one wood: two oak and two pine are refused. Machines already part-built follow the same rule.
- **Machine sounds follow the machine volume.** `.exmod sound` and the game's volume sliders set the
  engine's gear hum, the pumps' water and the Mechanical Power Generator's grind. The grind is now
  the gearbox sound and quieter. A burning boiler's firebox sound is one loop per boiler that starts
  and stops with the fire, where copies used to pile up. A pipe's bubbling and trickle no longer
  overlap themselves.
- **Runs never join across the two lines.** With Iron Industry Expanded installed, its pipes and
  these pipes placed side by side form two runs. Machine ports take either line's pipe.

### Added

- **With Iron Industry Expanded or Steel Industry Expanded in the same world, this mod and
  Steelmaking Expanded close.** Machines already built keep working, but nothing new can be built
  from the two mods:
  - their recipes are gone, and their blocks and items leave the creative inventory and the
    handbook, the guide pages included;
  - their blocks drop none of their own items when broken; fuel, metal and the vanilla materials of
    a part-built machine still drop;
  - their items are removed from each player's inventories on joining, from chests, racks, ground
    storage and other containers as their chunks load, and from the ground;
  - a pump, twin-tub blower or Bessemer converter already part-built can be finished: its pipe
    stage takes the new line's straight pipe;
  - each player sees one chat notice the first time they join, and the server log says so once at
    start.

  Not covered, so an item left there stays: the inventories of boats, pack animals, armour stands
  and traders; storage of other mods that is not a container block (a carried block, say); items
  inside another item, such as a bag in a chest; chunks that never load again; and an item a
  machine hands out later, which goes at the next sweep of the place it lands. Removing both new
  mods opens the line again.

### Removed

- The pipe tunables `LitresPerPipe`, `GasLeakRate`, `LiquidLeakRate`, `EvaporationLitresPerDay` and
  `PipeOverpressureSeconds`. The pipe network reads them from the `exlib` section of
  `ModConfig/ex_values.json`, with the same defaults. A value tuned in the old file is logged once
  at load, naming the key.

### Fixed

- **The log no longer warns about the old refractory gas pipes at every start.** With Steelmaking
  Expanded installed, 57 lines said a `smex:gaspipe-...-refractorytier...` code was "already mapped
  elsewhere". An old refractory gas pipe still becomes the pipe of the same brick, and a fire brick
  pipe only when Steelmaking Expanded is not installed.
- **The mechanical fluid pump shows only its base as an item**, in hand, in the inventory and dropped, as
  the engines and boilers do. It showed the whole pump.

## [0.6.9] - 2026-09-23

### Fixed

- **Breaking a part-built mechanical fluid pump crashed the game.** Its plank and pipe stages were
  refunded as codes the game could not resolve. The planks now come back as the wood they were, and
  the pipe stage takes a straight pipe of the same metal as the plates and rods before it (iron or
  steel). A pump records one wood and one metal: one paid in more than one of either gives its
  wooden parts back in the last wood paid and its metal parts in the last metal paid. A pump built
  before this release breaks cleanly too, and gives its planks back as oak, since it never recorded
  which wood they were. A pump built in creative mode with Ctrl held records neither, and broken
  in survival gives back oak and iron.
- The Mechanical Fluid Pump and the Manual Fluid Pump recipes take wooden support beams only.
- **The Watt engine recipe did not cost its gears.** Both recipes listed two gears and left them out
  of the grid. The gears now go in the top-left slot, beside the hammer.
- The Cornish boiler recipe listed a pipe its grid never used. The entry is gone; what the recipe
  costs is unchanged.

### Changed

- The English description of the pipe bend (`blockdesc-pipe-bend*`) reads "A 90 degree bend in a
  pipe run." without the degree sign.

## [0.6.8] - 2026-08-13

No changes to this mod. It requires Expanded Library 0.7.2, which stops a pipe losing what it knows
about its network when an update renames it - the fix lives there, and this release is what makes
sure you have it.

## [0.6.7] - 2026-08-09

### Changed

- **The engine fluid pump's configured rate is the rate you get.** It was quietly multiplied by three
  in code, so the number in the config meant nothing on its own. That factor is folded into the
  default and the pump now moves exactly what it says: 30 L/s on a Watt, 20/40/80 on a Cornish.
  Sized at three mechanical pumps to one engine pump on the same engine.
- The hand pump's delivery head is a config key rather than a fixed value.

### Fixed

- Engine sub-machines showed nothing when looked at. The water pump now reports its flow and delivery
  pressure - and says so when it has no intake on the source line - and the mechanical generator
  reports shaft speed against the engine's rating, including when the shaft is labouring.
- **A water wheel drove its whole network at the wrong speed after a world reload**, until you broke
  and replaced any axle. This is a bug in the game's own water wheel: its periodic water check
  overwrites the wheel's gearing with a fixed value, and the check always fires once on load. A
  network is only ever as fast as its gearing says, so every machine on the shaft ran wrong with it -
  a geared blower fed a blast furnace a fraction of the air it should have, and the furnace went out.
  Patched here, since nothing downstream can work around it.
- **Steam engines sprayed water at the outlet even with a pipe connected.** A condensate line that is
  full - which is what a closed water loop always is - reads the same as no line at all to the code
  that decided this. Only an outlet with nothing plumbed onto it, or one plumbed into a line carrying
  gas, sprays now; a line that is merely backed up takes what it can and the rest is lost quietly.
- **The mechanical fluid pump read the raw network speed**, ignoring its own gear ratio. A network's
  speed is held in the frame of whichever machine started it, so a geared pump's stroke rate changed
  between one world load and the next.
- **The Engines article recommended a build that bursts the engine.** It said a single Cornish
  boiler can safely power a Watt engine; the boiler holds up to 5 atm and the Watt wears toward a
  burst above 4. The article now sends you to a pressure valve between the two.
- **A temperature difference was converted like a temperature.** The imperial conversion added the
  freezing-point offset to a delta, so any figure that is a number of degrees gained or lost - the
  converter's scrap heat cost - read 32 °F too high. Absolute temperatures were always right.
- The Fluid Intake link in the starter walkthrough went to an empty handbook search in English.
- **A pipe carrying its whole load reported "Empty"** (player-reported, of blast furnace tuyeres). A
  run drained as fast as it is fed holds nothing, and its medium label clears with the last litre -
  and the throughput line was shown only when that label was set, so the pipes working hardest were
  the ones that looked dead. Throughput is now reported whenever gas is moving, named when the run
  still knows what it carries.

## [0.6.6] - 2026-08-09

Covers the 0.6.4 and 0.6.5 development bumps, which were never published separately.

### Added

- **Mechanical Fluid Pump** - a walking-beam pump driven from the mechanical power
  network instead of by steam, so it can fill a boiler whose fire is out. It sits between
  the hand crank and the engine pump: 8 L/s at full axle speed against a fixed 1.5 atm
  head, scaling down to nothing below half speed. Right-click constructed - the grid
  recipe gives a wooden frame, and the axle, piston, pipework and reservoir follow. The
  axle couples on the east face; water is drawn from beneath the far cell and delivered
  from its top.

### Changed

- **The mechanical-power overstress ceiling scales with the shaft.** It judged a shared
  network's whole resistance against a single engine's rating, so adding engines could not
  raise it and a bank stalled well below what it should carry. The load an engine holds
  per unit of power was raised from 0.875 to 1.37 to match, which puts three Cornish
  engines on one shaft at roughly 500 W where they previously reached about 320 W.
- **The engine pump's `x3` throughput factor is now a named constant.** It is playtest
  calibration rather than a stray coefficient: the quoted rates are what the pump actually
  delivers once the intake draw and the output main's free capacity bite. Both the pump's and
  the blower's rates are now pinned by tests.

### Fixed

- **Engine stroke sounds no longer fire on a backward step.** The cycle predicate read a
  decreasing frame as "the animation wrapped", but a backward-running cycle arrives the
  same way - so a single engine emitted roughly twenty plays a second per keyframe instead
  of two a revolution. A handful of machines then exhausted the game's concurrent-sound
  cap, at which point all audio starts being dropped, the game's own included.
- **Pipe bends no longer pop out of the world** when a neighbour changes beside a
  connector that faces a solid wall.
- **A blocked or over-pressured line no longer bursts from one long server tick.**

## [0.6.3] - 2026-06-21

### Added

- **Manual boiler draining with buckets** - take water back out of a boiler by hand.
- **Localizable measurement units.** `.exmod measure` reports your display units and
  `.exmod measure metric` / `imperial` switches them (L/atm/°C vs gal/psi/°F); a
  display-only change, the simulation stays metric.
- **Recipe-cost levels** for ppex's construction recipes, switchable via the shared
  `/exmod recipes` command.
- **Russian and Ukrainian** translations.

### Changed

- **Boilers no longer have an upper boil limit** - water is gated on the way in, so
  the old hard cap was removed - and the boiler **water-draw speed is gated to
  10 L/s**, so it no longer gulps its whole intake buffer in one tick.
- An **open boiler lid drops pressure to 0 atm while idle**.
- **Molten chiselling generalized** into the shared behaviour (consistent
  tool/sound/recovery handling).
- **Boilers no longer drop their base block when broken** - a broken boiler scatters
  its build materials (custom salvage ratio) instead of dropping the whole mega-block.
- **Raised break-tool requirements** for mega-blocks.
- Machines **read live config changes** without a world reload.

### Fixed

- The **Watt engine** now displays its togglable pressure band correctly.
- The **Cornish engine** now correctly costs bricks to construct.
- Assorted **valve** issues.
- Network blocks that are not pipes could incorrectly **burst**.
- Right-click-constructable blocks ignored their **last construction stage** when
  computing dropped materials.
- **Handbook**: command strings displayed incorrectly, and measurement units did not
  refresh mid-session after a `.exmod measure` change.
- Block display-name ordering and assorted localization issues.

## [0.6.2] - 2026-06-18

### Added

- The **handbook now documents** the mod's chat commands.

The boiler bucket-draining and localizable measurement-unit work from this cycle are
listed under 0.6.3 above.

## [0.6.1] - 2026-06-16

### Added

- **Craftable iron and steel gears** for the machine recipes.

### Changed

- Tuned **steam-engine power scaling**.

### Fixed

- The **fluid network** now displays pressures below 1 atm; corrected engine power
  calculation.

## [0.6.0] - 2026-06-14

### Changed

- Build and packaging maintenance (resolved Cake build warnings) ahead of the new
  publish pipeline.

## [0.5.1] - 2026-06-14

### Added

- **Manual hand-cranked fluid pump** - an engine-free water pump.

## [0.5.0] - 2026-06-13

The first release of **Pipes and Power Expanded**, split out from Steelmaking
Expanded as the home of the new steam-power system.

### Added

- **Unified pipe network** carrying gas, steam or water, with network-wide pressure
  and temperature.
- **Boilers** and **steam engines** (Watt and Cornish).
- **Sub-machines** driven by the engines: a water pump and an air blower.
- **Gas/pressure valves**, a directional **pressure-relief valve**, and a **condenser**.
- **Mechanical-power integration** so engines can drive vanilla MP machines.
