---
title: Commands and config
covers:
  - "ppex:boilercornish-*"
  - "ppex:boilerlancashire-*"
  - "ppex:engine*"
  - "ppex:manualfluidpump-*"
  - "ppex:mpfluidpump-*"
  - "ppex:steamcondenser-*"
  - "ppex:pipe-fluidintake-*"
order: 6
version: 0.7.0
---

Every number on these pages is a config value, and all of them can be read and set in game. The
commands live on Expanded Library's shared `/exmod` root, and the mod id is the section name, so
this mod is `ppex`.

## Commands

| command | what it does |
|---|---|
| `.exmod network hi` | tints every networked block, one translucent colour per network, and keeps up as you build |
| `.exmod network unhi` | turns the highlight off |
| `.exmod measure` | reports your display units |
| `.exmod measure metric` | litres, atm and degrees Celsius |
| `.exmod measure imperial` | gallons, psi and degrees Fahrenheit |
| `/exmod config` | lists every mod that has manageable config |
| `/exmod config ppex` | lists this mod's values and their current settings |
| `/exmod config ppex <key>` | prints one value |
| `/exmod config ppex <key> <value>` | sets one value, applied at once and written to disk |
| `/exmod recipes ppex` | reports the recipe cost level |
| `/exmod recipes ppex cheap` | sets it, applied on the next world reload |

The two `.exmod` commands are client-side and affect only what you see: the unit switch converts
the look-at lines and the game's own handbook, and changes nothing in the simulation. The `/exmod`
commands are server-side and need the `controlserver` privilege.

## The files

The `ppex` section of `ModConfig/ex_values.json` holds the values below, and `/exmod config ppex`
edits that section live. The `ppex` section of `ModConfig/ex_recipes.json` holds the per-recipe
ingredient counts for each cost level; it is edited on disk, not through a command, and `/exmod
recipes` only chooses which level of it is active. Both files are shared with the other mods on
Expanded Library, one section per mod.

A world from 0.6.9 or earlier carries `ppex_values.json` (or the older `ppex.json`) and
`ppex_recipes.json`. The first load folds them into the two sections with their values kept and
renames them to `.migrated`.

A value that comes back from the file as not-a-number, infinite or negative is reset to its
default when the world loads, and a few keys are clamped to a range.

Some releases push a new default out over your saved tuning. When a world crosses one of those
versions only the named keys are reset and the rest of your file is untouched: 0.6.0 and 0.6.7 reset
the engine pump rate, 0.6.5 and 0.6.6 the shaft load an engine holds, and 0.6.6 also the salvage a
machine returns when it is mined intact. That last one is why setting `RccBrokenDropsRatio` looked
like it did nothing in mid-2026: the release had already raised it to 1.0, and mining a machine
intact now returns everything it was built from.

## The values

### Pipes

| key | default | what it sets |
|---|---|---|
| `IronPipeBurstPressure` | 5 atm | burst rating of an iron pipe |
| `SteelPipeBurstPressure` | 10 atm | burst rating of a steel pipe |
| `ChimneyGasDrawRate` | 16 L/s | gas one chimney draws off a passthrough or outlet |
| `BoilingPoint` | 100 C | where water becomes steam and steam becomes water |
| `SteamExpansionFactor` | 16 | litres of steam per litre of water, both ways |
| `SteamSaturationExponent` | 0.25 | exponent of the steam temperature curve |

The pipe network's own numbers are Expanded Library's, in the `exlib` section of
`ModConfig/ex_values.json`, and `/exmod config exlib` sets them. They apply to every mod's pipes:

| key | default | what it sets |
|---|---|---|
| `LitresPerPipe` | 30 L | capacity one pipe adds to its run at 1 atm |
| `PipeOverpressureSeconds` | 30 s | time at the burst rating before a pipe lets go |
| `GasLeakRate` | 8 L/s | gas each open end bleeds at 1 atm, in proportion to the run's pressure |
| `LiquidLeakRate` | 10 L/s | water each open end drains |
| `EvaporationLitresPerDay` | 50 L | water lost per in-game day from a run and from a boiler |
| `PipeGasCoolPerSecond` | 2 C/s | how fast a gas run cools |
| `PipeAmbientTemperature` | 20 C | the temperature a gas run cools toward |

Up to 0.6.9 the first five were ppex keys. A value tuned in the old file is not carried over; the
server log names it once at load.

### Boilers (the plain names are the Lancashire)

| key | default | what it sets |
|---|---|---|
| `BoilerCapacity` | 1200 L | vessel capacity, water and steam together |
| `BoilerMinBoilWater` | 200 L | water needed before it heats |
| `BoilerMaxBoilWater` | 800 L | most water it holds |
| `BoilerSteamPerSecond` | 48 L/s | steam while boiling |
| `BoilerMaxOutputPressure` | 12 atm | choke pressure, and the explosion threshold |
| `BoilerExplosionRadius` | 4 blocks | blast radius |
| `CornishBoilerCapacity` | 800 L | the Cornish vessel |
| `CornishBoilerMinBoilWater` | 150 L | the Cornish floor |
| `CornishBoilerMaxBoilWater` | 500 L | the Cornish ceiling |
| `CornishBoilerSteamPerSecond` | 32 L/s | the Cornish output |
| `CornishBoilerMaxOutputPressure` | 5 atm | the Cornish choke |
| `CornishBoilerExplosionRadius` | 3 blocks | the Cornish blast |
| `BoilerHeatUpSeconds` | 180 s | lighting to first steam |
| `BoilerOverpressureSeconds` | 30 s | at the choke pressure before it explodes |
| `BoilerShutdownDelaySeconds` | 10 s | grace after the fire dies |
| `BoilerChokeExtinguishSeconds` | 10 s | choked before the fuel pile is snuffed |
| `BoilerShutdownCondenseRate` | 200 L/s | steam condensing back to water after shutdown |
| `BoilerExhaustPerSecond` | 16 L/s | exhaust a burning boiler makes |
| `ExhaustMaxOutputPressure` | 0.8 atm | back pressure at which the fire is choked |
| `BoilerLidVentRate` | 200 L/s | steam an open lid dumps |
| `BoilerSteamLeakRate` | 16 L/s | the least steam an open outlet vents per second at 1 atm; the boiler's own make is used when larger |
| `BoilerWaterIntakeFillFraction` | 0.5 | share of capacity the automatic intake fills to |
| `BoilerWaterIntakeRate` | 10 L/s | automatic intake draw |
| `WaterPressureSteamBoost` | 1 | extra litres of steam per litre admitted per atm over 1 atm |
| `BoilerBlastResistanceThreshold` | 20 | blocks under this resistance are shattered by a burst |
| `BoilerExplosionDropRatio` | 0.4 | share of build materials salvaged from a burst |
| `RccBrokenDropsRatio` | 1.0 | share returned when a machine is mined intact |
| `BoilerWaterSurfaceLowLevel` | 0.2 | rendered water height below the operating level |
| `BoilerWaterSurfaceHighLevel` | 0.99 | rendered water height once operating |

### Engines and sub-machines

| key | default | what it sets |
|---|---|---|
| `WattEngineEngagePressure` | 2 atm | where the Watt starts |
| `WattEngineBreakPressure` | 4 atm | where the Watt starts wearing |
| `WattEngineSteamRate` | 30 L/s | Watt steam draw |
| `WattEngineMaxPower` | 0.3 | Watt power |
| `WattEngineWaterRate` | 1 L/s | Watt condensate |
| `CornishEngineEngagePressureLow` | 5 atm | Cornish engage, low |
| `CornishEngineEngagePressureNormal` | 6 atm | Cornish engage, normal |
| `CornishEngineEngagePressureHigh` | 7 atm | Cornish engage, high |
| `CornishEngineBreakPressureLow` | 8 atm | Cornish wear point, low |
| `CornishEngineBreakPressureNormal` | 8 atm | Cornish wear point, normal |
| `CornishEngineBreakPressureHigh` | 8 atm | Cornish wear point, high |
| `CornishEngineSteamLow` | 8 L/s | Cornish steam draw, low |
| `CornishEngineSteamNormal` | 16 L/s | Cornish steam draw, normal |
| `CornishEngineSteamHigh` | 32 L/s | Cornish steam draw, high |
| `CornishEnginePowerLow` | 0.2 | Cornish power, low |
| `CornishEnginePowerNormal` | 0.4 | Cornish power, normal |
| `CornishEnginePowerHigh` | 0.8 | Cornish power, high |
| `CornishEngineWaterLow` | 0.3 L/s | Cornish condensate, low |
| `CornishEngineWaterNormal` | 0.6 L/s | Cornish condensate, normal |
| `CornishEngineWaterHigh` | 1.2 L/s | Cornish condensate, high |
| `CornishEngineMaxPower` | 1.0 | the Cornish nominal figure the readout quotes |
| `CornishEngineOverclockVolume` | 1.8 | sound volume scale on high |
| `CornishEngineOverclockPitch` | 0.8 | gear hum pitch scale on high |
| `EngineOverPressureSeconds` | 60 s | above the band before an engine bursts |
| `SteamEngineEfficiency` | 0.75 | share of inlet pressure a sub-machine delivers at |
| `MpRatedSpeed` | 1.0 | shaft speed a generator holds at its rated load |
| `MpLoadPerEnginePower` | 2.0 | shaft load an engine holds per unit of power |
| `PumpWaterPerSecond` | 100 L/s | engine pump rate per unit of engine power |
| `ManualPumpWaterPerSecond` | 2 L/s | hand crank rate |
| `ManualPumpDeliveryPressure` | 1 atm | hand crank head |
| `MpPumpLitresPerSpeed` | 20 L/s | mechanical pump rate per unit of axle speed |
| `MpPumpDeliveryPressure` | 1.5 atm | mechanical pump head |
| `MpPumpBaseLoad` | 0.05 | shaft load of the beam itself |
| `MpPumpLoadPerAtm` | 0.05 | extra shaft load per atm of head |
| `FluidIntakeWaterDepth` | 3 blocks | side of the water cube an intake needs below it |
| `FluidIntakeExclusionRange` | 6 blocks | spacing between intakes |
| `CondenserSteamPerSecond` | 30 L/s | steam a condenser pulls |
| `CondenserWaterThroughput` | 50 L/s | water a condenser passes |

### Recipes

| key | default | what it sets |
|---|---|---|
| `RecipeLevel` | normal | recipe cost level, normal or cheap |
## What goes wrong

**The command is not found.** `/exmod config` and `/exmod recipes` need server privileges; on a
server you are not an admin on, they are not yours to run. `.exmod network` and `.exmod measure`
start with a dot, not a slash, because they run on your own client.

**A recipe level change did nothing.** It applies on the next world reload, not at once.

**A value reverted after an update.** See the migration list above. Anything not on it keeps what
you set.
