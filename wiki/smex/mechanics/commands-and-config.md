---
title: Commands and config
covers:
  - "smex:toolmold*"
order: 7
version: 0.10.0
---

Every number on the other pages is a config value, and all of them can be changed in place. The
commands belong to Expanded Library, not to this mod: the mod id is the section name, so tuning
this mod is always `/exmod config smex ...`.

## The commands

| command | what it does |
|---|---|
| `/exmod config smex` | lists every key and its current value |
| `/exmod config smex <key>` | reads one key |
| `/exmod config smex <key> <value>` | sets one key and writes it to disk. Applies live |
| `/exmod recipes smex` | reads the recipe cost level |
| `/exmod recipes smex <normal or cheap>` | sets it. Applies on the next world reload |
| `/exmod molds <plate, ingot, rod or all>` | reports whether those molds are available |
| `/exmod molds <plate, ingot, rod or all> <on or off>` | switches them. Placed molds stop casting at once; the clay-forming recipe and the in-game handbook entry change on the next world reload |
| `.exmod measure <metric or imperial>` | a per-player display choice from Pipes and Power Expanded: litres, atm and C, or gallons, psi and F. It changes the panels, never the simulation |
| `.exmod network hi` and `.exmod network unhi` | Expanded Library's per-network block highlight, which is the fastest way to see what a pipe or canal run is actually connected to |

The server commands need the `controlserver` privilege. The two that start with a dot are client
side and need nothing.

Older in-game text names `/exmod steel <level>` for the recipe level; that command does not exist.
Use `/exmod recipes smex <level>`.

## The files

The `smex` section of `ModConfig/ex_values.json` holds the values below. The machines re-read it
every tick, so a `/exmod config` change reaches a furnace that is already running. A saved value
that is not a number, or is negative, resets to its default on load.

The `smex` section of `ModConfig/ex_recipes.json` holds the recipe cost catalogue: which grid
recipes and which right-click construction the level applies to, with a `normal` profile filled in
from the recipes as shipped and a `cheap` profile at half cost. Edit the per-recipe numbers there;
the command only picks which profile is live, and the change lands on the next world reload.

Both files are shared with the other mods on Expanded Library, one section per mod. A world from
0.9.9 or earlier carries `smex_values.json` (or the older `smex.json`) and `smex_recipes.json`. The
first load folds them into the two sections with their values kept and renames them to `.migrated`.

Upgrading the mod can reset individual keys. Each release that rebalances something lists the keys
it pushes out, and only those are reset; the rest of your tuning is kept. 0.9.7 reset most of the
furnace and converter numbers at once, because a saved config from before it left a furnace that
could not reach its own melting point.

## The values

### Blast furnace

| key | default | sets |
|---|---|---|
| `BurdenRequiredToFire` | 320 | burden in the hearth before the furnace will fire |
| `BurdenRequiredToRun` | 144 | burden a lit furnace must keep, or it goes out |
| `BurdenBurnTime` | 300 | seconds a lit burden pile burns outside a working furnace |
| `BfIgnitionTemperature` | 900 | hearth temperature the moment the charge catches, in C |
| `BfIronMeltingPoint` | 1482 | hearth temperature that starts melting, in C |
| `BfNaturalMaxTemp` | 1540 | hearth ceiling on cold blast, in C |
| `BfBoostedMaxTemp` | 1740 | hearth ceiling on blast at the reference temperature, in C |
| `BfBlastTempReference` | 1240 | blast temperature that buys the full boost, in C |
| `BfHeatRateBase` | 4 | climb on cold blast, in C per second |
| `BfHeatRateHot` | 8 | climb on fully hot blast, in C per second |
| `BfHeatRateUnblown` | 2 | climb with no blast, on a chimney or smoke stack's draught, in C per second |
| `BfMeltSpeedBase` | 0.2 | melt rate at zero margin over the melting point |
| `BfMeltGainPer100C` | 0.7 | extra melt rate per 100 C of margin |
| `BfMeltSpeedMin` | 0.5 | floor on the melt rate once melting |
| `BfMeltSpeedMax` | 2.0 | ceiling on the melt rate |
| `BfMeltIntervalSec` | 10 | seconds per melt cycle at rate 1.0 |
| `BfIronPerMeltCycle` | 102 | units of iron per cycle |
| `BfSlagPerMeltCycle` | 17 | units of slag per cycle |
| `BfBurdenPerMeltCycle` | 16 | burden per cycle |
| `BfMaxMoltenIron` | 4800 | units of iron the hearth holds before the melt stalls |
| `BfMaxMoltenSlag` | 1200 | units of slag it holds before the melt stalls |
| `BfIronTapDrainPerTick` | 40 | units per second through the lower tap |
| `BfSlagTapDrainPerTick` | 40 | units per second through the upper tap |
| `BfMeltStallSeconds` | 30 | seconds under the melting point before melting falls back to firing |
| `BfDisruptionGraceSeconds` | 30 | seconds one disruption is tolerated |
| `BfDoorOpenGraceSeconds` | 10 | seconds an open door is tolerated |
| `BfUnblownIgnitionSeconds` | 10 | seconds a hearth being lit survives with no blast |
| `BfAirDemandIgnition` | 0.25 | share of a tuyere's rated air a just-lit hearth draws |
| `TuyereIntakeVolume` | 20 | air one tuyere draws at melt rate 1.0, in L/s |
| `BfBlastPressureThreshold` | 1.5 | pressure at a tuyere that counts as blast, in atm |
| `BfExhaustOutputPressure` | 2.0 | pressure the furnace pushes its exhaust to, in atm |
| `BfExhaustPerAirDrawn` | 1.0 | litres of flue gas per litre of blast drawn |
| `BfExhaustBaseVolume` | 24 | flue gas on natural draught, in L/s |
| `BfExhaustTempFraction` | 0.8 | share of the hearth temperature the flue gas carries |

### Hoppers

| key | default | sets |
|---|---|---|
| `HopperMaxMagazineCapacity` | 48 | burden the bell hopper buffers |
| `HopperIronOreRequired` | 12 | crushed iron ore per batch |
| `HopperNuggetRequired` | 12 | iron nuggets per batch |
| `HopperRoastedOreBonus` | 2 | extra ore units a roasted piece is worth |
| `HopperCokeRequired` | 2 | coke per batch |
| `HopperCharcoalRequired` | 4 | charcoal per batch |
| `HopperLimeRequired` | 1 | lime per batch |
| `HopperBurdenProduced` | 16 | burden per batch |
| `HopperDropAmount` | 4 | burden dropped per second |

### Blowers

| key | default | sets |
|---|---|---|
| `AirBlowerOutputPerSecond` | 300 | air the steam blower injects per unit of engine power, in L/s |
| `MpBlowerMaxLitres` | 110 | air the twin-tub blower approaches at unbounded axle speed, in L/s |
| `MpBlowerHalfOutputSpeed` | 1.3 | axle speed at which it delivers half of that |
| `MpBlowerMaxPressure` | 2.0 | pressure its bellows seal against, in atm |
| `MpBlowerBaseLoad` | 0.05 | shaft load against an empty main |
| `MpBlowerLoadPerAtm` | 0.05 | extra shaft load per atm of back-pressure |

### Cowper stove and smoke stack

| key | default | sets |
|---|---|---|
| `CowperMaxTemperature` | 1240 | ceiling on the stove core, in C |
| `CowperHeatingSpeedAnthracite` | 0.0064 | share of the gap the core closes per second on anthracite |
| `CowperHeatingSpeedOtherCoal` | 0.0048 | the same on any other coal |
| `CowperHeatingSpeedDefault` | 0.0012 | the same with no coal pile |
| `CowperCoolingSpeedExhaust` | 0.3 | rate the soaked exhaust gives up its heat |
| `CowperCoolingSpeedAir` | 0.0012 | rate the core loses heat into the air at a full draw |
| `CowperIdleCoolingSpeed` | 0.0006 | rate a stove bleeds to ambient whatever it is doing |
| `CowperSpentExhaustTempFraction` | 0.4 | share of its temperature spent exhaust keeps |
| `CowperIntakeVolume` | 24 | gas a stove draws per intake, in L/s |
| `SmokestackGasIntakeVolume` | 96 | gas the smoke stack vents, in L/s |

### Molten system

| key | default | sets |
|---|---|---|
| `MoltenCooldownSpeed` | 24 | cooling rate stamped on molten metal the system creates |
| `BarrelCooldownCoefficient` | 1.0 | multiplier on it for metal in a barrel |
| `TapMoldCooldownCoefficient` | 1.0 | the same for a mold under a canal tap |
| `MoldPedestalCooldownCoefficient` | 1.0 | the same for a mold on a pedestal |
| `MoltenFlowRate` | 100 | units crossing one canal connection per second |
| `MoltenMinFlowAmount` | 1 | smallest gap between two cells that still moves metal |
| `MoltenUnitsPerBit` | 5 | units per metal bit, for recovery and for converter scrap |
| `CanalDefaultUnitCapacity` | 100 | cell capacity when the block names none |
| `CanalDefaultDrainSpeed` | 20 | canal tap drain, in units per second |
| `MoldDefaultUnits` | 100 | mold capacity when the mold names none |
| `BarrelDefaultMaxUnits` | 800 | barrel capacity when the block names none |
| `CanalSealClayCost` | 4 | fire clay to seal a straight canal |
| `CanalUnsealClayRefund` | 2 | fire clay returned by breaking the seal |
| `MoldBurnMinTemperature` | 200 | temperature above which a carried mold burns bare hands, in C |

### Bessemer converter

| key | default | sets |
|---|---|---|
| `BlastPressureThreshold` | 2.5 | pressure that counts as blast for the converter, in atm |
| `BessemerConverterCapacity` | 2400 | units the vessel holds, metal and scrap together |
| `BessemerBlastPerSecond` | 24 | air drawn at conversion rate 1.0, in L/s |
| `BessemerProcessDuration` | 300 | seconds of blast a charge needs at rate 1.0 |
| `BessemerSpeedMin` | 0.5 | conversion rate at the gate pressure |
| `BessemerSpeedMax` | 2.0 | conversion rate ceiling |
| `BessemerPressureReference` | 3.5 | atm over the gate that buys the full rate |
| `BessemerBaseTemperature` | 1850 | bath temperature the blow itself makes, in C |
| `BessemerRadiationLoss` | 50 | temperature the bath sheds continuously, in C |
| `BessemerRefineTemperature` | 1500 | temperature the bath must hold to refine, in C |
| `BessemerPressureTempGainMax` | 580 | ceiling on the bath heat pressure can buy, in C |
| `BessemerPressureTempGainHalf` | 0.7 | atm over the gate that buys half of it |
| `BessemerColdScrapLossCoefficient` | 0.8 | bath temperature lost per unit of cold scrap, in C |
| `BessemerScrapCodes` | `game:metalbit-iron,game:metalbit-steel` | the items the converter remelts |
| `BessemerCooldownCoefficient` | 0.5 | multiplier on the molten cooldown for the bath |
| `BessemerChiselMaxFraction` | 0.2 | share of capacity below which a frozen heat can be chiselled out |
| `BessemerPowerSpeedThreshold` | 0.1 | geared axle speed that counts as powered |
| `BessemerTransmissionResistance` | 0.25 | shaft load the vessel puts on its axle network |
| `BessemerRequiredGears` | 1 | large gears to raise the vessel |
| `BessemerRequiredRods` | 8 | rods to raise the vessel |
| `BessemerPourHoldSeconds` | 1 | seconds the pour lever is held before it commits |
| `RccBrokenDropsRatio` | 1.0 | share of construction materials returned when the vessel is broken |

### Molds and recipes

| key | default | sets |
|---|---|---|
| `EnablePlateMold` | true | whether the plate mold exists |
| `EnableIngotMold` | true | whether the double ingot mold exists |
| `EnableRodMold` | true | whether the quad rod mold exists |
| `RecipeLevel` | `normal` | the live profile in the recipe catalogue |

## The recipe catalogue

The catalogue names what the level applies to, not what it costs: the right-click construction of
the converter vessel, and the grid recipes of the tuyere, the furnace door, the furnace tap, the
converter's intake, control and transmission, the cowper stove intake, the heat sink, the air
blower, the smoke stack intake, both hoppers, the molten barrel, every molten canal piece, and the
three slag paths. Each entry carries a `normal` profile read from the shipped recipe and a `cheap`
profile at half of it, and both are yours to edit.

## Reading a value back

If a change through `/exmod config` does not seem to show, read the value back with the same
command: it prints what the server actually holds. `RccBrokenDropsRatio` was reset to 1.0 for
everyone in 0.9.6, so on a current install breaking the converter returns all of its construction
materials whatever the file says.
