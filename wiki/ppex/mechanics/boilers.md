---
title: Boilers
covers:
  - "ppex:boilercornish-*"
  - "ppex:boilerlancashire-*"
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
water intake, 200 L/s out of an open lid, 16 L/s out of an unpiped steam outlet, and 50 L a day
lost to evaporation.

At full output a Cornish drinks its 500 L of boil water in about 4 minutes and a Lancashire its
800 L in about 4 and a half. Nothing runs for long without a feed.

## Water

The vessel takes water three ways. Opening the lid and pouring from a container fills it up to the
boil-water ceiling. A water line on the passthrough bend under the boiler block fills it
automatically at up to 10 L/s, but only to half the vessel's capacity, which leaves room for steam.
Condensate from an engine and water from a Steam Condenser are ordinary water and count the same.

Two cautions. Pour one container at a time: pouring from a stack loses the contents of every
container but one. And gate pumped feedwater. Water admitted at more than 1 atm flashes to extra
steam as it enters a boiling vessel, one extra litre of steam per litre admitted per atm of feed
pressure over 1 atm, which is a fast way to drive a healthy boiler into its danger band.

A bucket bails water back out while the lid is open, but only down to the level the boiler needs to
run; the rest sits below the reach of a bucket.

## Fire and exhaust

The fire is a vanilla coal pile in the firebox slot, lit by hand. The boiler counts as burning only
while that pile burns and its exhaust has somewhere to go. Exhaust leaves through the pipe outlet
at the far end at 16 L/s, and the boiler will not push it into a network already at 0.8 atm.

If the exhaust backs up to that ceiling the boiler is choked, the look-at line says so, and after
10 s the fuel pile is snuffed out as if the flue were blocked. A chimney standing on the exhaust
outlet draws exactly the 16 L/s the fire makes. A bare open pipe end only bleeds 8 L/s, which is
half of what is needed, so a run that ends in open air chokes the fire instead of venting it.

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

A boiler explodes when three things hold together for 30 continuous seconds: it is boiling, the
fire is lit, and its internal pressure is at or above the choke pressure with the lid shut. The
blast shatters every block within its radius under 20 blast resistance, which is pipes, ports, coal
piles and soft ground, and scatters 40 per cent of the construction materials. Taking a boiler down
with a pick returns all of them.

Three things do **not** blow a boiler up: a choked exhaust (it snuffs the fire instead), a full
vessel (there is no upper water cutoff), and an unpiped steam outlet, which bleeds 16 L/s and is
nowhere near enough to save an over-pressured vessel.

## How players blew theirs up

**No route for the exhaust.** The commonest one. 16 L/s of exhaust against 8 L/s through an open
pipe end means the fire snuffs, and players who then piped the exhaust into a run that was already
carrying something else at pressure got the same result. Give the exhaust a chimney or a stack of
its own.

**A pump straight into a boiling vessel.** Pressurised feedwater flashes to steam, the pressure
climbs while the player watches the water level rise, and the 30 s timer starts. Gate the feed with
a Piping (Pressure Valve), or feed by hand while the boiler is boiling.

**Trusting the relief valve through a restart.** Boilers have been found at critical pressure
immediately after a chunk reload, a relog or a server restart, with a relief valve fitted and the
hatch open. That case is open as of 2026-09-14 and no fix exists. The only mitigation players have
used is to vent the boiler and stop the run before a restart.

**Not knowing about the lid.** Holding right-click on the lid opens it, which vents 200 L/s and
resets the over-pressure timer while it stays open. It is the emergency release, and a boiler in
its danger band shows warning vapour before it goes.
