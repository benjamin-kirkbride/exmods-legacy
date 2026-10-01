---
title: Boilers
covers:
  - "ppex:boilercornish-*"
  - "ppex:boilerlancashire-*"
order: 2
version: 0.6.8
---

A boiler burns a coal pile beside a water vessel and pushes the steam into the pipe run on its top
outlet. One litre of water becomes 16 L of steam. There are two: the Cornish Boiler, the iron-age
entry tier, and the Lancashire Boiler, the steel tier that carries enough pressure to run a Cornish
Engine.

## Cornish against Lancashire

| fact | Cornish | Lancashire |
|---|---|---|
| vessel capacity, water and steam together | 800 L | 1200 L |
| water needed before it will heat | 150 L | 200 L |
| most water it will hold | 500 L | 800 L |
| level the automatic intake fills to | 400 L | 600 L |
| steam output while boiling | 32 L/s | 48 L/s |
| water that costs, at full output | 2 L/s | 3 L/s |
| choke pressure | 5 atm | 12 atm |
| explosion radius | 3 blocks | 4 blocks |
| vessel material | iron or steel | steel only |

Both share the rest: 16 L/s of exhaust, an exhaust back-pressure ceiling of 0.8 atm, 180 s of
heating before the first steam, a 10 s grace after the fire dies, 10 L/s through the automatic
water intake, 200 L/s out of an open lid, at least 16 L/s at 1 atm out of an open steam outlet,
and 50 L a day lost to evaporation.

At full output a Cornish drinks its 500 L of boil water in about 4 minutes and a Lancashire its
800 L in about 4 and a half. Nothing runs for long without a feed.

## Water

The vessel takes water three ways. Opening the lid and pouring from a container fills it up to the
boil-water ceiling. A water line on the passthrough bend under the boiler block fills it
automatically at up to 10 L/s, but only to half the vessel's capacity, which leaves room for steam.
Condensate from an engine and water from a Steam Condenser are ordinary water and count the same.

Two cautions. Pour one container at a time: pouring from a stack loses the contents of every
container but one. And watch the pressure of a pumped feed. Water admitted above 1 atm flashes to
extra steam as it enters a boiling vessel, one extra litre of steam per litre admitted per atm of
feed pressure over 1 atm. A Manual Fluid Pump delivers at exactly 1 atm and flashes nothing, a
Mechanical Fluid Pump at 1.5 atm flashes half a litre per litre, and an engine Fluid Pump delivers
at three quarters of its engine's inlet pressure, so a Watt Engine on a 3 atm line pushes water in
at 2.25 atm and adds 12.5 L/s of steam on top of the boiling while the intake runs.

A bucket bails water back out while the lid is open, but only down to the level the boiler needs to
run; the rest sits below the reach of a bucket.

## Fire and exhaust

The fire is a vanilla coal pile in the firebox slot, lit by hand. The boiler counts as burning only
while that pile burns and its fire has draught. Exhaust leaves through the pipe outlet at the far
end at 16 L/s, and the boiler will not push it into a network already at 0.8 atm.

The fire draws only through a chimney, standing on the fireclay pipe outlet or on a passthrough's
open top, or through a Steelmaking Expanded smoke stack on its exhaust run. A chimney draws exactly
the 16 L/s the fire makes. Open pipe ends carry exhaust away but give no draught, and a pressure
valve ends the boiler's run, so a stack behind a valve gives that boiler none. Without draught the
boiler is choked and the look-at line reads "Choked: no draught! The exhaust needs a chimney or a
smoke stack." If the exhaust backs up to 0.8 atm the boiler is choked as well. Either way, after
10 s the fuel pile is snuffed out as if the flue were blocked.

When the fire goes out the boiler keeps running for 10 s, then shuts down. Leftover steam condenses
back to water at 200 L/s while it stays under the boil-water ceiling; anything above that stays
trapped in the vessel until the lid is opened.

## Pressure and the explosion

Internal pressure is the steam divided by the space the water is not using. A Cornish holding 300 L
of water and 400 L of steam has 500 L of free vessel and reads 0.8 atm.

The boiler pushes steam out to its steam line the way two connected vessels equalise: it moves
steam until the line reads what the vessel reads, and it never empties itself into the run. It
stops pushing altogether at the choke pressure, 5 atm on a Cornish and 12 atm on a Lancashire, and
stops boiling there too.

A boiler whose steam outlet has no pipe, or whose steam run has an open end anywhere, blows down to
about 1 atm. Each second it vents the larger of 16 L/s and its own make, 32 L/s on a Cornish and
48 L/s on a Lancashire, measured at 1 atm and in proportion to its internal pressure. It cannot
burst that way, but nothing on that run gets working pressure, and every boiler on a shared open
run does the same.

A boiler explodes when three things hold together for 30 continuous seconds: it is boiling, the
fire is lit, and its internal pressure is at or above the choke pressure with the lid shut. The
blast shatters every block within its radius under 20 blast resistance, which is pipes, ports, coal
piles and soft ground, and scatters 40 per cent of the construction materials. Taking a boiler down
with a pick returns all of them.

Three things do **not** blow a boiler up: a choked exhaust (it snuffs the fire instead), a full
vessel (there is no upper water cutoff), and an open steam outlet or steam run, which blows the
vessel down to about 1 atm.

## How players lose a boiler

**No draught for the fire.** The commonest one, and it kills the fire rather than the vessel. An
exhaust run that only ends in open pipe ends, or reaches its stack only through a pressure valve,
gives no draught, so the boiler is choked and the fuel pile is snuffed 10 s later. Players who
piped the exhaust into a run that was already carrying something else at pressure got the same
result. Give the exhaust a chimney or a stack of its own.

**A pump straight into a boiling vessel.** Pressurised feedwater flashes to steam, the pressure
climbs while the player watches the water level rise, and the 30 s timer starts. Put a
Piping (Valve) in the feed line and shut it while the boiler is at pressure, or feed a boiling
vessel from a low-head pump and keep the engine pump for filling a cold one.

**Trusting the relief valve through a restart.** Boilers have been found at critical pressure
immediately after a chunk reload, a relog or a server restart, with a relief valve fitted and the
hatch open. That case is open as of 2026-09-14 and no fix exists. The only mitigation players have
used is to vent the boiler and stop the run before a restart.

**Not knowing about the lid.** Holding right-click on the lid opens it, which vents 200 L/s and
resets the over-pressure timer while it stays open. It is the emergency release, and a boiler in
its danger band shows warning vapour before it goes.
